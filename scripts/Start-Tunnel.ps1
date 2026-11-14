$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Instance.ps1')
Assert-OtherInstanceStopped
$agent=Join-Path $projectRoot '.tools/ngrok/ngrok.exe'
if(!(Test-Path -LiteralPath $agent)){
 $installed=Get-Command ngrok -ErrorAction SilentlyContinue
 if(!$installed){throw 'Install ngrok from Microsoft Store first: winget install ngrok -s msstore'}
 $agent=$installed.Source
}
$response=Invoke-WebRequest -UseBasicParsing "http://localhost:$($instance.frontendPort)" -TimeoutSec 10
if($response.StatusCode -ne 200){throw 'PUBLICATE frontend is not available on port 5173'}
& $agent http $instance.frontendPort --url $instance.publicUrl
