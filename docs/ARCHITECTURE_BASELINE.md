# ARCHITECTURE_BASELINE

## Status

**LOCKED baseline v1.0 — 2026-08-23**

Ovaj dokument definira obavezni tehnološki i arhitekturni baseline za PLUS 5. Senior developer i AI moraju ga poštovati zajedno s `PROJECT_RULES.md`, `ROADMAP.md` i specifičnim tehničkim standardima iz ovog paketa.

Promjena zaključane odluke zahtijeva eksplicitno odobrenje vlasnika proizvoda i ADR zapis u `DECISION_LOG.md`.

## 1. Ciljevi arhitekture

PLUS 5 mora biti:

- sigurna aplikacija po principu security-by-design
- održiva i testabilna kroz višefazni razvoj
- spremna za više od 10.000 registriranih/aktivnih korisnika bez promjene temeljnog stacka
- sposobna za horizontalno skaliranje API sloja kada mjerenja pokažu potrebu
- modularna bez preuranjenog uvođenja mikroservisa
- deployable kroz Docker
- spremna za kasniji produkcijski deployment na VPS

> `10.000+ korisnika` nije isto što i `10.000 istovremenih korisnika`. Stvarni concurrency/SLA mora se dokazati load testovima prije produkcijskog locka.

## 2. Zaključani tehnološki stack

### Backend

- C#
- ASP.NET Core
- .NET 10 kao početni target runtime
- Entity Framework Core za standardni relational persistence
- async I/O za mrežu, bazu, storage i druge I/O operacije

### Frontend

- React
- TypeScript sa strict provjerama
- Vite
- SPA pristup dok poslovni zahtjev ne zatraži SSR/SEO

### Database

- Microsoft SQL Server
- schema management isključivo kroz verzionirane EF Core migracije
- raw SQL dopušten samo kada je opravdan i testiran; nije zamjena za nejasan model

### API

- HTTPS REST/JSON API
- eksplicitni API contracti
- versioning od prvog javnog contracta, preferirano `/api/v1/...`
- standardizirani error contract

### Containers / deployment

- Docker je standardni način pakiranja aplikacije
- Docker Compose je dopušten za lokalni razvoj i inicijalnu single-VPS topologiju
- produkcijski deployment na VPS uvodi se tek u odgovarajućoj ROADMAP fazi

## 3. Arhitekturni stil

Početni sustav je **modularni monolit**.

Obavezno:

- jasne granice modula/domene
- business logika izvan kontrolera i React komponenti
- dependency smjer prema domeni/aplikacijskom sloju
- vanjski servisi iza adaptera kada postoji realna zamjenjivost ili testna granica
- bez kružnih ovisnosti između modula
- bez shared “god” projekta koji postaje odlagalište svega

Mikroservisi nisu dopušteni bez izmjerenog razloga i novog ADR-a.

## 4. Backend slojevi

Preporučeni logical boundaries:

1. **Domain** — entiteti, value objecti, invarianti i čista poslovna pravila
2. **Application** — use caseovi, orchestration, ports/interfaces, authorization intent
3. **Infrastructure** — EF Core, email, object storage, external providers, clock/IO adapters
4. **API** — HTTP contract, authentication boundary, validation mapping, serialization

Točan folder/project layout može se prilagoditi repozitoriju, ali dependency pravila ne smiju biti narušena.

## 5. Frontend granice

Frontend je klijent backend sustava, ne drugi business engine.

Frontend smije:

- prikazivati podatke i UI state
- provoditi UX/client validation radi korisničkog iskustva
- upravljati lokalnim interakcijama
- cacheirati server state kroz standardizirani data-fetching sloj kada se uvede

Frontend ne smije biti jedino mjesto za:

- autorizaciju
- business invariants
- financijske/knowledge/readiness izračune koji su dio poslovnog contracta
- odluke o dopuštenim prijelazima stanja

## 6. Scalability baseline

Arhitektura mora omogućiti:

- stateless API instance gdje god je moguće
- session/state koji je potreban za horizontalno skaliranje ne smije biti skriven samo u memoriji jedne instance
- pagination za potencijalno velike liste
- selektivne DB projekcije umjesto nekontroliranog učitavanja cijelih aggregate graphova
- indekse prema stvarnim query patternima
- connection pooling
- cache tek kada postoji dokazani benefit i definirana invalidacija
- background processing tek kada use case to zahtijeva
- object storage za veće datoteke umjesto spremanja file blobova u glavne relational tablice, kada se uvede Materials storage

## 7. Security baseline

Security je cross-cutting requirement u svakoj fazi.

Obavezno:

- HTTPS u produkciji
- autentikacija i autorizacija na serveru
- least privilege
- deny-by-default za zaštićene resurse
- input validation na trust boundaryju
- zaštita od injectiona kroz parametrizirane upite/ORM
- tajne izvan source controla
- sigurno logiranje bez tokena, lozinki i osjetljivih payloadova
- rate limiting za osjetljive javne endpointove kada se uvedu
- dependency vulnerability provjera u CI-u
- secure headers i restriktivan CORS u produkciji

Detalji su u `SECURITY_ENGINEERING_STANDARD.md`.

## 8. Persistence baseline

- relacijski model je defaultno u 3. normalnoj formi
- foreign key i unique constrainti moraju čuvati integritet i kada aplikacijski kod pogriješi
- denormalizacija je iznimka koja zahtijeva mjerenje, komentar razloga i ADR ako mijenja canonical model
- migracije moraju biti reproducibilne i reviewane
- produkcijska baza se ne mijenja ručno kao normalan workflow

Detalji su u `DATABASE_DESIGN_STANDARD.md`.

## 9. Testing baseline

Minimalni portfolio kroz razvoj:

- unit testovi za čista poslovna pravila
- integration testovi za persistence/API granice koje nose rizik
- architecture tests za zaključana dependency pravila
- frontend component/contract testovi gdje nose vrijednost
- end-to-end testovi kritičnih journeyja prije releasea
- load/performance testovi prije tvrdnje da konkretan concurrency/SLA može biti podržan

Detalji su u `TESTING_QUALITY_STANDARD.md`.

## 10. Deployment baseline

- build proizvodi immutable Docker image
- produkcijski container ne radi kao root
- runtime image ne sadrži source, dev alate ni tajne
- DB nije javno izložena internetu
- konfiguracija dolazi iz environment/secrets sloja
- health endpointi moraju razlikovati procesnu dostupnost od readinessa gdje je relevantno
- backup/restore se mora praktično testirati prije produkcijskog releasea

Detalji su u `DOCKER_DEPLOYMENT_STANDARD.md`.

## 11. Tehnologije koje nisu dopuštene bez ADR-a

Bez eksplicitnog razloga nije dopušteno dodati:

- mikroservise
- message broker
- Kubernetes
- Redis
- Elasticsearch
- alternativni ORM
- drugu primarnu bazu
- novi frontend framework
- server-side business state samo u process memoryju

Ovo nije zabrana njihovog budućeg korištenja; to je zabrana preuranjene kompleksnosti.

## 12. Otvorene odluke koje ostaju fazno zaključane

Tehnološki baseline je zaključan, ali detaljni business contracti ostaju gated prema ROADMAP-u, uključujući:

- točan authentication/identity UX i permissions model
- file policy i dopuštene formate/veličine
- background jobs koji će stvarno biti potrebni
- AI provider/privacy contract
- konačni VPS sizing i topology prema load testovima
