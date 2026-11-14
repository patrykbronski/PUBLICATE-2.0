$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
$localDotnet=Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
$dotnet=if(Test-Path -LiteralPath $localDotnet){$localDotnet}else{'dotnet'}
if(Test-Path -LiteralPath $localDotnet){$env:DOTNET_ROOT=Split-Path $localDotnet;$env:DOTNET_CLI_HOME=Join-Path $projectRoot '.tools/cli';$env:NUGET_PACKAGES=Join-Path $projectRoot '.tools/packages'}
& $dotnet test tests/Publicate.Tests -c Composer --logger 'trx;LogFileName=unit-tests.trx'
if($LASTEXITCODE -ne 0){throw 'Testy backendu nie powiodły się'}
Set-Location src/publicate-web
npm.cmd run build
if($LASTEXITCODE -ne 0){throw 'Kompilacja frontendu nie powiodła się'}
