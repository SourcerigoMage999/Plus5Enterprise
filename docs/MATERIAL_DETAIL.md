# Material detail

## Status

**Phase 6.4 — FINAL LOCKED, odobreno 2026-10-03.**

Ovaj dokument je source of truth za Screen 4.2 read-only detalj aktivne Material verzije.
Primjenjuje zaključane `MATERIAL_FOUNDATION.md` i `MATERIAL_METADATA_MAPPING.md` contracte bez
uvođenja upload, edit, share-mutation ili Lesson workflowa. Phase 6.5 read-only proširenje
prikazuje version-bound procjenjive Task metapodatke prema `ASSESSABLE_TASK_METADATA.md`.

## Route i API

- frontend: `/materials/:materialId`;
- API: `GET /api/v1/materials/{materialId}`;
- pristup: samo autentificirani Teacher;
- identitet Teachera dolazi isključivo iz autentificirane server-side sesije.

API vraća detalj samo kada je Material `Active`, nije arhiviran, tražena verzija je njegov
`CurrentVersionId`, verzija je `Active`, a pripadajući file je `Clean`. Teacher mora biti owner
ili eksplicitni recipient `MaterialShare` granta na `Shared` materijalu. Missing, foreign,
private, revoked, archived, non-current, draft i non-clean rezultat jednako daju `404`.

## Read model

Detail odgovor sadrži samo korisnički vidljive podatke zaključanog snapshot-a:

- Material i MaterialVersion ID, version number, title, description, type, subject i language;
- Program, SchoolGrade i ProficiencyLevel/CEFR reference;
- learning goal i slobodne tagove;
- display file metadata: format, originalni sigurni filename, MIME i stvarna/declared veličina;
- točne version-bound KnowledgeComponent veze uz KnowledgeArea te KnowledgeModel code/version/status;
- točne version-bound CurriculumOutcome veze uz Curriculum code/name/version;
- `isOwner` i eventualni `View`/`Use` share access.

Response nikad ne izlaže storage provider/container/object key, checksum, scanner detalje,
credentiale, quarantine lokaciju ili binary payload. Tag nije Knowledge Component i ne ulazi u
Knowledge mapping.

## UI contract

Ekran slijedi canonical `4.2 Pregled materijala.png`:

- breadcrumbs, Screen 4.2 naslov i sažetak materijala;
- type/version, Program/Grade/Subject/Level, opis i file metadata;
- centralni format-aware content prostor;
- cilj učenja, kontrolirane Knowledge Components i slobodni tagovi;
- kurikulumski ishodi;
- desktop two-column i mobile stacked layout;
- loading, recoverable error i sigurno indistinguishable not-found stanje.

`Kopiraj link` je lokalna navigation akcija i aktivna je. `Otvori`, `Preuzmi` i `Prezentiraj`
ostaju disabled jer production storage-read/R2 adapter i preview runtime prema zaključanom
ROADMAP-u dolaze uz 6.6/deployment. UI ne fabricira javni URL, thumbnail, file sadržaj ili
preview. Edit/version history dolazi u 6.7, Add-to-Lesson u Phase 9.

## Task / Evidence granica

Screen prikazuje eksplicitnu granicu da sam instructional Material nije dokaz znanja. Od Phase
6.5 detail response i `Zadaci i procjena` zona prikazuju stvarni Task/TaskVersion ID/version,
prompt, tip, difficulty, scoring, EvidenceType, answer/criterion i leaf-only per-task Knowledge
mapping. Prazan Task skup nije `0 %` ni dokaz neuspjeha.

Task prikaz je read-only i ne stvara Attempt ili EvidenceEvent. Assistance i EvidenceContext
određuje tek konkretni budući runtime pokušaj, a ne authoring metadata.
Readiness, activity count, usage history, recommendations i Lesson veze ne izračunavaju se niti
simuliraju bez svojih zaključanih domena. Različite KnowledgeModel verzije ostaju označene, ne
spajaju se u jednu prividnu semantiku.

## Security i acceptance

- owner/share filter izvršava se u SQL queryju prije projekcije detalja;
- `View` daje metadata/Clean-content read semantiku, `Use` ne daje edit ili re-share;
- API ponovno provjerava authorization bez oslanjanja na frontend;
- anonymous zahtjev dobiva `401`;
- stvarni SQL test potvrđuje owner/share/foreign behavior i EF translation;
- canonical desktop `1536×1024` i mobile `390×844` evidence koriste stvarni login/API, bez
  auth bypassa, API interceptiona, DOM mutationa ili business writea.

## Izvan Phase 6.4

Nema write endpointa, binary access endpointa, storage adaptera, PDF/PPTX
renderera, Attempt/Evidence emissiona, upload/importa, edita/version
historyja, share managementa, arhiviranja, dupliciranja ni Lesson Builder integracije.
