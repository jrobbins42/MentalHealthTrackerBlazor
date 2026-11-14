
# Mental Health Tracker (Blazor)

A Blazor-based web application for tracking mood, habits, and journal entries over time.  
The goal of this project is to provide a simple, privacy-focused way for individuals to observe patterns in their mental health and daily life.

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running the Application](#running-the-application)
  - [Database & Migrations](#database--migrations)
- [Domain Model](#domain-model)
- [Testing](#testing)
- [Roadmap / Future Improvements](#roadmap--future-improvements)
- [What I Learned](#what-i-learned)

---

## Features

- **User mood tracking**
  - Log daily mood entries with rating (e.g., 1–10) and notes.
  - View recent entries and trends over time.

- **Journal entries**
  - Create, edit, and delete text-based journal entries.
  - Associate entries with dates and (optionally) mood.
  - 
- **Dashboard / Overview**
  - See recent mood entries and journal entries in a single view.
  - Basic filtering by date.

> Note: This is a learning / portfolio project and not a medical product.  
> It is **not** a substitute for professional mental health care.

## Tech Stack

- **Frontend / UI**
  - [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) 
  - Razor components

- **Backend**
  - ASP.NET Core
  - C# 

- **Data Access**
  - Entity Framework Core
  - SQL Server

**Data flow example** (high-level):

1. User creates a new mood entry via a Blazor page.
2. The page calls an `IMoodEntryService` in the Application layer.
3. The service validates the request, constructs a `MoodEntry` domain entity, and calls a repository.
4. The repository (Infrastructure layer) persists the entity via EF Core and returns the result.
5. The UI updates to show the new entry.

---

### Running the Application

1. **Clone the repository**

   ```bash
   git clone https://github.com/jrobbins42/MentalHealthTrackerBlazor.git
   cd MentalHealthTrackerBlazor
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Apply migrations & create the database** (see next section).

4. **Run the app**

   From the solution root (or the Presentation project folder):

   ```bash
   dotnet run --project src/Presentation/Presentation.csproj
   ```

   Then open a browser and go to:

   ```text
   https://localhost:5001
   ```

   or the URL shown in the console 
## Domain Model

Core entities 

- **MoodEntry**
  - `Id` (GUID/int)
  - `Date`
  - `Rating` (e.g., 1–10)
  - `Notes`
  - Optional relationships to habits or tags

- **JournalEntry**
  - `Id`
  - `Date`
  - `Title`
  - `Content`
  - Optional link to a `MoodEntry` or mood rating

---

## Testing

The solution includes (or will include) tests in the `tests` folder:

- **Unit tests**
  - Application services (e.g., `MoodEntryService`, `JournalService`).
  - Domain logic (e.g., validation of ratings, dates).

- **Integration tests**
  - EF Core DbContext + repository flow for creating and retrieving entries.

To run tests:

```bash
dotnet test
```

---

## Roadmap / Future Improvements

Planned or potential enhancements:

- [ ] Authentication & user accounts (per-user data, privacy)
- [ ] Basic analytics (charts of mood over time)
- [ ] Tagging / categorization of entries
- [ ] Export data (CSV/JSON)
- [ ] Dark mode / UI styling improvements
- [ ] Better validation and error handling
- [ ] Docker support and deployment to Azure / another cloud

---

## What I Learned

This project was a way to deepen my experience with:

- Structuring a .NET solution using a **clean / layered architecture**.
- Building a UI with **Blazor** and Razor components.
- Using **Entity Framework Core** for data access and migrations.
- Working with **dependency injection** and separating concerns between:
  - UI
  - Application logic
  - Domain
  - Infrastructure
- Thinking about user experience and data modeling in the context of a real-world problem domain (mental health tracking).

---

> If you have feedback or suggestions, feel free to open an issue or reach out via GitHub.
```
