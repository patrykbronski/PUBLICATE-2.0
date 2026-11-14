$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Instance.ps1')
foreach($file in Get-ChildItem $PSScriptRoot -Filter '*.ps1'){
 $tokens=$null;$errors=$null
 [void][Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
 if($errors.Count){throw "Invalid PowerShell: $($file.Name)"}
}
if($instance.backendPort -eq $instance.otherBackendPort -or $instance.frontendPort -eq $instance.otherFrontendPort){throw 'Conflicting profiles'}
# Use an ephemeral loopback socket, never an application port or provider API.
$listener=New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0)
$listener.Start()
try {
 $instance.otherBackendPort=$listener.LocalEndpoint.Port
 $blocked=$false
 try {Assert-OtherInstanceStopped}catch{$blocked=$true}
 if(!$blocked){throw 'Other-instance guard did not block occupied port'}
} finally {$listener.Stop()}
Write-Host 'PASSED: PowerShell syntax, distinct ports, occupied-port guard. No applications started.'
$temp=Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString()+'.json')
try {
 '[{"value":[{"Id":123,"Started":"456","Name":"old"}],"Count":1},{"Id":789,"Started":"012","Name":"new"}]'|Set-Content $temp
 $records=@(Read-LauncherProcesses $temp)
 if($records.Count -ne 2 -or $records[0].Id -ne 123 -or $records[1].Id -ne 789){throw 'Process state normalization failed'}
}finally{Remove-Item -LiteralPath $temp -ErrorAction SilentlyContinue}
Write-Host 'PASSED: legacy nested launcher state normalized to individual verified records.'
