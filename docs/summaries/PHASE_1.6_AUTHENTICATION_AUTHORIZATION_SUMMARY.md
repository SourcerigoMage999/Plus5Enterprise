# Phase 1.6 — Authentication & authorization

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-24`

## Cilj faze

Implementirati dokumentirani Teacher-only account, authentication i authorization temelj bez budućih korisničkih uloga ili feature permissionsa.

## Implementirano

- `UserAccount`, `AuthenticatedSession` i jednokratni `AccountToken` domain modeli
- javna Teacher registracija uz obaveznu potvrdu e-maila
- resend verification, login, logout, forgot/reset/change password i session status API
- framework password hashing i CSPRNG tokeni čiji se SHA-256 hash jedini sprema u bazu
- revocable osmosatne server-side sesije uz sigurni HttpOnly/SameSite cookie
- opoziv svih sesija pri resetu/promjeni lozinke ili deaktivaciji računa
- eksplicitna antiforgery validacija svakog auth write endpointa
- auth rate limit `10/min` po IP adresi
- fallback deny-by-default authorization i eksplicitni `Teacher` policy
- restriktivni credentialed CORS za točno jedan konfigurirani frontend origin
- shared EF-backed ASP.NET Core Data Protection key ring preko restarta i više API instanci
- obavezna PKCS#12 zaštita Data Protection ključeva u Staging/Production, uz fail-fast konfiguraciju
- SMTP verification/recovery poruke bez logiranja tokena
- hrvatski frontend flowovi za sva obavezna auth stanja, centralni 401/403 handling i route guard
- fail-closed UI kada provjera sesije nije dostupna
- nginx same-origin `/api/` proxy za container deployment

## Namjerno nije implementirano

- Student, Guardian ili Admin accounti
- admin-created/invitation onboarding
- JWT bearer token u browser storageu
- social login, MFA i Remember me
- permissions ili ownership pravila budućih business modula
- produkcijski SMTP provider, mail queue/outbox ili deployment certifikat

## Persistence i migracije

- `20260824193909_AddTeacherAuthenticationFoundation`
  - `UserAccounts`, `AuthenticatedSessions`, `AccountTokens`
  - unique normalizirani e-mail, session/token lookup indeksi i jedan aktivni token po account/purpose
  - check constraints, referential integrity i `Restrict` delete ponašanje
- `20260824202105_PersistSharedDataProtectionKeys`
  - framework `DataProtectionKeys` tablica za zajednički i trajni key ring
- clean apply i ponovljeni no-op apply prolaze; EF nema pending model promjena

## API contract

Svi endpointi su pod `/api/v1/auth`: `csrf`, `register`, `verify-email`, `resend-verification`, `login`, `logout`, `forgot-password`, `reset-password`, `change-password` i `session`. Točan request/response/status contract dokumentiran je u `API_CONVENTIONS.md`.

## Security / authorization

- Protected API je zatvoren po defaultu; anonymous pristup postoji samo kada je eksplicitno označen.
- Browser ne sprema session, reset ili verification token u local/session storage.
- Produkcijski cookie koristi `__Host-` prefiks, `Secure`, `HttpOnly`, `SameSite=Strict` i nema Domain atribut.
- Svaki request ponovno potvrđuje server-side session zapis pa je revocation neposredan.
- Verification/recovery odgovori ne otkrivaju postoji li account.
- Password, cookie, raw auth token, SMTP credential i certificate password ne logiraju se.
- Data Protection key ring ostaje u bazi preko restarta; izvan Developmenta key XML mora biti šifriran deployment certifikatom.

## Ovisnosti

- production: `Microsoft.Extensions.Identity.Core` 10.0.11
- production: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.11
- test: `Microsoft.EntityFrameworkCore.InMemory` 10.0.11
- nema nove frontend production ovisnosti

## Završne provjere

| Provjera | Rezultat |
|---|---|
| locked NuGet restore | PASS |
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/infrastructure testovi u službenom .NET 10 Linux SDK containeru | PASS — 82/82 |
| architecture testovi | PASS — 4/4 |
| `.NET format` | PASS |
| EF pending-model provjera | PASS |
| NuGet audit | PASS — bez poznatih ranjivosti |
| `npm ci` / npm audit | PASS — 0 ranjivosti |
| frontend lint / typecheck / build | PASS |
| frontend testovi | PASS — 3 files, 13/13 |
| desktop i mobile `390 × 844` browser review | PASS — sva auth stanja, redirecti, fail-closed stanje i 0 console grešaka |
| clean Docker build/runtime | PASS |
| health endpointi | PASS — live 200, ready 200 |
| same-origin frontend proxy | PASS — anonymous session 401, CSRF 200 |
| clean schema | PASS — 3 migracije i 4 auth/infrastrukturne tablice |
| Data Protection key persistence | PASS — 1 ključ prije i nakon API restarta |
| ponovljena migracija | PASS — database already up to date |
| non-root runtime | PASS — API UID 1654, frontend `nginx` |
| test resource cleanup | PASS — containeri, mreža i privremeni volume uklonjeni; imageovi ostavljeni |

## Browser review nalaz

Browser provjera prema dostupnom browser-control skillu materijalno je poboljšala implementaciju: otkrila je da nginx nije prosljeđivao `/api/` requestove API containeru te da mrežna pogreška provjere sesije treba eksplicitno fail-closed stanje. Oba nalaza su ispravljena i ponovno provjerena na desktop i mobilnom viewportu.

## Self-review

- [x] implementiran je samo zaključani Phase 1.6 scope
- [x] samo Teacher ima `UserAccount`
- [x] nema fake uloga, permissionsa ili business podataka
- [x] nema vlastite kriptografije ni browser auth storagea
- [x] CSRF, rate limiting, revocation i deny-by-default imaju test coverage
- [x] migracije, Docker startup redoslijed i key persistence stvarno su provjereni
- [x] konfiguracija i secret pravila dokumentirani su i fail-fast
- [x] nema poznatih dependency ranjivosti
- [x] testni containeri, mreža, volume i privremeni secret file uklonjeni su

## Poznati rizici / operativni release gateovi

- Prije Staging/Production releasea treba odabrati stvarni TLS SMTP provider, konfigurirati SPF/DKIM/DMARC i sigurno isporučiti credentials.
- Deployment mora montirati PKCS#12 Data Protection certifikat i njegov password dostaviti kroz secrets sloj.
- SMTP slanje je trenutačno sinkrono s timeoutom od 10 sekundi; outbox/retry uvodi se tek uz dokumentiranu potrebu.
- Potrebno je dovršiti produkcijski OWASP/auth threat review iz security release gatea.
- Windows Application Control na ovom hostu blokira učitavanje lokalno generiranog nepotpisanog `Plus5.Api.dll` u VSTest procesu. Isti finalni suite prolazi 86/86 u službenom .NET 10 Linux SDK containeru, a Windows Release build prolazi s 0 warninga i 0 grešaka.

## Točna početna točka za sljedeću fazu

Sljedeća dopuštena faza je **2.1 Program, grade/level and curriculum foundation**. Prije implementacije treba iz dokumentacije zaključati minimalne entitete, kardinalnosti i curriculum ownership granice koje će kasnije koristiti Student, Group, Material i Knowledge Model, bez otvaranja njihovog feature scopea.
