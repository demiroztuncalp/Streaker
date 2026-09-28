using System.Net;
using System.Net.Http.Json;
using Streaker.Application.Contracts;

namespace Streaker.Api.Tests;

public class HabitsApiTests(StreakerApiFactory factory) : IClassFixture<StreakerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<HabitDto> CreateHabit(string name = "Read")
    {
        var response = await _client.PostAsJsonAsync("/api/habits", new CreateHabitRequest(name, null, "📚"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<HabitDto>())!;
    }

    [Fact]
    public async Task Health_endpoint_is_up()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Web_ui_is_served_from_the_root()
    {
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Streaker", html);
    }

    [Fact]
    public async Task Created_habit_can_be_fetched_by_id()
    {
        var created = await CreateHabit("Meditate");

        var fetched = await _client.GetFromJsonAsync<HabitDto>($"/api/habits/{created.Id}");

        Assert.Equal("Meditate", fetched!.Name);
        Assert.Equal(0, fetched.CurrentStreak);
    }

    [Fact]
    public async Task Check_ins_persist_and_build_a_streak()
    {
        var habit = await CreateHabit();

        await _client.PostAsJsonAsync($"/api/habits/{habit.Id}/check-ins", new CheckInRequest(Today.AddDays(-1)));
        var response = await _client.PostAsJsonAsync($"/api/habits/{habit.Id}/check-ins", new CheckInRequest(null));
        var afterCheckIn = await response.Content.ReadFromJsonAsync<HabitDto>();
        var reloaded = await _client.GetFromJsonAsync<HabitDto>($"/api/habits/{habit.Id}");

        Assert.Equal(2, afterCheckIn!.CurrentStreak);
        Assert.True(reloaded!.DoneToday);
        Assert.Equal(2, reloaded.TotalCheckIns);
    }

    [Fact]
    public async Task Undoing_a_check_in_resets_the_streak()
    {
        var habit = await CreateHabit();
        await _client.PostAsJsonAsync($"/api/habits/{habit.Id}/check-ins", new CheckInRequest(null));

        var response = await _client.DeleteAsync($"/api/habits/{habit.Id}/check-ins/{Today:yyyy-MM-dd}");
        var habitAfter = await response.Content.ReadFromJsonAsync<HabitDto>();

        Assert.Equal(0, habitAfter!.CurrentStreak);
        Assert.False(habitAfter.DoneToday);
    }

    [Fact]
    public async Task Unknown_habit_returns_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/habits/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Business_rule_violations_return_422()
    {
        var response = await _client.PostAsJsonAsync("/api/habits", new CreateHabitRequest("   ", null, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Future_check_in_is_rejected()
    {
        var habit = await CreateHabit();

        var response = await _client.PostAsJsonAsync($"/api/habits/{habit.Id}/check-ins", new CheckInRequest(Today.AddDays(2)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Archived_habits_are_hidden_from_the_default_list_and_dashboard()
    {
        var habit = await CreateHabit("Temporary");
        await _client.PostAsync($"/api/habits/{habit.Id}/archive", null);

        var visible = await _client.GetFromJsonAsync<List<HabitDto>>("/api/habits");
        var all = await _client.GetFromJsonAsync<List<HabitDto>>("/api/habits?includeArchived=true");

        Assert.DoesNotContain(visible!, h => h.Id == habit.Id);
        Assert.Contains(all!, h => h.Id == habit.Id);
    }

    [Fact]
    public async Task Stats_include_a_28_day_heatmap()
    {
        var habit = await CreateHabit();
        await _client.PostAsJsonAsync($"/api/habits/{habit.Id}/check-ins", new CheckInRequest(null));

        var stats = await _client.GetFromJsonAsync<HabitStatsDto>($"/api/habits/{habit.Id}/stats");

        Assert.Equal(30, stats!.Last30Days.Count);
        Assert.EndsWith("🟩", stats.Heatmap);
    }

    [Fact]
    public async Task Deleting_a_habit_removes_it()
    {
        var habit = await CreateHabit();

        var delete = await _client.DeleteAsync($"/api/habits/{habit.Id}");
        var get = await _client.GetAsync($"/api/habits/{habit.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
