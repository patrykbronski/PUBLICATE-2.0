# Applies the reviewed migration after a local database backup. Does not start services.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
$config=Get-Content -LiteralPath (Join-Path $root 'src/Publicate.Api/appsettings.Local.json') -Raw | ConvertFrom-Json
$connection=New-Object System.Data.Common.DbConnectionStringBuilder
$connection.set_ConnectionString($config.ConnectionStrings.Database)
$pgDump=Join-Path $root '.tools/postgresql/pgsql/bin/pg_dump.exe'
if(!(Test-Path -LiteralPath $pgDump)){throw 'pg_dump missing. Back up your database before applying migrations.'}
$backupDir=Join-Path $root '.data/backups'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$backup=Join-Path $backupDir ('before-composer-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.dump')
$env:PGPASSWORD=[string]$connection.get_Item('Password')
try {
 & $pgDump -h ([string]$connection.get_Item('Host')) -p ([string]$connection.get_Item('Port')) -U ([string]$connection.get_Item('Username')) -d ([string]$connection.get_Item('Database')) -Fc -f $backup
 if($LASTEXITCODE -ne 0){throw 'Database backup failed; migration not applied.'}
} finally {Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue}
$env:DOTNET_ROOT=Join-Path $root '.tools/dotnet'
$env:PATH=$env:DOTNET_ROOT+';'+$env:PATH
$env:DOTNET_CLI_HOME=Join-Path $root '.tools/cli'
$env:NUGET_PACKAGES=Join-Path $root '.tools/packages'
& (Join-Path $root '.tools/ef/dotnet-ef.exe') database update --project src/Publicate.Api --configuration Composer
if($LASTEXITCODE -ne 0){throw 'Migration failed. Existing backup retained in .data/backups.'}
Write-Host 'Composer migration applied. Backup retained locally in .data/backups. Restart the existing backend.'
