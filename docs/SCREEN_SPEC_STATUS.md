# SCREEN_SPEC_STATUS

Ovaj dokument je gate protiv AI-haluciniranja nedokumentiranih funkcionalnosti.

| Modul | Ekrani | Status specifikacije | Implementacijski status dopuštenosti |
|---|---|---|---|
| Radni stol | 1.1 | detaljan DOCX + vizual | može u implementaciju nakon foundation faza |
| Učenici | 2.1–2.9 | detaljni DOCX + vizuali | može u implementaciju nakon domain/data foundationa |
| Knowledge / readiness | 2.4–2.5 | detaljan funkcionalni opis + posebna logika | zahtijeva prethodno zaključan Knowledge/Evidence model |
| Raspored | 3.1–3.4 | detaljni DOCX + vizuali | može nakon Student/Group foundationa |
| Materijali | 4.1 | novi detaljni source + postojeći PNG | stari 0 B blocker riješen; može nakon Material storage/permissions foundationa |
| Materijali | 4.2 | detaljan opis | može nakon Material + Knowledge metadata modela |
| Materijali | 4.3 Prezentacija | detaljan opis | može nakon Material foundationa; editor treba zaseban tehnički dizajn |
| Materijali | 4.4–4.5 | detaljni import/edit/versioning source specovi | nakon Material storage, versioning, permissions i AI-confirmation contracta |
| Priprema sata | 5.1–5.6 | detaljni Lesson Builder source specovi | screen-flow gate riješen; zahtijeva Lesson Plan/Activity i Knowledge/Material contracte |
| PLUS 5 Ploča | 6.1–6.5 | detaljni runtime/lifecycle source specovi | screen-flow gate riješen; zahtijeva Lesson Session/Evidence architecture |
| Sati/Povijest | 7.1–7.2 | detaljni source specovi | može nakon Lesson Session persistencea i povijesnog version contracta |
| Domaće zadaće | 8.1–8.3 | detaljni source specovi | zahtijeva Homework/Evidence contract i participant access pravila |
| Poruke | 9.1–9.2 | detaljni source specovi | zahtijeva participant/permission/retention/delivery contract |
| Izvještaji | 10.1–10.9 | detaljni source specovi | zahtijeva metric definitions/privacy/export/Report Snapshot contract |
| Financije | 11.1–11.3 | detaljni source behavior | zahtijeva formalni finance/tax/invoice/fiscalization boundary |
| Postavke | 12.1–12.7 | detaljne mape posljedica | audit single-source-of-truth i MVP rez ostaju obavezni |
| Obavijesti | 13.1 | detaljni event/read-resolved model | zahtijeva notification delivery/retention contract |
| Profil/Auth | 14.1–14.3 | screen source + zaključani auth contract | Phase 1.6 auth contract ima prednost |
| Master sitemap/audit | cross-module | početni master sitemap izveden | završni functional audit i MVP rez nisu zaključani |
| DS-001 | cross-platform | odobreni DS-001 source + UI kit | alignment prema postojećim tokenima i PNG-ovima obavezan prije sljedećeg UI featurea |
| Lesson Builder KB katalog | KB-001–KB-025 | detaljni DRAFT pedagoški opisi | zahtijeva formalni Lesson Plan/Activity/Knowledge/Evidence/AI/privacy contract |
| Student aplikacija | 1.1, 2.1–10.4 | 44 detaljna screen DOCX/PNG para + 10 master/modulskih site-mapova | blokirano dok se ne zaključe Student/Guardian auth, minor consent, permissions i shared-domain contracti |
| Cross-role baseline | FS-001–FS-003, sitemap C1–C11 | business intent i lifecycle mape | koristiti kao input; noviji ADR/security/phase contracti imaju prednost kod konflikta |

## Pravilo statusa

“Mapa ekrana” nije dovoljna za produkcijsku implementaciju featurea koji stvara ili mijenja trajne podatke. Prije takve faze moraju biti definirani barem business rules, permissions, states/transitions, validation, error behavior i acceptance kriteriji.

Oznake `ODOBRENO` ili `DRAFT` unutar vanjskog DOCX-a opisuju status tog izvora. Ne
preskaču projektni documentation, architecture, security, test i visual acceptance gate.
