# Lesson Builder i Knowledge Block katalog

## Status izvora

Izvorni `Lesson Builder AI integracija u app.docx` i KB-001–KB-025 dokumenti označeni su
`DRAFT v1.0`. Oni proširuju budući Phase 9, ali ne zatvaraju formalni Lesson Plan,
Knowledge Block, Evidence, AI/privacy ili provider contract.

## Temeljna pravila

Lesson Builder kreće od cilja sata, a ne od aktivnosti. Povezuje stvarno dostupne podatke
o učeniku/grupi, Knowledge Model, prethodne sate, trajanje, temu, domaću zadaću, Teacher
preferencije i materijale. Ne smije pretpostaviti podatak koji ne postoji.

AI predlaže najmanji smisleni skup blokova, redoslijed, trajanje, metode i materijale.
Učitelj može prihvatiti, urediti, presložiti, skratiti, proširiti ili odbaciti prijedlog.
Bez jasnog cilja plan se ne generira. Sadržaj se ne prikazuje učeniku niti šalje bez
učiteljeve odluke.

## Katalog 25 blokova

| ID | Naziv |
|---|---|
| KB-001 | Small Talk |
| KB-002 | Aktivacija predznanja |
| KB-003 | Pregled domaće zadaće |
| KB-004 | Objašnjenje novog gradiva |
| KB-005 | Interaktivni zadaci |
| KB-006 | Mini provjera razumijevanja |
| KB-007 | Završna refleksija |
| KB-008 | Speaking |
| KB-009 | Listening |
| KB-010 | Reading |
| KB-011 | Writing |
| KB-012 | Vocabulary Practice |
| KB-013 | Grammar Practice |
| KB-014 | Revision |
| KB-015 | Domaća zadaća |
| KB-016 | Procjena znanja |
| KB-017 | Rad na pogreškama |
| KB-018 | Motivacija učenika |
| KB-019 | Projektni zadatak |
| KB-020 | Suradnički zadatak |
| KB-021 | Edukativna igra |
| KB-022 | Simulacija stvarne situacije |
| KB-023 | Izazov |
| KB-024 | Priča |
| KB-025 | Misija |

Svaki izvorni KB dokument opisuje svrhu, problem, pedagoški cilj, kada ga AI predlaže ili
ne predlaže, Teacher/Student prikaz, akcije, podatke koji nastaju i veze s Knowledge
Modelom. Katalog je pedagoška taksonomija; nije dopušteno iz njega unaprijed izvesti
tablice, scoring, automatsko Evidence ažuriranje ili model-trening ponašanje.

## Obavezni gateovi prije implementacije

- formalni lifecycle i verzioniranje Lesson Plan / Activity Template / Lesson Activity
- razlika Knowledge Blocka od Knowledge Componenta i materijala
- podržani inputi, missing-data ponašanje i explainability svake preporuke
- pravilo potvrde AI prijedloga, regeneracija, audit i poništavanje
- privacy/retention za interese, motivaciju, bilješke i „učenje iz odluka učitelja”
- Evidence emission/correction i zabrana izravnog ažuriranja Knowledge Modela bez contracta
- vrijeme: min/optimal/max, collision pravila i ponašanje nakon ručne promjene
- group fairness i individualni rezultat bez otkrivanja podataka drugog učenika
- material generation/storage/licensing/moderation i provider boundary

## Vizualni izvori

Pet PNG-a u `Priprema sata/` prikazuju pripremu, aktivnosti na ploči, biblioteku,
edukacijski izvještaj i pripremu u profilu učenika. Koriste se za visual acceptance
odgovarajuće buduće faze, zajedno s canonical 5.1–5.6 teacher PNG-ovima.
