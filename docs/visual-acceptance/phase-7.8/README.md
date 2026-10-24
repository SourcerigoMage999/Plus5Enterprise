# Phase 7.8 visual acceptance

## Status

**PASS — 2026-10-24.**

## Canonical source

- izvor: `4.3 P6.png`;
- lokalna neizmijenjena kopija: `canonical.png`;
- SHA-256: `19912221190BDD2CDFB1772CF13814C2347DD2AD7823250232F020520C601EF4`.

## Minimalni stvarni capture

- `presentation-review-desktop-1536x1024.png`;
- `presentation-review-mobile-390x844.png`;
- `measurements.json`.

Capture koristi stvarni Teacher cookie login, aktualni lokalni API, Docker SQL Server,
S3-compatible private storage i ClamAV. Fixture je legalno nastao stvarnim Material import,
Draft i Presentation initialize workflowom. Nisu korišteni auth bypass, API interception ni
DOM mutation. Tijekom desktop/mobile capturea nema business writeova ni browser/page grešaka.

## Measurements

| Prikaz | clientWidth | scrollWidth | Rezultat |
|---|---:|---:|---|
| desktop 1536×1024 | 1536 | 1536 | PASS |
| mobile 390×844 | 390 | 390 | PASS |

Desktop dokazuje četvrti korak, stvarne osnovne/pedagoške podatke, spremljeni slide preview,
content counts, spremno stanje i publish akciju. Mobile fokusira isti stvarni slide preview,
thumbnails i content summary u jednostupčanom rasporedu bez globalnog horizontalnog
overflowa.

## Runtime publish evidence

Nakon capturea isti stvarni Draft objavljen je jednom kroz produkcijski flow:

```text
review 200
→ publish 200
→ Material detail 200 s application/vnd.plus5.presentation+zip
→ originalFileName plus5-presentation.zip
→ bivši Draft review 404
```

Time su zajedno dokazani finalni review state, deterministic private package i atomska
aktivacija. Runtime gate nije ponavljan nakon uspjeha.

## Canonical usporedba

Očuvani su canonical P6 app shell, četiri koraka, dominantni metadata/pedagogy sažetak,
desni preview/quick-summary prostor, spremno stanje i završna save akcija. Aktualni PLUS 5
layout koristi zaključani 204 px shell i postojeće design tokene. Preview renderira stvarni
typed graph, bez hardkodiranog `Present Perfect` sadržaja ili canonical demo brojki.

Namjerna odstupanja:

- canonical demonstrira bogatu prezentaciju s 12 slajdova; evidence koristi minimalni legalni
  stvarni Draft s jednim praznim slajdom, bez lažnih content elemenata;
- nema `Spremi nacrt` na review ekranu jer je Draft već spremljen Phase 7.7 autosaveom;
- package, concurrency, ownership, CSRF i validation failure putovi nisu duplicirani
  screenshotovima jer su pokriveni targeted testovima/runtime gateom.

## Reprodukcija

`frontend/tests/visual/phase78.mjs` stvara legalni lokalni fixture, radi točno jedan desktop i
jedan mobile viewport capture te zatim jednom objavljuje isti Draft. Lokalna lozinka nije
spremljena u Git ni visual evidence.
