$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$configPath=Join-Path $root 'src/Publicate.Api/appsettings.Local.json'
if(!(Test-Path -LiteralPath $configPath)){throw 'Configure PUBLICATE locally first.'}
$config=Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$id=(Read-Host 'X OAuth 2.0 Client ID (not API Key)').Trim()
if([string]::IsNullOrWhiteSpace($id) -or $id -match '\s'){throw 'Invalid Client ID.'}
$secret=Read-Host 'X OAuth 2.0 Client Secret (hidden; never paste in chat)' -AsSecureString
$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
 if([string]::IsNullOrWhiteSpace($plain)){throw 'Client Secret is required.'}
 $config.Providers | Add-Member -Force -NotePropertyName X -NotePropertyValue ([pscustomobject]@{ClientId=$id;ClientSecret=$plain;AllowPaidCalls=$false})
 [IO.File]::WriteAllText($configPath,($config | ConvertTo-Json -Depth 20),(New-Object Text.UTF8Encoding($false)))
}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr);$plain=$null;$secret.Dispose()}
Write-Output 'X credentials saved locally. Paid calls remain BLOCKED, including profile lookup during connection.'
Write-Output 'No API calls or purchases performed. Approve costs separately before enabling the connection.'
Write-Output ('Authorized redirect URI: '+$config.OAuth.CallbackBase.TrimEnd('/')+'/api/oauth/X/callback')
