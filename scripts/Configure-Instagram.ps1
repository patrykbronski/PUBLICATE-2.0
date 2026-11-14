$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$configPath=Join-Path $projectRoot 'src/Publicate.Api/appsettings.Local.json'
$config=Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$version=Read-Host 'Graph API version shown in Meta dashboard (format vNN.0)'
if($version -notmatch '^v[0-9]+\.0$'){throw 'Invalid version format'}
$secret=Read-Host 'Meta App Secret (hidden; never paste in chat)' -AsSecureString
$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
 if([string]::IsNullOrWhiteSpace($plain)){throw 'App Secret is required'}
 $config.Providers.Instagram.ClientSecret=$plain
 $config.Providers.Instagram.Version=$version
 [IO.File]::WriteAllText($configPath,($config | ConvertTo-Json -Depth 20),(New-Object Text.UTF8Encoding($false)))
} finally {[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr);$plain=$null;$secret.Dispose()}
Write-Output 'Instagram configuration saved locally. Restart PUBLICATE backend.'
