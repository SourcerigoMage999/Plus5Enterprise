# Presentation slide document model

## Status

**Phase 7.1 — IMPLEMENTED / REVIEW READY — 2026-10-09.**

Ovaj dokument zaključava tehnički model nativne PLUS 5 prezentacije prije implementacije
slide CRUD-a, elemenata, Quick Checka, autosavea ili preview/publish flowa. Primjenjuje
`MATERIAL_FOUNDATION.md`, `MATERIAL_METADATA_MAPPING.md` i
`ASSESSABLE_TASK_METADATA.md`; ne mijenja njihove zaključane invarijante.

## Scope Phase 7.1

Phase 7.1 definira:

- version-bound PresentationDocument aggregate;
- relacijski identitet i redoslijed slajdova i elemenata;
- versionirani, tipizirani payload sadržajnog elementa;
- granicu instructional sadržaja i postojećeg AssessableTask modela;
- asset, concurrency, snapshot, publish-package i schema-evolution contract;
- sigurnosne i operativne limite koje moraju primijeniti kasnije Phase 7 implementacije.

Phase 7.1 je isključivo tehnički design gate. Ne dodaje domain klase, EF model, migraciju,
API, frontend route, editor, file conversion ili production podatke.

## Canonical aggregate i verzijska granica

```text
Material (stabilni Teacher-owned identitet)
└── MaterialVersion (Draft | Active | Superseded)
    ├── PresentationDocument (0..1; samo MaterialTypeCode = PRESENTATION)
    │   ├── PresentationSlide (1..N prije objave)
    │   │   ├── PresentationElement (0..N)
    │   │   └── 0..N instructional KnowledgeComponent mappinga
    │   └── PresentationAsset (0..N, version-bound)
    └── AssessableTaskVersion (0..N; postojeći Phase 6.5 model)
```

- `Material` ostaje ownership, visibility i sharing root.
- Dokument pripada točno jednoj `MaterialVersion`; nije mutable child samog `Materiala`.
- `Draft` dokument i njegova djeca smiju se mijenjati. `Active` i `Superseded` dokument,
  slajdovi, elementi, asset reference i mapping retci immutable su.
- Nova izmjena objavljene prezentacije prvo stvara novu Draft MaterialVersion i kopira cijeli
  dokument snapshot. Povijesna verzija ostaje nepromijenjena.
- Slide/element ID je stabilan tijekom uređivanja iste Draft verzije. Kopija u novu
  MaterialVersion dobiva nove version-bound ID-eve; povijesni identitet čuva exact
  `MaterialVersionId`, ne cross-version slide lineage.
- Samo `PRESENTATION` MaterialVersion smije imati PresentationDocument. Importani PPTX nije
  automatski nativni dokument bez zasebnog, eksplicitnog conversion workflowa.

## Planirani relacijski model

Nazivi ispod su canonical imena za kasnije persistence faze; Phase 7.1 ne stvara tablice.

### PresentationDocument

Jedan red po MaterialVersionu:

- `MaterialVersionId` — PK i restrictive FK;
- `DocumentSchemaVersion` — pozitivna verzija cijelog document contracta;
- `CanvasWidth = 1600`, `CanvasHeight = 900` — v1 logical 16:9 koordinatni prostor;
- `CreatedAtUtc`, `UpdatedAtUtc`;
- SQL `rowversion` — concurrency authority za slide/element authoring.

Logical canvas nije spremljena bitmap rezolucija. Renderer jednako skalira položaje i veličine
na desktop, preview, Board ili export površinu. Drugi aspect ratio nije Phase 7 v1 scope.

### PresentationSlide

- vlastiti GUID `Id`;
- obavezni `MaterialVersionId`;
- nenegativni `SortOrder`, jedinstven unutar dokumenta;
- bez title/notes polja dok ih source ili zasebna faza ne zahtijevaju.

Aplikacijski write održava contiguous `0..N-1` redoslijed u jednoj transakciji. Unique
constraint štiti duplikat pozicije; reorder ne mijenja Slide ID.

### PresentationElement

Relacijski envelope čuva identitet i činjenice nad kojima baza mora imati integritet:

- vlastiti GUID `Id`;
- `MaterialVersionId` i `PresentationSlideId` sa same-version composite FK zaštitom;
- bounded uppercase `ElementTypeCode` i pozitivni `ElementSchemaVersion`;
- `ZIndex`, jedinstven unutar slajda;
- `X`, `Y`, `Width`, `Height` u logical canvas jedinicama;
- bounded `PayloadJson` za type-specific presentation sadržaj;
- nullable exact `AssessableTaskVersionId` za procjenjivi element.

