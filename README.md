# TicketRush

Billetterie de concerts en ligne, **fictive**, qui sert de fil rouge au cours **Stratégie de tests** (INF243, MAALSI, CESI Rouen).

Le jour de l'ouverture des ventes, la CI est verte... et pourtant des places sont vendues deux fois.
Toute la journée sert à comprendre comment c'est possible, et comment l'éviter.

> Support de cours : dossier [`docs/`](docs/index.html) (publiable avec GitHub Pages).

## Avant le cours (obligatoire)

1. Installez **Git** et **Docker Desktop** (ou Rancher Desktop), puis démarrez Docker.
2. Facultatif mais recommandé pour la piste code : le **SDK .NET 10**.
3. Clonez le dépôt et lancez la vérification :

   ```powershell
   git clone https://github.com/Niaderwind2/ticketrush
   cd ticketrush
   powershell -ExecutionPolicy Bypass -File scripts/check-env.ps1   # Windows
   ./scripts/check-env.sh                                           # macOS, Linux
   ```

4. Envoyez une capture du résultat au formateur, ou signalez votre blocage **avant** le cours.

**Si l'installation échoue :** ouvrez le dépôt dans **GitHub Codespaces** (bouton *Code* > *Codespaces*). Tout est préinstallé.

## Deux pistes, au choix

| Piste | Pour qui | Ce qu'il faut |
|---|---|---|
| **Code** | à l'aise en C# ou curieux de s'y mettre | SDK .NET 10 + Docker |
| **Boîte noire** | préfère un autre langage | Docker seulement : on teste l'API HTTP en k6, Python, Java... (voir [`boite-noire/`](boite-noire/README.md)) |

## Commandes utiles

| Commande | Effet |
|---|---|
| `docker compose up -d --build --wait` | Lance TicketRush sur http://localhost:8080 |
| `docker compose down -v` | Arrête tout et efface la base |
| `dotnet test tests/TicketRush.UnitTests` | Tests unitaires |
| `dotnet test tests/TicketRush.IntegrationTests` | Tests d'intégration (PostgreSQL dans un conteneur) |
| `dotnet test --collect:"XPlat Code Coverage"` | Tests avec couverture |
| `cd tests/TicketRush.UnitTests && dotnet tool restore && dotnet stryker` | Mutation testing |
| `docker compose --profile charge run --rm k6` | Simule l'ouverture des ventes |

## Branches

| Branche | Contenu |
|---|---|
| `main` | Point de départ de tous les labs |
| `lab-1-solution` | Lab 1 corrigé : test de concurrence, unicité garantie par la base |
| `lab-2-solution` | Lab 2 corrigé : tests du calcul de prix, score de mutation 87,5 % |

## Organisation du code

```
src/TicketRush.Domain/        règles métier (prix, quota, blocage), service de réservation
src/TicketRush.Api/           API HTTP + accès PostgreSQL en SQL direct
tools/TicketRush.FakePayment/ faux prestataire de paiement
tests/TicketRush.UnitTests/   tests unitaires (xUnit, NSubstitute) + configuration Stryker
tests/TicketRush.IntegrationTests/ tests avec un vrai PostgreSQL (Testcontainers)
k6/                           scénarios de charge et test de double vente
boite-noire/                  tests de l'API dans d'autres langages
docs/                         support de cours HTML
```
