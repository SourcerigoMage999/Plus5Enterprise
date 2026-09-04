# Student application sitemap

## Status

`SOURCE-DERIVED — 2026-09-04`. Izvedeno iz `UČENIK/Master site map A.docx`, modulskih
site-mapova i 44 detaljna screen DOCX/PNG para. Student/Guardian account, permissions,
minor consent, payment i messaging pravila nisu zaključana.

## Moduli i ekrani

| Modul | Ekrani iz paketa |
|---|---|
| 1 Početna | 1.1 Početna; izvor navodi da još nije finalizirana |
| 2 Učenje self study | 2.1 Moje učenje; 2.2 Odaberi što želiš učiti; 2.3 Odabir teme; 2.4 Pregled learning cjeline; 2.5 Aktivnost zadatak; 2.6 Rezultat aktivnosti; 2.7 Završetak learning cjeline; 2.8 Priprema za sljedeći ispit |
| 3 Moji sati | 3.1 Moji sati; 3.2 Detalji sata; 3.3 Pridruži se satu; 3.4 PLUS 5 Ploča sat uživo; 3.5 Završetak sata; 3.6 Detalji završenog sata; 3.7 Pregled završenih sati |
| 4 Domaće zadaće | 4.1 Moje domaće zadaće; 4.2 Pregled; 4.3 Rješavanje; 4.4 Rezultat; 4.5 Završena zadaća; 4.6 Pregled završenih zadaća |
| 5 Treba mi pomoć | 5.1 Treba mi pomoć; 5.2 Opis poteškoće; 5.3 PLUS 5 prijedlog pomoći; 5.4 Pronađi učitelja; 5.5 Odabir i potvrda termina; 5.6 Pomoć dogovorena |
| 6 Moj napredak | 6.1 Moj napredak; 6.2 Moje znanje; 6.3 Detalji teme; 6.4 Moji rezultati; 6.5 Moj razvoj |
| 7 Učitelji | 7.1 Učitelji; 7.2 Profil učitelja; 7.3 Pronađi učitelja; 7.4 Odabir i potvrda termina |
| 8 Poruke | 8.1 Poruke; 8.2 Razgovor |
| 9 Profil | 9.1 Moj profil |
| 10 Postavke | 10.1 Postavke; 10.2 Obavijesti; 10.3 Račun i sigurnost; 10.4 Pretplata i plaćanja |

## Otvorene teme iz izvornog XLSX-a

Prioritet izvora: finalizirati studentsku početnu, zatim P-01 roditelj/skrbnik, BR-ST-01
Student–Teacher odnos, BR-PAY-01 plaćanja, BR-MSG-01 komunikacija/moderiranje, BR-ACC-01
account lifecycle te BR-TM-01/02 reputacija i verifikacija učitelja. Notification Center
iza zvona naveden je kao manji UX zadatak.

Ovo je backlog/redoslijed iz izvora, ne zaključana odluka. Posebno P-01 mora prethoditi
plaćanju i rezervaciji za maloljetnika.

## Granice implementacije

Trenutačni Phase 1.6 dopušta samo Teacher account. Student i Guardian nemaju login niti
permissions contract. Nijedan student-facing ekran ne smije u implementaciju prije nove
auth/authorization odluke, parental-consent modela, ownership/visibility pravila i
privacy threat reviewa. Self-study/progress ovise o Knowledge/Evidenceu; live lesson o
Lesson Sessionu; homework, messaging, discovery/booking i payments o vlastitim gateovima.
