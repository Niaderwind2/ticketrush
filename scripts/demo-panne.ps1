# Démo d'ouverture du cours (Windows / PowerShell) : la CI est verte, et pourtant...
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

Write-Host "==> Démarrage de TicketRush (base, API, paiement)"
docker compose up -d --build --wait db payment api | Out-Null

Write-Host "==> Remise en vente de toutes les places"
Invoke-RestMethod -Method Post http://localhost:8080/admin/reset | Out-Null

Write-Host "==> Ouverture des ventes : 300 fans, 200 places, 30 secondes"
docker compose --profile charge run --rm k6

Write-Host ""
Write-Host "==> Ce que les tests n'ont pas vu :"
Invoke-RestMethod http://localhost:8080/admin/stats | Format-List
