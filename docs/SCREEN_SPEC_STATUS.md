# SCREEN_SPEC_STATUS

Ovaj dokument je gate protiv AI-haluciniranja nedokumentiranih funkcionalnosti.

| Modul | Ekrani | Status specifikacije | Implementacijski status dopuštenosti |
|---|---|---|---|
| Radni stol | 1.1 | detaljan DOCX + vizual | može u implementaciju nakon foundation faza |
| Učenici | 2.1–2.9 | detaljni DOCX + vizuali | može u implementaciju nakon domain/data foundationa |
| Knowledge / readiness | 2.4–2.5 | detaljan funkcionalni opis + posebna logika | zahtijeva prethodno zaključan Knowledge/Evidence model |
| Raspored | 3.1–3.4 | detaljni DOCX + vizuali | može nakon Student/Group foundationa |
| Materijali | 4.1 | PNG postoji; DOCX je 0 B | **BLOCKED za punu implementaciju dok se opis ne obnovi** |
| Materijali | 4.2 | detaljan opis | može nakon Material + Knowledge metadata modela |
| Materijali | 4.3 Prezentacija | detaljan opis | može nakon Material foundationa; editor treba zaseban tehnički dizajn |
| Materijali | 4.4–4.5 | samo mapa ekrana | dokumentacijski gate prije implementacije |
| Priprema sata | 5.1–5.7 | mapa ekrana | dokumentacijski gate prije implementacije |
| PLUS 5 Ploča | 6.1–6.5 | mapa ekrana | dokumentacijski gate prije implementacije |
| Sati/Povijest | 7.1–7.2 | mapa ekrana | dokumentacijski gate prije implementacije |
| Domaće zadaće | 8.1–8.3 | mapa ekrana | dokumentacijski gate prije implementacije |
| Poruke | 9.1–9.2 | mapa ekrana | dokumentacijski gate prije implementacije |
| Izvještaji | 10.1–10.9 | mapa ekrana | dokumentacijski gate prije implementacije |
| Financije | 11.1–11.3 | mapa ekrana | dokumentacijski gate prije implementacije |
| Postavke | 12.1–12.6 | mapa ekrana | dokumentacijski gate prije implementacije |
| Obavijesti | 13.1 | mapa ekrana | dokumentacijski gate prije implementacije |
| Profil/Auth | 14.1–14.3 | mapa ekrana | auth requirements moraju biti posebno definirani prije implementacije |

## Pravilo statusa

“Mapa ekrana” nije dovoljna za produkcijsku implementaciju featurea koji stvara ili mijenja trajne podatke. Prije takve faze moraju biti definirani barem business rules, permissions, states/transitions, validation, error behavior i acceptance kriteriji.
