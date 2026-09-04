# Source package audit 2026 09 04

## Zaključak

Dostavljeni paket proširuje projekt u četiri područja koja prethodni `docs` nije potpuno
obuhvaćao: studentska aplikacija, DS-001 Design System, detaljni Lesson Builder s 25
Knowledge Blockova te master sitemap C i stariji bazni cross-role dokumenti. Postojeći
učiteljski screen source uglavnom se preklapa s ranijim source refreshom i nije ponovno
kopiran preko postojećih snapshotova.

Ovaj audit ne mijenja dovršene implementacijske statuse, prihvaćene ADR-ove ni zaključane
security/domain contracte. Vanjske oznake `ODOBRENO` i `DRAFT` bilježe status izvora;
ne predstavljaju automatsko odobrenje promjene koda.

## Pregled paketa

| Folder | DOCX | PNG | Ostalo | Što je novo za projektni docs |
|---|---:|---:|---:|---|
| `Za programera - novo` | 147 | 122 | 1 XLSX, 1 TXT | cijela studentska aplikacija, teacher master sitemapovi i detaljnije settings consequence mape |
| `Design System` | 1 | 1 | 0 | DS-001 v1.0 i UI kit |
| `Priprema sata` | 27 | 5 | 0 | Lesson Builder AI specifikacija i KB-001–KB-025 |
| `Baza Ekrana` | 3 | 5 | 0 | bazni FS-001 dashboard, FS-002 digitalni dosje i FS-003 PLUS 5 Ploča |
| **Ukupno** | **178** | **133** | **2** | **313 datoteka** |

Uspješno je tekstualno pregledano 177 od 178 DOCX datoteka i radni list iz XLSX-a.
`UČITELJ/12.0 Postavke/12.3 Postavke nastave/12.3.1 Trajanje i način rada/12.3.1 Trajanje i način rada.docx`
nije valjan DOCX paket (`File is not a zip file`). Povezana mapa posljedica postoji,
ali sadržaj neispravne datoteke nije rekonstruiran niti nagađan.

Vizualni skup čini 78 Teacher PNG-ova, 44 Student PNG-a, jedan DS-001 UI kit, pet
Lesson Builder PNG-ova i pet baznih FS PNG-ova. Tako se svih 133 vizuala može vezati uz
odgovarajući izvorni ekran ili cross-role referencu bez kopiranja binarnih datoteka u Git.

## Selektivni merge

- `source_specs/STUDENT_APPLICATION_SITEMAP.md` — novi student-facing modul i 44 detaljna ekrana
  uz pripadajuće site-map dokumente.
- `source_specs/DESIGN_SYSTEM_DS001.md` — pravila DS-001 i zabilježene razlike prema
  postojećim frontend tokenima.
- `source_specs/LESSON_BUILDER_KNOWLEDGE_BLOCK_CATALOG.md` — DRAFT Lesson Builder i
  katalog 25 pedagoških blokova.
- `source_specs/CROSS_ROLE_BASELINE.md` — master sitemap C i bazni FS dokumenti koji
  povezuju Teacher, Student, Group, Session, Evidence, Homework, Materials, Messaging,
  Reports, Finance i Settings.
- `ROADMAP.md` — dodan DS alignment gate, preciziran Phase 9, dodana studentska aplikacija
  kao Phase 17, a release hardening pomaknut u Phase 18.
- `OPEN_QUESTIONS.md` — dodani guardian/minor, Student–Teacher odnos, commercial/payment,
  messaging/moderation, account lifecycle i Teacher marketplace gateovi iz XLSX-a.
- `SCREEN_SPEC_STATUS.md`, `SOURCE_DOCUMENT_INDEX.md`, `DOCUMENTATION_MANIFEST.md` i
  `FRONTEND_FOUNDATION.md` — prošireni statusi i pravila izvora.

## Što nije uvezeno kao projektni source of truth

- 122 teacher/student PNG-a, UI kit i pet dodatnih PNG-a ostaju canonical vizualni izvori
  na dostavljenoj lokaciji; nisu duplicirani u Git samo radi dokumentacijskog mergea.
- Raniji teacher snapshotovi nisu prepisani jer postoje u `source_specs/` i već su povezani
  s ROADMAP-om.
- Emoji statusi, primjer postoci, procijenjene ocjene i AI „učenje iz odluka” nisu pretvoreni
  u domenske ili podatkovne contracte bez privacy, explainability i Evidence odluka.
- Otvorene teme iz XLSX-a nisu proglašene zahtjevima. One blokiraju pripadajuće faze dok
  vlasnik proizvoda ne donese odluku.
- Ovaj zadatak ne implementira ekrane i ne mijenja bazu, API ni frontend.

## Izvorne lokacije

- `C:/Users/arodr/Downloads/5/Plus 5 aplikacija/Za programera - novo/`
- `C:/Users/arodr/Downloads/5/Plus 5 aplikacija/Design System/`
- `C:/Users/arodr/Downloads/5/Plus 5 aplikacija/Priprema sata/`
- `C:/Users/arodr/Downloads/5/Plus 5 aplikacija/Baza Ekrana/`

Lokacije su lokalni provenance zapis. Za budući dugoročno reproducibilan archive potrebno
je zasebno odlučiti hoće li se originalni binarni paket verzionirati izvan Git repozitorija.
