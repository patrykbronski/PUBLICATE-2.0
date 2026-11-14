param([int]$DatabasePort=0,[switch]$PortableDatabase)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Instance.ps1')
if($DatabasePort -eq 0){$DatabasePort=$instance.databasePort}
Set-Location $projectRoot
$configPath=Join-Path $projectRoot 'src/Publicate.Api/appsettings.Local.json'
if(Test-Path -LiteralPath $configPath){throw 'Konfiguracja już istnieje. Edytuj ją lokalnie; skrypt nie nadpisuje sekretów.'}
function New-Secret { $buffer=New-Object byte[] 32; $rng=[Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($buffer);$rng.Dispose();return [Convert]::ToBase64String($buffer) }
$databasePassword=New-Secret

if($PortableDatabase){$DatabasePort=$instance.databasePort}
$config=@{
 ConnectionStrings=@{Database="Host=localhost;Port=$DatabasePort;Database=publicate;Username=publicate;Password=$databasePassword"}
 Local=@{Origin="http://localhost:$($instance.frontendPort)"}
 OAuth=@{CallbackBase="http://localhost:$($instance.backendPort)"}
 Providers=@{
  LinkedIn=@{ClientId='';ClientSecret='';Version='202607'}
  Meta=@{ClientId='';ClientSecret='';Version=''}
  Instagram=@{ClientId='';ClientSecret='';Version=''}
  TikTok=@{ClientId='';ClientSecret='';PublishingApproved=$false}
  X=@{ClientId='';ClientSecret='';AllowPaidCalls=$false}
 }
 Ai=@{ApiKey='';Model='';AllowPaidCalls=$false}
 Storage=@{Provider='Local';Root='../../.data/media';PublicBaseUrl='';RetentionHours=48;InactiveDays=30}
}
$ffprobe=Get-ChildItem -LiteralPath (Join-Path $projectRoot '.tools/ffmpeg') -Filter ffprobe.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if($ffprobe){$config.Media=@{FfprobePath=$ffprobe.FullName}}
New-Item -ItemType Directory -Force (Join-Path $projectRoot '.data') | Out-Null
if($PortableDatabase){
 $pgBin=Join-Path $projectRoot '.tools/postgresql/pgsql/bin'
 $pgData=Join-Path $projectRoot '.data/postgres'
 if(Test-Path -LiteralPath $pgData){throw 'Katalog bazy już istnieje; nie zostanie nadpisany.'}
 $passwordFile=Join-Path $projectRoot '.data/pg-init-password'
 [IO.File]::WriteAllText($passwordFile,$databasePassword)
 try {
  & (Join-Path $pgBin 'initdb.exe') -D $pgData -U publicate --auth=scram-sha-256 --encoding=UTF8 --locale=C --pwfile=$passwordFile
  if($LASTEXITCODE -ne 0){throw 'initdb nie powiodło się'}
 } finally {Remove-Item -LiteralPath $passwordFile -Force}
 Add-Content -LiteralPath (Join-Path $pgData 'postgresql.conf') -Value "`nlisten_addresses = 'localhost'`nport = $DatabasePort"
 & (Join-Path $pgBin 'pg_ctl.exe') -D $pgData -l (Join-Path $projectRoot '.data/postgres.log') -w start
 if($LASTEXITCODE -ne 0){throw 'PostgreSQL nie wystartował'}
 $env:PGPASSWORD=$databasePassword
 try { & (Join-Path $pgBin 'createdb.exe') -h localhost -p $DatabasePort -U publicate publicate;if($LASTEXITCODE -ne 0){throw 'Nie utworzono bazy'} } finally {Remove-Item Env:PGPASSWORD}
} else {
 [IO.File]::WriteAllText((Join-Path $projectRoot '.env'),"POSTGRES_PASSWORD=$databasePassword`n")
}
[IO.File]::WriteAllText($configPath,($config|ConvertTo-Json -Depth 8))
Write-Host 'Utworzono lokalną konfigurację. Sekrety nie zostały wypisane.'
Write-Host 'Konto PUBLICATE utworzysz w przegladarce: Zaloz konto.'
Write-Host 'Następnie uruchom migracje: scripts/Start-Backend.ps1 -MigrateOnly'
