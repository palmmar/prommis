# Prommis

En webbapp byggd i **ASP.NET Core Razor Pages** med **Identity**, **EF Core** och **SQLite**. Sidan fokuserar på att visa statistik för hur många steg användare har tagit per dag, både individuellt och i grupper. UI är byggt med **Tailwind CSS** och **Alpine.js**, grafer renderas med **Chart.js**.

## Funktioner
- Registrering och inloggning (ASP.NET Identity)
- Logga flera stegposter per dag
- Redigera/ta bort endast dagens poster
- Tre grafer: vecka (dag), månad (dag), år (månad)
- Gruppfunktioner: skapa grupp, bjud in via länk, se totalsumma
- Gruppägare kan hantera medlemmar och överföra ägarskap

## Teknik
- ASP.NET Core (Razor Pages)
- Identity + EF Core + SQLite
- Tailwind CSS (CLI build)
- Alpine.js
- Chart.js

## Kom igång

### 1) Installera beroenden
```bash
npm install
```

### 2) Bygg Tailwind CSS
```bash
npm run build:css
```

### 3) Databas & migrationer
```bash
dotnet ef database update
```

### 4) Starta appen
```bash
dotnet run
```

Öppna:
- https://127.0.0.1:5001
- eller http://127.0.0.1:5000

## Dev‑läge
Kör Tailwind‑watch + appen samtidigt:
```bash
npm run dev
```

## Mappstruktur (översikt)
- `Pages/` — Razor Pages
- `Areas/Identity/` — Identity‑UI (scaffoldad)
- `Models/` — Domänmodeller
- `Services/` — Statistiklogik
- `Data/` — DbContext + migrationer
- `Styles/` — Tailwind‑entry (`tailwind.css`)
- `wwwroot/` — statiska filer

## Vanliga kommandon
```bash
npm run build:css
npm run watch:css
npm run dev

dotnet ef migrations add <Namn>
dotnet ef database update
```

## Noteringar
- `wwwroot/css/app.css` byggs via Tailwind CLI och är **ignorerad i git**.
- SQLite‑databasen (`app.db`) är lokal och ignoreras i git.

## Licens
Privat projekt.
