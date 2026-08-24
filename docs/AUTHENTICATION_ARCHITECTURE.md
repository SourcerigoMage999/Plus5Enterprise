# AUTHENTICATION_ARCHITECTURE

## Status

**APPROVED v1.0 — 2026-08-24**

Ovaj dokument zaključava tehnički authentication/authorization baseline za ROADMAP Phase 1.6. Business pravila iz `AUTHENTICATION_REQUIREMENTS.md` imaju prednost ako se pojavi kontradikcija.

---

## 1. Architecture decision

PLUS 5 koristi ASP.NET Core/.NET 10 standardne security primitive i **server-controlled cookie authentication** za browser Teacher aplikaciju.

Zaključano:

- credentials: e-mail + password
- secure browser auth cookie
- nema JWT/bearer tokena u `localStorage` ili `sessionStorage`
- server-side revocable session record
- server-side authorization i ownership checks
- framework/provjerene kriptografske primitive; bez custom cryptography

ASP.NET Core Identity primitives mogu se koristiti za password hashing, token/security-stamp primitive i account management, uz domenski/persistence model koji ostaje usklađen s modularnim monolitom i postojećim engineering standardima.

## 2. User identity model

Phase 1.6 ima jedan interaktivni account type:

- `Teacher`

Minimalni identity podaci:

- immutable internal user/account identifier
- normalized unique e-mail
- password hash
- email-confirmed state
- account status: `PendingEmailVerification`, `Active`, `Deactivated`
- security/session invalidation marker prema odabranom framework modelu
- timestamps/audit metadata prema persistence standardu

Ne uvoditi tablice/roleove za Student, Guardian ili Administrator account samo radi budućeg scopea.

## 3. Session model

Autentificirana browser sesija mora imati **server-side revocation boundary**.

Minimalno:

- svaka login sesija dobiva server-side session identity/record ili ekvivalentan revocable framework-backed record
- auth cookie identificira i dokazuje autentificiranu sesiju bez izlaganja passworda ili recovery secreta
- API request mora moći utvrditi je li sesija još aktivna
- logout opoziva aktualnu sesiju
- password change/reset i account deactivation opozivaju sve sesije accounta
- session state koji je potreban za ispravnost ne smije postojati samo u memoriji jedne API instance

Time se zadržava kompatibilnost s horizontalnim skaliranjem iz `ARCHITECTURE_BASELINE.md`.

## 4. Cookie contract

Production auth cookie mora biti:

- `HttpOnly`
- `Secure`
- ograničen na potreban host/path scope
- bez auth secreta dostupnog JavaScriptu

`SameSite` politika mora biti odabrana kao najsigurnija vrijednost kompatibilna s podržanim first-party auth flowom. Za trenutni Phase 1.6, koji nema external login, preferira se restriktivan first-party cookie model.

Ne smije se oslanjati samo na `SameSite` kao jedinu CSRF kontrolu.

## 5. CSRF

Svi state-changing endpointi koji prihvaćaju cookie-authenticated browser request moraju imati eksplicitnu CSRF zaštitu prikladnu ASP.NET Core arhitekturi.

Zahtjevi:

- unsafe HTTP metode ne smiju se smatrati sigurnima samo zato što zahtijevaju auth cookie
- frontend i API koriste dokumentirani anti-forgery/CSRF contract
- CORS nije zamjena za CSRF zaštitu
- CORS ostaje restriktivan prema već zaključanom public frontend origin contractu

## 6. Password security

- password hashing koristi provjerenu ASP.NET Core Identity/framework implementaciju
- nema custom hash algoritma niti reversible password storagea
- password policy mora biti centralno konfigurirana i server-side enforced
- client-side validation može poboljšati UX, ali nije security boundary
- login error poruke ostaju generičke za neispravne credentials

Konkretne minimalne password-complexity vrijednosti mogu se implementirati kao security konfiguracija uz testove, ali ne smiju kontradiktirati business contractu niti oslabiti framework baseline.

## 7. Email verification token

Verification token:

- generira se CSPRNG/framework sigurnim mehanizmom
- vremenski je ograničen
- ne sprema se/logira u obliku koji omogućuje zlouporabu ako persistence/log procuri
- nakon uspješne potvrde više ne smije biti valjan za novu potvrdu
- resend mora opozvati ili na siguran način zamijeniti prethodni aktivni verification credential ako implementacijski model koristi vlastite persisted tokene

Endpoint i UI moraju sigurno obraditi invalid/expired/already-used stanje.

## 8. Password recovery token

Password reset token:

