# Brew & Work - Co-working Website

## Overview
A .NET 8.0 ASP.NET Core MVC website for a cafe and co-working space business called "Brew & Work". The site showcases both the cafe offerings and the co-working space facilities.

## Project Structure
- **co-working/** - Main ASP.NET Core MVC project
  - **Controllers/** - MVC controllers (HomeController, PagesController)
  - **Models/** - Data models
  - **Views/** - Razor views for pages (Home, Pages, Shared layouts)
  - **wwwroot/** - Static assets (CSS, JS, images)
  - **Program.cs** - Application entry point

## Running the Application
The application runs on port 5000 with the workflow "Co-working Website":
```bash
cd co-working && dotnet run
```

## Technology Stack
- .NET 8.0
- ASP.NET Core MVC
- Bootstrap (via wwwroot/lib)
- jQuery and jQuery Validation

## Configuration
- `appsettings.json` - Main configuration
- `appsettings.Development.json` - Development-specific settings
- Forwarded headers are configured to work behind Replit's proxy
