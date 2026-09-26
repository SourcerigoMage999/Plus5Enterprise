# Phase 5.6 — Procjena spremnosti učenika

## Status

`FINAL LOCKED`

## Datum

`2026-09-26`

## Cilj faze

Otvoriti prvi read-only UI nad zaključanim `readiness-v1` projekcijama bez uvođenja nove formule,
školske ocjene, preporuka ili ručno upisane procjene. Učitelj mora vidjeti rezultat u kontekstu
pouzdanosti, količine dokaza i vremena izračuna, a strani i arhivirani učenici moraju ostati
nerazlučivi od nepostojećih.

## Implementirano

- autorizirani `GET /api/v1/students/{studentId}/readiness`
- owner + non-archived provjera prije čitanja projekcija
- student, razred i Knowledge Model code/version kontekst
- sortirane KnowledgeArea projekcije s `Score`, `Readiness`, `Confidence`, `EvidenceCount`,
  `EffectiveEvidenceWeight`, `CalculatedAtUtc` i `AlgorithmVersion`
- zaseban `/students/:studentId/readiness` ekran i breadcrumb povratak u digitalni dosje
- kartice područja s rezultatom samo kada stvarna projekcija ima score
- canonical hrvatski prikaz readiness/confidence statusa
- eksplicitno prazno stanje bez lažno preciznog postotka
- poveznica "Pogledaj detalje spremnosti" iz 2.2 Digitalnog dosjea
- responsive prikaz za desktop, tablet i mobilni layout

## Namjerne granice

Phase 5.5 ne sadrži jednu ukupnu student/test readiness projekciju. Zato Phase 5.6 ne računa
prosjek područja u browseru niti ga predstavlja kao ukupnu spremnost. Također ne prikazuje:

- predviđenu hrvatsku školsku ocjenu ili vjerojatnost uspjeha
- proizvoljne "glavne faktore", angažman ili redovitost
- automatski izvedene snage/slabosti i preporuke za sljedeći sat
- PDF export
- handoff u Lesson Builder
- Teacher override ili ručni postotak

Te funkcije zahtijevaju zaseban zaključani business contract i odgovarajuće projekcije. UI ne
smije postati drugi authority niti implicitno promijeniti `readiness-v1` algoritam.

## Visual acceptance status

**PASS.** Canonical `2.4 Procjena spremnosti učenika.png` uspoređen je sa stvarnim prijavljenim
ekranom na desktopu 1536×1024 i mobitelu 390×844. Stvarni Evidence događaji prošli su postojeći
emission/projection servis i stvarni owner-scoped API; nisu korišteni auth bypass, API
interception, DOM mutation ni hardkodirani frontend podatci. Poseban stvarni Student bez
projekcija potvrđuje no-data stanje. Oba viewporta imaju `scrollWidth == clientWidth`, dvije
KnowledgeModel verzije ostaju odvojene, dossier linkovi rade i nema browser grešaka.

Dokazi, canonical hash, mjerenja i eksplicitna odstupanja za overall/test score, grading,
faktore, recommendations, PDF i Lesson Builder nalaze se u
`visual-acceptance/phase-5.6/README.md`.

## Sigurnost i konzistentnost

- query najprije provjerava Teacher ownership i `ArchivedAtUtc == null`
- missing, foreign i archived Student vraćaju isti `404` rezultat
- frontend ne dohvaća Evidence history i ne izlaže sirove učenikove odgovore
- endpoint je read-only i ne može promijeniti Evidence ili projection state
- model/version se prikazuje kako se rezultati iz različitih verzija ne bi nevidljivo pomiješali

## Testovi i evidence

| Gate | Rezultat |
|---|---|
| Release backend/architecture suite bez SQL opt-ina | PASS — 178 passed, 37 očekivano skipped |
| Novi backend query testovi | PASS — projection metadata, empty state i ownership granice |
| API auth regression | PASS — anonymous `401`, prijavljeni Teacher dobiva vlastiti snapshot |
| Frontend regression | PASS — 62/62 |
| Novi frontend screen testovi | PASS — projection, no-data i safe 404 scenariji |
| TypeScript | PASS |
| Frontend lint | PASS |
| Frontend production build | PASS |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| `git diff --check` | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker rebuild i migration apply | PASS — migrations exit `0` |
| Runtime HTTP smoke | PASS — API live/ready i frontend `200`; anonymous readiness `401` |
| Non-root runtime | PASS — API UID `1654`, frontend `nginx` |
| Stvarni visual acceptance | PASS — desktop, mobile, no-data, dvije model verzije, bez overflowa |

## Točna početna točka za sljedeću fazu

Phase 5.7 može otvoriti 2.5 Knowledge detail kao owner-scoped drill-down u `MasteryEstimates`.
Prije implementacije treba zaključati kako birati aktivnu Knowledge Model verziju za učenika i
smije li se povijesna/retired verzija prikazati zasebno. Recommendation, grade prediction i
overall test readiness ostaju izvan scopea dok ne dobiju zaseban podatkovni i pedagoški ugovor.
