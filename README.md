# Streaker

Streaker is a small habit tracker. You create habits, check in every day, and it counts how many days in a row you kept going (your streak). It has a REST API written in C# with ASP.NET Core, and a simple web page on top of it.

## Why I made this

This is a learning project. I'm learning C# and .NET, and I wanted to build something bigger than the small examples you usually see in tutorials. My goal was to practise a clean, layered structure the way it is used in real projects, not just to make something that works.

Things I wanted to practise:

- splitting a project into layers (Domain, Application, Infrastructure, Api) so that each layer only depends on the ones below it
- putting the business rules inside the domain instead of the controllers
- using an interface in the Application layer and implementing it in Infrastructure (dependency inversion)
- building a REST API with ASP.NET Core and Entity Framework Core
- writing unit tests and integration tests
- calling my own API from a JavaScript page

It is not finished and I'm sure some parts could be done better. I'm still learning, so suggestions are welcome.

## What it does

- create, edit, archive and delete habits
- check in for today, or for a day in the past, and undo it
- current streak and longest streak for each habit
- completion rate for the last 7 and 30 days
- a dashboard with today's progress and the top 3 streaks
- a web page where you can do all of this

When the app runs in Development mode it adds a few demo habits, so the page is not empty on the first run.

## How the layers work

```
Api  ->  Infrastructure  ->  Application  ->  Domain
```

| Layer | What is in it |
|---|---|
| Domain | The `Habit` class and its rules (for example, you can't check in for a future day), and the streak calculation. It doesn't depend on anything. |
| Application | The services that the API calls, the DTOs, and the `IHabitRepository` interface. |
| Infrastructure | Entity Framework Core with SQLite. It implements `IHabitRepository`. Also the demo data. |
| Api | The controllers, error handling, an API docs page (Scalar), and the web page in `wwwroot`. |

The Domain layer knows nothing about the database or HTTP. The Application layer only knows the repository interface, not how it is implemented.

## Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/Streaker.Api
```

Then open http://localhost:5180. The API documentation is at http://localhost:5180/scalar/v1.

Run the tests:

```bash
dotnet test
```

There are 25 tests. 14 are unit tests for the domain (streak logic and rules), and 11 are integration tests that start the whole API and call it over HTTP.

I also wrote a Dockerfile, but I haven't tested it yet.

## API

| Method | Route | What it does |
|---|---|---|
| GET | `/api/dashboard` | today's progress and top 3 streaks |
| GET | `/api/habits` | list habits (`?includeArchived=true` to include archived ones) |
| POST | `/api/habits` | create a habit |
| GET | `/api/habits/{id}` | get one habit |
| PUT | `/api/habits/{id}` | update a habit |
| DELETE | `/api/habits/{id}` | delete a habit |
| POST | `/api/habits/{id}/check-ins` | check in (optional body: `{ "date": "2026-05-20" }`, default is today) |
| DELETE | `/api/habits/{id}/check-ins/{date}` | undo a check-in |
| GET | `/api/habits/{id}/stats` | streaks, completion rates, last 30 days |
| POST | `/api/habits/{id}/archive` | archive a habit |
| POST | `/api/habits/{id}/restore` | restore an archived habit |
| GET | `/health` | health check |

Example:

```bash
curl -X POST http://localhost:5180/api/habits \
  -H "Content-Type: application/json" \
  -d '{ "name": "Learn Rust", "emoji": "🦀" }'
```

## Project structure

```
Streaker.slnx
├── src/
│   ├── Streaker.Domain/
│   ├── Streaker.Application/
│   ├── Streaker.Infrastructure/
│   └── Streaker.Api/
│       └── wwwroot/          the web page (plain HTML, CSS and JavaScript)
└── tests/
    ├── Streaker.Domain.Tests/
    └── Streaker.Api.Tests/
```

## Things I want to do next

- use EF Core migrations (right now the database is created with `EnsureCreated`)
- add users and login, so every person has their own habits
- habits that are not daily, for example three times a week
- try building the front end again with a framework
