# Phase 3.5 — Screen 2.7 Groups

## Status

**DONE — implementation, SQL runtime and visual acceptance gates PASS. Commit/push pending approval.**

## Datum

2026-09-02

## Cilj faze

Implementirati organizacijski pregled grupa iz source-speca 2.7, bez otvaranja
create/edit group faza, Knowledge/Evidence, privacy, storage ili drugih otvorenih gateova.

## Implementirano

- Teacher-owned, nearhivirani popis grupa s pretragom, program/status filtrima i bounded pagination.
- Organizacijska statistika, detalj grupe, kapacitet, važeća recurrence pravila i stvarni nadolazeći termini.
- Paginirani članovi i kandidati; prioritet podudaranja programa/razreda bez zabrane drugih razreda.
- CSRF/rowversion-zaštićeno dodavanje i uklanjanje članstva, eksplicitna potvrda i čuvanje povijesti.
- Transfer kroz postojeći Phase 3.4 Student edit; dossier poveznice bez paralelnog dosjea.
- `/students/groups`, postojeći shell/Students aktivno, master-detail layout, četiri kartice,
  tabovi i responsive CSS prema canonical PNG-u; stvarni desktop/mobile prikaz pregledan je i uspoređen.
- Loading/empty/error/retry, puni kapacitet, neutralni Knowledge/attendance/materials/notes prikazi.
- Opt-in stvarni SQL Server test koji stvara i uklanja isključivo vlastitu izoliranu testnu bazu.

## Namjerno nije implementirano

- Nova/Uredi grupa (3.6/3.7), generiranje recurrence instanci, kalendar/detail termina.
- PDF izvoz, materijali, bilješke, procjene znanja, attendance, poruke, photos/storage i permissions.
- Novi entiteti, produkcijski seed, promjena autentifikacije ili zaobilaženje prijave radi screenshotova.

## Promijenjene / dodane datoteke

| Datoteka | Vrsta | Razlog |
|---|---|---|
| `backend/src/Plus5.Application/Groups/GroupContracts.cs` | added | Query i membership application contract |
| `backend/src/Plus5.Infrastructure/Groups/EfGroupQuery.cs` | added | Owner-scoped EF projekcije i statistike |
| `backend/src/Plus5.Infrastructure/Groups/EfGroupMembershipService.cs` | added | Transakcija, kapacitet i concurrency |
| `backend/src/Plus5.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` | changed | Registracija servisa |
| `backend/src/Plus5.Api/Groups/GroupEndpoints.cs` | added | DTO, validacija, Teacher policy i CSRF |
| `backend/src/Plus5.Api/Program.cs` | changed | Mapiranje endpointa |
| `backend/tests/Plus5.Api.Tests/Groups/GroupScreenTests.cs` | added | Query i membership testovi |
| `backend/tests/Plus5.Api.Tests/Groups/GroupSqlRuntimeTests.cs` | added | SQL translacija, rowversion i last-seat konkurencija |
| `backend/tests/Plus5.Api.Tests/Identity/AuthenticationApiTests.cs` | changed | Group auth/validation/CSRF API testovi |
| `frontend/src/groups/groupsApi.ts` | added | Tipovi, API i abort/stale-safe read hook |
| `frontend/src/groups/GroupListPage.tsx` | added | Ekran grupa |
| `frontend/src/groups/GroupListPage.css` | added | Canonical layout i responsive stilovi |
| `frontend/src/app/AppRoutes.tsx` | changed | Static groups ruta |
| `frontend/src/students/StudentListPage.tsx` | changed | Ulazna poveznica Grupe |
| `frontend/src/students/StudentListPage.css` | changed | Jednaki žuti gumbi Grupe/Dodaj učenika, jedan iznad drugog |
| `frontend/src/api/apiClient.ts` | changed | Membership conflict poruka |
| `frontend/tests/Groups.test.tsx` | added | 5 UI testova |
| `frontend/tests/StudentCreate.test.tsx` | changed | Determinističko čekanje učitanih opcija; bez promjene produkcijskog create flowa |
| `frontend/tests/StudentList.test.tsx` | changed | Odredišta, stil i redoslijed dvaju gumba |
| `docs/GROUP_LIST.md` | added | Contract, metrika, scope i vizualna odstupanja |
| `docs/ROADMAP.md` | changed | DONE nakon stvarnog visual gatea |
| `docs/summaries/PHASE_3.5_GROUPS_SUMMARY.md` | added | Ovaj summary |
| `docs/visual-acceptance/README.md` | changed | Phase 3.5 usporedba, mjerenja i odstupanja |
| `docs/visual-acceptance/phase-3.5-groups-canonical.png` | added | Neizmijenjena canonical referenca |
| `docs/visual-acceptance/phase-3.5-groups-desktop-1536x1024.png` | added | Stvarni popunjeni desktop |
| `docs/visual-acceptance/phase-3.5-groups-mobile-390x844.png` | added | Stvarna mobilna prilagodba |
| `docs/visual-acceptance/phase-3.5-groups-mobile-actions-confirmation.png` | added | Mobilne akcije i potvrda bez spremanja |
| `docs/visual-acceptance/phase-3.5-groups-schedule-desktop.png` | added | Stvarni prazan raspored |
| `docs/visual-acceptance/phase-3.5-groups-empty-desktop.png` | added | Prazan filtrirani popis |
| `docs/visual-acceptance/phase-3.5-groups-confirmation-desktop.png` | added | Desktop potvrda bez spremanja |

