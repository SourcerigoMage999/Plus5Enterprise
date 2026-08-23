# PLUS 5 — Product and Domain Glossary

## Status

**Canonical glossary v1.0 — Phase 0.2 — 2026-08-23**

Ovaj dokument zaključava značenje ključnih pojmova u PLUS 5. Nazivi iz stupca **Canonical term** koriste se u kodu, API contractima, bazi i tehničkoj dokumentaciji. Hrvatski naziv koristi se u korisničkom sučelju i poslovnoj komunikaciji gdje je naveden.

Glossary definira pojmove, ali ne zaključava nedokumentirane kardinalnosti, permissions, algoritme ni persistence model. Takve odluke pripadaju odgovarajućim ROADMAP fazama.

## Pravila imenovanja

1. Jedan pojam ima jedan canonical naziv kroz domenu, bazu, backend, API i frontend.
2. UI prijevod nije drugi domenski pojam.
3. Ne koristi se zajednički model ili polje `GradeLevel`: školski razred i CEFR razina odvojeni su pojmovi.
4. Ne koristi se `Lesson` kao tehnički sinonim za termin, pripremu sata i izvedenu nastavnu aktivnost. Koristi se precizniji pojam iz ovog dokumenta.
5. Ne koristi se `Activity` za atomsko pitanje/zadatak niti `Tag` za komponentu znanja.
6. Naziv entiteta ne pretpostavlja da osoba ima korisnički račun. Identity i permissions model zaključavaju se u ROADMAP 1.6.

## Actors and people

| Canonical term | Hrvatski/UI naziv | Definicija i granica |
|---|---|---|
| `Teacher` | Učitelj | Primarni korisnički akter trenutačno dokumentirane aplikacije koji organizira učenike, grupe, termine, materijale i nastavu. Točan identity i permissions contract još nije definiran. |
| `Student` | Učenik | Osoba koju učitelj podučava i za koju se vode administrativni, nastavni i knowledge/evidence podaci. `Student` nije sinonim za korisnički račun. |
| `Guardian` | Roditelj/skrbnik | Komunikacijski kontakt povezan s učenikom. `Guardian` je canonical naziv jer obuhvaća roditelja i drugog skrbnika. Specifikacija još ne potvrđuje da Guardian ima vlastiti račun niti definira permissions. |
| `UserAccount` | Korisnički račun | Autentikacijski identitet koji omogućuje pristup sustavu. Ne poistovjećuje se automatski s Teacher, Student ili Guardian zapisom dok se auth model ne zaključa. |

## Teaching organization

| Canonical term | Hrvatski/UI naziv | Definicija i granica |
|---|---|---|
| `Program` | Program | Pedagoška ponuda koja opisuje **što** učenik pohađa, npr. Grammar Focus ili General English. Program nije grupa, školski razred, CEFR razina ni način rada. |
| `DeliveryMode` | Način rada | Način na koji učenik pohađa nastavu: `Individual` ili `Group`. Mora biti eksplicitan podatak; ne zaključuje se samo iz postojanja grupe. |
| `Group` | Grupa | Trajna organizacijska jedinica za učenike koji zajedno pohađaju određeni program i imaju zajedničku organizaciju nastave. Grupa definira **tko zajedno** radi i **kada**, a ne individualno znanje članova. |
| `GroupMembership` | Članstvo u grupi | Veza učenika i grupe. Članstvo ne prenosi zajednički knowledge rezultat na učenike; svaki rezultat ostaje individualan. Povijest, vremenska valjanost i dopuštene kardinalnosti zaključavaju se u domain fazi. |
| `GroupCapacity` | Kapacitet grupe | Najveći dopušteni broj aktivnih članova grupe. Ne smije biti manji od broja članova; točan concurrency i persistence contract definira se u fazi grupe. |
| `SchoolGrade` | Razred | Razred formalnog školovanja učenika ili ciljana školska godina sadržaja, npr. 8. razred. Nije sinonim za CEFR razinu. |
| `ProficiencyLevel` | Razina znanja / CEFR razina | Razina jezične kompetencije, npr. A1–C2 kada se koristi CEFR. Može biti procijenjena iz znanja; ne predstavlja školski razred. |

## Scheduling and lesson delivery