Koordinate moraju biti konačne decimalne vrijednosti, širina/visina pozitivne, a pravokutnik
unutar 1600×900 canvasa. Aplikacijski write održava contiguous z-order. Rotation, animation,
transitions i arbitrary transforms nisu dio v1 contracta.

JSON je dopušten samo za fleksibilni presentation payload poput tekstualnih runova, boje,
shape postavke ili ćelije tablice. Ne smije sadržavati ownership, lifecycle, redoslijed,
foreign ID liste, Knowledge mapping, task scoring ili storage URL. Payload je najviše 256 KiB
po elementu, mora proći strogo tipiziranu server-side validaciju i `ISJSON` DB zaštitu.

## Element type registry i schema evolution

`ElementTypeCode` nije proizvoljan user input. Backend koristi zatvoreni registry podržanih
type handlera. Source predviđa, ali Phase 7.1 još ne implementira:

- Phase 7.3 core: `TEXT`, `IMAGE`, `SHAPE`, `TABLE`;
- Phase 7.4 teaching: `RULE`, `EXAMPLE`, `VOCABULARY`, `QUESTION`, `VIDEO`, `AUDIO`;
- Phase 7.5 assessable: `QUICK_CHECK` i odobreni interaktivni question tipovi.

Svaki handler definira DTO, maksimalne duljine, normalizaciju, renderer i dopuštene migracije.
Raw HTML, script, arbitrary iframe/embed i CSS nisu dopušteni. Rich text je strukturirani
allowlisted model koji React renderer escapira.

`DocumentSchemaVersion` i `ElementSchemaVersion` evoluiraju eksplicitno:

- Draft se može migrirati naprijed kontroliranim server-side upgradeom uz concurrency check;
- Active/Superseded snapshot ne prepisuje se;
- povijesni renderer čita njegovu spremljenu verziju contracta;
- nepoznati type/schema prikazuje sigurni unsupported placeholder i blokira publish, bez
  gubitka ili best-effort interpretacije payloada.

## Instructional i assessable granica

Instructional element:

- nema `AssessableTaskVersionId`;
- može poučavati ili objašnjavati;
- pregled, prikaz ili dovršetak slajda ne stvara StudentAttempt ni EvidenceEvent.

Assessable element:

- mora referencirati točan `AssessableTaskVersion` iste MaterialVersion;
- jedan TaskVersion može pripadati najviše jednom presentation elementu u snapshotu;
- prompt, scoring, Difficulty, EvidenceType, answer/criterion i leaf Knowledge mapping ostaju
  u postojećem Phase 6.5 modelu, ne dupliciraju se u element JSON;
- element payload čuva samo renderer/interaction presentation podatke;
- niti postojanje elementa niti njegov preview ne stvara EvidenceEvent. Budući valjani
  StudentAttempt i server-controlled emission ostaju Phase 10/12 contract.

Dupliciranje budućeg assessable slajda/elementa mora stvoriti novi stabilni AssessableTask i
novi TaskVersion snapshot. Dvije vizualno odvojene provjere ne dijele isti task identity.

## Knowledge mapping

Tri razine ostaju eksplicitno odvojene:

1. `MaterialVersionKnowledgeComponent` opisuje što cijela prezentacija poučava.
2. Budući `PresentationSlideKnowledgeComponent` opcionalno opisuje instructional fokus jednog
   slajda; dopušta Published/Retired komponentu na bilo kojoj razini i nikad nije Evidence.
3. `AssessableTaskVersionKnowledgeComponent` opisuje što točno pitanje provjerava; ostaje
   obavezan, Published/Retired i leaf-only prema Phase 6.5.

Sustav ne kopira, širi ili stapa te mappinge automatski. CurriculumOutcome mapping ostaje na
MaterialVersionu dok izvor ne zatraži precizniju razinu.

## Asset contract

`PresentationAsset` je version-bound privatni objekt za IMAGE/VIDEO/AUDIO i druge odobrene
binary elemente. Kasnija implementacija mora ponovno koristiti Phase 6 storage adapter,
opaque server-generated key, allowlist/signature provjeru, size limit, quarantine i fail-closed
malware scan. Asset bez `Clean` statusa ne smije se renderirati, objaviti ili dijeliti.

Element ne sprema javni/remote URL. Veza element–asset je relacijska i same-version; asset se
može ponovno koristiti na više elemenata iste MaterialVersion bez kopiranja bytesa. Kopiranje
u novu Draft MaterialVersion stvara novi version-bound asset zapis i private object copy kako
povijesni snapshot ne bi ovisio o mutable sadržaju.

