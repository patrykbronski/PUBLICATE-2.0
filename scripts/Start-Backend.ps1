param([switch]$MigrateOnly)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Instance.ps1')
Assert-OtherInstanceStopped
$env:Urls="http://localhost:$($instance.backendPort)"
$localDotnet=Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
$dotnet=if(Test-Path -LiteralPath $localDotnet){$localDotnet}else{'dotnet'}
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
if(Test-Path -LiteralPath $localDotnet){$env:DOTNET_ROOT=Split-Path $localDotnet;$env:DOTNET_CLI_HOME=Join-Path $projectRoot '.tools/cli';$env:NUGET_PACKAGES=Join-Path $projectRoot '.tools/packages'}
Set-Location (Join-Path $projectRoot 'src/Publicate.Api')
if($MigrateOnly){& $dotnet run -- --migrate}else{& $dotnet run}
exit $LASTEXITCODE
