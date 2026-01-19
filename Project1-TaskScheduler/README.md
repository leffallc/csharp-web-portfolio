# Task Scheduler Web App

A task management web application built with ASP.NET Core MVC and Entity Framework Core.  
The application supports full CRUD operations with persistent storage using SQL Server LocalDB.

## Features
- Create, view, edit, and delete tasks
- Mark tasks as complete or pending with one click
- Filter tasks by status (All / Pending / Complete)
- Server-side validation with user-friendly error messages
- Persistent storage using Entity Framework Core and SQL Server LocalDB
- Clean MVC architecture with dependency injection

## Tech Stack
- ASP.NET Core MVC (.NET 8)
- C#
- Entity Framework Core
- SQL Server LocalDB
- Bootstrap (for basic styling)

## Getting Started

### Prerequisites
- Visual Studio 2022+
- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio)

### Running the Application
1. Clone the repository
2. Open the solution in Visual Studio
3. Build the project (`Ctrl + Shift + B`)
4. Run the application (`F5`)
5. Navigate to `/Tasks` to use the app

### Database Setup
The application uses Entity Framework Core migrations.

If needed, run:
```powershell
Add-Migration InitialCreate
Update-Database
