# Setup Instructions

## User Secrets Configuration

This project uses User Secrets for sensitive configuration in development.

### PostgreSQL Password

To configure the PostgreSQL password for the Aspire Host:

```bash
dotnet user-secrets set "Parameters:postgres-password" "YourPassword" --project src/AspireHost/AspireHost.csproj
```

### JWT Configuration

The JWT settings in `appsettings.json` contain placeholder values. For production:

1. Generate a strong secret key (minimum 32 characters)
2. Update the following settings in production configuration:
   - `Jwt:Issuer` - Your API domain
   - `Jwt:Audience` - Your API domain or client app domain
   - `Jwt:Key` - Strong secret key (store in Azure Key Vault, AWS Secrets Manager, etc.)

**NEVER commit real JWT keys to source control!**

## Database Migrations

Migrations run automatically on startup when `Ef:MigrateOnStartup` is set to `true` in `appsettings.Development.json`.

To create a new migration:

```bash
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Presentation
```

To apply migrations manually:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation
```

## Running the Application

```bash
# Using Aspire CLI
aspire run

# Or using dotnet CLI
dotnet run --project src/AspireHost
```

This will start:

- The Aspire Dashboard
- PostgreSQL with pgAdmin (optional)
- Redis with RedisInsight (optional)
- The API application
