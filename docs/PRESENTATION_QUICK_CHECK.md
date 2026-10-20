# Presentation Quick Check

## Status

**Phase 7.5 — IMPLEMENTED / REVIEW READY.**

Ovaj contract primjenjuje zaključane ADR-0025 i ADR-0026 na prvi procjenjivi element
Presentation Editora. Ne uvodi runtime odgovaranja učenika, `StudentAttempt`, `EvidenceEvent`
ni Knowledge mapping iz Phase 7.6.

## Business semantika

`QUICK_CHECK` je single-choice procjenjivi element s točno jednim točnim odgovorom. U v1
obavezno ima:

- prompt do 4.000 znakova;
- od 2 do 8 nepraznih odgovora, svaki do 1.000 znakova;
- točno jedan correct-answer key;
- Difficulty od 1 do 5;
- jedan zaključani `EvidenceType` (`Recognition`, `Understanding`, `Application` ili
  `Production`);
- `MaxPoints = 1` i bez evaluation criteriona.

Ključeve odgovora `A`–`H` generira server prema stabilnom redoslijedu. Browser ne šalje niti
određuje canonical ključeve. Ne uvode se multiple-select, proizvoljni bodovi, djelomično
bodovanje, ručno ocjenjivanje ni rich HTML.

## Versioning i canonical model

Jedan `QUICK_CHECK` PresentationElement referencira točno jedan
`AssessableTaskVersion`:

```text
PresentationElement (QUICK_CHECK)
        → exact same-MaterialVersion AssessableTaskVersion
        → TaskTypeCode = SINGLE_CHOICE_QUICK_CHECK
```

Scoring i pedagoški metadata nalaze se samo na TaskVersionu. Element payload je zatvoreni
`{"schemaVersion":1}` i ne duplicira prompt, odgovore, correct answer, Difficulty,
EvidenceType ni bodove. Authoring options snapshot je bounded JSON na TaskVersionu jer je dio
točne, reproducibilne verzije zadatka.

Create je atomski: nastaju stabilni `AssessableTask`, njegov v1 `AssessableTaskVersion` i
`PresentationElement`, ili ne nastaje ništa. Update atomically mijenja Draft TaskVersion i
geometry/z-index elementa pod istim document rowversionom. Duplicate slajda stvara novi
stabilni Task i novi TaskVersion snapshot; restore u novu Draft MaterialVersion radi isto.
Delete uklanja element, njegove Draft mappinge, TaskVersion i stabilni Task kada nema drugih
verzija.

## Knowledge mapping granica

Phase 7.5 dopušta Draft Quick Check bez Knowledge targeta kako bi Teacher mogao prvo napisati
pitanje. Postojeći Phase 6.5 activation invariant ostaje authority: MaterialVersion se ne može
aktivirati dok svaki AssessableTaskVersion nema barem jedan valjani leaf
KnowledgeComponent target iz Published/Retired modela.

Editor zato prikazuje neutralni `Povezivanje ciljeva dolazi u Phase 7.6` state. Ne izmišlja
Knowledge mapping i ne zaobilazi activation trigger.

## Persistence integrity

Migracija `AddPresentationQuickChecks` dodaje:

- nullable `AssessableTaskVersions.AuthoringPayloadJson` koji je obavezan samo za
  `SINGLE_CHOICE_QUICK_CHECK`;
- nullable `PresentationElements.AssessableTaskVersionId`;
- composite same-MaterialVersion FK;
- filtered unique index kojim jedan TaskVersion pripada najviše jednom elementu;
- closed element/task payload CHECK constraints.

SQL `51246` odbija nevaljan Quick Check authoring snapshot: pogrešan schema version, broj ili
redoslijed opcija, prazne/preduge tekstove, correct answer izvan opcija, bodove različite od
1 ili evaluation criterion. SQL `51247` odbija nepodudaranje `QUICK_CHECK` elementa i
`SINGLE_CHOICE_QUICK_CHECK` TaskVersiona. Postojeća Draft-only snapshot zaštita ostaje
nepromijenjena.

## API i security

- `POST /api/v1/materials/{materialId}/presentation/slides/{slideId}/quick-checks`
- `PUT /api/v1/materials/{materialId}/presentation/slides/{slideId}/quick-checks/{elementId}`

Oba writea zahtijevaju Teacher session, CSRF, postojeći Teacher-scoped Presentation rate
limit, bounded 16 KiB body, owner scope, Draft PRESENTATION status i aktualni document
rowversion. Missing, foreign i nedostupan resurs vraćaju isti `404`; stale write vraća `409`.
Generic element endpoint ne prima `QUICK_CHECK` kako se ne bi zaobišla atomska TaskVersion
validacija.

## Editor UX

Toolbar nudi zasebni `Quick Check`. Property panel uređuje prompt, 2–8 odgovora, jedan točan
odgovor, Difficulty i EvidenceType. Canvas prikazuje odgovor(e) bez izvršavanja učenikova
runtimea. Knowledge mapping je jasno odgođen, a postojeće autosave, preview i publish kontrole
ostaju disabled do svojih faza.

## Out of scope

- Knowledge Component mapping (7.6);
- autosave i recovery (7.7);
- preview, activation/publish i package/export (7.8);
- student response runtime, Attempt i eksplicitni Evidence emission (Phase 10 contract);
- analytics, AI generiranje i AI evaluacija.

## Evidence

Implementacijski rezultat nalazi se u
`summaries/PHASE_7.5_PRESENTATION_QUICK_CHECK_SUMMARY.md`, a minimalni stvarni UI dokaz u
`visual-acceptance/phase-7.5/`.
