$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Instance.ps1')
Assert-OtherInstanceStopped
Set-Location (Join-Path (Split-Path $PSScriptRoot -Parent) 'src/publicate-web')
npm.cmd run dev
