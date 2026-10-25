# Phase 8.2 visual acceptance

## Status

**PASS — 2026-10-26.**

## Canonical source

- izvor: `Radni stol učitelja.png`;
- lokalna neizmijenjena kopija: `canonical.png`;
- SHA-256: `D3F70E454094D5BF5DEAFF76B99BF25C9D1A4348DC0836BC5CBA24DDCB240AF8`.

## Minimalni stvarni capture

- `teacher-dashboard-desktop-1536x1024.png`;
- `teacher-dashboard-mobile-390x844.png`;
- `measurements.json`.

Capture koristi stvarni Teacher cookie login, aktualni lokalni API, stvarni SQL Server i
postojeće owner-scoped Student/Group/Session podatke. Nisu korišteni auth bypass, API
interception ni DOM mutation. Tijekom capturea nema business writeova.

## Measurements

| Prikaz | clientWidth | scrollWidth | Rezultat |
|---|---:|---:|---|
| desktop 1536×1024 | 1536 | 1536 | PASS |
| mobile 390×844 | 390 | 390 | PASS |

Dashboard API vratio je HTTP 200 u oba viewporta. Browser/page errors: 0.

## Canonical usporedba

Očuvani su stalni PLUS 5 shell, žuti aktivni `Radni stol`, pozdrav, canonical quick actions,
današnji raspored, dominantni sljedeći sat i Student pregled. Desktop zadržava dvostupčani
schedule/next-session odnos; mobile slaže kartice bez document overflowa.

Namjerna odstupanja:

- pozdrav ne izmišlja Teacherovo osobno ime jer profil još nema canonical first-name podatak;
- poruke, homework, test/ispit, financije, Lesson Plan i pokretanje sata ostaju disabled;
- notification/activity/recently-used/finance sekcije nisu prikazane bez svojih source modula;
- Student pregled koristi Active/OnHold/Inactive status, ne nepostojeće readiness/performance
  bandove ili ukupni postotak.

## Reprodukcija

`frontend/tests/visual/phase82.mjs` koristi lokalne `PLUS5_REVIEW_*` varijable i
`PLUS5_DASHBOARD_CANONICAL`, radi točno jedan desktop i jedan mobile capture te odbija
unexpected business write, dashboard API status različit od 200, browser error ili document
overflow. Lokalna lozinka nije spremljena u Git ni visual evidence.
