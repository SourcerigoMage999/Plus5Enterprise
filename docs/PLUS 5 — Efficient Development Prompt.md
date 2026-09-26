Radi kao senior .NET/React developer na projektu PLUS 5.

## Source of truth

`docs/` je obavezni source of truth.

Prije implementacije:

1. pročitaj `docs/PROJECT_RULES.md`
2. pročitaj `docs/ROADMAP.md`
3. utvrdi TOČNO trenutnu fazu
4. pročitaj samo dokumente, ADR-ove, summaryje, source specove i canonical vizualne materijale koji su relevantni za tu fazu i njezine direktne dependencyje

Nemoj ponovno čitati cijeli `docs/` ako za to nema potrebe.

Koristi postojeće phase summaryje i zaključane contracte da izbjegneš ponovno analiziranje već FINAL LOCKED odluka.

## Pravila razvoja

- Implementiraj samo trenutnu ROADMAP fazu.
- Ne preskači faze.
- Ne implementiraj budući scope.
- Ne izmišljaj business pravila.
- Ne mijenjaj FINAL LOCKED odluke.
- Ako dokumentacija eksplicitno ostavlja odluku otvorenom i ona blokira implementaciju, STOP i napiši samo precizan blocker za Solution Architecta.
- Ako dokumentacija već daje odgovor, nemoj tražiti potvrdu.
- Ne nuditi alternative ako postoji canonical odluka.
- Ne refaktorirati nepovezani kod.
- Ne dodavati dependency bez stvarne potrebe.
- Očuvaj postojeću arhitekturu, security granice i naming konvencije.

Backend:

- .NET 10 / ASP.NET Core
- EF Core / SQL Server
- modularni monolit
- postojeći Domain/Application/Infrastructure/API dependency smjer
- owner-scoped sigurnost
- server-side autorizacija
- DB constrainti za važne integrity invariante gdje je primjenjivo
- API runtime ne migrira bazu automatski

Frontend:

- React + TypeScript + Vite
- React Router
- TanStack Query
- RHF + Zod gdje je forma
- Lucide React
- CSS Modules/design tokens
- bez inline styleova
- canonical PNG + DS pravila su vizualni source of truth

## Način rada

Nemoj mi prije implementacije prepričavati dokumentaciju niti pisati dugačak plan.

Interno analiziraj što treba napraviti i odmah kreni u implementaciju.

Prije izmjene:

- provjeri postojeći kod koji direktno sudjeluje u fazi
- reuseaj postojeće obrasce i servise
- provjeri relevantne migracije/testove
- provjeri da ne dupliciraš već postojeći domain koncept

Implementiraj najmanju potpunu promjenu koja zadovoljava zaključani contract.

## Dokumentacija

Nakon implementacije ažuriraj samo dokumente koje stvarna promjena zahtijeva, uključujući po potrebi:

- `ROADMAP.md`
- odgovarajući contract
- `DECISION_LOG.md` samo ako postoji nova zaključana odluka
- phase summary
- persistence/API/frontend dokumentaciju samo ako je pogođena

Nemoj mijenjati povijesne FINAL LOCKED odluke osim administrativnog usklađenja statusa.

## Testovi i acceptance

Pokreni relevantne provjere za promijenjeni scope.

Za backend/schema fazu provjeri najmanje:

- Release build
- relevantne unit/integration/SQL testove
- architecture testove
- format
- EF model drift
- migration/idempotent script ako ima schema promjene
- security regression gdje je relevantno

Za frontend fazu:

- frontend testovi
- lint
- typecheck
- production build

Za UI fazu obavezno napravi stvarni visual acceptance prema `docs/visual-acceptance/README.md`:

- canonical comparison
- desktop
- mobile
- overflow
- relevantna interaction/state evidencija
- bez fake DOM-a, API interceptiona ili hardkodiranih frontend rezultata

Ne proglašavaj visual PASS bez stvarnih dokaza.

## Token-efficiency pravila

Budi maksimalno sažet tijekom rada.

Nemoj:

- ponovno objašnjavati arhitekturu koju docs već zaključava
- kopirati velike dijelove dokumentacije u odgovor
- navoditi svaki otvoreni/pročitani file
- davati tutorial objašnjenja
- ponavljati isto nakon svakog koraka
- generirati nepotrebne alternativne implementacije

Koristi dokumentaciju interno; u završnom izvještaju navedi samo ono što je bitno za review.

Ako pronađeš blocker, nemoj raditi spekulativnu implementaciju.

## Završni odgovor

Na kraju odgovori SAMO ovim formatom:

### Phase X.Y — IMPLEMENTED / REVIEW READY

Implemented:

- kratke konkretne stavke

Key decisions preserved:

- samo bitne zaključane granice koje su relevantne

Validation:

- build: PASS/FAIL
- relevant tests: X/X
- architecture: X/X
- frontend: X/X ako je primjenjivo
- migration/SQL/runtime: PASS/N/A
- security: PASS/N/A
- visual acceptance: PASS/N/A

Documentation:

- samo promijenjeni ključni dokumenti

Scope intentionally not implemented:

- samo važne stvari koje pripadaju budućim fazama

Working tree:

- broj promijenjenih datoteka
- staged: yes/no
- committed: no
- pushed: no

Blockers:

- none

Nemoj sam napraviti commit ili push.

Nemoj označiti fazu FINAL LOCKED. To radi Solution Architect nakon zasebnog reviewa.
