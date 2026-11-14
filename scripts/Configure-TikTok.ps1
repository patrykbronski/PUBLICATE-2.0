$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$configPath=Join-Path $root 'src/Publicate.Api/appsettings.Local.json'
if(!(Test-Path -LiteralPath $configPath)){throw 'Configure PUBLICATE locally first.'}
$config=Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$id=(Read-Host 'TikTok Sandbox Client key (not App ID)').Trim()
if($id -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Invalid Client key.'}
$prefix=(Read-Host 'Verified URL prefix (Enter = https://cofounder-morse-reflex.ngrok-free.dev/)').Trim()
if(!$prefix){$prefix='https://cofounder-morse-reflex.ngrok-free.dev/'}
$uri=$null
if(![Uri]::TryCreate($prefix,[UriKind]::Absolute,[ref]$uri) -or $uri.Scheme -ne 'https' -or $uri.IsLoopback -or $uri.UserInfo -or $uri.Query -or $uri.Fragment){throw 'Expected verified public HTTPS prefix.'}
$approved=(Read-Host 'Direct Post enabled and video.publish listed in this Sandbox? Type YES').Trim() -ceq 'YES'
$secret=Read-Host 'TikTok Sandbox Client secret (hidden; never paste in chat)' -AsSecureString
$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
 $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
 if([string]::IsNullOrWhiteSpace($plain)){throw 'Client secret is required.'}
 $config.Providers | Add-Member -Force -NotePropertyName TikTok -NotePropertyValue ([pscustomobject]@{ClientId=$id;ClientSecret=$plain;PublishingApproved=$approved;VerifiedUrlPrefix=$prefix.TrimEnd('/')+'/';Environment='Sandbox'})
 [IO.File]::WriteAllText($configPath,($config | ConvertTo-Json -Depth 20),(New-Object Text.UTF8Encoding($false)))
}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr);$plain=$null;$secret.Dispose()}
Write-Output 'TikTok configuration saved locally. Restart PUBLICATE backend. No API requests performed.'
Write-Output ('Redirect URI: '+$config.OAuth.CallbackBase.TrimEnd('/')+'/api/oauth/TikTok/callback')
