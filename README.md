# ESOP – Staff Management

A staff management web app built with **ASP.NET Core MVC** on **.NET 10**, made as a project for my **Enterprise Programming in C#** class.

A single system administrator logs in and manages staff records (add, edit, delete, search and sort). Staff are stored in a SQLite database through Entity Framework Core.

## Features

- **Admin login** with cookie authentication. Every staff page requires login (`[Authorize]`)
- **Staff CRUD** with Bootstrap modal forms for adding, editing and deleting staff members
- **Validation** of required fields, email format and length limits, with clear success and error messages
- **Unique emails**: a staff member's email can't be reused (case-insensitive), checked in the service and enforced by a unique index in the database
- **Search** by ID, name, email, position or unit (case-insensitive, matches any part of the text)
- **Sortable columns**: click a header to sort, click again to reverse. The search and sort are kept in the URL and remembered after adding, editing or deleting
- **Persistent storage** in SQLite. The database is created and migrated automatically on startup
- **Security**: anti-forgery (CSRF) tokens on all forms, the admin password stored only as a salted hash, and all output HTML-encoded

## Tech stack

| Area | Technology |
|---|---|
| Framework | ASP.NET Core MVC, .NET 10 |
| Data access | Entity Framework Core 10 (SQLite) |
| Authentication | Cookie authentication, `PasswordHasher` |
| UI | Razor views, Bootstrap 5 |
| Tests | xUnit, in-memory SQLite |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run the app

```bash
git clone https://github.com/DogCE1/ESOP.git
cd ESOP/ESOP
dotnet run
```

Then open the URL shown in the terminal (e.g. `http://localhost:5214`). You can also open `ESOP.slnx` in Rider or Visual Studio and run the `ESOP` project.

**Login:** username `admin`, password `admin`

On first run the app creates `esop.db` in the `ESOP` project folder. To look inside it you can use a tool like [DB Browser for SQLite](https://sqlitebrowser.org/).

### Run the tests

From the repository root:

```bash
dotnet test
```

Stop the app first, because a running app locks the build output.

## Project structure

```
ESOP.slnx                  Solution file
ESOP/                      Web app
├── Controllers/
│   ├── AccessController   Login, logout and error page
│   └── StaffController    Staff list (search/sort), create, edit, delete
├── Data/
│   └── EsopDbContext      EF Core database context (Staff table, unique email index)
├── Migrations/            EF Core migrations that create/update the database
├── Models/
│   ├── StaffMember        Staff entity with validation rules
│   ├── SystemAdmin        Login form model
│   └── StaffIndexViewModel  Data for the staff page (list + current search/sort)
├── Services/
│   └── StaffService       Business logic: CRUD, unique email check, search and sort
├── Views/                 Razor views (login, staff page, layout, error page)
├── Program.cs             Service registration, authentication and request pipeline
└── appsettings.json       Configuration, including the admin username and password hash
ESOP.Tests/                xUnit tests
├── StaffServiceTests      CRUD, IDs, unique emails, search and sort
├── StaffControllerTests   Page messages and redirects
├── AccessControllerTests  Login success and failure
└── StaffMemberValidationTests  Model validation rules
```

## Database changes

If you change the `StaffMember` model, add a migration from the `ESOP` folder. The app applies it automatically on the next start:

```bash
dotnet tool restore
dotnet ef migrations add <MigrationName>
```

`dotnet-ef` is installed as a local tool for this repo (see `dotnet-tools.json`), so `dotnet tool restore` is all that's needed.
