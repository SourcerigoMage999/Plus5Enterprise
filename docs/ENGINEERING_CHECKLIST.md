# ENGINEERING_CHECKLIST

## Status

**MANDATORY review checklist v1.0 — 2026-08-23**

Senior developer/AI koristi ovu checklistu prije zaključavanja svake podfaze.

## Scope

- [ ] Pročitani su `PROJECT_RULES.md`, `ARCHITECTURE_BASELINE.md`, `ROADMAP.md` i relevantni source specovi.
- [ ] Implementirano je samo ono što pripada trenutnoj podfazi.
- [ ] Nije uvedena buduća infrastruktura/feature bez eksplicitnog preduvjeta.

## Domain / backend

- [ ] Business pravila nisu u controlleru/UI-u.
- [ ] Dependency smjer ostaje ispravan.
- [ ] Validation i authorization su na serveru gdje trebaju biti.
- [ ] Async I/O i cancellation koriste se smisleno.
- [ ] Error contract je konzistentan.

## Database

- [ ] Model je defaultno 3NF.
- [ ] PK/FK/UNIQUE/CHECK/NOT NULL constrainti čuvaju integritet.
- [ ] Nullability i delete behavior su namjerni.
- [ ] Velike liste imaju pagination.
- [ ] Nema N+1/full graph učitavanja bez razloga.
- [ ] Indeksi odgovaraju query patternu.
- [ ] Migration je reproducibilna i nema neprihvaćen data-loss rizik.
- [ ] Concurrency je obrađen ako use case to zahtijeva.

## Security

- [ ] Nema secreta u kodu/repozitoriju/logovima.
- [ ] Endpoint/resource ima odgovarajuću auth/authorization zaštitu.
- [ ] Input i file trust boundary je validiran.
- [ ] Ne postoji očit injection/XSS/CSRF/IDOR rizik.
- [ ] Sensitive podaci su minimizirani.

## Frontend

- [ ] TypeScript contracti nisu zaobiđeni `any`-jem bez opravdanja.
- [ ] Loading/error/empty state postoji gdje je potreban.
- [ ] UI validation nije jedina zaštita.
- [ ] API pristup koristi standardni client/boundary.
- [ ] Osnovna accessibility pravila su zadovoljena.

## Tests / quality

- [ ] Build prolazi.
- [ ] Relevantni unit testovi prolaze.
- [ ] Relevantni integration testovi prolaze.
- [ ] Architecture/frontend testovi prolaze gdje postoje.
- [ ] Bug fix ima regression test gdje je izvedivo.
- [ ] Nema lažne tvrdnje da je test pokrenut ako nije.

## Docker / operations

- [ ] Image ne sadrži secrets/dev artefakte.
- [ ] Runtime je non-root.
- [ ] Config je environment-driven.
- [ ] Persistent data nije na ephemeral container filesystemu.
- [ ] Health/readiness ponašanje je smisleno ako ga faza dodiruje.

## Documentation

- [ ] `ROADMAP.md` status je ažuriran samo ako su acceptance kriteriji stvarno prošli.
- [ ] `DECISION_LOG.md` je ažuriran za novu trajnu odluku.
- [ ] `OPEN_QUESTIONS.md` sadrži neriješene kontradikcije/gateove.
- [ ] Phase summary postoji i navodi točne test rezultate i promijenjene datoteke.
