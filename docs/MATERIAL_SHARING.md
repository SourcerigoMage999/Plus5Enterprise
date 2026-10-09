# Material sharing, visibility and permissions

## Status

Phase 6.8 je **FINAL LOCKED — odobreno 2026-10-09**. Ovaj dokument primjenjuje zaključani
`MATERIAL_FOUNDATION.md` i ADR-0023 contract bez proširenja permission modela.

## Scope i routeovi

Samo owner Teacher upravlja dijeljenjem vlastitog aktivnog, nearhiviranog Materiala čija je
current verzija `Active` i datoteka `Clean`:

- UI: `/materials/{materialId}/sharing`;
- `GET /api/v1/materials/{materialId}/sharing`;
- `PUT /api/v1/materials/{materialId}/sharing`.

Shared recipient nema sharing workspace niti re-share ovlast. Missing, foreign, archived,
non-current ili non-clean resurs daje isti `404`; anonymous request daje `401`.

## Zaključani permission model

Vidljivost je samo:

- `Private` — pristup ima samo owner i grantovi ne smiju postojati;
- `Shared` — pristup imaju samo eksplicitno dodani Teacher računi.

Svaki jedinstveni `MaterialShare` grant pripada drugom aktivnom Teacher accountu i ima jednu
razinu:

- `View` — metadata i dopušteni Clean-content read/preview;
- `Use` — View plus buduća uporaba u vlastitom Lesson workflowu ili eksplicitno dupliciranje.

`Use` ne daje edit, re-share ili ownership transfer. Material share ne daje pristup Studentu,
Groupi, Evidenceu, readinessu ili Lesson historyju. Nema public/marketplace/link/org scopea.

## Write contract

Request šalje `expectedRowVersion`, ciljnu `private|shared` vidljivost i potpuni željeni skup
grantova s točnom recipient e-mail adresom i `view|use` razinom. Server:

1. validira rowversion, vidljivost, e-mail oblik, access code i duplicate e-mail;
2. ownera izvodi isključivo iz autentificirane sesije;
3. razrješava samo točno navedene aktivne Teacher račune, bez user-directory endpointa;
4. odbija self-share, unknown/deactivated/drugi nedopušteni recipient jednim generičkim,
   nerazlučivim rezultatom i odbija grantove uz `Private`;
5. u jednoj SQL transakciji zaključava Material rowversion, mijenja vidljivost i usklađuje
   grantove;
6. pri povratku na `Private` prvo uklanja sve grantove i tek zatim sprema Private vidljivost;
7. vraća kontrolirani `409 concurrency_conflict` za stale rowversion.

Sharing PUT ima zaseban fixed-window rate limit od 10 pokušaja u minuti po izvornoj IP adresi.
Ograničenje je dodatna abuse zaštita exact-email ulaza; autentikacija, owner scope, CSRF i sve
business validacije ostaju obavezni.

Postojeći `TR_Materials_ValidateCurrentVersion` i `TR_MaterialShares_RejectOwnerSelfShare`
ostaju DB authority za Private-with-grants, self-share i Shared-grant granice. Phase 6.8 ne
uvodi novu tablicu, stupac, trigger ni migraciju.

## API response i greške

Owner workspace vraća samo Material ID, current title, rowversion, vidljivost i postojeće
grantove s recipient Teacher ID-em, canonical e-mailom i `view|use` razinom. Ne izlaže password,
session, storage key, checksum, scanner ili druge account podatke.

Stabilni business kodovi:

- `material_share_invalid_request` — nevaljana vidljivost/grant/e-mail/duplicate granica;
- `material_share_invalid_recipient` — neutralni `400` za unknown, deaktiviran, self ili drugi
  nedopušteni recipient; odgovor ne otkriva postoji li račun;
- `material_not_found` — missing/foreign/neupotrebljiv Material;
- `concurrency_conflict` — stale `Material.RowVersion`;
- `invalid_csrf_token` — write bez valjanog CSRF tokena.

Frontend za `material_share_invalid_recipient` prikazuje samo neutralno "Nije moguće dodati ovog
učitelja." Nepostojeći, deaktivirani i drugi nedopušteni recipienti namjerno imaju isti status,
code i poruku kako exact-email flow ne bi postao account-enumeration oracle.

## UI contract

Owner ulazi preko akcije `Dijeli` na Material detailu. Ekran prikazuje:

- Private/Shared izbor i trenutačni status;
- eksplicitni popis Teacher grantova;
- exact-email unos bez pretraživog imenika korisnika;
- View/Use izbor, dodavanje i uklanjanje;
- pojašnjenje permission i data-scope granice;
- loading, error/retry, private/shared empty, saving, success i dirty-navigation stanja.

Odabir `Private` lokalno uklanja grantove, a tek eksplicitni save šalje atomsku promjenu.
Frontend validacija služi UX-u; backend i SQL ostaju authority.

## Izvan Phase 6.8

- uređivanje tuđeg/shared Materiala, re-share i ownership transfer;
- public link, marketplace, organization/team ili role matrica;
- user directory/search i invitations;
- binary preview/download koji još nema zaseban read-access contract;
- dupliciranje, Lesson Builder/Board uporaba i audit history permission promjena;
- notification/e-mail prema recipientu;
- slide/content editor, Quick Check, AI authoring i export iz Phase 7+.
