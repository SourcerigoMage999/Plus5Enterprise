# Teacher dashboard screen

## Status

**Phase 8.2 — FINAL LOCKED — odobreno 2026-10-26.**

Mjerodavni izvori su `source_specs/Radi_stol_uU010ditelja.md`, canonical
`Radni stol učitelja.png`, `TEACHER_DASHBOARD_READ_MODEL.md`, frontend standardi i DS-001.
Ekran ne proširuje Phase 8.1 business contract.

## Route i podaci

Teacherov početni route `/` koristi isključivo:

```http
GET /api/v1/dashboard
```

UI ne šalje Teacher ID, ne spaja tuđe podatke i ne računa novu business metriku. Prikazuje
loading, retryable error i legitimna empty stanja za raspored, sljedeći sat i Student pregled.

## Prikazane cjeline

- pozdrav bez izmišljenog osobnog imena;
- šest canonical quick-action kartica;
- današnji raspored s lokalnim vremenom, Student/Group kontekstom, razredom, Programom,
  statusom i participant countom;
- sljedeći `Scheduled` sat s lokacijom/online signalom i vezom na postojeći Session detail;
- nearhivirani Student total te Active, OnHold i Inactive brojevi;
- stalni PLUS 5 shell, aktivni žuti `Radni stol` i postojeći Teacher profile prostor.

Aktivne su samo akcije čiji route i contract već postoje: dodavanje učenika, Materials,
Schedule/Session i Student/Group kontekst. Poruke, homework, test/ispit, financije, Lesson Plan
i pokretanje sata ostaju stvarni disabled control s objašnjenjem. Notification, activity feed,
recently-used Materials, predicted grade i overall readiness nisu simulirani.

Student status barovi predstavljaju administrativne statuse i eksplicitno nisu procjena znanja
ili spremnosti. UI ne prikazuje postotke koji ne postoje u API contractu.

## Responsive i accessibility

- desktop čuva canonical sidebar i dvostupčani schedule/next-session odnos;
- ispod `48rem` shell prelazi u postojeću horizontalnu navigaciju, quick actions u dvije
  kolone, a dashboard kartice u jedan stupac;
- quick-action linkovi i disabled buttoni ostaju semantički različiti;
- schedule i Student cjeline imaju imenovane regione, vidljive headinge i keyboard focus;
- dokument nema horizontalni overflow na 1536×1024 ni 390×844.

## Visual acceptance

Minimalni stvarni gate nalazi se u `visual-acceptance/phase-8.2/`:

- canonical neizmijenjena kopija i SHA-256;
- jedan desktop screenshot 1536×1024;
- jedan mobile screenshot 390×844;
- stvarni Teacher cookie login, aktualni API i SQL;
- `GET /api/v1/dashboard` HTTP 200 u oba viewporta;
- auth bypass, API interception i DOM mutation nisu korišteni;
- browser/page errors i business writeovi tijekom capturea: 0.

Canonical odstupanja su namjerna: budući moduli ostaju disabled/izostavljeni, a Student
pregled koristi zaključane administrativne statuse umjesto nepostojećih performance bandova.
