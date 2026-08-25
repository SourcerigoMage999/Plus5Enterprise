# Student aggregate and profile foundation

## Status

**LOCKED foundation v1.0 — Phase 2.2 — 2026-08-25**

Ovaj dokument definira najmanji trajni Student profile contract potreban za buduće ekrane 2.1, 2.2, 2.3 i 2.6. Ne uvodi feature API/UI, Group aggregate, Knowledge Model niti izvedene pokazatelje napretka.

## Zaključane granice

- `Student` je Teacher-owned osoba, nije `UserAccount` i nema login.
- Svaki Student pripada točno jednom Teacheru i jednom `SchoolGrade` zapisu.
- `Program` i `DeliveryMode` čine opcionalnu organizacijsku cjelinu: ili su oba zadana ili oba izostaju.
- Studentov Program mora pripadati istom Teacheru. Cross-Teacher Program veza nije valjana ni kada su oba ID-a tehnički postojeća.
- `DeliveryMode` je eksplicitan `Individual` ili `Group`; ne izvodi se iz članstva u grupi.
- `Group` i `GroupMembership` ne uvode se unaprijed. Dok Phase 2.3 ne može atomarno povezati grupni način rada s aktivnim članstvom, nema feature write endpointa koji sprema `DeliveryMode.Group`.
- Student ima status `Active`, `OnHold` ili `Inactive`. Arhivirani Student mora biti `Inactive`, ostaje u bazi i izostavlja se iz uobičajenih aktivnih prikaza.
- Fizičko brisanje nije dio korisničkog workflowa. Retention, legal erasure i anonimizacija moraju se zaključati prije production delete funkcije.
- Student može imati nula ili više `Guardian` kontakata, ali najviše jedan kontakt označen kao primarni.
- Guardian je kontakt unutar jednog Student agregata, nije `UserAccount` i ne dijeli se automatski između učenika.

## Student

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `TeacherAccountId` | obavezni ownership FK na Teacher `UserAccount` |
| `SchoolGradeId` | obavezni FK na globalni `SchoolGrade` |
| `ProgramId` | opcionalni FK na Program istog Teachera; paired s `DeliveryMode` |
| `FirstName` | obavezni trimani tekst, najviše 100 znakova |
| `LastName` | obavezni trimani tekst, najviše 100 znakova |
| `Nickname` | opcionalni trimani tekst, najviše 100 znakova |
| `DateOfBirth` | opcionalni datum bez vremena |
| `SchoolName` | opcionalni slobodni naziv škole, najviše 200 znakova |
| `Gender` | opcionalni slobodni prikazni podatak, najviše 64 znaka; nema zaključanog kataloga niti poslovne logike |
| `Email` | opcionalni kontaktni tekst, najviše 320 znakova; nije login identitet i nije jedinstven |
| `Phone` | opcionalni kontaktni tekst, najviše 32 znaka; nije login identitet i nije jedinstven |
| `DeliveryMode` | opcionalni `Individual` ili `Group`; paired s Programom |
| `Status` | obavezni `Active`, `OnHold` ili `Inactive` |
| `CreatedAtUtc` | obavezni UTC trenutak stvaranja |
| `UpdatedAtUtc` | obavezni UTC trenutak zadnje promjene |
| `ArchivedAtUtc` | opcionalni UTC trenutak arhiviranja; kada postoji, status je `Inactive` |

Student nema jedinstvenost po imenu, e-mailu ili telefonu: dvije različite osobe mogu legitimno dijeliti iste vrijednosti, a maloljetni učenici često nemaju vlastiti kontakt.

## Guardian

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `StudentId` | obavezni restriktivni FK na Student |
| `FirstName` | obavezni trimani tekst, najviše 100 znakova |
| `LastName` | obavezni trimani tekst, najviše 100 znakova |
| `Relationship` | opcionalni opis odnosa, najviše 100 znakova; katalog nije zaključan |
| `Email` | opcionalni kontaktni tekst, najviše 320 znakova |
| `Phone` | opcionalni kontaktni tekst, najviše 32 znaka |
| `IsPrimary` | oznaka primarnog kontakta; najviše jedan po Studentu |
| `CreatedAtUtc` | obavezni UTC trenutak stvaranja |

Guardian nema account, auth credential, conversation, consent ili notification preference u ovom foundationu.

## Persistence contract

- tablice su `Students` i `Guardians`
- svi tekstovi koriste bounded `nvarchar(n)`, datum rođenja je SQL `date`, a trajni trenutci su `datetimeoffset(7)`/UTC application contract
- restriktivni FK-ovi sprječavaju cascade gubitak Studenta ili Guardiana
- Student ownership ima direktni FK na `UserAccounts`
- composite FK `Students(TeacherAccountId, ProgramId)` prema `Programs(TeacherAccountId, Id)` sprječava cross-Teacher Program vezu
- CHECK constrainti ograničavaju Student status, DeliveryMode, paired Program/DeliveryMode i archived/inactive odnos
- filtered unique indeks dopušta najviše jednog primarnog Guardiana po Studentu
- nema seed/backfill redaka; postojeći Program i referentni podaci ostaju nepromijenjeni
- `rowversion` se ne uvodi bez stvarnog concurrent write endpointa; ponovno se procjenjuje u CRUD fazi

## Security i privatnost

- Student i Guardian sadrže PII; vrijednosti se ne smiju nepotrebno logirati niti vraćati izvan owner scopea.
- Budući API izvodi Teacher ID iz autentificirane sesije i provjerava object-level ownership za svaki Student/Guardian ID.
- Client nikada ne određuje autoritativni `TeacherAccountId`.
- Program ownership se provjerava i relacijskim constraintom, ne samo aplikacijskim kodom.
- Student/Guardian e-mail i telefon nisu identiteti za autentikaciju.

## Izvan Phase 2.2

- Student CRUD/application use caseovi, API contracti, pretraživanje i UI
- `Group`, `GroupMembership` i puna Group-mode invarijanta
- fotografija/file storage
- teacher notes, communication, conversations i notification preferences
- privacy toggles bez stvarne funkcije
- ProficiencyLevel target/estimate, Knowledge Model, mastery, readiness, progress i evidence
- history/audit trail izvan osnovnih UTC vremena
- production retention, legal erasure i anonimizacija