- generira se CSPRNG/framework sigurnim mehanizmom
- vremenski je ograničen
- jednokratan je u poslovnom smislu
- tretira se kao secret
- ne logira se
- uspješna potrošnja invalidira token i sve aktivne account sesije

Forgot-password API uvijek vraća semantički generičan odgovor koji ne potvrđuje postoji li account.

## 9. Authorization

API koristi deny-by-default authorization posture.

Tehnički zahtjevi:

- protected route groups/endpoints zahtijevaju authenticated Teacher principal
- anonymous access dodjeljuje se samo eksplicitno dopuštenim auth/recovery/health endpointima
- role/type claim može identificirati `Teacher`, ali ownership se ne zaključuje samo iz rolea
- object-level ownership provjera izvršava se server-side
- request-supplied owner/teacher IDs nisu authorization evidence
- endpoint handler/use case mora dobiti pouzdani current-user identity preko centralnog abstractiona, ne proizvoljno iz request payloada

## 10. Rate limiting and abuse protection

Obavezno za javne auth površine:

- login
- registration kada može biti zloupotrijebljen
- resend verification
- forgot password
- reset/recovery pokušaji prema riziku

Limitiranje mora biti server-side, testirano i ne smije zahtijevati spremanje osjetljivih credentials u logove.

Account lockout može se koristiti kao dodatna obrana ako je implementiran tako da ne uvodi jednostavan denial-of-service nad poznatim accountom; rate limiting ostaje obavezan boundary.

## 11. Session expiry and invalid session behavior

- session ima konačan lifetime
- expired/revoked session rezultira unauthenticated ponašanjem (`401` prema API contractu)
- authenticated principal bez dopuštenja rezultira `403`
- frontend centralno obrađuje session-expired stanje i vraća korisnika u login flow bez izlaganja zaštićenog sadržaja

## 12. Logging and observability

Dozvoljeno je bilježiti sigurnosno relevantne događaje bez secreta, primjerice:

- uspješna/neuspješna autentikacija kao bounded event category
- account/session revocation
- password reset completion
- email verification completion
- rate-limit rejection

Nikada ne logirati:

- password
- password hash
- auth cookie vrijednost
- session secret
- verification token
- password reset token
- cijeli Authorization/Cookie header

Sve slijedi `OBSERVABILITY.md` i `SECURITY_ENGINEERING_STANDARD.md`.

## 13. API surface categories

Phase 1.6 smije uvesti samo auth/account endpoint kategorije potrebne za:

- register
- verify email
- resend verification
- login
- logout
- forgot password
- reset password
- change password za autentificiranog Teachera
- current authenticated account/session state potreban frontend shellu

Točni URL-ovi slijede `/api/v1` conventions i moraju biti dokumentirani testovima/API contractom tijekom implementacije.

## 14. Frontend contract

Frontend:

- ne sprema bearer/auth tokene u browser storage
- šalje cookie credentials samo prema konfiguriranom API originu
- ima centralnu obradu `401` i `403`
- nema client-side security pretpostavku da skrivena ruta/akcija zamjenjuje backend authorization
- ne prikazuje Student/Guardian/Admin auth opcije
- ne uvodi external auth ili MFA UI

## 15. Persistence and migrations

Identity/session persistence mora slijediti:

- `DATABASE_DESIGN_STANDARD.md`
- `PERSISTENCE.md`
- EF Core migrations
- SQL constraints/indexe za jedinstveni normalized e-mail i integritet session/account relacija

Sensitive token persistence, ako je potreban vlastiti persisted token model, mora koristiti one-way/siguran representation prikladan za provjeru tokena; raw recovery/verification secret ne smije biti trajno spremljen kao običan tekst.

## 16. Test obligations

Minimalno testirati:

- register success + duplicate e-mail
- pending account ne dobiva zaštićeni pristup
- verification success/invalid/expired/already-used
- login success + generic failure
- rate limiting/abuse boundary
- authenticated protected endpoint
- anonymous protected endpoint -> 401
- forbidden authorization -> 403 gdje postoji authorization case
- logout revocation
- password change invalidates all sessions
- password reset invalidates all sessions
- deactivated account/session je odbijen
- forgot-password account enumeration protection
- cookie security attributes u production-like runtime contractu
- CSRF zaštitu za state-changing cookie-authenticated requestove
- frontend session-expired/access-denied flow

## 17. Explicit non-decisions / out of scope

Ne uvoditi u Phase 1.6:

- JWT browser auth
- refresh-token flow za SPA bearer auth
- OAuth/OIDC social login
- MFA
- Student/Guardian/Admin account model
- invitation system
- future permission matrix bez dokumentiranog business use casea
