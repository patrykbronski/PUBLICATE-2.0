$projectRoot=Split-Path $PSScriptRoot -Parent
$instance=Get-Content -LiteralPath (Join-Path $projectRoot 'instance.json') -Raw | ConvertFrom-Json
function Test-InstancePort([int]$Port) {
 foreach($address in @('127.0.0.1','::1')) {
  $client=New-Object Net.Sockets.TcpClient ([Net.IPAddress]::Parse($address).AddressFamily)
  try {$task=$client.ConnectAsync($address,$Port);if($task.Wait(300)-and $client.Connected){return $true}}catch{}finally{$client.Dispose()}
 }
 return $false
}
function Assert-OtherInstanceStopped {
 if((Test-InstancePort $instance.otherBackendPort)-or(Test-InstancePort $instance.otherFrontendPort)){
  throw 'Druga wersja PUBLICATE jest uruchomiona. Zatrzymaj ja jej skryptem Stop-Publicate.ps1; nie przelaczaj tunelu podczas publikacji lub OAuth.'
 }
}
function Read-LauncherProcesses([string]$Path) {
 if(!(Test-Path -LiteralPath $Path)){return}
 function Expand-ProcessRecord($record) {
  if($null -eq $record){return}
  if($record -is [array]){foreach($child in $record){Expand-ProcessRecord $child};return}
  if($record.PSObject.Properties['value']){Expand-ProcessRecord $record.value;return}
  if($record.Id -and $record.Started -and $record.Name){$record}
 }
 Expand-ProcessRecord (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
}
