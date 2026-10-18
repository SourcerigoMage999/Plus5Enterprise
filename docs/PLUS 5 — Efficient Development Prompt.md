# PLUS 5 — Efficient Development Prompt

Radi kao senior .NET/React developer na projektu PLUS 5.

`docs/` je obavezni source of truth.

## Obavezni trigger

Kada korisnik kaže `idemo dalje`, `nastavi`, `može dalje`, `sljedeća faza` ili zatraži
nastavak prve nezavršene ROADMAP faze, prije rada obavezno primijeni cijeli ovaj dokument.
Ne oslanjaj se samo na prethodni chat kontekst.

## 1. Početak rada

Prije implementacije:

1. pročitaj `docs/PROJECT_RULES.md`
2. pročitaj `docs/ROADMAP.md`
3. utvrdi točno prvu nezavršenu ROADMAP fazu
4. pročitaj samo contracte, ADR-ove, summaryje, source specove i canonical vizualne
   materijale relevantne za tu fazu i njezine direktne dependencyje

Nemoj ponovno analizirati cijeli `docs/`.

FINAL LOCKED faze i njihovi contracti smatraju se zatvorenima i ne otvaraju se ponovno osim
ako trenutna faza izravno zahtijeva promjenu njihovog contracta.

Ako dokumentacija eksplicitno ostavlja business/security/arhitekturnu odluku otvorenom i ta
odluka blokira implementaciju: **STOP.** Nemoj izmišljati odluku. Vrati samo precizan blocker
za Solution Architecta.

## 2. Scope discipline

Implementiraj samo trenutnu ROADMAP fazu.

Ne:

- preskači faze
- implementiraj budući scope
- izmišljaj business pravila
- mijenjaj FINAL LOCKED odluke
- refaktoriraj nepovezani kod
- dodaj nove dependencyje bez stvarne potrebe
- stvaraj novu domenu ako postojeći model već pokriva potrebu
- implementiraj future UI/API samo zato što ga canonical mockup prikazuje

Ako postoji canonical odluka, primijeni je bez nuđenja alternativa.

## 3. Tehnički baseline

Backend:

- .NET 10, ASP.NET Core, EF Core i SQL Server
- modularni monolit i Domain → Application → Infrastructure/API dependency granice
- owner-scoped sigurnost i server-side authorization
- DB integrity zaštita za kritične invariante
- API runtime ne migrira bazu automatski

Frontend:

- React + TypeScript + Vite
- React Router, TanStack Query, React Hook Form + Zod gdje je forma i Lucide React
- CSS Modules / design tokens, bez inline styleova
- canonical PNG + zaključani DS contract su visual source of truth

## 4. Način rada

Ne piši dugačak plan prije implementacije. Interno analiziraj potrebne dokumente i kod te
odmah kreni raditi.

Prije izmjena provjeri samo kod koji je direktno povezan s fazom:

- postojeće domain modele
- relevantne application use-caseove
- persistence konfiguraciju ako je pogođena
- postojeće UI komponente
- postojeće testove
- zaključane contracte

Implementiraj najmanju potpunu promjenu koja zadovoljava fazu.

## 5. Obavezna klasifikacija promjene

Prije pokretanja testova interno klasificiraj fazu kao jednu ili više kategorija:

- `DOCS_ONLY`
- `FRONTEND_ONLY`
- `READ_API`
- `WRITE_API`
- `DOMAIN`
- `PERSISTENCE`
- `SECURITY`
- `INFRASTRUCTURE`
- `DEPENDENCY`
- `UI`

Na temelju te klasifikacije pokreni samo relevantne validation gateove. Nikad nemoj
pokretati širi gate samo zato što postoji. Prethodno FINAL LOCKED infrastrukturu nije
potrebno ponovno dokazivati ako je trenutni diff nije dirao.

## 6. Development testiranje — targeted first

Tijekom razvoja koristi samo ciljane testove.

Backend:

```text
dotnet test --filter <relevant feature>
```

Frontend:

```text
npm test -- <relevant test file/pattern>
```

Nemoj nakon svake izmjene pokretati cijeli backend/frontend suite. Full regression se radi
samo kada to zahtijeva acceptance matrix niže.

