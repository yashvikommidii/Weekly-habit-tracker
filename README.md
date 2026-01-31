# Weekly Habit Tracker

A web app to track your habits throughout the week, visualize progress with charts, earn awards, and get personalized advice from an AI assistant.

## 🌐 Live Website

**[https://weekly-habit-tracker.onrender.com](https://weekly-habit-tracker.onrender.com)**

---

## Features

- **Add, edit, and delete habits** – Create habits, rename them, or remove them (and all their tracking records)
- **Weekly tracker** – Log ✓ (did it) or ✗ (didn't) for each habit by day
- **Progress chart** – View weekly or monthly completion as a line graph
- **Awards of the month** – Top performer, habit that needs a boost, longest streak
- **"What's up?"** – Random motivational quotes from the database
- **Habit Tracker Assistant** – Chat with an AI that answers questions about tracking, progress, and tips
- **Per-browser data** – Habits and entries stored in your browser's localStorage, so each person sees only their own data

---

## Tech Stack

- **Backend:** ASP.NET Core 8, C#
- **Frontend:** HTML, CSS, JavaScript, Chart.js
- **Database:** SQL Server (for motivational quotes)
- **AI:** OpenAI API (for chat assistant)
- **Storage:** Browser localStorage (habits & entries), JSON file (server-side fallback)

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (or connection to a hosted instance)
- OpenAI API key (for chat feature)

---

## Local Setup

### 1. Clone the repo

```bash
git clone https://github.com/yashvikommidii/Weekly-habit-tracker.git
cd Weekly-habit-tracker
```

### 2. Configure secrets

Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (recommended) or create `appsettings.Development.json`:

```bash
dotnet user-secrets set "ConnectionStrings__DefaultConnection" "Data Source=YOUR_SERVER;Initial Catalog=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
dotnet user-secrets set "OpenAI__ApiKey" "sk-proj-your-openai-api-key"
```

Or copy the example config and fill in your values:

```bash
cp appsettings.Example.json appsettings.json
# Edit appsettings.json with your connection string and OpenAI key
```

### 3. Restore and run

```bash
dotnet restore
dotnet run
```

### 4. Open in browser

- **Homepage:** http://localhost:5080 (or the port shown in the terminal)
- **Swagger API:** http://localhost:5080/swagger

---

## Environment Variables (for deployment)

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `OpenAI__ApiKey` | OpenAI API key for chat |
| `ASPNETCORE_ENVIRONMENT` | `Production` (for deployed app) |

---

## Deployment (Render)

1. Connect your GitHub repo to [Render](https://render.com)
2. Create a **Web Service** and select **Docker**
3. Add environment variables (see table above)
4. Deploy — Render builds from the included `Dockerfile`

---

## Project Structure

```
├── Controllers/         # API endpoints (Habits, Chat, MotivationalQuotes)
├── Data/                # AppDbContext, DbSeeder
├── Migrations/          # EF Core migrations
├── Models/              # Habit, HabitEntry, MotivationalQuote, etc.
├── Services/            # HabitStorage (JSON persistence)
├── wwwroot/             # Static files (HTML, CSS, JS)
├── Dockerfile           # For Docker deployment
├── Program.cs           # App entry point
└── appsettings.*.json   # Configuration (use Example as template)
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/habits` | List habits |
| POST | `/api/habits` | Create habit |
| GET | `/api/habits/graph` | Get graph data (weekly/monthly) |
| GET | `/api/habits/awards` | Get monthly awards |
| POST | `/api/chat` | Send message to AI assistant |
| GET | `/api/MotivationalQuotes` | List motivational quotes |

> **Note:** Habits and entries are stored in the browser (localStorage). The habits API is used when not using localStorage; the deployed app uses localStorage for per-user data.

---

## License

MIT
