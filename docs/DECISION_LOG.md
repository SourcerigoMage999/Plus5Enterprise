# DECISION_LOG

### ADR-0017 — Rolling recurrence replenishment zatvara Schedule fazu
- **Datum:** 2026-09-16
- **Status:** Accepted — SA odluka nakon Phase 4.5 reviewa.
- **Kontekst:** open-ended `RecurringSessionSeries` ima `EndsOn = null`, ali početni create
  materializira samo zaključanih 12 tjedana. Bez replenishmenta canonical serija ostaje
  aktivna, dok se novi konkretni `Session` zapisi nakon isteka početnog prozora više ne bi
  pojavljivali.
- **Odluka:** prije Phase 5 uvodi se Phase 4.6 — Recurrence materialization replenishment.
  Periodički proces održava približno sljedećih 12 tjedana za aktivne open-ended serije i
  idempotentno stvara samo nedostajuće occurrencee u ograničenim batchovima.
- **Invarijante:** postojeći exception, `Cancelled`, `Held` i `InProgress` occurrence datumi
  ne regeneriraju se; poštuju se supersession lineage, DST i conflict pravila. Izvršavanje
  mora biti sigurno u više instanci, observable i dokazano stvarnim SQL concurrency/
  idempotency testovima.
- **Granice:** nema novog UI-ja. Točna cadence/trigger i operativno ponašanje pojedinačnog
  conflict occurrencea moraju se eksplicitno zaključati u Phase 4.6 contractu prije
  implementacije; Teacher conflict override ostaje zasebna odgođena odluka.
- **Posljedice:** Phase 4 nije završena prihvaćanjem 4.5. Phase 5.1 ne počinje prije završnog
  acceptancea Phase 4.6.

### ADR-0016 — Promjena Programa grupe samo bez aktivnih članstava
- **Datum:** 2026-09-11
- **Status:** Accepted — SA odluka za Phase 3.7.
- **Kontekst:** promjena `Group.ProgramId` dok grupa ima aktivne članove mogla bi implicitno
  promijeniti Student aggregate ili neočekivano završiti povijesna članstva.
- **Odluka:** `Group.ProgramId` smije se promijeniti samo kada grupa nema aktivnih
  `GroupMembership` redaka. Pokušaj promjene uz `activeMembershipCount > 0` odbija se
  kontroliranim business rezultatom. Frontend unaprijed zaključava Program i objašnjava
  razlog, ali backend uvijek ponovno provjerava pravilo u transakciji.
- **Posljedice:** nema masovnog ažuriranja Student Programa i nema automatskog završetka
  članstava. Teacher mora učenike eksplicitno ukloniti ili premjestiti, promijeniti Program
  prazne grupe te ih po potrebi ponovno dodati postojećim membership workflowom.
- **Alternative:** implicitna promjena Programa svih učenika i automatsko zatvaranje
  članstava eksplicitno su odbijeni.

### ADR-0015 — Group create, opcionalna otvorena serija i početnih 12 tjedana
- **Datum:** 2026-09-11
- **Status:** Accepted — SA odluke za nastavak 3.6.
- **Odluka:** grupa se sprema kao Active s 0..N učenika i 0..N rasporeda. Broj članova ne mijenja status; automatska deaktivacija je rejected behavior. Nema MinimumStudents.
- **Raspored:** canonical serija može imati `EndsOn = null`. Početna materializacija obuhvaća 12 tjedana (84 lokalna dana), od kasnijeg od današnjeg datuma u Europe/Zagreb i StartsOn, s exclusive gornjom granicom; raniji uneseni EndsOn uključiv je i skraćuje prozor. Ne generiraju se već započeti termini. Nema business limita trajanja serije.
- **Integritet:** grupa, početna članstva, serije i početni Sessioni jedna su Serializable transakcija. Ownership, capacity, Student rowversion, aktivno članstvo i vremenski konflikti validiraju se prije commita. Konflikt nema override.
- **Operativne granice:** početni generator je bounded; Phase 4.6 replenishment mora ponovno provjeriti konflikte/DST pri svakom širenju prozora. Spremanje nije jamstvo nekonfliktnosti svih budućih beskonačnih pojava. Nema background servisa u 3.6.
- **Posljedice:** nullable EndsOn migracija bez izmjene postojećih datuma; postojeći read model mora uključiti otvorene serije. Zaštita veličine pojedinačnog create zahtjeva: najviše 100 početnih članova i 14 tjednih slotova; to nije ograničenje Capacity grupe.
- **Navigacija:** postojeći React Router data-router adapter omogućuje službeni useBlocker za potpuni SPA Back/Forward gate; nema nove biblioteke ili server-state migracije.

