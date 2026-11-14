param([switch]$NoBrowser)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Instance.ps1')
Assert-OtherInstanceStopped
Set-Location $root
$runtime=Join-Path $root '.data/launcher'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$stateFile=Join-Path $runtime 'processes.json'
$owned=@()
if(Test-Path $stateFile){$owned=@(Read-LauncherProcesses $stateFile)}
function Save-State { ConvertTo-Json -InputObject @($script:owned) -Depth 5 | Set-Content -LiteralPath $stateFile -Encoding UTF8 }
function Test-Port([int]$Port) {
 foreach($address in @('127.0.0.1','::1')) {
  $client=New-Object Net.Sockets.TcpClient ([Net.IPAddress]::Parse($address).AddressFamily)
  try { $task=$client.ConnectAsync($address,$Port); if($task.Wait(500) -and $client.Connected){return $true} } catch {} finally {$client.Dispose()}
 }
 return $false
}
function Start-Component([string]$Name,[string]$Script,[int]$Port) {
 if(Test-Port $Port){Write-Host "$Name already listening on port $Port; keeping existing process.";return}
 $existing=@($script:owned | Where-Object {$_.Name -eq $Name} | Where-Object {
  $p=Get-Process -Id $_.Id -ErrorAction SilentlyContinue
  $p -and $p.StartTime -and $p.StartTime.ToUniversalTime().Ticks.ToString() -eq $_.Started
 })
 if($existing.Count){throw "$Name is already starting or unhealthy. Check .data/launcher logs before retrying."}
 $scriptPath=Join-Path $PSScriptRoot $Script
 $process=Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`"" -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime "$Name.out.log") -RedirectStandardError (Join-Path $runtime "$Name.err.log") -PassThru
 $script:owned+=@{Name=$Name;Id=$process.Id;Started=$process.StartTime.ToUniversalTime().Ticks.ToString()}
 Save-State
 $deadline=(Get-Date).AddSeconds(60)
 while((Get-Date) -lt $deadline){
  if(Test-Port $Port){Write-Host "$Name ready on port $Port.";return}
  $process.Refresh()
  if($process.HasExited){throw "$Name exited. Check .data/launcher/$Name.err.log and $Name.out.log."}
  Start-Sleep -Milliseconds 500
 }
 throw "$Name did not open port $Port within 60 seconds. Check .data/launcher logs. Started components remain running."
}
try {
 # DPAPI keys belong to the Windows user who originally ran PUBLICATE.
 if([Environment]::UserName -like 'codexsandbox*'){throw 'Run this launcher from your own Windows PowerShell, not the Codex sandbox.'}
 if(!(Test-Port $instance.databasePort)){
  $pg=Join-Path $root '.tools/postgresql/pgsql/bin/pg_ctl.exe'
  if(!(Test-Path $pg)){throw 'Local PostgreSQL tools missing. Start your configured database manually.'}
  & $pg -D (Join-Path $root '.data/postgres') -l (Join-Path $root '.data/postgres.log') -w start
  if($LASTEXITCODE -ne 0){throw 'PostgreSQL failed to start. See .data/postgres.log.'}
 } else {Write-Host "PostgreSQL port $($instance.databasePort) is already available."}
 Start-Component 'backend' 'Start-Backend.ps1' $instance.backendPort
 Start-Component 'frontend' 'Start-Frontend.ps1' $instance.frontendPort
 Start-Component 'ngrok' 'Start-Tunnel.ps1' 4040
 $url=$instance.publicUrl
 $active=$false
 for($attempt=0;$attempt -lt 20;$attempt++){
  try {
   $tunnels=Invoke-RestMethod 'http://127.0.0.1:4040/api/tunnels' -TimeoutSec 2
   $active=@($tunnels.tunnels | Where-Object {$_.public_url -eq $url -and $_.config.addr -match "(^|:)$($instance.frontendPort)/?$"}).Count -gt 0
  } catch {$active=$false}
  if($active){break}
  Start-Sleep -Milliseconds 500
 }
 if(!$active){
  throw 'ngrok is running but the PUBLICATE URL is not active. Check the tunnel configuration and .data/launcher logs.'
 }
 Write-Host "$($instance.name): $url"
 Write-Host 'Logs: .data/launcher. You can close this terminal.'
 Write-Host 'Stop launcher-owned processes: powershell -ExecutionPolicy Bypass -File scripts/Stop-Publicate.ps1'
 if(!$NoBrowser){Start-Process $url}
} catch {Write-Error $_;exit 1}
