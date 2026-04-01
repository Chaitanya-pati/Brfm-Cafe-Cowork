# Brew & Work - Co-working Website

## Overview
A .NET 8.0 ASP.NET Core MVC website for a cafe and co-working space business called "Brew & Work". The site showcases both the cafe offerings and the co-working space facilities.

## Project Structure
- **co-working/** - Main ASP.NET Core MVC project
  - **Controllers/** - MVC controllers
    - `HomeController.cs` - Default home controller
    - `PagesController.cs` - Main pages (Index, Cafe, Coworking, Privacy)
    - `ContactController.cs` - Contact form → email via Microsoft Graph API
    - `ChatController.cs` - AI chatbot endpoint (delegates to LocalChatService)
  - **Services/** - Business logic
    - `EmailService.cs` - Sends emails using Microsoft Graph API (client credentials flow)
    - `ChatService.cs` - Custom, fully self-contained AI chatbot (no external APIs) — score-based knowledge engine
  - **Models/** - Data models for cafe menu and co-working plans
  - **Views/** - Razor views
    - `Pages/` - Index, Cafe, Coworking, PrivacyPolicy (all use `Layout = null`)
    - `Shared/_ChatWidget.cshtml` - AI chat bubble widget (included in all main pages)
    - `Shared/_Contact.cshtml` - Contact form partial
    - `Shared/_Footer.cshtml` - Footer partial
  - **Data/** - JSON data files
    - `cafe-menu.json` - Full cafe menu with prices
    - `coworking-plans.json` - Co-working plans, amenities, pricing
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
- MailKit (removed — now using Microsoft Graph API for email)
- Custom score-based AI knowledge engine (no external API)

## Features
- **Home page** — Split-panel hero (cafe + co-working), contact form
- **Cafe page** — Full menu, specials, amenities
- **Co-Working page** — Plans, pricing, amenities
- **Contact form** — Sends internal notification + confirmation email via Microsoft Graph
- **AI Chat Widget** — Floating chat button on all pages; answers questions about menu, prices, timings, location, workspace plans, and meeting room using the custom LocalChatService
- **Meeting / Conference Room** — Listed in co-working knowledge, timings response, and chat fallback; bookable by phone

## Chat Engine — How it works
`LocalChatService` (singleton) uses a scored knowledge base:
1. Named menu item lookup first (e.g. "how much is affogato?" → exact price)
2. Each `KnowledgeEntry` has StrongPhrases (+3 pts), Keywords (+1 pt), Blockers (disqualify)
3. All entries scored; highest scorer above MinScore wins
4. Responses array — multiple variants chosen randomly for variety

## Email Logging
Every contact form submission is logged to `co-working/Data/email-logs.json` regardless of whether the email sends successfully or fails. Each log entry contains:
- `id` — unique UUID
- `timestamp` — UTC time of the attempt
- `status` — `"success"` or `"failed"`
- `name`, `email`, `phone`, `interest`, `message` — form data
- `error` — error message if status is `"failed"`, otherwise null

Logging is handled by `EmailLogService` (singleton) using a semaphore lock for safe concurrent writes.

## Environment Variables
| Variable | Purpose |
|---|---|
| `GRAPH_TENANT_ID` | Microsoft Entra/Azure AD tenant ID |
| `GRAPH_CLIENT_ID` | Azure app registration client ID |
| `GRAPH_CLIENT_SECRET` | Azure app registration client secret |

## Configuration
- `appsettings.json` - Graph email settings (SenderEmail, ToEmail, FromName)
- `appsettings.Development.json` - Development-specific settings
- Forwarded headers are configured to work behind Replit's proxy
- App listens on `http://0.0.0.0:5000`