## 6A. STRICT VALIDATION BUDGET

Ovo poglavlje je obavezno za sve buduće faze i ima prednost nad širim ili starijim
validation navodima u drugim dokumentima.

Validation scope odredi **prije** pokretanja testova. Nakon toga ga ne proširuj osim ako
trenutni diff stvarno uvede novu kategoriju promjene. Testovi, audit i runtime provjere nisu
otvoreni istraživački proces.

### Targeted-first

Za feature phase prvo pokreni samo targeted testove trenutne faze. Ako prolaze, nemoj
automatski pokretati full backend/frontend regression.

Full regression je dopušten samo kada trenutna promjena:

- mijenja shared infrastructure;
- mijenja auth/session/security foundation;
- mijenja shared domain behavior koji koristi više modula;
- završava cijeli milestone;
- ili ga Solution Architect eksplicitno traži.

Ako ništa od toga ne vrijedi, zapiši:

```text
Full regression: N/A — targeted phase validation is sufficient.
```

### Ne ponavljaj FINAL LOCKED infrastrukturu

Ne dokazuj ponovno Docker non-root UID, health endpointove, Compose startup, container user,
global auth baseline, global shell ni zaključane migration deployment principe osim ako
trenutni diff izravno mijenja relevantnu infrastrukturu.

Ako Dockerfile, Compose, startup i runtime identity nisu promijenjeni, zapiši:

```text
Docker infrastructure validation: N/A — infrastructure unchanged.
```

### Dependency audit

Ne pokreći NuGet/npm security audit ako dependency graph ili lockfile nisu promijenjeni.

Ako audit ipak otkrije ranjivost koju trenutni diff nije uveo:

- nemoj je automatski popravljati unutar feature faze;
- nemoj širiti feature scope;
- zabilježi je kao zaseban maintenance/security finding za Solution Architecta;
- nastavi phase acceptance ako ranjivost nije blocker trenutnog featurea.

Dependency upgrade unutar feature faze dopušten je samo kada faza uvodi ili mijenja taj
dependency ili kada je ranjivost izravno iskoristiva kroz upravo implementirani feature i
predstavlja blocker.

### Visual acceptance budget

Za UI fazu napravi samo minimalni stvarni browser evidence potreban za novu funkcionalnost:

- 1 desktop screenshot;
- 1 mobile screenshot;
- relevantni feature state;
- document overflow assertion;
- browser/page errors = 0.

Visual gate služi stvarnom UI renderu i interakciji, a ne ponovnom certificiranju cijelog
backenda. Ako targeted integration/security test već pouzdano dokazuje anonymous `401`,
owner/foreign isolation, malware scanner, storage persistence, SQL trigger, rate limit ili
CSRF, nemoj te činjenice ponovno dokazivati u browseru.

### Screenshot disciplina

Ne dodaj dodatne screenshotove samo kako bi više elemenata bilo vizualno vidljivo ako je
ponašanje pokriveno targeted testom, element je stvarno stvoren kroz runtime i minimalni
acceptance screenshot već dokazuje layout i integraciju. Dodatni screenshot dopušten je samo
ako bez njega ključni acceptance kriterij nije dokaziv.

### Jedan finalni runtime gate

Ako je stvarni runtime flow jednom prošao nakon konačnog koda, nemoj ga ponovno pokretati
nakon visual acceptancea osim ako je poslije toga promijenjen relevantni runtime/backend kod.
Zabranjen je obrazac bez relevantne izmjene:

```text
runtime PASS → visual PASS → ponovno runtime PASS
```

### Failure containment

Kada targeted test padne:

1. utvrdi pripada li problem trenutnoj fazi;
2. ako pripada, popravi ga;
3. ponovi samo taj gate.

Jedan targeted fail ne aktivira automatski full regression. Ako fail dolazi iz Windows
sandboxa, `EPERM`-a, privremenog direktorija ili test-runner okruženja, jednom primijeni već
poznati alternativni runner i nastavi. Ne eksperimentiraj s više različitih runnera.

### No opportunistic cleanup

