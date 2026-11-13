#!/usr/bin/env bash
# Vérifie que le poste est prêt pour le cours (macOS / Linux / Codespaces).
cd "$(dirname "$0")/.."
ok=1
step() {
  local label="$1" help="$2"; shift 2
  printf '  %s ... ' "$label"
  if "$@" >/dev/null 2>&1; then echo "OK"; else echo "ÉCHEC"; echo "     -> $help"; ok=0; fi
}
echo "Vérification du poste pour TicketRush"
step "Git installé" "Installez Git" git --version
step "Docker démarré" "Lancez Docker et attendez qu'il soit prêt" docker info
step "Téléchargement des images" "Vérifiez votre connexion, puis relancez" sh -c "docker compose pull db && docker pull grafana/k6:latest"
step "Construction et démarrage de TicketRush" "Envoyez la sortie de 'docker compose up --build' au formateur" docker compose up -d --build --wait
step "L'API répond" "Le port 8080 est peut-être déjà utilisé" curl -fsS http://localhost:8080/health
if command -v dotnet >/dev/null; then
  step "SDK .NET 10" "Installez le SDK .NET 10" sh -c "dotnet --list-sdks | grep -q '^10\.'"
  step "Tests d'intégration avec Testcontainers" "Docker doit être démarré" dotnet test tests/TicketRush.IntegrationTests
else
  echo "  SDK .NET absent : vous suivrez la piste « boîte noire » ou Codespaces (voir README)."
fi
docker compose down >/dev/null 2>&1
if [ "$ok" = 1 ]; then echo; echo "Poste prêt. Envoyez une capture de cet écran au formateur."; else echo; echo "Il reste des points à corriger."; exit 1; fi