| Canonical term | Hrvatski/UI naziv | Definicija i granica |
|---|---|---|
| `RegularGroupSchedule` | Redoviti raspored grupe | Trajni organizacijski obrazac kada grupa uobičajeno održava nastavu. Definira se uz grupu. Točan model serije, iznimki i generiranja termina ostaje otvoreni gate ROADMAP-a 2.4. |
| `Session` | Termin | Jedna konkretna, kalendarski određena instanca nastave s početkom, završetkom i individualnim ili grupnim kontekstom. Termin nije grupa ni redoviti raspored grupe. |
| `RecurringSessionSeries` | Serija termina | Skup konkretnih termina nastalih iz pravila ponavljanja. Nije sinonim za grupu; odnos prema `RegularGroupSchedule` mora se formalizirati prije DB locka rasporeda. |
| `Location` | Lokacija / učionica | Mjesto održavanja termina, fizičko ili online. Pravila rezervacije, preklapanja i videopoveznica pripadaju fazi rasporeda. |
| `LessonPlan` | Priprema sata | Strukturirani plan ciljeva, aktivnosti i materijala za budući ili konkretni termin. Nije sam termin i ne predstavlja dokaz znanja. |
| `LearningActivity` | Nastavna aktivnost | Organizirana cjelina rada tijekom sata, domaće zadaće, testa ili samostalne vježbe. Može sadržavati nula ili više procjenjivih zadataka. |

## Curriculum and knowledge model

| Canonical term | Hrvatski/UI naziv | Definicija i granica |
|---|---|---|
| `Curriculum` | Kurikulum | Strukturirani službeni okvir očekivanog učenja. Nije popis slobodnih tagova niti individualni rezultat učenika. |
| `CurriculumOutcome` | Kurikularni ishod | Definirani očekivani ishod unutar kurikuluma s kojim se sadržaj i komponente znanja mogu povezati. |
| `LearningGoal` | Cilj učenja / meta | Namjeravani rezultat rada s materijalom, aktivnošću, grupom ili pripremom sata. Cilj ne dokazuje da je učenik rezultat ostvario. |
| `KnowledgeArea` | Područje znanja | Široka kategorija poput Grammar, Vocabulary, Reading, Listening, Speaking ili Writing. Preširoka je da bi sama zamijenila preciznu komponentu znanja. |
| `KnowledgeComponent` | Komponenta znanja | Kontrolirana i dovoljno precizna jedinica znanja ili vještine u hijerarhiji Knowledge Modela, npr. Grammar → Present Perfect → negative form. Samo kontrolirane komponente mogu izravno primati evidence signal. |
| `KnowledgeModel` | Model znanja | Strukturirani model područja, komponenti i njihovih veza s kurikulumom koji omogućuje tumačenje individualnih dokaza učenika. Nije AI model i ne zahtijeva AI za osnovni rad. |
| `MasteryEstimate` | Procjena ovladanosti | Izračunata procjena trenutačnog ovladavanja određenom komponentom ili područjem znanja na temelju više relevantnih dokaza. Nije ručno upisan postotak, readiness za ispit niti jamstvo ocjene. |
| `ReadinessEstimate` | Procjena spremnosti | Izračunata procjena spremnosti učenika za **konkretan cilj, ispit ili skup relevantnih komponenti**. Izvodi se iz Knowledge Modela i Evidence Eventa; algoritam je i dalje blokiran do ROADMAP-a 5.5. |
| `ConfidenceLevel` | Razina pouzdanosti | Pokazatelj koliko je procjena potkrijepljena količinom, relevantnošću i drugim definiranim svojstvima dokaza. Nije isto što i procijenjeni rezultat. |

## Materials, tasks and evidence

| Canonical term | Hrvatski/UI naziv | Definicija i granica |
|---|---|---|
| `Material` | Materijal | Nastavni sadržaj ili resurs, npr. prezentacija, PDF, slika, video ili interaktivni sadržaj. Samo korištenje/pregled materijala nije dokaz znanja. |
| `InstructionalMaterial` | Materijal za učenje | Materijal čija je primarna svrha poučavanje ili objašnjavanje. Može evidentirati korištenje, ali bez procjenjivog odgovora ne stvara dokaz znanja. |
| `AssessableMaterial` | Procjenjivi materijal | Materijal koji sadrži jedan ili više procjenjivih zadataka i može proizvesti standardizirane dokaze. |
| `AssessableTask` | Procjenjivi zadatak | Atomsko pitanje ili zadatak s kriterijem vrednovanja, povezan s jednom ili više Knowledge Components. Ne koristi se generički `Activity` za ovaj pojam. |
| `StudentAttempt` | Pokušaj učenika | Konkretan učenikov odgovor ili izvedba procjenjivog zadatka, s rezultatom, vremenom, pomoći i kontekstom. Pokušaj je ulaz za Evidence Event, a nije agregirana procjena znanja. |
| `EvidenceEvent` | Dokazni događaj / dokaz znanja | Standardizirani zapis pedagoški relevantnog rezultata jednog pokušaja u odnosu na Knowledge Component(s), uz metapodatke poput težine, vrste dokaza, pomoći i konteksta. Ne mijenja procjenu izravno proizvoljnim postotkom. Lifecycle i korekcije zapisa zaključavaju se u evidence fazi. |
| `EvidenceType` | Vrsta dokaza | Kontrolirana klasifikacija onoga što rezultat pokazuje, npr. prepoznavanje, razumijevanje, primjena ili produkcija. Konačan katalog zaključava se u fazi 5.4. |
| `Difficulty` | Težina zadatka | Kontrolirana procjena zahtjevnosti procjenjivog zadatka. Konačna skala i utjecaj na izračun zaključavaju se u fazama 5.4–5.5. |
| `AssistanceLevel` | Razina pomoći | Evidencija koliko je učenik bio samostalan pri pokušaju, npr. samostalno, uz manju pomoć ili uz značajnu pomoć. Konačan katalog i weighting nisu zaključani. |
| `EvidenceContext` | Kontekst dokaza | Izvorna nastavna situacija pokušaja, npr. sat, domaća zadaća, test ili samostalna vježba. Svi konteksti proizvode isti standardizirani Evidence Event contract. |
| `Tag` | Tag / oznaka | Fleksibilna oznaka za pretraživanje, organizaciju, filtriranje i preporuku sadržaja. Tag nije Knowledge Component i sam ne utječe na procjenu znanja. |
| `MaterialVersion` | Verzija materijala | Identificirana verzija sadržaja materijala potrebna za sigurno ponovno korištenje i tumačenje povezanih zadataka. Detaljna versioning pravila zaključavaju se u fazi materijala/editor fazi. |

