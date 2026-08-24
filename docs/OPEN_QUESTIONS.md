# OPEN_QUESTIONS

Ovo nisu pitanja koja AI smije sam riješiti pretpostavkom. Svako pitanje koje utječe na trajni business contract mora dobiti odluku prije odgovarajuće implementacijske faze.

## Blocking

1. ~~Koji je službeni tehnološki stack i ciljane verzije?~~ **RIJEŠENO 2026-08-23:** React + TypeScript + Vite frontend; C# + ASP.NET Core/.NET 10 backend; SQL Server + EF Core; Docker. Vidi `ARCHITECTURE_BASELINE.md` i ADR-0001–0005.
2. ~~Koji je detaljni authentication model, vrste korisnika i permissions model?~~ **RIJEŠENO 2026-08-24:** samo Teacher ima account u Phase 1.6; javna Teacher registracija s potvrdom e-maila; Student/Guardian bez accounta u ovoj fazi; nema Administrator rolea; revocable secure cookie auth i deny-by-default ownership authorization. Vidi `AUTHENTICATION_REQUIREMENTS.md`, `AUTHENTICATION_ARCHITECTURE.md` i ADR-0009.
3. `4.1 Biblioteka materijala.docx` je 0 B. Potrebno je obnoviti/ponovno spremiti opis 4.1.
4. Koji je točan algoritam readiness procjene iz Evidence Eventa (weighting, decay, broj dokaza, confidence, thresholds)?
5. Kako se modeliraju redoviti termini grupe naspram konkretnih instanci termina i promjena serije?
6. Koja je politika pohrane datoteka/materijala, maksimalne veličine i podržani formati?
7. Koja je granica AI funkcionalnosti u prezentacijama/lesson builderu i mora li učitelj potvrditi svaki AI prijedlog prije objave/korištenja?

## Dokumentacijski nedostaci prije kasnijih modula

- detaljni 4.4 i 4.5
- 5.1–5.7 Lesson Builder
- 6.1–6.5 PLUS 5 Ploča
- 7.1–7.2 Povijest sati
- 8.1–8.3 Domaće zadaće
- 9.1–9.2 Poruke
- 10.1–10.9 Izvještaji
- 11.1–11.3 Financije
- 12.x Postavke
- 13.1 Obavijesti
- 14.x Profil/Auth

## Operativne odluke prije produkcijskog releasea

- odabrati produkcijski SMTP provider, verificiranu sender domenu/adresu, SPF/DKIM/DMARC postavke i secret provisioning; Phase 1.6 zadržava provider-neutralni TLS SMTP adapter i lokalni capture contract

## Pravilo

Kada se pitanje riješi, odluka se prebacuje u odgovarajući source-of-truth dokument i po potrebi u `DECISION_LOG.md`; ovo pitanje se označava riješenim.
