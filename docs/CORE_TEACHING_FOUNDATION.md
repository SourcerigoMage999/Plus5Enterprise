# Program, grade/level and curriculum foundation

## Status

**LOCKED foundation v1.0 — Phase 2.1 — 2026-08-25**

Ovaj dokument definira najmanji trajni domain contract koji kasnije koriste Student, Group, Material i Knowledge Model. Ne definira njihove veze unaprijed i ne uvodi feature UI/API.

## Zaključane granice

- `Program` opisuje što učenik pohađa i pripada točno jednom Teacher `UserAccountu`.
- `SchoolGrade` je referentna formalna školska godina i nije CEFR razina.
- `ProficiencyLevel` je referentna razina kompetencije unutar imenovanog okvira, npr. kod `B1` u okviru `CEFR`.
- `Curriculum` je zajednički, verzionirani službeni okvir. Nije Teacher-owned radni sadržaj niti Knowledge Model.
- Program nema ugrađeni SchoolGrade, ProficiencyLevel ni Curriculum FK. Source specifikacije koriste ih kao odvojene dimenzije, a njihove stvarne veze pripadaju Student/Group/Material/Knowledge fazama.
- Reference se ne seedaju dok vlasnik proizvoda ne odobri canonical katalog/import. Foundation zato ne pretpostavlja raspon školskih razreda, isključivo CEFR katalog niti jednu aktualnu verziju kurikuluma.

## Minimalni entiteti

### Program

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `TeacherAccountId` | obavezni FK na Teacher `UserAccount`; ownership boundary |
| `Name` | obavezni trimani naziv, najviše 160 znakova |
| `NormalizedName` | invariant uppercase oblik za case-insensitive jedinstvenost po Teacheru |
| `CreatedAtUtc` | obavezni UTC trenutak stvaranja |

Jedan Teacher ne može imati dva programa istog normaliziranog naziva. Program lifecycle, rename/archive/delete i management UI ostaju zaseban gate prije feature implementacije.

### SchoolGrade

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `Code` | obavezni stabilni machine code, najviše 32 znaka |
| `Name` | obavezni prikazni naziv, najviše 100 znakova |
| `SortOrder` | nenegativan redoslijed prikaza |

`Code` je globalno jedinstven. Foundation ne propisuje stvarne retke kataloga.

### ProficiencyLevel

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `FrameworkCode` | obavezni code klasifikacijskog okvira, najviše 32 znaka |
| `Code` | obavezni level code unutar okvira, najviše 32 znaka |
| `Name` | obavezni prikazni naziv, najviše 100 znakova |
| `SortOrder` | nenegativan redoslijed prikaza unutar okvira |

Kombinacija `FrameworkCode + Code` je globalno jedinstvena. Ne pretpostavlja se da je CEFR jedini budući okvir.

### Curriculum

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `Code` | obavezni stabilni machine/službeni code, najviše 64 znaka |
| `Name` | obavezni naziv, najviše 200 znakova |
| `Version` | obavezna identifikacija verzije izdanja, najviše 64 znaka |

Kombinacija `Code + Version` je globalno jedinstvena. Ishodi, podishodi, hijerarhija, vremenska valjanost i mapiranje na Knowledge Components pripadaju Phase 5.1+.

## Persistence contract

- tablice su `Programs`, `SchoolGrades`, `ProficiencyLevels` i `Curricula`
- svi tekstovi imaju bounded `nvarchar(n)` duljine
- natural/composite keys zaštićeni su unique indeksima
- `Programs.TeacherAccountId` ima restriktivni FK na `UserAccounts`; brisanje accounta ne briše programe
- nema cascade deletea, JSON struktura, denormaliziranih veza, seed podataka ni soft-delete/status polja bez dokumentiranog lifecyclea
- nema `rowversiona` dok stvarni write use case ne uvede concurrency rizik

## Security

Program ownership uvijek se izvodi iz autentificiranog Teacher accounta; budući API ne smije prihvatiti proizvoljni `TeacherAccountId` kao autoritet. Zajednički katalozi nisu javni write endpointi u ovoj fazi. Svaki budući read/write endpoint ostaje deny-by-default i dobiva object-level authorization testove.

## Izvan Phase 2.1

- Program CRUD/API/UI, lifecycle i arhiviranje
- canonical seed/import sadržaj referentnih kataloga
- Student–Program, Group–Program i sve grade/level veze
- CurriculumOutcome i curriculum hijerarhija
- KnowledgeArea, KnowledgeComponent i KnowledgeModel
- Material metadata, LearningGoal, readiness/evidence logika
