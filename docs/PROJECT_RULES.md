# PROJECT_RULES

## 1. Source of truth

1. Markdown dokumentacija u ovom paketu ima prednost nad postojećim kodom.
2. Izvorni DOCX/PNG mockupovi služe kao poslovni i vizualni izvor; njihove Markdown konverzije služe za AI čitljivost.
3. Ako dvije specifikacije proturječe jedna drugoj, AI ne smije proizvoljno odabrati jednu. Kontradikcija se zapisuje u `OPEN_QUESTIONS.md` i implementira se samo nesporni dio.

## 2. Rad po fazama

1. Radi se točno jedna ROADMAP podfaza po zadatku.
2. Faza N+1 ne počinje dok faza N nema zadovoljene acceptance kriterije.
3. Zabranjeno je unaprijed uvoditi modele, API-je, tablice, servise ili UI flowove koji pripadaju budućoj fazi, osim minimalnog tehničkog preduvjeta eksplicitno navedenog u ROADMAP-u.
4. Svaka faza mora imati jasno: cilj, scope, out-of-scope, ulazne dokumente, deliverablee, acceptance kriterije i testove.

## 3. Arhitektura

1. AI se ponaša kao senior solution architect + senior developer + reviewer.
2. Prije koda provjerava utjecaj na domenu, podatke, sigurnost, API, frontend, testove i migracije.
3. Ne uvodi novi framework, biblioteku, pattern ili infrastrukturni servis bez opravdanja i zapisa u `DECISION_LOG.md`.
4. Ne stvara “god classes”, skrivene globalne ovisnosti niti poslovnu logiku u UI komponentama ili kontrolerima.
5. Domenski pojmovi i nazivi moraju ostati konzistentni kroz bazu, backend, API i frontend.


## 3.1 Obavezni tehnički standardi

Kada podfaza dodiruje odgovarajuće područje, AI/senior mora primijeniti:

- `ARCHITECTURE_BASELINE.md`
- `DATABASE_DESIGN_STANDARD.md`
- `BACKEND_ENGINEERING_STANDARD.md`
- `FRONTEND_ENGINEERING_STANDARD.md`
- `SECURITY_ENGINEERING_STANDARD.md`
- `DOCKER_DEPLOYMENT_STANDARD.md`
- `TESTING_QUALITY_STANDARD.md`
- `ENGINEERING_CHECKLIST.md`

Ovi dokumenti su obavezni engineering constraints. Ako business/source specifikacija zahtijeva ponašanje koje je u konfliktu sa sigurnošću ili integritetom podataka, implementacija se ne nagađa: konflikt se evidentira i traži se eksplicitna odluka.

## 4. Implementacijski postupak

Za svaku podfazu AI mora:

1. pročitati obavezne dokumente i relevantne source specove
2. pregledati postojeći kod koji podfaza dodiruje
3. navesti granicu podfaze
4. implementirati samo taj scope
5. dodati/izmijeniti testove
6. pokrenuti build i relevantne testove
7. popraviti pronađene probleme unutar scopea
8. napraviti self-review
9. ažurirati ROADMAP status
10. napisati phase summary prema `PHASE_SUMMARY_TEMPLATE.md`

## 5. Definition of Done

Podfaza nije dovršena dok nisu zadovoljeni svi primjenjivi uvjeti:

- kod se kompajlira/build prolazi
- automatski testovi prolaze
- nema poznatog regressiona u postojećem scopeu
- error/empty/loading stanja postoje gdje su potrebna
- autorizacija i validacija postoje gdje su potrebne
- podatkovne migracije su provjerene ako ih podfaza uvodi
- dokumentacija je usklađena s implementacijom
- phase summary je zapisan

## 6. Summary je obavezan

Svaki summary mora sadržavati barem:

- naziv i ID faze
- što je implementirano
- što namjerno nije implementirano
- popis izmijenjenih/dodanih datoteka
- migracije i promjene sheme
- API contract promjene
- testove i rezultat izvršavanja
- arhitekturne odluke
- poznate rizike / otvorena pitanja
- preciznu početnu točku za sljedeću fazu

## 7. Zabranjeno

AI ne smije:

- označiti fazu dovršenom bez provjere
- izmisliti ekran ili ponašanje koje specifikacija ne definira
- preskočiti dokumentacijski gate
- refaktorirati nevezani kod “usput”
- mijenjati zaključanu poslovnu odluku bez eksplicitne odluke vlasnika proizvoda
- tvrditi da je test pokrenut ako nije pokrenut
