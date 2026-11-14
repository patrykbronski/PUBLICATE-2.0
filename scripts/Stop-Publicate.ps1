$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Instance.ps1')
# Never interrupt an in-flight upload/publish merely to switch workspaces.
if(Test-InstancePort $instance.backendPort){
 $config=Get-Content -LiteralPath (Join-Path $root 'src/Publicate.Api/appsettings.Local.json') -Raw | ConvertFrom-Json
 $connection=New-Object System.Data.Common.DbConnectionStringBuilder
 $connection.set_ConnectionString($config.ConnectionStrings.Database)
 $previousPassword=$env:PGPASSWORD
 try {
  $env:PGPASSWORD=[string]$connection.get_Item('Password')
  $active=& (Join-Path $root '.tools/postgresql/pgsql/bin/psql.exe') -h ([string]$connection.get_Item('Host')) -p ([string]$connection.get_Item('Port')) -U ([string]$connection.get_Item('Username')) -d ([string]$connection.get_Item('Database')) -At -v ON_ERROR_STOP=1 -f (Join-Path $PSScriptRoot 'active-publications.sql')
  if($LASTEXITCODE -ne 0){throw 'Nie mozna sprawdzic aktywnych publikacji. Niczego nie zatrzymano.'}
  if([int]$active -gt 0){throw 'Trwa upload lub publikacja. Poczekaj na jej zakonczenie przed zatrzymaniem.'}
 } finally {$env:PGPASSWORD=$previousPassword}
}
$stateFile=Join-Path $root '.data/launcher/processes.json'
if(!(Test-Path $stateFile)){Write-Host 'No launcher-owned processes recorded. Manually started services are unchanged.';exit 0}
$items=@(Read-LauncherProcesses $stateFile)
$remaining=@()
[array]::Reverse($items)
foreach($item in $items){
 $process=Get-Process -Id $item.Id -ErrorAction SilentlyContinue
 if(!$process){continue}
 if(!$process.StartTime){Write-Warning "Cannot verify PID $($item.Id); skipping stale or inaccessible process.";continue}
 if($process.StartTime.ToUniversalTime().Ticks.ToString() -ne $item.Started){Write-Warning "PID reused; skipping $($item.Name).";continue}
 # Kill the recorded wrapper and its children, never unrelated node/dotnet processes.
 & taskkill.exe /PID $item.Id /T /F
 if($LASTEXITCODE -ne 0){$remaining+=$item;Write-Warning "Could not stop $($item.Name)."}
}
ConvertTo-Json -InputObject @($remaining) -Depth 5 | Set-Content -LiteralPath $stateFile -Encoding UTF8
Write-Host 'Done. PostgreSQL and services started manually remain running.'
