# Frontend application foundation

## Status

**LOCKED foundation v1.0 — 2026-08-24**

Ovaj dokument definira app-shell, routing, design-token i component-test contract uveden u Phase 1.5. Vrijedi zajedno s `FRONTEND_ENGINEERING_STANDARD.md`, `SECURITY_ENGINEERING_STANDARD.md`, `CONFIGURATION.md` i screen/source specifikacijama.

## Granica faze

Phase 1.5 uvodi zajednički presentation okvir, ali ne implementira business ekrane. Svaki glavni modul trenutačno prikazuje neutralno foundation stanje koje jasno kaže da njegov sadržaj dolazi u odgovarajućoj ROADMAP fazi.

Namjerno nema:

- fake učenika, termina, poruka, financija, statistika ili obavijesti
- API poziva, server-state cachea ili globalnog state storea
- login/profile funkcionalnosti, sessiona, tokena ili client-side authorization pretpostavki
- feature formi, workflowa ili business pravila
- vanjskog UI/design-system frameworka

## App shell

`AppShell` je stalni okvir učiteljske aplikacije i sadrži:

- PLUS 5 brand link na Radni stol
- semantički `nav` s canonical redoslijedom iz source specifikacije
- aktivno stanje trenutne rute
- zajednički header s trenutačnim naslovom
- fokusabilni `main` outlet za sadržaj rute
- skip link za izravan prijelaz na glavni sadržaj
- eksplicitnu napomenu da profil dolazi tek nakon auth contracta

Na desktopu navigacija je sticky lijevi stupac. Ispod `48rem` prelazi u horizontalnu, touch/tipkovnica-scrollable navigaciju iznad sadržaja. Ne uvodi se hamburger state samo radi foundationa. Layout nema horizontalni overflow dokumenta na provjerenom viewportu `390 × 844`.

## Canonical route registry

URL slugovi koriste canonical engleske tehničke nazive; korisničke oznake ostaju hrvatske.

| Route | UI naziv | Trenutačno ponašanje |
|---|---|---|
| `/` | Radni stol | foundation placeholder |
| `/students` | Učenici | foundation placeholder |
| `/schedule` | Raspored | foundation placeholder |
| `/materials` | Materijali | foundation placeholder |
| `/lesson-plans` | Priprema sata | foundation placeholder |
| `/board` | PLUS 5 Ploča | foundation placeholder |
| `/homework` | Domaće zadaće | foundation placeholder |
| `/messages` | Poruke | foundation placeholder |
| `/reports` | Izvještaji | foundation placeholder |
| `/finance` | Financije | foundation placeholder |
| `/settings` | Postavke | foundation placeholder |

Nepoznata ruta prikazuje eksplicitni client-side 404 unutar istog shella i siguran link na `/`. Nginx i Vite development server koriste SPA fallback kako bi izravno otvaranje definirane rute radilo bez server-side route tablice.

Route registry u `src/app/navigation.ts` jedini je source za glavni navigacijski redoslijed, label i URL. Feature faza zamjenjuje samo odgovarajući `FoundationPage` stvarnim route sadržajem; ne duplicira shell ni globalnu navigaciju.

## Routing odluka

React Router koristi declarative mode (`BrowserRouter`, `Routes`, `Route`, `NavLink`, `Outlet`). Foundation trenutačno ne treba data-router loadere, actione, framework mode ni route-level server state.

Route URL je navigation state. Ne duplicira se u globalnom storeu. `NavLink` daje standardni `aria-current=page` za aktivno odredište.

## Design tokeni

Globalni CSS custom properties nalaze se u `src/styles/tokens.css` i grupirani su kao:

- brand/accent/surface/text/border/focus boje
- tipografija i line-height
- spacing skala
- radiusi i sjene
- focus/motion primitive
- shell dimenzije

Feature CSS koristi semantic tokene umjesto stvaranja paralelnih brand boja i spacing skala. Novi token dodaje se samo kada predstavlja ponovljivu odluku, ne jednokratnu vrijednost komponente.

Početni vizualni jezik slijedi source-spec signal: tamna stabilna navigacija, žuto aktivno stanje, svijetle površine i jasna hijerarhija. Tokeni su foundation contract, ali nisu tvrdnja da je konačni puni design system završen.

## Accessibility contract

- hrvatski dokument ostaje `lang=hr`
- semantički `aside`, imenovani `nav`, `header`, `main`, region i jedan route `h1`
- skip link postaje vidljiv na fokusu
- svaki route link ima vidljiv `:focus-visible` ring
- aktivno stanje ima tekstualni/ARIA signal i ne ovisi samo o boji
- numerički dekoratori navigacije su `aria-hidden`
- animacije se praktično uklanjaju uz `prefers-reduced-motion: reduce`
- mobilna navigacija ostaje stvarni skup linkova, bez skrivenog nedostupnog izbornika

Feature faze moraju zadržati ove landmarke i dodati specifično loading/empty/error ponašanje tek kada stvarno uvedu podatke.

## DS 001 source refresh

Novi `source_specs/DESIGN_SYSTEM_DS001.md` uvodi odobreni poslovni design source i UI kit,
ali njegove boje nisu identične postojećim zaključanim tokenima niti svim canonical screen
PNG-ovima. Phase 1.7 mora prije sljedećeg novog poslovnog UI ekrana odlučiti token mapping
i napraviti visual regression pregled. Do tada postojeći tokeni ostaju aktivni baseline;
nema parcijalnog masovnog restylea dovršenih ekrana.

## Component-test temelj

Frontend testovi koriste Vitest + jsdom + Testing Library/jest-dom. Testovi provjeravaju korisnički vidljivo ponašanje:

- svih 11 dokumentiranih navigacijskih stavki i redoslijed
- jedinstvene apsolutne route pathove
- aktivni link i neutralni route sadržaj
- skip link i imenovane landmarke
- eksplicitni 404 i povratak na Radni stol
- postojeći public-environment contract

Browser smoke review dodatno provjerava desktop/mobile layout, SPA click navigation, deep link, 404 i console errors. Component testovi ne zamjenjuju kasnije E2E testove kritičnih business journeyja.

## Pravila za sljedeće feature faze

1. Dodati stvarni sadržaj samo na route čija ROADMAP faza ima zadovoljene preduvjete.
2. Ne uklanjati neutralni placeholder drugog modula usput.
3. Ne stavljati business pravila u shell ili navigation registry.
4. Ne vezati vidljivost sigurnosno osjetljive akcije uz UI kao jedinu authorization kontrolu.
5. Loading, empty, error i retry stanja definirati uz stvarni data flow.
6. Auth/profile/notification kontrole ne aktivirati prije njihovih dokumentacijskih gateova.
