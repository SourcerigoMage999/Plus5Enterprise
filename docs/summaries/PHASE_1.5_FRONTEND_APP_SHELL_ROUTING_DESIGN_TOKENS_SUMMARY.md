# Phase 1.5 — Frontend app shell, routing & design tokens

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-24`

## Cilj faze

Uvesti održiv i pristupačan zajednički presentation temelj prije prvog feature ekrana: responsive teacher app shell, stabilan URL routing contract, centralne design tokene i component-test granicu bez izmišljanja business sadržaja ili auth modela.

## Implementirano

- responsive persistent shell s desktop sidebarom i mobilnom horizontalnom navigacijom
- svih 11 dokumentiranih glavnih teacher modula u canonical redoslijedu
- centralni typed route registry s engleskim canonical URL slugovima i hrvatskim UI labelima
- React Router 8 declarative `BrowserRouter`/nested routes/`NavLink`/`Outlet` contract
- aktivno route stanje i zajednički route naslov
- neutralni foundation placeholder za svaki modul, bez fake podataka ili akcija
- eksplicitni client-side 404 s povratkom na Radni stol
- Nginx/Vite SPA deep-link ponašanje
- skip link, imenovani landmarki, jedan route `h1`, focus-visible i reduced-motion pravila
- mobile layout bez document horizontal overflowa
- centralni CSS color/type/spacing/radius/shadow/focus/motion/shell tokeni
- Vitest + jsdom + Testing Library/jest-dom component-test foundation
- canonical `FRONTEND_FOUNDATION.md` i ADR-0008

## Namjerno nije implementirano

- stvarni Radni stol ili sadržaj bilo kojeg business modula
- učenici, termini, materijali, poruke, izvještaji, financije ili druge fake/demo vrijednosti
- API pozivi, data fetching, server-state cache, loading/empty/error state bez stvarnog data flowa
- login, profile, obavijesti, auth session/token ili client authorization pravila
- global state library, UI framework, icon library ili vanjski font
- React Router data/framework mode, loaders ili actions
- feature-level breadcrumbs, forme ili workflowi

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `README.md` | changed | repository status i canonical frontend foundation link |
| `frontend/README.md` | changed | aktualni frontend scope i quality commands |
| `frontend/package.json` | changed | routing i component-test dependency contract |
| `frontend/package-lock.json` | changed | reproducibilni npm dependency lock |
| `frontend/vite.config.ts` | changed | jsdom/Vitest konfiguracija |
| `frontend/tsconfig.test.json` | changed | strict TSX/Vitest test compilation |
| `frontend/src/App.tsx` | changed | BrowserRouter composition root |
| `frontend/src/main.tsx` | changed | environment-before-style startup redoslijed |
| `frontend/src/index.css` | changed | global reset, token import i focus baseline |
| `frontend/src/App.css` | changed | responsive shell i foundation presentation |
| `frontend/src/styles/tokens.css` | added | centralni design-token contract |
| `frontend/src/app/navigation.ts` | added | typed canonical route registry |
| `frontend/src/app/AppShell.tsx` | added | persistent navigation/header/main outlet |
| `frontend/src/app/AppRoutes.tsx` | added | declarative route tree i 404 |
| `frontend/src/app/FoundationPage.tsx` | added | neutralni module i not-found states |
| `frontend/tests/setup.ts` | added | deterministični jest-dom i cleanup setup |
| `frontend/tests/App.test.tsx` | added | shell/routing/accessibility contract testovi |
| `frontend/tests/publicEnvironment.test.ts` | changed | postojeći contract migriran na canonical Vitest runner |
| `docs/FRONTEND_FOUNDATION.md` | added | canonical app-shell/routing/token contract |
| `docs/DECISION_LOG.md` | changed | ADR-0008 |
| `docs/README.md` | changed | frontend foundation u obaveznom čitanju |
| `docs/ROADMAP.md` | changed | Phase 1.5 status nakon provjera |
| `docs/summaries/PHASE_1.5_FRONTEND_APP_SHELL_ROUTING_DESIGN_TOKENS_SUMMARY.md` | added | završni phase handoff |

## Domain / database promjene

- Novi entiteti/value objecti: nema.
- Business pravila: nema promjene.
- EF model/migracije: nema promjene; pending-model provjera prolazi.
- Backfill/data migration: nema.

## API promjene

- Nema endpoint, request/response, status-code ni serialization promjena.
- Frontend u ovoj fazi ne radi API pozive.

## Frontend promjene

- Bootstrap status zamijenjen je stvarnim, ali business-neutralnim app shellom.
- Routeovi `/`, `/students`, `/schedule`, `/materials`, `/lesson-plans`, `/board`, `/homework`, `/messages`, `/reports`, `/finance` i `/settings` dijele isti shell.
- Nepoznata ruta ima eksplicitno 404 UI stanje; static server i dalje vraća SPA entry dokument.
- Tokeni i shell postaju zajednički foundation za buduće feature stranice.
- Svaki budući modul zamjenjuje samo svoj placeholder kada njegova faza postane dopuštena.

## Security / authorization

- Nema auth tokena, localStorage sessiona, user identityja ni client authorization pretpostavki.
- Profil je samo eksplicitna neinteraktivna boundary napomena; login/profile funkcionalnost nije simulirana.
- Nema `dangerouslySetInnerHTML`, untrusted HTML-a, API inputa ni secreta u bundleu.
- Placeholderi ne prikazuju stvarne ili lažne PII/business podatke.
- 404 ne reflektira proizvoljni URL ili query u DOM.
- Lockana instalacija i aktualni npm audit prolaze bez poznatih ranjivosti.

## Ovisnosti

- production: `react-router` 8.3.0
- development/test: `vitest` 4.1.11, `jsdom` 30.0.1, `@testing-library/react` 16.3.2, `@testing-library/dom` 10.4.1 i `@testing-library/jest-dom` 7.0.1
- nema UI, icon, state-management, data-fetching ili CSS framework ovisnosti

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `npm ci` | PASS — 119 paketa, 0 ranjivosti |
| `npm run lint` | PASS |
| `npm run typecheck` | PASS — strict TypeScript |
| `npm run test` | PASS — 2 files, 9/9 testova |
| `npm run build` | PASS — Vite production build |
| `npm audit --audit-level=high` | PASS — 0 ranjivosti |
| app shell component/accessibility contract | PASS — nav redoslijed, active route, landmarki, skip link, 404 i path invarianti |
| desktop browser smoke | PASS — shell, aktivno stanje, SPA click, deep link, 404, 0 console errors |
| mobile browser smoke `390 × 844` | PASS — responsive shell, vidljivi nav/main, bez document horizontal overflowa |
| `dotnet build .\Plus5Enterprise.sln --configuration Release --no-restore` | PASS — 0 warnings, 0 errors |
| backend test u službenom .NET 10 SDK containeru | PASS — API 50/50, architecture 4/4, ukupno 54/54 |
| local architecture suite | PASS — 4/4 |
| `dotnet format ... --verify-no-changes` | PASS |
| EF pending-model provjera | PASS — nema pending model promjena |
| NuGet vulnerable audit | PASS — nema poznatih ranjivih paketa |
| frontend Docker build | PASS — clean `npm ci` + production build |
| izolirani frontend container runtime | PASS — root/deep-link/SPA fallback/health 200; deep link servira PLUS 5 app |
| frontend container user | PASS — `nginx` |
| test container cleanup | PASS; lokalni Phase 1.5 image ostavljen |

## Self-review

- [x] scope nije proširen izvan Phase 1.5
- [x] nema nedokumentiranih business pretpostavki ili fake podataka
- [x] auth/profile/notification funkcionalnost nije izmišljena
- [x] routeovi i UI nazivi slijede glossary/source spec
- [x] frontend lint/typecheck/test/build prolaze
- [x] backend regression suite prolazi u izoliranom službenom SDK runtimeu
- [x] EF model nije promijenjen
- [x] dependency auditi prolaze
- [x] desktop/mobile browser smoke i accessibility contract su provjereni
- [x] non-root Docker runtime i SPA deep-link su provjereni
- [x] dokumentacija i ROADMAP su ažurirani

## Arhitekturne odluke

- ADR-0008 — Minimalni declarative frontend router i CSS token foundation (`Accepted`).
- ADR-0001 — React + TypeScript frontend ostaje nepromijenjen.

## Poznati rizici / tehnički dug

- Vizualni tokeni su početni foundation; puni component katalog i feature-specific responsive ponašanje nastaju uz stvarne ekrane.
- Svi module routeovi su namjerno placeholderi dok njihovi preduvjeti nisu dovršeni.
- Bundle ostaje malen za trenutni scope; route splitting nije uveden bez mjerljivog feature bundle razloga.
- Windows Application Control na ovom hostu počeo je blokirati novogenerirani nepotpisani `Plus5.Api.Tests.dll`. Source nije promijenjen; isti Release suite prolazi 54/54 u službenom read-only mounted .NET 10 Linux SDK containeru. Lokalni policy treba pratiti ako se blokada nastavi u sljedećoj fazi.

## Otvorena pitanja

- Nema pitanja koje blokira Phase 1.5.
- Phase 1.6 ostaje `BLOCKED`: identity, account types, login/session transport, recovery, permissions i auth UX nisu dovoljno definirani.

## Točna početna točka za sljedeću fazu

Ne otvarati Phase 1.6 implementaciju dok vlasnik proizvoda ne odobri detaljan authentication/authorization business i security contract. Sljedeća aktivnost treba biti dokumentacijski auth gate: definirati actor-to-account veze, prijavu/session transport, logout/revocation, recovery, server policies, object scope i UX states. Tek nakon toga Phase 1.6 može prijeći iz `BLOCKED` u `READY`.
