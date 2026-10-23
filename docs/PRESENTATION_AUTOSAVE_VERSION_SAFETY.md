# Presentation autosave and version safety

## Status

**Phase 7.7 — IMPLEMENTED / REVIEW READY — 2026-10-23.**

Ovaj contract primjenjuje zaključani `PresentationDocument.RowVersion` authority iz Phase
7.1–7.6 na automatsko spremanje u Slide Editoru. Ne mijenja document model, API write
contracte, persistence niti Draft/Active/Superseded lifecycle.

## Scope

Phase 7.7 uvodi:

- automatsko spremanje valjanih promjena odabranog elementa nakon 800 ms bez novog unosa;
- eksplicitni `Spremi nacrt` / `Spremi sada` flush istog autosave contracta;
- vidljiva i pristupačna stanja `saved`, `dirty`, `saving`, `error` i `conflict`;
- tab-scoped recovery zadnje nespremljene forme;
- mrežni retry bez gubitka lokalnog unosa;
- sigurno, neprepisujuće ponašanje pri optimistic-concurrency sukobu;
- upozorenje preglednika pri napuštanju stranice dok postoji nespremljena promjena.

Strukturne slide/element create, delete, duplicate i reorder naredbe i dalje se spremaju
odmah kroz postojeće atomske write endpointove. Nisu pretvorene u client-side batch.

## Debounce i manual flush

- Debounce je fiksnih **800 ms** od posljednje promjene forme.
- Autosave šalje samo trenutno odabrani element kroz njegov postojeći eksplicitni typed
  command; ne šalje cijeli document graph i ne koristi JSON Patch.
- Nevaljana ili nedovršena forma ostaje `dirty` i ne šalje zahtjev dok ponovno ne postane
  valjana.
- Dok traje jedan save, njegova polja kratko su zaključana. Time nema paralelnih writeova nad
  istom baznom document revizijom.
- `Spremi nacrt` i `Spremi sada` odmah izvršavaju isti save; nisu zaseban lifecycle ili
  publish command.
- Nakon uspjeha editor ponovno čita server workspace i novu authoritative rowversion
  vrijednost prije sljedećeg writea.

## Recovery

Nespremljeni element snapshot sprema se best-effort u browser `sessionStorage`:

- ključ je vezan uz exact `MaterialVersionId` i `PresentationElementId`;
- snapshot sadrži samo typed form values i potpis baznog elementa;
- nema auth tokena, storage ključa, Student/Guardian/Evidence podataka ni binarnih asseta;
- snapshot vrijedi samo u istoj browser kartici;
- automatski se vraća samo kada se bazni server element nije promijenio;
- uspješan save ili eksplicitni reload novije verzije uklanja snapshot;
- corrupt ili version-mismatched snapshot odbacuje se, bez best-effort mergea.

Recovery je zaštitna mreža za reload, prekid veze i neuspjeli write. Nije offline-first queue,
trajna lokalna kopija niti collaborative history.

## Concurrency i conflict UX

Svaki autosave nosi aktualni `expectedDocumentRowVersion`. SQL/EF rowversion i backend
ostaju konačni authority.

```text
stale document rowversion
→ HTTP 409 concurrency_conflict
→ autosave se zaustavlja
→ lokalni unos ostaje u formi i session recoveryju
→ nema automatskog overwritea ni last-write-wins ponašanja
→ Teacher eksplicitno učitava noviju spremljenu verziju
```

Phase 7.7 ne radi field-level merge. To bi zahtijevalo zaseban collaborative editing
contract. Ostale write akcije privremeno su onemogućene dok je element `dirty`, `saving`,
`error` ili `conflict`, kako se ne bi paralelno trošila ista document revizija.

## Failure ponašanje

- mrežna/server greška ostavlja lokalni unos i nudi `Pokušaj ponovno`;
- konflikt nudi `Učitaj noviju verziju`, što eksplicitno odbacuje lokalni recovery snapshot;
- `beforeunload` upozorenje aktivno je dok stanje nije `saved`;
- error/conflict poruke ne izlažu interne SQL, storage ili stack-trace podatke.

## Sigurnost

Postojeći Teacher session, owner scope, Draft-only pravilo, CSRF, bounded body, rate limit i
server-side payload validation ostaju obavezni. Browser recovery nije authority i ne može
zaobići ni jednu backend provjeru.

## Namjerno izvan Phase 7.7

- undo/redo history i server-side revision history;
- field-level merge, presence, locking i real-time collaboration;
- offline queue, service worker/background sync i cross-device recovery;
- autosave asset uploada koji još nije prošao security pipeline;
- preview, deterministic package, activation/publish i export (Phase 7.8);
- AI assistance (Phase 7.9, BLOCKED).

## Validation budget

Klasifikacija: `FRONTEND_ONLY + UI + DOCS_ONLY`.

- targeted `PresentationEditor.test.tsx`;
- frontend lint, TypeScript i production build;
- jedan desktop i jedan mobile stvarni browser screenshot, autosave status, document
  overflow assertion i browser/page errors = 0;
- backend, SQL, EF drift, architecture, full regression, dependency audit i Docker
  infrastructure validation: N/A jer njihove zaključane granice nisu promijenjene.