Ovdje se zapisuju arhitekturne i trajne implementacijske odluke.

## Format

### ADR-0001 — <Naziv odluke>
- **Datum:** YYYY-MM-DD
- **Status:** Proposed / Accepted / Superseded
- **Kontekst:** ...
- **Odluka:** ...
- **Razlozi:** ...
- **Posljedice:** ...
- **Alternative:** ...

---

Tehnološki baseline za Phase 0.3 zaključan je kroz Accepted ADR-0001–ADR-0005.

## Accepted decisions

### ADR-0001 — React + TypeScript frontend
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** PLUS 5 zahtijeva bogat interaktivni web UI i fazni razvoj odvojen od server business logike.
- **Odluka:** Frontend koristi React + TypeScript + Vite.
- **Razlozi:** snažan ecosystem, tipizirani contracti, modularan UI, prikladno za kompleksne interaktivne ekrane.
- **Posljedice:** frontend je API klijent; server ostaje autoritet za business/security pravila.
- **Alternative:** Blazor i drugi frontend frameworkovi nisu odabrani.

### ADR-0002 — ASP.NET Core / .NET backend
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** Potreban je siguran, testabilan backend s kompleksnom poslovnom logikom i SQL persistenceom.
- **Odluka:** Backend koristi C# + ASP.NET Core na .NET 10 baselineu.
- **Razlozi:** stabilan web stack, dobar performance, security tooling, EF Core i kvalitetna testabilnost.
- **Posljedice:** nove backend komponente moraju slijediti `BACKEND_ENGINEERING_STANDARD.md`.
- **Alternative:** nisu odabrane u trenutnom baselineu.

### ADR-0003 — SQL Server + EF Core migrations
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** PLUS 5 ima izrazito relacijske domene: korisnici, učenici, grupe, raspored, kurikulum, evidence i materijali.
- **Odluka:** Primarna OLTP baza je Microsoft SQL Server; schema evolution kroz verzionirane EF Core migracije.
- **Razlozi:** transakcijska konzistentnost, constrainti, relacijski upiti, zreo .NET integration.
- **Posljedice:** 3NF je default; ručne production schema izmjene nisu normalan workflow.
- **Alternative:** NoSQL nije odabran kao primarna baza.

### ADR-0004 — Modularni monolit prije mikroservisa
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** Cilj je siguran fazni razvoj i 10.000+ korisnika bez nepotrebne operativne složenosti.
- **Odluka:** PLUS 5 započinje kao modularni monolit s jasnim domenskim granicama i mogućnošću horizontalnog skaliranja API-ja.
- **Razlozi:** niži razvojni i operativni trošak; 10.000+ korisnika ne zahtijeva mikroservise po defaultu.
- **Posljedice:** mikroservisi/message broker/Kubernetes zahtijevaju novi ADR i izmjeren razlog.
- **Alternative:** microservices-first je odbijen.