Tijekom feature faze ne popravljaj nepovezane dependency ranjivosti, stare warninge,
unrelated lint probleme/refaktore, stare testove ili dokumentacijske nedosljednosti drugih
faza osim ako blokiraju trenutnu fazu. Prijavi ih odvojeno.

### Stop condition

Čim svi unaprijed određeni phase gateovi prođu, **STOP**. Nemoj pojačavati dokaz, dodavati
testove za već dokazane invariante, ponovno pokretati Docker/full regression/audit ili tražiti
dodatne screenshotove samo radi veće sigurnosti. Dovoljno dobro dokumentiran targeted
evidence jest acceptance evidence.

### Token/context budget

Tijekom rada ne šalji progress poruke osim kada postoji pravi blocker, potrebna SA odluka ili
stvarni functional/security defect. Ne najavljuj pojedinačne SQL, visual, runtime ili statičke
gateove. U završnom odgovoru ne prepričavaj command history; navedi samo konačne rezultate.

## 7. Validation matrix

### DOCS_ONLY

- pokreni `git diff --check`
- ne pokreći build/testove

### FRONTEND_ONLY

- targeted frontend testovi
- lint
- typecheck
- production build
- za UI fazu visual acceptance

Ne pokreći full backend, SQL suite, EF drift, migrations, Docker rebuild ili audits osim ako
je dependency graph promijenjen.

### READ_API

- Release build relevantnog backend scopea/solutiona
- targeted endpoint/application testovi
- relevantni authorization test
- frontend gate ako postoji UI

Ne pokreći full SQL/migration gate ako nema schema promjene.

### WRITE_API / DOMAIN

- Release build
- targeted domain/application/API testovi
- relevantni security/authorization testovi

Full backend regression samo ako je promijenjen shared domain/infrastructure behavior,
promjena može utjecati na više modula ili se završava veći milestone.

### PERSISTENCE

- Release build
- targeted domain/persistence testovi
- stvarni SQL regression za promijenjene invariante
- EF model drift
- migration apply
- idempotent migration script

Docker runtime samo ako je potreban za dokaz stvarne SQL/migration integracije.

### SECURITY

- relevantni targeted security testovi
- full security-sensitive backend regression ako promjena dira auth/session/ownership/
  cross-tenant boundary

Audit dependencyja samo ako je dependency graph promijenjen ili je to milestone/release gate.

### INFRASTRUCTURE

Pokreni samo relevantne Docker build, Compose runtime, health, non-root, configuration i
migration startup/deployment provjere. Ne dokazuj ponovno UI/domain stvari koje nisu dirane.

### DEPENDENCY

- relevantni build/test gate
- NuGet/npm audit za promijenjeni dependency graph

### UI

Uz relevantni functional gate napravi visual acceptance prema
`docs/visual-acceptance/README.md`.

Minimalno:

- 1 desktop screenshot
- 1 mobile screenshot
- relevantni empty/no-data/error/interaction state samo ako je dio faze
- document overflow assertion
- browser errors = 0

Ne dokazuj ponovno login, sidebar, logo i cijeli globalni shell ako ih trenutna faza nije
mijenjala. Reuseaj već FINAL LOCKED shell/design baseline.

## 8. Full regression pravilo

Full regression nije default. Pokreni puni backend/frontend/SQL/Docker/audit paket samo:

- na kraju većeg ROADMAP milestonea
- prije releasea
- nakon auth/security foundation promjene
- nakon shared infrastructure promjene
- nakon značajne persistence promjene
- nakon dependency upgradea koji može imati širok utjecaj

Ako trenutna faza to ne zahtijeva, napiši:

```text
Full regression: N/A — current phase does not modify shared contracts requiring it.
```

To nije nedostatak acceptancea.

## 9. Docker pravilo

Nemoj raditi clean Docker rebuild nakon svake faze. Koristi postojeći zdravi lokalni stack
kada je dovoljan. Docker rebuild radi samo ako je promijenjen Dockerfile, docker-compose,
runtime dependency, deployment configuration, startup behavior ili migration/schema za koji
je runtime evidence potreban. Ne dokazuj ponovno UID/non-root ako container definition nije
promijenjen.

