# OPEN_QUESTIONS

Ovo nisu pitanja koja AI smije sam riješiti pretpostavkom. Svako pitanje koje utječe na trajni business contract mora dobiti odluku prije odgovarajuće implementacijske faze.

## Blocking

1. ~~Koji je službeni tehnološki stack i ciljane verzije?~~ **RIJEŠENO 2026-08-23:** React + TypeScript + Vite frontend; C# + ASP.NET Core/.NET 10 backend; SQL Server + EF Core; Docker. Vidi `ARCHITECTURE_BASELINE.md` i ADR-0001–0005.
2. ~~Koji je detaljni authentication model, vrste korisnika i permissions model?~~ **RIJEŠENO 2026-08-24:** samo Teacher ima account u Phase 1.6; javna Teacher registracija s potvrdom e-maila; Student/Guardian bez accounta u ovoj fazi; nema Administrator rolea; revocable secure cookie auth i deny-by-default ownership authorization. Vidi `AUTHENTICATION_REQUIREMENTS.md`, `AUTHENTICATION_ARCHITECTURE.md` i ADR-0009.
3. ~~`4.1 Biblioteka materijala.docx` je 0 B.~~ **RIJEŠENO 2026-09-01:** novi teacher source detaljno opisuje 4.1 kroz `source_specs/4.1_Biblioteka_materijala.md`.
4. Koji je točan algoritam readiness procjene iz Evidence Eventa (weighting, decay, broj dokaza, confidence, thresholds)?
5. ~~Kako se modeliraju redoviti termini grupe naspram konkretnih instanci termina i promjena serije?~~ **RIJEŠENO 2026-08-28:** versioned weekly `RecurringSessionSeries`, materialized `Session`, one-occurrence exception i successor-series contract zaključani su u `SCHEDULING_FOUNDATION.md` i ADR-0013.
6. Koja je politika pohrane datoteka/materijala, maksimalne veličine i podržani formati?
7. Koja je granica AI funkcionalnosti u prezentacijama/lesson builderu i mora li učitelj potvrditi svaki AI prijedlog prije objave/korištenja?
8. Koji je formalni `KnowledgeModel` / `KnowledgeComponent` / `KnowledgeBlock` / `EvidenceEvent` lifecycle i correction contract?
9. Koji je finalni participant/permission/retention/delivery contract za Poruke i privitke?
10. Koje su finalne metric definitions, privacy, export/PDF i immutable `ReportSnapshot` politike za Izvještaje?
11. Je li Finance samo interna evidencija ili uključuje payment processing, račune, fiskalizaciju ili porezne obveze; koji su currency/precision contracti?
12. Koji je notification event/delivery/retention contract i koji kanali postoje izvan web centra?
13. Audit postavki 12.1–12.7: potvrditi da svaka poslovna odluka ima jedan source of truth.
14. Završni functional audit i MVP rez: označiti featuree kao MVP / nakon MVP-a / kasnije.
15. **P-01 Student/Guardian i maloljetnici:** tko daje privolu, upravlja računom, vidi
    napredak, odobrava rezervaciju/otkazivanje i smije kupiti uslugu za maloljetnika?
16. **BR-ST-01 Student–Teacher odnos:** kako nastaje i prestaje, dopušta li više Teachera,
    koji se podaci dijele te kako rade referral, block i povlačenje pristupa?
17. **BR-PAY-01 commercial/payment:** subscription, credits/paketi sati, provizija,
    payment provider, refund, otkazivanje/no-show, failed payment, obnova, grace period,
    račun/billing dokument i porezna/fiskalna granica.
18. **BR-MSG-01 cross-role komunikacija:** tko kome smije pisati, Guardian vidljivost,
    block/report, moderiranje, kontaktni podaci, privitci i životni vijek razgovora?
19. **BR-ACC-01 Student account lifecycle:** registracija/verifikacija/recovery,
    dobna granica, deaktivacija, brisanje, retention, anonimizacija i Guardian control?
20. **BR-TM-01/02 Teacher marketplace:** postoje li recenzije/reputation te što znači
    verificirani Teacher, tko verificira i koji dokaz/status lifecycle vrijedi?
21. Kako DS-001 `#f8b91b` / `#0f4d80`, žuti primary action i narančasti warning mapirati
    na postojeće tokene i canonical PNG-ove bez regresije dovršenih ekrana?