### ADR-0005 — Docker kao deployment artifact; VPS kasnije
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** Razvoj koristi Docker, a produkcija će kasnije biti deployana na VPS.
- **Odluka:** Aplikacija se pakira u Docker image; Docker Compose je dopušten za lokalni razvoj i inicijalni single-VPS topology. Produkcijski VPS detalji zaključavaju se u release/deployment fazi.
- **Razlozi:** reproducibilan environment, jednostavniji deployment i kasniji scaling path.
- **Posljedice:** image mora biti non-root, bez secreta; DB mora ostati private; backup/restore i TLS su production gateovi.
- **Alternative:** manual host deployment bez containera nije standardni put.

### ADR-0006 — Versionirani Minimal API i ProblemDetails contract
- **Datum:** 2026-08-24
- **Status:** Accepted
- **Kontekst:** Budući PLUS 5 moduli trebaju zajednički HTTP trust boundary prije prvog business endpointa, bez vezanja na third-party validation/error framework.
- **Odluka:** Javni business API koristi ASP.NET Core Minimal API route group `/api/v1`, built-in .NET 10 validation i RFC `ProblemDetails` proširen stabilnim `code` i `traceId` poljima. Potencijalno velike liste počinju bounded page/pageSize contractom (default 25, maksimum 100).
- **Razlozi:** službeni framework primitives smanjuju dependency i maintenance rizik; stabilni machine code odvaja klijenta od teksta; URL versioning štiti buduću kompatibilnost; bounded pagination sprječava neograničene list queryje.
- **Posljedice:** budući endpointi moraju koristiti canonical route group, kontrolirani validation/error mapping i `PagedResponse<T>` gdje je lista potencijalno velika. Neočekivani 500 ne izlaže interne detalje. Cursor pagination ili druga breaking promjena zahtijeva eksplicitnu novu odluku.
- **Alternative:** custom error envelope i third-party validation/versioning paketi nisu uvedeni jer built-in .NET 10 mogućnosti pokrivaju trenutni scope.

### ADR-0007 — JSON stdout i vendor-neutral OpenTelemetry temelj
- **Datum:** 2026-08-24
- **Status:** Accepted
- **Kontekst:** Containerizirani API treba zajedničku korelaciju logova, HTTP odgovora, traceova i osnovnih metrika prije uvođenja business endpointa, ali produkcijski observability vendor i topology još nisu odabrani.
- **Odluka:** API zapisuje strukturirane JSON logove na stdout, koristi W3C trace ID kao javni correlation contract te OpenTelemetry ASP.NET Core/runtime instrumentaciju. OTLP export je opcionalan i isključen bez validiranog endpointa; collector ili vendor servis nije dio trenutnog Composea.
- **Razlozi:** stdout odgovara container runtimeu; W3C i OTLP su interoperabilni standardi; route-template logging i sanitizacija smanjuju privacy, secret i high-cardinality rizik; odgođeni backend izbor izbjegava preuranjeni operativni lock-in.
- **Posljedice:** novi backend request log/enrichment mora koristiti canonical trace ID, low-cardinality strukturirana polja i pravila iz `OBSERVABILITY.md`. Production endpoint mora koristiti HTTPS, a exporter credentials, retention, dashboardi i alerting zahtijevaju deployment/security odluku.
- **Alternative:** vendor-specific logging SDK, committed collector stack, raw request logging i puni URL/query tagovi nisu prihvaćeni u foundation fazi.

