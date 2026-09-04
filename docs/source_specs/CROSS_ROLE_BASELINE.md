# Cross role baseline source

## Izvori

- teacher master sitemap A/B/C i C1–C11 lifecycle dokumenti
- FS-001 Radni stol učitelja, FS-002 Digitalni dosje učenika, FS-003 PLUS 5 Ploča
- pripadajući PNG-ovi iz `Baza Ekrana`

Dokumenti su izvori business namjere. Oznaka `ODOBRENO ZA RAZVOJ` u starijim FS
dokumentima ne nadjačava novije projektne ADR-ove, sigurnosne standarde, fazne contracte
ni zabranu lažnih podataka.

## Master sitemap C domene

1. Učenik
2. Grupa
3. Termini i održavanje termina
4. Znanje i dokazi znanja
5. Ciklus domaće zadaće
6. Ciklus radnog materijala
7. Procjena spremnosti učenika
8. Komunikacija i poruke
9. Ciklus izvještaja
10. Ciklus ponuda i financija
11. Postavke

Mapa potvrđuje da Teacher i Student sučelja dijele iste canonical business entitete i
lifecyclee. Ne stvaraju se paralelni Student, Group, Session, Homework, Material ili
Message modeli samo zato što ih druga uloga prikazuje drugim ekranom.

## Bazne funkcionalne namjere

FS-001 želi da Teacher dashboard u manje od minute pokaže današnje instrukcije,
pripremljenost materijala, prijedlog rada, 3–5 brzih akcija, očekivanu dnevnu zaradu,
tjedni raspored i važne obavijesti. AI prijedlog je objašnjen i pod kontrolom Teachera.

FS-002 vidi digitalni dosje kao objedinjeni read model učenika: profil, sljedeći ispit,
readiness, idući plan i materijali, zadnji sat, napredak, stručna analiza, povijest,
komunikacija i privatne bilješke. Procjene moraju imati dokaz i ne smiju jamčiti rezultat.

FS-003 definira Ploču kao Lesson Session workspace, ne samo whiteboard: Teacher upravlja,
Student vidi samo odobreni sadržaj, pokušaji nastaju tijekom rada, AI je u pozadini,
grupni učenici ne vide tuđe zadatke/rezultate, a slanje homeworka ili poruke zahtijeva
Teacher potvrdu.

## Konflikti i zaštitne ograde

- Bazni dokumenti spominju budući Parent/Administrator prikaz, ali Phase 1.6 zaključava
  samo Teacher account; nove uloge zahtijevaju odluku.
- „AI uči iz odluka” nije dopušteno implementirati prije definiranja consent/privacy,
  data minimization, retention, explainability i provider contracta.
- Readiness, očekivane ocjene i postoci ostaju blokirani do formalnog Evidence modela.
- Financijska vrijednost na dashboardu ovisi o zaključanom finance contractu.
- Student live view zahtijeva novu real-time/session authorization arhitekturu.
