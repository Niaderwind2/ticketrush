# Vérifie que le poste est prêt pour le cours (Windows / PowerShell).
#   powershell -ExecutionPolicy Bypass -File scripts/check-env.ps1
$ok = $true
function Step($label, [scriptblock]$test, $help) {
    Write-Host -NoNewline "  $label ... "
    try { & $test | Out-Null; if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "code $LASTEXITCODE" }; Write-Host "OK" -ForegroundColor Green }
    catch { Write-Host "ÉCHEC" -ForegroundColor Red; Write-Host "     -> $help"; $script:ok = $false }
}
Set-Location (Join-Path $PSScriptRoot "..")
Write-Host "Vérification du poste pour TicketRush"
Step "Git installé" { git --version } "Installez Git : https://git-scm.com"
Step "Docker démarré" { docker info } "Lancez Docker Desktop (ou Rancher Desktop) et attendez qu'il soit prêt"
Step "Téléchargement des images (quelques minutes la première fois)" { docker compose pull db; docker pull grafana/k6:latest } "Vérifiez votre connexion, puis relancez"
Step "Construction et démarrage de TicketRush" { docker compose up -d --build --wait } "Envoyez la sortie de 'docker compose up --build' au formateur"
Step "L'API répond" { Invoke-RestMethod http://localhost:8080/health } "Le port 8080 est peut-être déjà utilisé sur votre poste"
$hasDotnet = $null -ne (Get-Command dotnet -ErrorAction SilentlyContinue)
if ($hasDotnet) {
    Step "SDK .NET 10" { if (-not ((dotnet --list-sdks) -match '^10\.')) { throw "absent" } } "Installez le SDK .NET 10 : https://dotnet.microsoft.com/download"
    Step "Tests d'intégration avec Testcontainers" { dotnet test tests/TicketRush.IntegrationTests } "Docker doit être démarré ; envoyez la sortie au formateur"
} else {
    Write-Host "  SDK .NET absent : vous suivrez la piste « boîte noire » ou l'environnement Codespaces (voir README)." -ForegroundColor Yellow
}
docker compose down | Out-Null
if ($ok) { Write-Host "`nPoste prêt. Envoyez une capture de cet écran au formateur." -ForegroundColor Green }
else { Write-Host "`nIl reste des points à corriger : voir les messages ci-dessus." -ForegroundColor Red; exit 1 }