### ADR-0008 — Minimalni declarative frontend router i CSS token foundation
- **Datum:** 2026-08-24
- **Status:** Accepted
- **Kontekst:** Prije prvog feature ekrana učiteljska aplikacija treba stabilan responsive shell, URL navigation state, aktivne linkove, centralne design tokene i testabilan component boundary, bez uvođenja business sadržaja ili preuranjenog frontend framework modea.
- **Odluka:** React SPA koristi React Router 8 u declarative modu s centralnim route registryjem, nested `AppShell` outletom i client-side 404 stanjem. Vizualni foundation koristi CSS custom-property tokene i lokalni CSS bez UI frameworka. Component testovi koriste Vitest, jsdom i Testing Library.
- **Razlozi:** standardni browser history/link/active-state primitive smanjuje custom routing rizik; declarative mode pokriva trenutačni scope uz najmanju kompleksnost; CSS tokeni čuvaju vizualnu konzistentnost bez vendor lock-ina; Testing Library provjerava korisnički vidljiv i accessibility contract.
- **Posljedice:** glavni routeovi i labeli mijenjaju se centralno; feature stranice ostaju unutar zajedničkog shella; URL state se ne duplicira u storeu; data-router/framework mode, global state i UI library zahtijevaju stvarnu buduću potrebu i review.
- **Alternative:** vlastiti History API router, route-by-conditional-JSX, full React Router framework/data mode, global store i gotovi UI framework nisu uvedeni jer povećavaju rizik ili scope bez trenutačnog benefita.

### ADR-0009 — Teacher-only account scope i revocable secure cookie authentication
- **Datum:** 2026-08-24
- **Status:** Accepted
- **Kontekst:** ROADMAP 1.6 bio je blokiran jer dokumentacija nije definirala tko ima korisnički račun, kako Teacher dobiva account niti koji browser auth/session model koristi aplikacija.
- **Odluka:** U Phase 1.6 samo `Teacher` ima `UserAccount`. Teacher se javno samostalno registrira e-mailom i lozinkom te mora potvrditi e-mail prije punog pristupa. Student i Guardian nemaju login/account u ovoj fazi, a Administrator role se ne uvodi. Browser autentikacija koristi sigurni `HttpOnly`/`Secure` server-controlled cookie s revocable server-side session boundaryjem; bearer/JWT tokeni se ne spremaju u browser storage. API je deny-by-default i Teacher-owned resursi zahtijevaju server-side object ownership authorization.
- **Razlozi:** dokumentirani proizvod trenutno je Teacher application; uvođenje Administrator rolea ili Student/Guardian accounta stvorilo bi nedokumentirani scope. Revocable cookie session podržava siguran first-party web flow, password/session invalidation i horizontalno skaliranje bez izlaganja bearer credentials browser storageu.
- **Posljedice:** Phase 1.6 je READY; implementacija mora slijediti `AUTHENTICATION_REQUIREMENTS.md` i `AUTHENTICATION_ARCHITECTURE.md`. Password change/reset i deaktivacija opozivaju sve sesije; login/recovery imaju abuse protection; CSRF je obavezan za state-changing cookie-authenticated requestove. Budući Student/Guardian account, external login, MFA ili Administrator role zahtijevaju zasebnu business odluku i po potrebi novi ADR.
- **Alternative:** administrator-created Teacher accounti, invitation-only onboarding, Student/Guardian accounti u početnom scopeu i SPA JWT bearer tokeni u browser storageu nisu odabrani.

### ADR-0010 — Odvojeni Teacher Program i globalni grade/level/curriculum referentni korijeni
- **Datum:** 2026-08-25
- **Status:** Accepted
- **Kontekst:** Student, Group, Material i budući Knowledge Model trebaju stabilne pojmove Program, SchoolGrade, ProficiencyLevel i Curriculum, ali source specifikacije ih izričito koriste kao odvojene dimenzije i još ne definiraju njihove feature veze ni katalog sadržaj.
- **Odluka:** `Program` je Teacher-owned pedagoška ponuda s jedinstvenim nazivom unutar Teacher scopea. `SchoolGrade`, `ProficiencyLevel` i verzionirani `Curriculum` su zajednički referentni korijeni. Nijedan Program ne sadrži grade, level ili curriculum FK. Phase 2.1 ne seeda kataloge i ne uvodi curriculum ishode/hijerarhiju.
- **Razlozi:** model čuva zaključane glossary razlike, sprječava cross-Teacher IDOR granicu, ostaje u 3NF i ne zaključava nedokumentirane Student/Group/Material kardinalnosti ni hrvatski/CEFR katalog prije odobrenja sadržaja.
- **Posljedice:** buduće veze moraju referencirati zasebne entitete i poštovati Teacher ownership. Program lifecycle/management, reference-data provisioning i curriculum hijerarhija ostaju svojim dokumentacijskim/ROADMAP gateovima.
- **Alternative:** globalni shared Program, jedno polje `GradeLevel`, grade/level/curriculum stupci na Programu, hardkodirani enum/seed katalozi i preuranjeni CurriculumOutcome model nisu prihvaćeni.

