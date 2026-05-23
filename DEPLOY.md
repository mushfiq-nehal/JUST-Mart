# JUST-Mart — Azure Deployment Guide

Live URL: **https://just-mart.azurewebsites.net**

---

## Prerequisites (one-time setup)

```bash
# Install .NET 8 SDK
sudo apt-get install -y dotnet-sdk-8.0

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure (opens browser)
BROWSER=google-chrome az login
```

---

## Deploy an Update (every time)

### 1. Make your code changes, then:

```bash
cd /home/nehal/Dev/JUST-Mart

# Build
dotnet publish JustMartWeb/JustMartWeb.csproj -c Release -o ./publish_output

# Zip
cd publish_output && zip -r ../deploy_to_azure.zip . -q && cd ..

# Deploy
az webapp deploy \
  --resource-group just-mart-website \
  --name just-mart \
  --src-path deploy_to_azure.zip \
  --type zip

# Cleanup (optional but recommended)
rm -rf publish_output deploy_to_azure.zip
```

### 2. Push to GitHub

```bash
git add -A
git commit -m "your message"
git push origin main
```

---

## Database Migrations (only when models change)

Run this **before or after deploying** if you added/changed EF Core migrations:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"

ConnectionStrings__DefaultConnection="Server=tcp:just-mart-sql-server.database.windows.net,1433;Initial Catalog=just-mart-db;Persist Security Info=False;User ID=justmartadmin;Password=55305530@Nn;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
dotnet ef database update \
  --context ApplicationDbContext \
  --project JustMart.DataAccess \
  --startup-project JustMartWeb
```

> If `dotnet-ef` is not installed: `dotnet tool install --global dotnet-ef`

---

## Azure Resources

| Resource | Type | Resource Group |
|---|---|---|
| `just-mart` | App Service | `just-mart-website` |
| `ASP-justmartwebsite-92b6` | App Service Plan (Free) | `just-mart-website` |
| `justmartstorage` | Storage Account | `just-mart-website` |
| `just-mart-db` | SQL Database (Free Serverless) | `just-mart` |
| `just-mart-sql-server` | SQL Server | `just-mart` |

---

## App Service Environment Variables

All secrets are set in **Azure Portal → `just-mart` → Settings → Environment variables**.

> ⚠️ Use double underscores `__` as separator (e.g. `Brevo__ApiKey`).  
> ⚠️ Do NOT use the prefix `AzureBlobStorage` — it is reserved by Azure.  
> ⚠️ Do NOT use `ConnectionString` in an app setting name — add SQL strings in the "Connection strings" tab instead.

| Name | Section |
|---|---|
| `ConnectionStrings__DefaultConnection` | Application settings |
| `BlobStorage__Key` | Application settings |
| `BlobStorage__ContainerName` | Application settings |
| `Brevo__ApiKey` | Application settings |
| `Brevo__SenderEmail` | Application settings |
| `Brevo__SenderName` | Application settings |
| `SSLCommerz__StoreId` | Application settings |
| `SSLCommerz__StorePassword` | Application settings |
| `SSLCommerz__BaseUrl` | Application settings |
| `SSLCommerz__IsSandbox` | Application settings |
| `ASPNETCORE_ENVIRONMENT` | Application settings → `Production` |

---

## Demo Credentials

| Role | Email | Password |
|---|---|---|
| Admin | `admin@justmart.com` | `Admin123*` |
| Customer | `customer@justmart.com` | `Admin123*` |
| Company | `company@justmart.com` | `Admin123*` |

---

## Notes

- **Cold starts**: The SQL database (free serverless tier) auto-pauses after inactivity. The first request after idle may take 10–30 seconds. This is normal.
- **Blob storage**: Product images are stored in the `product-images` container in `justmartstorage`. The container has **Blob-level public read** access so images load in the browser.
- **Re-seeding**: The `SeedDatabase()` call in `Program.cs` is commented out. Only uncomment temporarily if you need to re-seed on a fresh database, then redeploy with it commented out again.
- **Secrets**: Never commit `appsettings.Local.json` — it is gitignored. All secrets live in Azure App Service environment variables only.