22. Koji su privacy, audit i model-improvement contracti za Lesson Builder tvrdnju da AI
    pamti Teacherove prihvaćene, uređene i odbijene prijedloge?

## Preostali feature/domain gateovi prije kasnijih modula

- prije Program management UI/API-ja zaključati rename/status/archive/delete lifecycle i permissions
- prije prvog unosa stvarnih referentnih podataka odobriti SchoolGrade, ProficiencyLevel framework i Curriculum katalog/import source
- prije Phase 5.1 zaključati CurriculumOutcome hijerarhiju, službene identifikatore, vremensku valjanost i mapiranje na Knowledge Model

- Materials: storage, format/size/upload security, ownership/sharing, versioning i AI-confirmation contract
- Lesson Builder: formalni Lesson Plan, Activity Template i Lesson Activity domain contract
- PLUS 5 Ploča: Lesson Session persistence, autosave/recovery i Evidence emission/invalidation contract
- Povijest sati: immutable historical Task/Material version references i void/audit semantics
- Domaće zadaće: Assignment/Submission, participant access, versioning i Evidence contract
- Poruke: participants, permissions, private broadcast replies, attachments, retention i delivery
- Izvještaji: metric definitions, insufficient-data behavior, privacy, export i Report Snapshot
- Financije: internal ledger naspram invoice/payment/tax/fiscalization boundary
- Postavke: 12.1–12.7 single-source-of-truth audit
- Obavijesti: event/read-resolved/delivery/retention contract
- Profil/Auth: zaključani Phase 1.6 contract ima prednost; novi account tipovi i MFA zahtijevaju novu odluku

Detaljni screen/lifecycle source sada postoji za 4.1, 4.4–4.5, 5.1–5.6, 6.1–6.5, 7.1–7.2, 8.1–8.3, 9.1–9.2, 10.1–10.9, 11.x, 12.x, 13.1 i 14.x. Screen dokumentacija zato više nije blocker; gore navedeni business/technical contracti jesu.

## Operativne odluke prije produkcijskog releasea

- odabrati produkcijski SMTP provider, verificiranu sender domenu/adresu, SPF/DKIM/DMARC postavke i secret provisioning; Phase 1.6 zadržava provider-neutralni TLS SMTP adapter i lokalni capture contract
- zaključati Student/Guardian retention, pravni zahtjev za erasure te anonimizaciju povezanih povijesnih zapisa prije production delete funkcije

## Odgođene Student odluke

- odobriti kontrolirani Gender katalog ili potvrditi trajni free-text contract prije nego se taj podatak koristi za filtere, izvještaje ili automatizaciju
- ~~Group faza mora zaključati vremensku valjanost i kardinalnost `GroupMembershipa` te atomarno provoditi pravilo da `DeliveryMode.Group` ima aktivno članstvo.~~ **RIJEŠENO 2026-08-25:** vremenski interval, najviše jedno aktivno članstvo, same-Teacher composite FK i transakcijski Student/Group rowversion contract zaključani su u `GROUP_FOUNDATION.md` i ADR-0012.
- fotografija učenika ostaje blokirana općom file-storage politikom i ne sprema se kao URL/path pretpostavka u Student foundationu
- novi student-facing source ne mijenja Teacher-only Phase 1.6: prije Student logina
  riješiti pitanja 15–20, prijetnje cross-role IDOR-a, consent i guardian access/revocation

## Odgođene Group odluke

- prije Group edit API/UI-ja odlučiti ponašanje promjene Group Programa kada postoje aktivni članovi: atomarna promjena Student Programa, završetak članstava ili odbijanje promjene
- minimalni broj učenika, draft lifecycle i pravo brisanje grupe nisu definirani; foundation koristi samo pozitivan maksimalni capacity i arhiviranje

## Odgođene Schedule odluke

- odabrati bounded recurrence preview/materialization horizon i replenishment cadence prije Phase 4 application implementacije
- definirati smije li Teacher svjesno overrideati conflict upozorenje i pod kojim audit pravilima
- arbitrary recurrence/overnight, shared room permissions, reminders i notification delivery ostaju zasebni gateovi

## Pravilo

Kada se pitanje riješi, odluka se prebacuje u odgovarajući source-of-truth dokument i po potrebi u `DECISION_LOG.md`; ovo pitanje se označava riješenim.