### ADR-0011 — Teacher-owned Student profil, child Guardian kontakti i arhiviranje
- **Datum:** 2026-08-25
- **Status:** Accepted
- **Kontekst:** Ekrani 2.1, 2.2, 2.3 i 2.6 trebaju zajednički Student zapis, ali Group, communication, file storage i Knowledge Model još nisu u scopeu. Source zahtijeva obavezni razred, opcionalnu organizaciju nastave, tri statusa, više Guardian kontakata i sigurno ponašanje umjesto neposrednog fizičkog brisanja.
- **Odluka:** `Student` je Teacher-owned aggregate root bez accounta, s obaveznim `SchoolGrade`, opcionalnim paired `Program` + `DeliveryMode`, statusom `Active`/`OnHold`/`Inactive` i arhivskim UTC vremenom. Program veza koristi composite same-Teacher FK. `Guardian` je Student-owned child kontakt; Student ima nula ili više kontakata i najviše jednog primarnog. Product delete je arhiviranje, a fizičko brisanje/erasure nije uvedeno. Group-mode write ostaje zatvoren do atomarne `GroupMembership` invarijante u Phase 2.3.
- **Razlozi:** model čuva glossary razlike, minimizira PII, sprječava IDOR/cross-owner vezu na razini baze, ostaje u 3NF i ne izmišlja buduće account, group, messaging, knowledge ili file-storage contracte.
- **Posljedice:** budući Student API mora koristiti session Teacher ownership i ne smije prihvaćati client ownership autoritet. Uobičajeni list queryji isključuju arhivirane retke. Phase 2.3 mora atomarno jamčiti da `DeliveryMode.Group` ima valjano aktivno članstvo, a production delete zahtijeva retention/legal-erasure odluku.
- **Alternative:** Student kao `UserAccount`, hard delete iz feature flowa, Guardian account, globalno dijeljeni Guardian bez dokumentiranog identity mergea, implicitni DeliveryMode iz Group FK-a, ručno upisani progress/readiness i preuranjeni Group/Knowledge modeli nisu prihvaćeni.

### ADR-0012 — Vremenski GroupMembership, one-active-group i optimistic capacity concurrency
- **Datum:** 2026-08-25
- **Status:** Accepted
- **Kontekst:** Ekrani 2.7–2.9 trebaju Teacher-owned grupe, promjenjivi kapacitet, dodavanje/uklanjanje/transfer učenika i očuvanje povijesti, ali raspored i feature API/UI pripadaju kasnijim fazama. Baza mora spriječiti cross-Teacher članstvo i paralelno prekoračenje kapaciteta bez triggera ili denormaliziranog broja članova.
- **Odluka:** `Group` pripada Teacheru, njegovom Programu i SchoolGradeu, ima pozitivan capacity, tri statusa, arhiviranje i SQL `rowversion`. `GroupMembership` čuva UTC interval i dopušta najviše jedno aktivno članstvo po Studentu. Mirrored TeacherAccountId u junctionu koristi se samo za composite ownership FK-ove. Budući membership use case u jednoj transakciji ažurira Student Program/DeliveryMode, membership i Group redak; Group rowversion čuva capacity od concurrent lost updatea. Izlazak bez transfera zadržava Program i mijenja DeliveryMode u Individual.
- **Razlozi:** model čuva Program–DeliveryMode–Group razliku, povijest bez hard deletea, tenant sigurnost na DB razini i normalizirani canonical member count bez stored countera ili triggera. Rowversion odgovara stvarnom konkurentnom capacity write use caseu.
- **Posljedice:** feature write ne smije postojati bez transakcije, active-count provjere i concurrency conflict mappinga. Student može imati više povijesnih, ali samo jedno aktivno članstvo. Raspored i location nisu Group stupci. Promjena Group Programa s aktivnim članovima ostaje zaseban product gate.
- **Alternative:** GroupId izravno na Studentu bez povijesti, više aktivnih grupa po Studentu, cascade/hard delete, stored active-member counter, DB trigger, implicitni DeliveryMode, Group-owned kopije Student podataka i preuranjeni schedule model nisu prihvaćeni.

