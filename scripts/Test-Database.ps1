$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$config=Get-Content -LiteralPath (Join-Path $projectRoot 'src/Publicate.Api/appsettings.Local.json') -Raw | ConvertFrom-Json
$connection=New-Object System.Data.Common.DbConnectionStringBuilder
$connection.set_ConnectionString($config.ConnectionStrings.Database)
$env:PGPASSWORD=[string]$connection.get_Item('Password')
$psql=Join-Path $projectRoot '.tools/postgresql/pgsql/bin/psql.exe'
if(!(Test-Path -LiteralPath $psql)){$psql='psql'}
try {
 & $psql -h ([string]$connection.get_Item('Host')) -p ([string]$connection.get_Item('Port')) -U ([string]$connection.get_Item('Username')) -d ([string]$connection.get_Item('Database')) -v ON_ERROR_STOP=1 -f (Join-Path $PSScriptRoot 'test-database.sql')
 if($LASTEXITCODE -ne 0){throw 'Test transakcyjny PostgreSQL nie powiodl sie'}
} finally {Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue}