## Domain / database promjene

- Nema novih entiteta, schema promjena, migracije ni backfilla.
- Koriste se postojeći Group/Student rowversion, aktivni unique membership indeks i temporalno članstvo.
- Runtime test primjenjuje postojeće migracije u zasebnu `Plus5_Phase35Test_<guid>` bazu.
- Testne baze uklonjene su nakon provjere; postojeća Plus5 baza i njeni podaci nisu mijenjani testom.

## API promjene

Non-breaking novi `GET /api/v1/groups`, `/overview`, `/{id}`, `/{id}/students`,
`/{id}/candidates`, `/{id}/sessions`; CSRF `POST /{id}/members/{studentId}`.
Detaljan contract, odgovori i ograničenja u `GROUP_LIST.md`.

## Security / authorization

Owner isključivo iz claims, Teacher policy, privacy-preserving 404, nema izloženog meeting URL-a.
Write zahtijeva CSRF i oba rowversiona. SQL transakcija čuva organization/membership/group invariant.
Last-seat konkurencija i unique-index konflikt ne dopuštaju drugi uspješan upis.
Nikakve lozinke nisu dodane u repo, dokumentaciju ili test output.

## Testovi i provjere

| Naredba / suite | Rezultat |
|---|---|
| `dotnet test Plus5Enterprise.sln -c Release --no-restore` | PASS — 132 API/domain/persistence + 4 architecture; opt-in SQL test eksplicitno SKIP bez env |
| `dotnet test backend/tests/Plus5.Api.Tests -c Release --no-restore --filter FullyQualifiedName~GroupSqlRuntimeTests` uz lokalni env | PASS — 1/1, sa stvarnim SQL Serverom, recurrence pravilom i Sessionom |
| Release build | PASS — 0 warnings/errors |
| `dotnet format Plus5Enterprise.sln --no-restore --verify-no-changes` | PASS |
| `npm run build` / typecheck | PASS |
| `npm run lint` | PASS |
| `npm test` | PASS — 28/28 |
| NuGet audit, include transitive | Bez poznatih ranjivosti |
| `npm audit --audit-level=low` | 0 ranjivosti |
| Docker Compose build i start | PASS |
| Non-root runtime | PASS — API UID 1654; frontend UID 101 / nginx |
| API `/health/live`, `/health/ready`, frontend HTTP | PASS — 200/200/200 |
| Canonical PNG visual comparison | PASS — stvarna aplikacija, dokumentirana odstupanja i kopija reference |
| Desktop screenshot comparison | PASS — 1536×1024 viewport, full-page screenshot |
| Mobile adaptation review | PASS — 390×844 viewport, full-page i scrolled screenshot |
| Dodatni responsive overflow check | PASS — 768, 1024, 1280, 1402 px; scrollWidth = clientWidth |
| Browser tab keyboard / confirmation cancel / empty filter | PASS — bez spremanja promjena članstva, bez pageerror događaja |

