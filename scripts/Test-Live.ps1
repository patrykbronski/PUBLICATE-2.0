param([Parameter(Mandatory=$true)][Guid]$VariantId)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Instance.ps1')
Assert-OtherInstanceStopped
$headers=@{'X-Publicate'='1'}
$credentials=Get-Credential -Message 'Konto PUBLICATE: e-mail i haslo (nie haslo do platformy spolecznosciowej)'
Invoke-RestMethod "http://localhost:$($instance.backendPort)/api/session" -Method Post -Headers $headers -ContentType application/json -Body (@{email=$credentials.UserName;password=$credentials.GetNetworkCredential().Password}|ConvertTo-Json) -SessionVariable session | Out-Null
$issues=Invoke-RestMethod "http://localhost:$($instance.backendPort)/api/variants/$VariantId/validate" -Method Post -Headers $headers -WebSession $session -ContentType application/json -Body '{}'
if($issues.issues.Count -gt 0){$issues.issues;throw 'Najpierw rozwiąż problemy walidacji'}
Write-Host "To uruchomi RZECZYWISTĄ publikację zapisanego wariantu $VariantId. Sprawdź treść i cel w panelu."
if((Read-Host 'Wpisz PUBLIKUJ, aby zatwierdzić ten post') -cne 'PUBLIKUJ'){throw 'Anulowano'}
$expose=(Read-Host 'Czy zatwierdzasz publiczny HTTPS plików dla Instagrama? Wpisz TAK') -ceq 'TAK'
$body=@{requestKey=[Guid]::NewGuid().ToString();confirmed=$true;exposeConfirmed=$expose}|ConvertTo-Json
# One request only. Never retry automatically after a timeout.
$job=Invoke-RestMethod "http://localhost:$($instance.backendPort)/api/variants/$VariantId/publish" -Method Post -Headers $headers -WebSession $session -ContentType application/json -Body $body -TimeoutSec 330
$job | Select-Object id,platform,state,externalId,operationId,url,diagnostic
