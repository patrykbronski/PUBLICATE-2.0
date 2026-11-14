$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$configPath=Join-Path $root 'src/Publicate.Api/appsettings.Local.json'
$config=Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$clientId=(Read-Host 'LinkedIn Client ID (Auth tab)').Trim()
if($clientId -notmatch '^[a-zA-Z0-9]+$'){throw 'Invalid Client ID.'}
$secret=Read-Host 'LinkedIn Client Secret (hidden; never paste in chat)' -AsSecureString
$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
 if([string]::IsNullOrWhiteSpace($plain)){throw 'Client Secret is required.'}
 $config.Providers.LinkedIn.ClientId=$clientId
 $config.Providers.LinkedIn.ClientSecret=$plain
 [IO.File]::WriteAllText($configPath,($config | ConvertTo-Json -Depth 20),(New-Object Text.UTF8Encoding($false)))
}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr);$plain=$null;$secret.Dispose()}
Write-Output 'LinkedIn configuration saved locally. Restart PUBLICATE backend.'
Write-Output ('Redirect URI: '+$config.OAuth.CallbackBase.TrimEnd('/')+'/api/oauth/LinkedIn/callback')