Frontend create test je tijekom paralelnog izvođenja otkrio raniju utrku: naslov postoji
prije učitavanja opcija. Sada čeka pripadnu option stavku umjesto oslanjanja na timing.
SQL test preko `localhost` prvotno nije prošao zbog IPv6 konekcije; `127.0.0.1,1433`
odgovara Compose IPv4 bindingu i završna provjera prolazi.
Docker Desktop pokrenut je standardnom naredbom; nije bilo brisanja runtime socketova,
promjena AI/secrets postavki, restarta drugih projekata ili uklanjanja volumeova.

## Visual acceptance — zatvoren 2026-09-02

Canonical: `C:/Users/arodr/Downloads/Plus5DokumentNEW/Za programera - novo/2.0 Učenici/2.7 Grupe/2.7 Grupe.png`.
Četiri kartice, odnos panela 44:56, shell, selected row, status badgeovi, tabovi, member
table i bilješke uspoređeni su sa stvarnim ekranom u Edge 152.0.4191.53.
Korišten je odobreni demo Teacher račun: jedna Demo grupa Orion, dva postojeća člana,
popunjeni kapacitet 2/2 i bez spremljenog rasporeda. Nisu mijenjani poslovni podaci,
niti su API odgovori ili DOM sadržaj zamijenjeni radi screenshotova.

Ispravljeno: akcije su ispod profilne zone s 11 px razmaka; status je uz naziv grupe;
redak je kompaktniji (oko 82 px na desktopu); boje statistika slijede referentne obitelji;
identitet grupe je naglašen, članovi imaju inicijale, tablica čitljiviju tipografiju i
fokusabilni lokalni horizontalni scroll na mobitelu. Regression assertions dodane su
u postojeći UI test. Završni frontend lint/build/typecheck/test i Docker rebuild prolaze.

Dokazi i puna checklist usporedbe: `../visual-acceptance/README.md` (Phase 3.5).
Ovo je kvalitativni high-fidelity pregled uz eksplicitne fazne i naslijeđene shell
razlike, ne tvrdnja pixel-perfect identičnosti ili test na fizičkom telefonu.

## Self-review

- [x] scope nije proširen izvan 3.5
- [x] business semantika i odstupanja dokumentirani
- [x] build i relevantni automatski testovi prolaze
- [x] SQL translacija i concurrency provjereni
- [x] auth/validation/CSRF provjereni
- [x] dokumentacija usklađena sa stvarnim dokazima
- [x] stvarni desktop/mobile visual acceptance

## Arhitekturne odluke

Nema novog ADR-a. Koriste se zaključani Group i Scheduling foundation contracti.
Transfer ponovno koristi postojeći workflow. Organizacijska statistika ne preuzima ulogu Knowledge Modela.

## Poznati rizici / otvorena pitanja

- Snimke prikazuju jednu stvarnu demo grupu s dva člana, ne osam grupa/šest članova iz PNG-a.
- Puni raspored nije vizualno dokazan ovim demo podacima; query/SQL testovi zasebno pokrivaju pravila i Sessione.
- Read model redovitog rasporeda ograničen je na 14 pravila; granica je vidljiva u UI-u.
- Knowledge/Evidence, storage, permissions, privacy/bilješke, finance/reports i MVP rez ostaju otvoreni.
- Prijava i spremanje demo screenshotova eksplicitno su odobreni; lozinka i sesija nisu spremljene u repo.

## Točna početna točka za nastavak

Nakon korisničkog reviewa i zasebnog odobrenja napraviti commit/push Phase 3.5.
Sljedeća implementacijska faza je 3.6 Screen 2.8 Create group; nije započeta ovim zadatkom.
Commit/push nisu napravljeni i čekaju zasebno odobrenje.

Aplikacijski kontejneri ostavljeni su pokrenuti za korisnički review. SQL volumeovi i
postojeći podaci sačuvani su; obnovljen je samo frontend kontejner.

## Naknadna korisnička UI korekcija — 2026-09-02

Na popisu učenika poveznica Grupe sada koristi isti žuti stil i širinu kao Dodaj učenika,
iznad njega s razmakom. Zadržana je link semantika i postojeće odredište.
Izmijenjen je i `frontend/src/students/StudentListPage.css`; test popisa provjerava oba
odredišta, zajednički action stil i redoslijed. Frontend build/typecheck/lint, 3/3 testa
popisa i rebuild samo frontend kontejnera prolaze. Naknadno je zatvoren i ukupni Phase 3.5
visual gate prema dokazima navedenima iznad.
