# AUTHENTICATION_REQUIREMENTS

## Status

**APPROVED v1.0 — 2026-08-24**

Ovaj dokument je business source of truth za ROADMAP Phase 1.6 — Authentication & Authorization.

Tehnički detalji implementacije definirani su u `AUTHENTICATION_ARCHITECTURE.md` i moraju ostati usklađeni s ovim dokumentom.

---

## 1. Scope Phase 1.6

Phase 1.6 uvodi korisnički račun i pristup aplikaciji za **Teacher** korisnika.

U ovoj fazi:

- `Teacher` je jedini poslovni akter koji ima `UserAccount` i može se autentificirati u PLUS 5
- `Student` nema korisnički račun i ne može se prijaviti u aplikaciju
- `Guardian` nema korisnički račun i ne može se prijaviti u aplikaciju
- budući Student/Guardian accounti nisu zabranjeni, ali su izvan scopea Phase 1.6 i ne smiju se implementirati unaprijed
- ne postoji `Administrator` account/role u trenutnom product scopeu

## 2. Teacher account lifecycle

Minimalni account lifecycle je:

`PendingEmailVerification -> Active -> Deactivated`

### PendingEmailVerification

- račun je kreiran registracijom
- e-mail još nije potvrđen
- korisnik nema puni pristup zaštićenom Teacher dijelu aplikacije

### Active

- e-mail je potvrđen
- račun se može autentificirati ako nisu aktivne sigurnosne zabrane

### Deactivated

- račun se ne može prijaviti niti koristiti postojeću autentificiranu sesiju
- razlog i administrativni workflow deaktivacije nisu dio Phase 1.6 osim tehničke mogućnosti da deaktivirani account bude odbijen

## 3. Registracija

Teacher ima **javnu samostalnu registraciju**.

Registracija zahtijeva najmanje:

- valjanu e-mail adresu
- lozinku koja zadovoljava sigurnosna pravila

Poslovna pravila:

- e-mail adresa mora biti jedinstvena među korisničkim računima
- registracija stvara Teacher account u statusu `PendingEmailVerification`
- korisnik mora potvrditi e-mail prije punog pristupa aplikaciji
- javna registracija ne smije stvarati Student, Guardian ili Administrator account
- ne uvoditi invitation-only ili administrator-created onboarding u Phase 1.6

## 4. Potvrda e-mail adrese

Nakon registracije sustav šalje verification poruku na registriranu e-mail adresu.

Zahtjevi:

- verification link/token mora biti jednokratan ili sigurno opoziv nakon uspješne potvrde
- token mora biti vremenski ograničen
- uspješna potvrda prebacuje account iz `PendingEmailVerification` u `Active`
- ponovni pokušaj s nevažećim, isteklim ili već iskorištenim tokenom ne smije aktivirati račun
- resend verification flow mora biti zaštićen od abusea/rate-limitiran
- response ne smije nepotrebno otkrivati interne account podatke

## 5. Prijava

Teacher se prijavljuje:

- e-mail adresom
- lozinkom

Prijava je dopuštena samo accountu koji je u dopuštenom aktivnom stanju.

Zahtjevi:

- pogrešan e-mail i pogrešna lozinka ne smiju davati korisniku razlikovne poruke koje omogućuju enumeraciju accounta
- login endpoint mora imati rate limiting / brute-force zaštitu
- autentikacija mora koristiti sigurnu server-controlled cookie sesiju prema `AUTHENTICATION_ARCHITECTURE.md`
- browser storage (`localStorage`, `sessionStorage`) ne smije sadržavati autentikacijske tokene ili druge bearer credentials
- Phase 1.6 ne uvodi `Remember me`
- Phase 1.6 ne uvodi Google, Microsoft ili drugi external login
- Phase 1.6 ne uvodi MFA

## 6. Odjava i session invalidation

### Logout

- odjava prekida aktualnu autentificiranu sesiju
- cookie/session credential mora nakon odjave prestati omogućavati pristup

### Logout all / sigurnosna invalidacija

Sustav mora imati mogućnost opozvati sve aktivne sesije jednog Teacher accounta kada security flow to zahtijeva.