### ADR-0014 — DS-001 semantic defaults i canonical screen iznimke
- **Datum:** 2026-09-11
- **Status:** Accepted — vlasnik projekta izričito prihvatio pristup i obavezni regression gate faze 1.7.
- **Odluka:** DS-001 je osnova za semantičke tokene i nove komponente. Canonical screen PNG određuje layout i dopušta eksplicitno dokumentirane iznimke (uključujući plave glavne akcije). Prihvaćeni shell ostaje 204 px s postojećom mobilnom navigacijom. Postojeći ekrani prelaze na zajedničke tokene postupno, uz ponovni visual acceptance.
- **Posljedice:** postojeći `--color-*` i lokalni screen stilovi čine popis iznimaka u `DESIGN_SYSTEM_ALIGNMENT.md`; novi `--ds-*` defaulti ne prepisuju ih globalno. Shared StatusBadge prima presentation tone i domenski label; administrativni status ne postaje readiness. Danger, upozorenje, disabled stanje i autorizacija ostaju zasebne semantike.
- **Acceptance:** obavezni frontend test/lint/build, stvarni login i ekrani 3.1–3.5 na desktopu i mobitelu, canonical usporedba, novi dokazi i popis odstupanja. Odluka rješava pitanje 21, ali sama ne označava 1.7 DONE.
- **Alternativa:** globalni DS restyle svih dovršenih ekrana nije odabran.

### ADR-0013 — Versioned weekly series, materialized Session instances i explicit exceptions
- **Datum:** 2026-08-28
- **Status:** Accepted
- **Kontekst:** Group i Schedule specifikacije zahtijevaju jedan source of truth za redoviti raspored, konkretne kalendarske termine, jednokratne iznimke i promjenu buduće serije. Potrebni su DST-safe UTC termini, očuvana povijest i zaštita od overlap/duplicate raceova.
- **Odluka:** `RecurringSessionSeries` je verzionirana tjedna definicija. Group-kind serija predstavlja canonical `RegularGroupSchedule`; individualna recurrence koristi isti mehanizam. Konkretni `Session` retci se materijaliziraju s jedinstvenim series/occurrence ključem. Jedna iznimka mijenja samo Session; future change supersedea staru i stvara nasljednu seriju. Intervalni konflikti provjeravaju se u Serializable transakciji, dok rowversion štiti update i unique indeks generiranje.
- **Razlozi:** model nema dvije kopije grupnog rasporeda, čuva povijest i auditabilnu lineage, omogućuje calendar query bez računanja beskonačne recurrence te ispravno razdvaja lokalno recurrence pravilo od UTC instanci.
- **Posljedice:** feature use case mora imati bounded generation horizon, DST validation, context/ownership provjeru, atomic future-series zamjenu i kontrolirani concurrency/conflict rezultat. Migracija ne generira business podatke.
- **Alternative:** RRULE string/JSON kao core model, mutiranje stare serije, samo virtualni termini, beskonačna pre-generacija, brisanje otkazanih termina, lokalni datetime bez timezonea i client-side conflict autoritet nisu prihvaćeni.
