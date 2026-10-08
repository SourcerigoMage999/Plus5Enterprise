# Material editing and version history

## Status

Phase 6.7 je **IMPLEMENTED / REVIEW READY** od 2026-10-08. Ovaj dokument proširuje
`MATERIAL_FOUNDATION.md`, `MATERIAL_METADATA_MAPPING.md`, `MATERIAL_DETAIL.md` i
`MATERIAL_IMPORT.md` bez promjene njihovih zaključanih invarijanata.

## Granica faze

Teacher može uređivati vlastiti aktivni Material na `/materials/{materialId}/edit`, pregledati
immutable povijest, spremiti novu Draft verziju, objaviti Draft i vratiti povijesni snapshot kao
novu Draft verziju. Shared recipient nema edit surface. Missing, foreign i archived Material
vraćaju isti `404`; anonymous pristup vraća `401`.

Phase 6.7 ne uvodi Presentation/slide document model. Centralna zona zato iskreno prikazuje
datoteku i granicu Phase 7, bez lažnog slide editora, previewa ili sadržaja.

## Lifecycle

```text
Active v1
  -> Save draft: Draft v2, Active v1 ostaje current
  -> Publish: Active v1 -> Superseded, Draft v2 -> Active/current

Superseded v1
  -> Restore: novi Draft v3 s istim snapshotom
```

- `Active` i `Superseded` verzije nikad se ne uređuju in-place;
- Material smije imati samo jednu otvorenu Draft verziju u ovom workflowu;
- ponovni Save Draft uređuje postojeći Draft, ne stvara paralelni Draft;
- restore je dopušten samo za povijesnu Active/Superseded verziju i stvara kasniji Draft;
- current Active restore nije akcija, a postojeći Draft blokira drugi restore;
- publish je transakcijski: root concurrency touch, supersede current i activate Draft moraju
  zajedno uspjeti ili se zajedno rollbackati;
- `Material.RowVersion` je optimistic-concurrency authority za sve write akcije.

## Snapshot kopiranje

Nova Draft verzija dobiva vlastiti `MaterialFile` i vlastiti opaque object key. Clean objekt
kopira se server-side unutar privatnog clean bucketa; binary se ne šalje kroz browser i ne
pretvara se lažno u novi upload/scan. Novi zapis čuva verified size/checksum, bilježi
`CLEAN_COPY`, ostaje bez scan pokušaja i može aktivirati samo vlastitu verziju.

Snapshot kopira:

- osnovne metapodatke, Program/Grade/CEFR i LearningGoal;
- tagove, CurriculumOutcome i KnowledgeComponent mappinge;
- svaki postojeći `AssessableTaskVersion` u novi TaskVersion istog stabilnog Task identiteta,
  zajedno s difficulty/EvidenceType/scoring i leaf KnowledgeComponent mappingom.

Time held/past planovi, Attempti i Evidence nikad se ne preusmjeravaju na novu verziju.
Zamjena fizičke datoteke nije 6.7 scope; budući replacement mora ponovno proći isti
validation/quarantine/scan/Clean put kao import.

## API

Svi endpointi su Teacher-only. Write endpointi zahtijevaju važeći antiforgery token i Teacher
identitet uzimaju samo iz autentificirane sesije.

| Metoda | Ruta | Semantika |
|---|---|---|
| `GET` | `/api/v1/materials/{materialId}/edit` | owner-scoped workspace, editable Draft ili current Active, history i picker options |
| `GET` | `/api/v1/materials/{materialId}/versions/{versionId}` | read-only snapshot jedne owner verzije |
| `PUT` | `/api/v1/materials/{materialId}/draft` | stvara ili ažurira jedini Draft uz `expectedRowVersion` |
| `POST` | `/api/v1/materials/{materialId}/draft/publish` | atomski objavljuje navedeni Draft |
| `POST` | `/api/v1/materials/{materialId}/versions/{versionId}/restore` | kopira povijesni snapshot u novi Draft |

Stabilne greške su `material_not_found`, `material_edit_invalid_request`,
`material_edit_reference_not_found`, `material_draft_exists`, `concurrency_conflict`,
`material_storage_unavailable` i `invalid_csrf_token`.

## UI contract

Screen 4.5 zadržava canonical hijerarhiju: naslov, version/history akcije, Save Draft i publish,
četverokoračni indikator, centralni content prostor te desni metadata panel. History drawer
razdvaja Draft/Active/Superseded i current verziju; povijesni detalj je read-only i restore
eksplicitno kaže da stvara novu skicu.

Visibility je u 6.7 read-only. Permission/share mutation pripada Phase 6.8. Duplicate je
prikazan disabled jer mora stvoriti novi Material identitet, a puni permission/use contract
još nije implementiran. Archive, AI suggestions, export, preview, presentation editing i Lesson
Builder nisu simulirani.

## Security i failure ponašanje

- owner filter primjenjuje se prije učitavanja snapshotova i writea;
- request ne prihvaća owner/teacher ID;
- reference se ponovno provjeravaju server-side; Program mora pripadati owneru;
- Knowledge mapping prihvaća samo Published/Retired model verzije;
- storage key, checksum, bucket i scanner detalji nisu dio API responsea;
- storage copy failure ne smije ostaviti SQL Draft; poznati destination objekt kompenzacijski se
  briše na SQL failureu;
- dirty form ima route i `beforeunload` zaštitu;
- loading, error/retry i disabled-action stanja postoje na UI-ju.

## Izvan Phase 6.7

- share grant, visibility mutation i permission management iz 6.8;
- slide CRUD, content elements, Quick Check editor, preview i autosave iz Phase 7;
- AI suggestion provider/privacy/provenance;
- binary replacement, transcoding i thumbnail pipeline;
- duplicate, archive/delete, export i Lesson Builder handoff.
