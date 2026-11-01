# Runs a SQL file against the MediGuard database using the project's own connection string.
# Usage: powershell -File runsql.ps1 <path-to.sql>
$root = "c:\Users\Abdullah\OneDrive\Documents\1StudyMatz\TEST\Mediguard\Mediguard"
$cfg = Get-Content (Join-Path $root "appsettings.Development.json") -Raw | ConvertFrom-Json
$env:DG_CONN = $cfg.ConnectionStrings.DefaultConnection
dotnet run --project (Join-Path $root "Tools\DbSql\DbSql.csproj") -- $args[0]
exit $LASTEXITCODE
