"use strict";

// ---------- tiny API client ----------
const api = {
  async request(method, url, body) {
    const res = await fetch(url, {
      method,
      headers: body === undefined ? undefined : { "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    if (!res.ok) {
      let message = `Request failed (${res.status})`;
      try { message = (await res.json()).detail ?? message; } catch { /* not json */ }
      throw new Error(message);
    }
    return res.status === 204 ? null : res.json();
  },
  dashboard: () => api.request("GET", "/api/dashboard"),
  habits: (archived) => api.request("GET", `/api/habits?includeArchived=${archived}`),
  stats: (id) => api.request("GET", `/api/habits/${id}/stats`),
  create: (b) => api.request("POST", "/api/habits", b),
  update: (id, b) => api.request("PUT", `/api/habits/${id}`, b),
  remove: (id) => api.request("DELETE", `/api/habits/${id}`),
  checkIn: (id, date) => api.request("POST", `/api/habits/${id}/check-ins`, { date }),
  undo: (id, date) => api.request("DELETE", `/api/habits/${id}/check-ins/${date}`),
  archive: (id) => api.request("POST", `/api/habits/${id}/archive`),
  restore: (id) => api.request("POST", `/api/habits/${id}/restore`),
};

// ---------- state ----------
const state = { dashboard: null, habits: [], stats: new Map(), showArchived: false, editingId: null, emoji: "✨", firstRender: true };

const EMOJIS = ["📚", "🏃", "💻", "💧", "🧘", "🎸", "🥗", "😴", "✍️", "🧹", "🦀", "🎨", "🚴", "🌱", "🧠", "☕"];

const $ = (sel) => document.querySelector(sel);

/** Safe element builder — user text only ever goes through textContent. */
function el(tag, props = {}, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (key === "class") node.className = value;
    else if (key === "text") node.textContent = value;
    else if (key.startsWith("on")) node.addEventListener(key.slice(2), value);
    else if (value === true) node.setAttribute(key, "");
    else if (value !== false && value != null) node.setAttribute(key, value);
  }
  node.append(...children.flat().filter((c) => c != null));
  return node;
}

// ---------- data ----------
async function load() {
  try {
    const [dashboard, habits] = await Promise.all([api.dashboard(), api.habits(state.showArchived)]);
    const stats = await Promise.all(habits.map((h) => api.stats(h.id)));
    state.dashboard = dashboard;
    state.habits = habits;
    state.stats = new Map(stats.map((s) => [s.habitId, s]));
    render();
  } catch (err) {
    toast(err.message, true);
  }
}

async function act(fn, okMessage) {
  const before = state.dashboard?.todayProgressPercent ?? 0;
  try {
    await fn();
    await load();
    if (okMessage) toast(okMessage);
    if (before < 100 && state.dashboard.todayProgressPercent === 100 && state.dashboard.activeHabits > 0) burst();
  } catch (err) {
    toast(err.message, true);
  }
}

// ---------- rendering ----------
function render() {
  renderHero();
  renderGrid();
  state.firstRender = false;
}

function renderHero() {
  const d = state.dashboard;
  const date = new Date(`${d.today}T00:00:00`);
  $("#today-label").textContent = date.toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" });
  $("#hero-message").textContent = d.message;

  // progress ring
  const r = 62, c = 2 * Math.PI * r;
  $("#ring").replaceChildren(
    svg(`<svg width="150" height="150" viewBox="0 0 150 150" aria-hidden="true">
      <defs><linearGradient id="ring-grad" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#ff7a3d"/><stop offset="1" stop-color="#ffb347"/></linearGradient></defs>
      <circle class="ring-track" cx="75" cy="75" r="${r}" fill="none" stroke-width="13"/>
      <circle class="ring-bar" cx="75" cy="75" r="${r}" fill="none" stroke-width="13"
        stroke-dasharray="${c}" stroke-dashoffset="${c}" data-target="${c * (1 - d.todayProgressPercent / 100)}"/>
    </svg>`),
    el("div", { class: "ring-label" },
      el("div", { class: "ring-pct", text: `${d.todayProgressPercent}%` }),
      el("div", { class: "ring-sub", text: "done today" })),
  );
  requestAnimationFrame(() => requestAnimationFrame(() => {
    const bar = $(".ring-bar");
    if (bar) bar.style.strokeDashoffset = bar.dataset.target;
  }));

  $("#chips").replaceChildren(
    chip(d.activeHabits, "active"),
    chip(d.completedToday, "done"),
    chip(d.pendingToday, "to go"),
  );

  const medals = ["🥇", "🥈", "🥉"];
  const rows = d.leaderboard.filter((e) => e.currentStreak > 0);
  $("#podium").replaceChildren(...(rows.length
    ? rows.map((e, i) => el("li", {},
        el("span", { class: "medal", text: medals[i] }),
        el("span", { class: "p-name", text: `${e.emoji} ${e.name}` }),
        el("span", { class: "p-streak", text: `🔥 ${e.currentStreak}` })))
    : [el("li", { class: "podium-empty", text: "No active streaks yet — check in to start one." })]));
}

function chip(n, label) {
  return el("span", { class: "chip" }, el("b", { text: String(n) }), ` ${label}`);
}

function svg(markup) {
  return new DOMParser().parseFromString(markup, "image/svg+xml").documentElement;
}

function renderGrid() {
  const grid = $("#grid");
  $("#empty").hidden = state.habits.length > 0;
  grid.replaceChildren(...state.habits.map(card));
}

function card(h, index) {
  const s = state.stats.get(h.id);
  const today = state.dashboard.today;
  const days = s.last30Days.slice(-28);

  const cells = days.map((day) => el("button", {
    class: `cell${day.done ? " on" : ""}${day.date === today ? " today" : ""}`,
    title: `${day.date}${day.done ? " ✓" : ""}`,
    "aria-label": `${day.date}: ${day.done ? "done" : "not done"}`,
    disabled: h.isArchived,
    onclick: () => act(() => (day.done ? api.undo(h.id, day.date) : api.checkIn(h.id, day.date))),
  }));

  const pct = (v) => `${Math.round(v * 100)}%`;

  return el("article", { class: `card${state.firstRender ? " enter" : ""}${h.doneToday ? " done" : ""}${h.isArchived ? " archived" : ""}`, style: `animation-delay:${index * 60}ms` },
    el("div", { class: "card-head" },
      el("div", { class: "card-emoji", text: h.emoji }),
      el("div", { class: "card-title" },
        el("h3", {}, h.name, h.isArchived ? el("span", { class: "badge-archived", text: "archived" }) : null),
        h.description ? el("p", { text: h.description }) : null),
      el("div", { class: "menu" },
        el("button", { class: "icon-btn", title: "Edit", "aria-label": "Edit habit", text: "✎", onclick: () => openDialog(h) }),
        el("button", {
          class: "icon-btn", title: h.isArchived ? "Restore" : "Archive", "aria-label": h.isArchived ? "Restore habit" : "Archive habit",
          text: h.isArchived ? "↩" : "🗄", onclick: () => act(() => (h.isArchived ? api.restore(h.id) : api.archive(h.id))),
        }),
        el("button", { class: "icon-btn danger", title: "Delete", "aria-label": "Delete habit", text: "🗑", onclick: () => confirmDelete(h) }))),
    el("div", { class: "streaks" },
      el("div", { class: `streak-box ${h.currentStreak > 0 ? "hot" : "cold"}` },
        el("div", { class: "num", text: `${h.currentStreak > 0 ? "🔥 " : ""}${h.currentStreak}` }),
        el("div", { class: "lbl", text: "current streak" })),
      el("div", { class: "streak-box" },
        el("div", { class: "num", text: `🏆 ${h.longestStreak}` }),
        el("div", { class: "lbl", text: "best" }))),
    el("div", { class: "heat", role: "group", "aria-label": "Last 4 weeks" }, cells),
    el("div", { class: "rates" },
      el("span", {}, "7 days ", el("b", { text: pct(s.completionRateLast7Days) })),
      el("span", {}, "30 days ", el("b", { text: pct(s.completionRateLast30Days) })),
      el("span", {}, "total ", el("b", { text: String(h.totalCheckIns) }))),
    el("button", {
      class: `checkin${h.doneToday ? " on" : ""}`,
      disabled: h.isArchived,
      text: h.doneToday ? "✓ Done today — tap to undo" : "Check in for today",
      onclick: () => act(() => (h.doneToday ? api.undo(h.id, today) : api.checkIn(h.id, null))),
    }),
  );
}

// ---------- dialog ----------
const dialog = $("#dialog");

function renderEmojiPicker() {
  $("#emoji-picker").replaceChildren(...EMOJIS.map((e) => el("button", {
    type: "button", class: "emoji-opt", role: "radio", "aria-checked": String(e === state.emoji), text: e,
    onclick: () => { state.emoji = e; renderEmojiPicker(); },
  })));
}

function openDialog(habit = null) {
  state.editingId = habit?.id ?? null;
  state.emoji = habit?.emoji ?? EMOJIS[0];
  $("#dialog-title").textContent = habit ? "Edit habit" : "New habit";
  $("#f-name").value = habit?.name ?? "";
  $("#f-desc").value = habit?.description ?? "";
  renderEmojiPicker();
  dialog.showModal();
  $("#f-name").focus();
}

$("#form").addEventListener("submit", (e) => {
  e.preventDefault();
  const body = { name: $("#f-name").value, description: $("#f-desc").value || null, emoji: state.emoji };
  dialog.close();
  act(
    () => (state.editingId ? api.update(state.editingId, body) : api.create(body)),
    state.editingId ? "Habit updated" : "Habit created 🎉",
  );
});
$("#cancel").addEventListener("click", () => dialog.close());
dialog.addEventListener("click", (e) => { if (e.target === dialog) dialog.close(); });

function confirmDelete(h) {
  if (confirm(`Delete “${h.name}” and all its history? This cannot be undone.`)) {
    act(() => api.remove(h.id), "Habit deleted");
  }
}

// ---------- feedback ----------
function toast(message, isError = false) {
  const node = el("div", { class: `toast${isError ? " error" : ""}`, text: message, role: "status" });
  $("#toasts").append(node);
  setTimeout(() => node.remove(), 3500);
}

function burst() {
  if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  const pieces = ["🎉", "🔥", "✨", "🏆", "💪"];
  for (let i = 0; i < 26; i++) {
    const angle = Math.random() * Math.PI * 2;
    const distance = 120 + Math.random() * 220;
    const node = el("span", { class: "confetti", text: pieces[i % pieces.length] });
    node.style.setProperty("--x", `${Math.cos(angle) * distance}px`);
    node.style.setProperty("--y", `${Math.sin(angle) * distance - 80}px`);
    node.style.setProperty("--r", `${Math.random() * 720 - 360}deg`);
    document.body.append(node);
    setTimeout(() => node.remove(), 1200);
  }
}

// ---------- wiring ----------
$("#new-habit").addEventListener("click", () => openDialog());
$("#empty-new").addEventListener("click", () => openDialog());
$("#show-archived").addEventListener("change", (e) => { state.showArchived = e.target.checked; load(); });

$("#grid").replaceChildren(...Array.from({ length: 3 }, () => el("div", { class: "skeleton" })));
load();
