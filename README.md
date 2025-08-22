Photo Gallery (ASP.NET Core, .NET 8)

A clean starter for a user-generated photo gallery built with .NET 8 + ASP.NET Core Identity + EF Core.  
It uses Azure Blob Storage for images, supports email confirmation, and ships with a clone-and-run dev experience.


Features
•	User registration & login with **email confirmation**
•	Create, edit, delete **galleries** and photos
•	**Azure Blob Storage** for media (Dev & Prod)  
  • Dev option: Azurite local emulator
•	**Auto DB migrations** on first run in Development
•	**Dev email “pickup”** — emails saved as `.html` files you can open and click
•	Clear layering: Controllers, Views, Data (EF Core), Services



Tech stack
•	**.NET 8 / ASP.NET Core MVC & Identity**
•	**EF Core 8** (SQLite by default in dev if no connection string is provided)
•	**Azure Storage Blobs** for image storage
•	**MailKit/SMTP** (prod) and **DevEmailSender** (dev-only)

The app is configured to use **Azure Blob** as the storage implementation. For local dev without a real Azure account, use **Azurite** and set `UseDevelopmentStorage=true` (see below).


Prerequisites
•	**.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0  
•	(Dev, optional) **Azurite** emulator — https://github.com/Azure/Azurite  
•	(Prod) An **Azure Storage account** and container (e.g., `photos`) — https://portal.azure.com/



Quick start (Development)

Development mode uses `appsettings.Development.json`, auto-applies EF Core migrations, and writes emails to `EmailDrop/*.html`.

Option A — Azurite (no Azure account required)
1. Install & run Azurite:
   npm i -g azurite
   azurite --silent
2. In `PhotoGallery.Web`, set user-secrets (do not commit secrets):
   dotnet user-secrets init
   dotnet user-secrets set "Storage:Azure:Blob:ConnectionString" "UseDevelopmentStorage=true"
   dotnet user-secrets set "Storage:Azure:Blob:Container" "photos"
3. Run the app:
   cd PhotoGallery.Web
   dotnet run
4. Confirm email: open the newest `.html` in `PhotoGallery.Web/EmailDrop/` and click the link.

Option B — Real Azure Storage account
1. Create a container (e.g., `photos`) in your storage account — https://portal.azure.com/  
2. Set user-secrets:
   cd PhotoGallery.Web
   dotnet user-secrets init
   dotnet user-secrets set "Storage:Azure:Blob:ConnectionString" "<your-azure-connection-string>"
   dotnet user-secrets set "Storage:Azure:Blob:Container" "photos"
3. Run:
   dotnet run



Configuration

The project reads configuration from `appsettings.json` + environment-specific files and environment variables / User-Secrets.

appsettings.Development.json (example)
{
  "ConnectionStrings": { "DefaultConnection": "Data Source=app.db" },

  "Storage": {
    "Azure": {
      "Blob": {
        "Container": "photos"
      }
    }
  },

  "Email": {
    "Mode": "Pickup",              
    "PickupDirectory": "EmailDrop"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

Secrets & environment variables
Set these via User-Secrets (dev) or environment variables (CI/Prod):

Azure Blob storage (required)
•	`Storage:Azure:Blob:ConnectionString`
•	`Storage:Azure:Blob:Container` (e.g., `photos`)

Database (optional in dev; required in prod if not using SQLite)
•	`ConnectionStrings:DefaultConnection`  
  Example for Azure SQL:  
  `Server=tcp:<host>,1433;Initial Catalog=PhotoGallery;User ID=<user>;Password=<pass>;Encrypt=True;TrustServerCertificate=False`

Email (Production)
•	`Email:Mode` = `Smtp` (or `SendGrid`)
•	For SMTP: `Email:Smtp:Host`, `Email:Smtp:Port`, `Email:Smtp:User`, `Email:Smtp:Password`, `Email:From`  
•	For SendGrid: `Email:SendGrid:ApiKey`, `Email:SendGrid:From`, `Email:SendGrid:FromName`



Database
•	In **Development**, if `ConnectionStrings:DefaultConnection` is missing, the app falls back to **SQLite** (`app.db`) so a fresh clone can run immediately.
•	EF Core migrations are **auto-applied** at startup in Development.
•	To use SQL Server/Azure SQL, set `ConnectionStrings:DefaultConnection` appropriately.



Email
•	**Development**: `Email:Mode = "Pickup"` → emails are saved to `EmailDrop/*.html`. Open the latest file and click the link to confirm.
•	**Production**: register your SMTP/SendGrid sender and set credentials as configuration.



Running in Production

1. Set environment to Production
   # Windows PowerShell
   $env:ASPNETCORE_ENVIRONMENT="Production"
   # Linux/macOS
   export ASPNETCORE_ENVIRONMENT=Production
2. Provide real configuration (env vars or a secure `appsettings.Production.json`):
   - `Storage:Azure:Blob:ConnectionString`, `Storage:Azure:Blob:Container`
   - `ConnectionStrings:DefaultConnection`
   - `Email:Mode=Smtp` (or `SendGrid`) + credentials
3. Publish & run
   dotnet publish PhotoGallery.Web/PhotoGallery.Web.csproj -c Release -o out
   dotnet ./out/PhotoGallery.Web.dll



Troubleshooting
•	**Uploads fail** → Check `Storage:Azure:Blob:ConnectionString` and container name. If using Azurite, ensure it’s running (`azurite`).
•	**No emails in dev** → Look in `EmailDrop/*.html`.
•	**DB errors in Prod** → Verify `ConnectionStrings:DefaultConnection` and that the DB exists; apply migrations if you disabled auto-migrate.
•	**404 on images** → Confirm container exists and permissions are correct.



Useful docs
•	ASP.NET Core — https://learn.microsoft.com/aspnet/core/  
•	EF Core — https://learn.microsoft.com/ef/core/  
•	Azure Storage Blobs — https://learn.microsoft.com/azure/storage/blobs/storage-blobs-introduction  
•	Azurite — https://github.com/Azure/Azurite  
•	User-Secrets — https://learn.microsoft.com/aspnet/core/security/app-secrets