Obavezno je opozvati sve postojeće sesije nakon:

- uspješnog reseta lozinke
- promjene lozinke
- deaktivacije accounta

## 7. Zaboravljena i resetirana lozinka

Teacher može zatražiti reset lozinke putem registrirane e-mail adrese.

Zahtjevi:

- forgot-password odgovor ne smije otkriti postoji li unesena e-mail adresa u sustavu
- recovery token/link mora biti jednokratan
- recovery token mora biti vremenski ograničen
- recovery token se tretira kao secret
- uspješan reset postavlja novu lozinku i invalidira sve postojeće sesije accounta
- nevažeći, istekao ili već iskorišten recovery token ne smije promijeniti lozinku
- forgot/reset endpointi moraju imati odgovarajući rate limiting / abuse protection

## 8. Authorization model

Security model je **deny by default**.

Za Phase 1.6 vrijedi:

- svi business API endpointi zahtijevaju autentikaciju osim eksplicitno javnih endpointa
- javni su samo endpointi nužni za registration/login/email verification/password recovery i health contract koji je već definiran foundation fazama
- server je jedini autoritet za authorization odluke
- UI skrivanje akcije nije authorization kontrola
- `Teacher` smije pristupati samo vlastitom business scopeu
- ownership se mora provjeravati server-side za svaki resurs koji pripada učitelju
- `teacherId`, owner ID ili slična vrijednost poslana s frontenda nikada sama po sebi nije dokaz prava pristupa
- authenticated identity i persistence/business veza određuju dopušteni ownership scope

Kada se kasnije uvedu Student, Group, Session, Material i drugi Teacher-owned resursi, njihovi endpointi moraju poštovati ovaj ownership contract.

## 9. UI states obavezni za Phase 1.6

Frontend mora imati barem sljedeća korisnička stanja/flowove:

- registracija Teacher accounta
- potvrda e-mail adrese
- prijava
- zaboravljena lozinka
- postavljanje nove lozinke
- istekla/nevažeća sesija
- zabranjen pristup (`403` / access denied)
- odjava

UI ne smije prikazivati Student/Guardian login ili Administrator onboarding.

## 10. Security i privacy zahtjevi

- passwordi se nikada ne spremaju u čistom tekstu
- auth/recovery/verification secrets ne smiju se logirati
- responsei i logovi ne smiju otkrivati password hash, session secret, recovery token ili verification token
- autentikacijski cookie u produkciji mora koristiti najmanje `HttpOnly` i `Secure`
- CSRF zaštita i `SameSite` politika moraju biti eksplicitno definirane u tehničkom auth contractu
- svi auth endpointi slijede `API_CONVENTIONS.md`, `SECURITY_ENGINEERING_STANDARD.md` i `OBSERVABILITY.md`

## 11. Explicit out of scope

Phase 1.6 ne implementira:

- Student account/login
- Guardian account/login
- Administrator account/role
- admin-created Teacher account workflow
- invitation-only onboarding
- social/external login
- MFA
- `Remember me`
- permissions za buduće roleove koje još ne postoje
- buduće module ili njihove business podatke

## 12. Acceptance criteria

Phase 1.6 se smatra business-complete samo ako je dokazano da:

1. Teacher se može registrirati i account počinje kao `PendingEmailVerification`.
2. E-mail mora biti potvrđen prije punog Teacher pristupa.
3. Aktivni Teacher može se prijaviti e-mailom i lozinkom.
4. Neaktivni/nepotvrđeni/deaktivirani account ne dobiva zaštićeni pristup.
5. Logout prekida aktualnu sesiju.
6. Password change/reset invalidira sve postojeće sesije.
7. Forgot-password ne omogućuje account enumeration.
8. Verification i reset tokeni su vremenski ograničeni i ne mogu se ponovno valjano koristiti nakon potrošnje.
9. Zaštićeni API je deny-by-default.
10. Browser ne sprema auth bearer tokene u `localStorage`/`sessionStorage`.
11. Student, Guardian i Administrator accounti nisu uvedeni.
12. Auth i recovery endpointi imaju testiranu abuse/rate-limit zaštitu.