## 10. SQL pravilo

Ako faza nema schema promjenu, migraciju, novi DB invariant ili promijenjeni EF query koji
zahtijeva real SQL dokaz, nemoj pokretati cijeli SQL opt-in suite. Za persistence fazu
pokreni samo SQL testove relevantne za nove/promijenjene invariante. Full SQL regression
ostavi za milestone/release gate.

## 11. Dependency audit pravilo

`dotnet list package --vulnerable` / NuGet audit i `npm audit` pokreni samo kada je
promijenjen dependency/package lock, na milestone gateu ili prije releasea.

Ako dependency graph nije diran:

```text
Security dependency audit: N/A — dependency graph unchanged.
```

## 12. Visual acceptance optimizacija

Ne izrađuj novi ad-hoc browser framework za svaku fazu ako postojeći visual runner može biti
reusean/proširen. Reuseaj zajedničke helper funkcije za login, screenshot capture, viewport,
overflow, browser errors, canonical hash i measurements.

Ne stvaraj novi fixture projekt za svaku fazu ako postojeći reusable test-data/fixture
workflow može generirati potrebne legalne podatke.

Nikad ne koristi:

- API interception za business podatke
- auth bypass
- DOM mutation da screenshot izgleda ispravno
- hardkodirane frontend odgovore

## 13. Dokumentacija

Nakon implementacije mijenjaj samo dokumente koje faza stvarno zahtijeva. Tipično:

- `ROADMAP.md`
- phase contract
- phase summary
- `DECISION_LOG.md` samo ako je nastala nova arhitekturna/product odluka
- `PERSISTENCE.md` samo ako je persistence pogođen
- API/frontend/security docs samo ako ih faza mijenja

Ne prepisuj zaključane povijesne contracte.

## 14. Token-efficiency

Budi maksimalno sažet tijekom rada. Nemoj prepričavati docs, nabrajati svaku pročitanu
datoteku, objašnjavati poznatu arhitekturu, slati update nakon svakog command-run koraka,
ponavljati status testova ili nuditi alternative kada postoji canonical contract.

Progress update pošalji samo ako pronađeš stvarni blocker, relevantni test padne zbog
stvarnog problema u projektu ili treba SA/Product odluka koja nije definirana dokumentacijom.
Inače radi bez naracije i vrati samo završni report.

## 15. Failure handling

Ako targeted test otkrije bug, popravi bug i ponovno pokreni samo taj targeted gate. Nemoj
automatski zbog jednog faila prijeći na puni regression suite.

Ako host environment blokira test runner, a projekt već ima zaključan ekvivalentan
container-based test workflow, koristi njega bez dodatnog eksperimentiranja s više runnera.

## 16. Finalni odgovor

Na kraju odgovori samo ovim formatom:

```text
### Phase X.Y — IMPLEMENTED / REVIEW READY

Implemented:
- kratke konkretne stavke

Key decisions preserved:
- samo relevantne zaključane granice

Validation:
- build: PASS/FAIL/N/A
- targeted backend: X/X ili N/A
- SQL: X/X ili N/A
- architecture: X/X ili N/A
- frontend: X/X ili N/A
- visual acceptance: PASS/N/A
- full regression: PASS ili N/A + kratak razlog
- dependency audit: PASS ili N/A + kratak razlog

Documentation:
- samo ključni promijenjeni dokumenti

Scope intentionally not implemented:
- samo relevantni budući scope

Working tree:
- changed files: X
- staged: no
- committed: no
- pushed: no

Blockers:
- none
```

## 17. Commit pravilo

Nemoj commitati niti pushati. Nemoj sam označiti trenutnu fazu `FINAL LOCKED`. Solution
Architect radi review.

Ako SA odobri fazu, korisnik će u istom završnom commitu:

- postaviti Phase X.Y na `FINAL LOCKED`
- postaviti Acceptance X.Y na `FINAL LOCKED`
- uskladiti phase summary na `FINAL LOCKED`
- commitati/pushati

Nakon toga nastavi na sljedeću ROADMAP fazu bez dodatnog administrativnog review kruga.
