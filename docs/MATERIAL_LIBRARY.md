# Material library

## Status

**IMPLEMENTED / REVIEW READY — Phase 6.3 — 2026-10-01**

Ovaj dokument definira read-only Screen 4.1 nad zaključanim Material foundationom iz
`MATERIAL_FOUNDATION.md` i `MATERIAL_METADATA_MAPPING.md`. Mjerodavni izvori su
`source_specs/4.1_Biblioteka_materijala.md`, canonical PNG i postojeći PLUS 5 shell/design
pravila. Faza ne uvodi create/import, detail, edit, duplicate, lesson-plan, share mutation,
archive/delete ni binary preview workflow.

## Read-model granica

Library prikazuje samo materijal koji ima:

- `ArchivedAtUtc == null`;
- `CurrentVersionId` koji pokazuje na `Active` `MaterialVersion` istog materijala;
- aktivni version-bound `MaterialFile` sa statusom `Clean`;
- valjani owner ili share scope trenutačno prijavljenog Teachera.

`Moji materijali` znači `Material.OwnerTeacherId == currentTeacherId`.
`Dijeljeni sa mnom` znači eksplicitni aktivni `MaterialShare` za current Teachera,
`Material.Visibility == Shared` i drugi owner. Private ili Revoked materijal ne ulazi u
shared rezultat. Teacher identitet nikada ne dolazi iz query stringa.

Jedan library red predstavlja trenutačnu aktivnu verziju, ne mutable spoj povijesnih
verzija. Naslov, opis, tip, Subject, Program, SchoolGrade, ProficiencyLevel i tagovi čitaju
se iz tog snapshot retka. `AddedAtUtc` je vrijeme nastanka aktivne verzije.

## API contract

### `GET /api/v1/materials`

Teacher-only paginirani endpoint. Parametri:

- `page` — default 1, najmanje 1;
- `pageSize` — default 24, 1–100;
- `ownership` — `1` Mine, `2` SharedWithMe;
- `sort` — `1` Newest, `2` Oldest, `3` Title;
- `search` — najviše 100 znakova; title/description/subject/tag pretraga;
- `subject`, `programId`, `schoolGradeId`, `materialTypeCode`, `tag` — kombinirani AND filtri.

Rezultat sadrži samo prikazive metadata podatke i `shareAccess`; ne vraća storage bucket,
object key, checksum, malware detalje ili tuđe sigurnosne podatke.

### `GET /api/v1/materials/overview`

Teacher-only facets za isti ownership i isti Clean/current/active scope:

- dostupni Subject, Program, SchoolGrade, material type i tag izbori;
- broj materijala po tipu;
- do četiri nedavno dodana materijala.

Brojevi i recent lista izračunavaju se server-side iz autoriziranog skupa. Frontend ne
učitava tuđe retke pa ih potom skriva.

## UI contract

Ruta `/materials` implementira:

- PLUS 5 shell s aktivnim žutim `Materijali` odredištem;
- naslov, opis, search i canonical `Novi materijal` poziciju;
- `Moji materijali` / `Dijeljeni sa mnom` tabove;
- server-side sort, filtre i paginaciju;
- grid i list prikaz bez drugog globalnog storea;
- type counts i recent summary zonu;
- eksplicitna loading, error/retry, empty i no-results stanja;
- URL-owned search/filter/sort/view/page state pogodan za refresh i deep link.

Canonical kartice sadrže ilustrirane thumbnaile, ali zaključani backend nema thumbnail ili
preview conversion contract. Zato UI prikazuje pošten format preview (`PDF`, `DOCX`, `PPTX`,
`MP4`, `ZIP`) i stvarne metadata podatke; ne generira lažne slike ili sadržaj.

## Buduće akcije

Source predviđa otvaranje detalja, import, edit, duplicate, add-to-lesson, share, archive i
delete. Njihovi workflowi pripadaju Phase 6.4 i 6.6–6.8. U 6.3 su `Novi materijal` i item
action kontrole vidljive na canonical mjestu, ali disabled s jasnim objašnjenjem. Nema
lažnog uspjeha, skrivenog writea ni preuranjene rute.

## Security i failure semantika

- anonymous zahtjev dobiva `401`;
- svaki query ponovno primjenjuje owner/share scope u SQL-u;
- client-provided Program/Grade/type/tag filter ne može proširiti autorizirani skup;
- storage detalji i binary sadržaj nisu dio library responsea;
- cancellation se propagira kroz endpoint i EF query;
- UI ne mijenja business podatke tijekom read-only flowa.

## Visual acceptance

Canonical Screen 4.1 uspoređen je sa stvarnom aplikacijom na desktopu `1536×1024` i
mobitelu `390×844`. Korišteni su normalna lokalna Teacher prijava, stvarni SQL podaci i
stvarni API, bez auth bypassa, API interceptiona ili DOM mutationa. Dokazi i dokumentirana
odstupanja nalaze se u `visual-acceptance/phase-6.3/`.

## Granice faze

Nema schema/migration promjene, novog storage providera, binary downloada, previewa,
signed URL-a, write endpointa, Material detaila, Task/Evidence modela, AI obrade, Lesson
Builder integracije, production kataloga ili production fixturea.
