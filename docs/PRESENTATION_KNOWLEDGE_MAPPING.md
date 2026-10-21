# Presentation Knowledge Component mapping

## Status

**Phase 7.6 — IMPLEMENTED / REVIEW READY.**

Ovaj contract primjenjuje zaključane ADR-0025 i ADR-0026 na Knowledge mapping procjenjivog
`QUICK_CHECK` elementa. Ponovno koristi postojeći
`AssessableTaskVersionKnowledgeComponent`; ne uvodi mapping na slajdu, PresentationElementu
ili Materialu.

## Canonical model

```text
PresentationElement (QUICK_CHECK)
        → exact same-MaterialVersion AssessableTaskVersion
        → 0..N AssessableTaskVersionKnowledgeComponents
        → leaf KnowledgeComponent iz exact KnowledgeModel verzije
```

TaskVersion je jedini authority za pedagoški target. Jedan Quick Check smije ciljati više
leaf komponenti, uključujući eksplicitno različite KnowledgeModel verzije. Browser ne širi
parent targete, ne dodjeljuje weight i ne izvodi automatsko mapiranje.

Draft Quick Check privremeno smije imati nula targeta. Postojeći Phase 6.5 activation
invariant ostaje authority: MaterialVersion se ne može aktivirati dok svaki procjenjivi
TaskVersion nema barem jedan valjani Knowledge target.

## Lifecycle pravila

- novi target mora biti leaf `KnowledgeComponent` statusa `Active` iz modela statusa
  `Published`;
- `Draft` KnowledgeModel, non-leaf komponenta, missing ID i nepostojeći target odbijaju se;
- već spremljen exact target iz `Retired` modela ili `Deprecated` komponenta ostaje vidljiv i
  može se zadržati ili eksplicitno ukloniti radi povijesne/recovery semantike;
- retired/deprecated target ne može se novo dodati kroz picker ni write;
- različite KnowledgeModel verzije ostaju eksplicitne kroz code + version oznaku;
- create/update Quick Checka i cijeli mapping set spremaju se u istoj transakciji i pod istim
  document rowversionom;
- invalid ili stale write ne smije ostaviti djelomičnu promjenu mappinga.

## API

### Candidate katalog

```http
GET /api/v1/materials/{materialId}/presentation/knowledge-components
    ?search={tekst}&page={1..}&pageSize={1..50}
```

Endpoint vraća samo `Published + Active + leaf` kandidate, deterministički sortirane i
paginirane. Search do 200 znakova obuhvaća naziv komponente, područje te KnowledgeModel code
i version. Prije queryja se provjeravaju Teacher ownership, Active/non-archived Material te
Draft `PRESENTATION` verzija. Missing, foreign i nedostupan Material vraćaju isti `404`.

### Quick Check write

Postojeći create/update request dobiva:

```json
{
  "knowledgeComponentIds": ["guid"]
}
```

Server ponovno učitava i validira svaki target. Ne vjeruje picker stateu ni browser status
oznakama. Nevaljan target vraća neutralni `400 presentation_invalid_knowledge_target`.
Postojeći Teacher session, CSRF, bounded body, Teacher-scoped rate limit, owner scope i
document optimistic concurrency ostaju nepromijenjeni.

## Editor UX

Property panel prikazuje:

- trenutno odabrane targete kao uklonjive chip/card zapise;
- naziv komponente, KnowledgeArea te KnowledgeModel code/version;
- eksplicitnu historical status oznaku kada postojeći target više nije Published/Active;
- bounded search picker samo za nove valjane kandidate;
- loading, error/retry i empty state;
- `Potrebno prije objave` kada mapping još ne postoji.

Odabir je lokalan dok Teacher ne aktivira postojeći `Spremi element`. Nema writea pri samom
otvaranju pickera ili kliku na kandidata.

## Persistence

Phase 7.6 nema novu migraciju ni tablicu. Koristi postojeći composite PK i restrictive FK
`AssessableTaskVersionKnowledgeComponents`, Draft-only snapshot trigger, leaf/model integrity
te activation minimum-target invariant iz Phase 6.5. Aplikacijski sloj dodatno ograničava
nove izbore na Published + Active leaf katalog.

## Izvan scopea

- automatsko/AI mapiranje i preporuke;
- weight, primary target ili parent expansion;
- CurriculumOutcome mapping u Presentation Editoru;
- autosave/recovery (7.7);
- preview, activation/publish i package/export (7.8);
- StudentAttempt, EvidenceEvent, grading i mastery/readiness promjene.

## Evidence

Implementacijski rezultat je u
`summaries/PHASE_7.6_PRESENTATION_KNOWLEDGE_MAPPING_SUMMARY.md`, a minimalni stvarni UI dokaz
u `visual-acceptance/phase-7.6/`.
