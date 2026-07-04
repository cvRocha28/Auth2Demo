# Auth2Demo

Estrutura mantida com **Solution Folders** (`docs`, `src`, `tests`) no Visual Studio, sem criar pastas físicas `src` e `tests`.

## Restore/build

```powershell
dotnet restore .\Auth2Demo.slnx
dotnet build .\Auth2Demo.slnx
```

## Migrations

```powershell
dotnet ef migrations add InitialIdentityServer `
  --project .\Auth2Demo.Infrastructure\Auth2Demo.Infrastructure.csproj `
  --startup-project .\Auth2Demo.Web\Auth2Demo.Web.csproj

 dotnet ef database update `
  --project .\Auth2Demo.Infrastructure\Auth2Demo.Infrastructure.csproj `
  --startup-project .\Auth2Demo.Web\Auth2Demo.Web.csproj
```