## Supporting product concepts

| Canonical term | Hrvatski/UI naziv | Definicija i granica |
|---|---|---|
| `StudentDossier` | Digitalni dosje učenika | Jedinstveni radni prikaz učenikovih administrativnih, nastavnih, knowledge/evidence i komunikacijskih podataka. Nije paralelna kopija tih domena. |
| `Homework` | Domaća zadaća | Učeniku zadan rad izvan termina koji može koristiti materijale i procjenjive zadatke. Rezultat stvara Evidence Event samo kada postoji valjani procjenjivi pokušaj. |
| `Conversation` | Razgovor | Komunikacijska cjelina između dopuštenih sudionika. Permissions, read state, retention i attachments ostaju dokumentacijski gate faze 13. |
| `Message` | Poruka | Jedna poruka unutar razgovora. Ne koristi se kao sinonim za obavijest. |
| `Notification` | Obavijest | Sustavski signal korisniku o relevantnom događaju. Nije isto što i poruka/razgovor; kanali i pravila nisu još definirani. |
| `Report` | Izvještaj | Strukturirani prikaz ili izvoz podataka i metrika. Izvori, privatnost i export contract zaključavaju se u fazi 14. |
| `FinancialEntry` | Financijska stavka | Evidencija financijskog događaja ili obveze. Ne pretpostavlja račun, plaćanje ili porezni dokument dok se scope faze 15 ne zaključa. |

## Zaključane razlike

| Ne poistovjećavati | Zaključana razlika |
|---|---|
| `Program` ↔ `Group` | Program definira što se pohađa; grupa definira tko zajedno radi i kada. |
| `SchoolGrade` ↔ `ProficiencyLevel` | Razred je formalna školska godina; razina je kompetencijska klasifikacija poput CEFR-a. |
| `DeliveryMode` ↔ `Group` | Način rada je eksplicitno Individual ili Group; grupa je konkretna organizacijska jedinica. |
| `RegularGroupSchedule` ↔ `Session` | Redoviti raspored je trajni obrazac; termin je konkretna kalendarska instanca. |
| `Session` ↔ `LessonPlan` | Termin određuje kada/tko/gdje; priprema sata određuje plan rada. |
| `LearningActivity` ↔ `AssessableTask` | Aktivnost je šira nastavna cjelina; procjenjivi zadatak je atomska jedinica vrednovanja. |
| `KnowledgeArea` ↔ `KnowledgeComponent` | Područje je široka kategorija; komponenta je kontrolirana mjerljiva jedinica. |
| `Tag` ↔ `KnowledgeComponent` | Tag služi organizaciji; komponenta prima evidence signal. |
| `StudentAttempt` ↔ `EvidenceEvent` | Pokušaj je učenikov odgovor/izvedba; Evidence Event je standardizirani dokaz izveden iz pokušaja. |
| `MasteryEstimate` ↔ `ReadinessEstimate` | Mastery se odnosi na znanje komponente; readiness na spremnost za konkretan cilj/ispit. |
| `Message` ↔ `Notification` | Poruka je dio razgovora; obavijest je sustavski signal. |

## Otvorene granice koje glossary namjerno ne rješava

- authentication, vrste računa i permissions model
- kardinalnost i vremenska valjanost veze Student–Program–Group
- model redovitog rasporeda, serije termina i iznimki
- algoritam za MasteryEstimate, ReadinessEstimate i ConfidenceLevel
- konačni katalozi Difficulty, EvidenceType, AssistanceLevel i EvidenceContext
- file storage politika i podržani formati
- pravila kasnijih modula koji imaju dokumentacijski gate

Te granice ostaju u `OPEN_QUESTIONS.md` i pripadaju svojim ROADMAP fazama.
