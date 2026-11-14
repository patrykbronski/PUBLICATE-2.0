$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$configPath=Join-Path $root 'src/Publicate.Api/appsettings.Local.json'
if(!(Test-Path -LiteralPath $configPath)){throw 'First configure PUBLICATE locally as described in README.md.'}
$config=Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$id=(Read-Host 'Google OAuth Web application Client ID').Trim()
if($id -notmatch '^[a-zA-Z0-9._-]+\.apps\.googleusercontent\.com$'){throw 'Invalid Google Client ID.'}
$secret=Read-Host 'Google Client Secret (hidden; never paste in chat)' -AsSecureString
$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
 if([string]::IsNullOrWhiteSpace($plain)){throw 'Client Secret is required.'}
 $config.Providers | Add-Member -Force -NotePropertyName YouTube -NotePropertyValue ([pscustomobject]@{ClientId=$id;ClientSecret=$plain})
 [IO.File]::WriteAllText($configPath,($config | ConvertTo-Json -Depth 20),(New-Object Text.UTF8Encoding($false)))
}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr);$plain=$null;$secret.Dispose()}
Write-Output 'YouTube configuration saved locally. Restart PUBLICATE backend.'
Write-Output ('Authorized redirect URI: '+$config.OAuth.CallbackBase.TrimEnd('/')+'/api/oauth/YouTube/callback')