## Publish package i postojeći MaterialFile invariant

Postojeći invariant da Active MaterialVersion ima vlastiti Clean `MaterialFile` ostaje.
Nativni editor koristi ovaj contract:

1. mutable relational document graph je canonical Draft source;
2. prije publish transakcije server ga deterministički serializira u interni ZIP manifest
   media type `application/vnd.plus5.presentation+zip`;
3. paket sadrži schema-versioned manifest i asset ID/checksum reference, ne javne URL-ove;
4. paket je server-generated iz validiranog grafa, dobiva size i SHA-256 te Clean kategoriju
   `SERVER_GENERATED`; user-uploadani asseti prethodno moraju zasebno proći scan;
5. aktivacija je dopuštena samo ako paket odgovara trenutnom document rowversionu;
6. Active/Superseded graph i paket nakon toga su immutable.

Paket je derived immutable delivery artifact; relacijski graph ostaje canonical source za
editor i integrity. Generiranje/storage I/O ne drži SQL transakciju otvorenom. Phase 7.8 mora
primijeniti staging + compensation i atomsku završnu provjeru rowversiona prije aktivacije.

## Concurrency i command granica

- Svaki write nosi `expectedDocumentRowVersion`.
- Server ponovno provjerava ownera, Draft status i same-version reference.
- Stale write završava kontroliranim `409 concurrency_conflict`; nema last-write-wins.
- Slide add/delete/reorder/duplicate i element mutation su atomske operacije nad jednim
  dokumentom.
- API ne prihvaća generic arbitrary JSON Patch nad cijelim dokumentom. Svaka naredba ima
  eksplicitni contract i type handler validation.
- Material `RowVersion` ostaje authority za create-Draft/publish/restore i root lifecycle;
  document rowversion je authority za česte editor/autosave promjene.

Phase 7.7 mora nad ovom granicom definirati debounce, recovery i UX konflikta. Ovaj document
ne proglašava autosave implementiranim.

## Bounded v1 limits

Operational abuse i payload zaštita za v1:

- najviše 200 slajdova po dokumentu;
- najviše 200 elemenata po slajdu;
- najviše 256 KiB JSON payloada po elementu;
- najviše 8 MiB ukupnog JSON payloada dokumenta;
- request/body limit po commandu mora biti uži od općeg API limita;
- asset limiti dolaze iz odobrenog formata i Phase 6 file policyja.

Count limiti su server-side aggregate validacija; jedinstveni redoslijed, geometry, JSON shape,
same-version FK i immutable snapshot imaju DB zaštitu kada se schema uvede.

## Ownership, sharing i sigurnost

- samo Material owner smije stvarati ili mijenjati Draft prezentaciju;
- `View` i `Use` grant ne daju Edit ni re-share;
- missing/foreign/archived/non-Presentation resursi ostaju indistinguishable `404`;
- browser ne šalje Teacher/owner ID i ne bira storage key;
- svi writeovi koriste cookie auth, CSRF, bounded body i odgovarajući abuse limit;
- tekst/payload ne smije sadržavati executable markup; renderer ne koristi unsanitized
  `dangerouslySetInnerHTML`;
- prezentacija ne sadrži Student/Guardian/Evidence/readiness podatke;
- structured logging bilježi ID-eve, operation, revision i outcome, ne puni sadržaj slajda.

## Acceptance contract za kasnije podfaze

- **7.2:** samo slide add/delete/reorder/duplicate nad praznim slajdovima i concurrencyjem.
- **7.3:** core type handleri i asset image granica.
- **7.4:** teaching-specific instructional handleri; bez Evidencea.
- **7.5:** interaktivni/Quick Check renderer i exact AssessableTaskVersion veza.
- **7.6:** task-level leaf Knowledge mapping kroz postojeći 6.5 model.
- **7.7:** autosave, recovery i conflict UX nad document rowversionom.
- **7.8:** preview, deterministic package, publish i povratak na Material detail.
- **7.9:** ostaje BLOCKED dok provider/privacy/prompt/retention/confirmation/fallback nisu
  eksplicitno odobreni.

## Namjerno izvan Phase 7.1

- domain/EF/schema/migration implementacija;
- endpointi, editor route i UI;
- element DTO-i/rendereri i asset upload;
- PPTX import/conversion i export u PowerPoint/PDF;
- transitions, animation, templates, collaboration/presence i real-time coauthoring;
- Attempt, Evidence emission, Student runtime i PLUS 5 Board;
- AI provider ili suggestion flow.
