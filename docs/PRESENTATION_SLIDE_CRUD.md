# Presentation slide CRUD — Phase 7.2

## Status i scope

**FINAL LOCKED — odobreno 2026-10-11.**

Ovaj contract implementira samo persistence i Teacher-owner editor za stvaranje nativnog
`PresentationDocument` zapisa te dodavanje, dupliciranje, brisanje i promjenu redoslijeda
praznih `PresentationSlide` redaka. Nastavlja FINAL LOCKED Phase 7.1 model bez content
elemenata, Quick Checka, autosavea, previewa ili publisha.

## Lifecycle i invarijante

- dokument pripada točno jednoj `MaterialVersion` vrste `PRESENTATION`;
- dokument se eksplicitno inicijalizira samo na najnovijoj ownerovoj Draft verziji;
- inicijalizacija stvara točno jedan prazan slajd i koristi `Material.RowVersion` jer document
  rowversion prije prvog inserta još ne postoji;
- svaki idući write koristi točno osmobajtni `PresentationDocument.RowVersion`;
- stale revision vraća kontrolirani `409 concurrency_conflict`; nema last-write-wins;
- dokument uvijek ima najmanje jedan i najviše 200 slajdova;
- `SortOrder` je contiguous `0..N-1` i jedinstven unutar MaterialVersiona;
- duplikat u 7.2 stvara novi prazni slide identitet neposredno nakon izvornog slajda;
- zadnji slajd se ne može obrisati;
- samo Draft graph je mutable; Active i Superseded Presentation snapshotovi su immutable.

## Persistence i SQL zaštita

`PresentationDocuments` koristi `MaterialVersionId` kao PK/FK, schema version `1`, logical
canvas `1600×900`, UTC audit vremena i SQL Server `rowversion`. `PresentationSlides` ima GUID
identitet, restrictive FK na document te unique `(MaterialVersionId, SortOrder)` indeks.

SQL triggeri su zadnja zaštita i odbijaju document/slide INSERT, UPDATE ili DELETE kada
pripadajuća MaterialVersion nije Draft `PRESENTATION`. Constrainti štite schema/canvas oblik i
nenegativan redoslijed. Migracija je aditivna: ne backfilla postojeće materijale i postojeći
Material/MaterialVersion retci ostaju nepromijenjeni.

Reorder/add/duplicate/delete izvode se u jednoj transakciji. Privremeni bounded sort offset
omogućuje sigurno preslagivanje bez kršenja unique indeksa, nakon čega se redoslijed normalizira
na `0..N-1` prije commita.

## API

Svi endpointi su Teacher-only i owner-scoped. Writeovi su CSRF-protected, imaju 16 KiB command
body limit i dedicated fixed-window limit od 120 writeova u minuti po autentificiranom Teacheru
(IP fallback postoji samo ako identitet nije dostupan):

| Metoda i ruta | Svrha |
|---|---|
| `GET /api/v1/materials/{materialId}/presentation` | ownerov najnoviji Draft PRESENTATION workspace |
| `POST /api/v1/materials/{materialId}/presentation` | eksplicitna inicijalizacija dokumenta |
| `POST /api/v1/materials/{materialId}/presentation/slides` | dodavanje praznog slajda |
| `POST /api/v1/materials/{materialId}/presentation/slides/{slideId}/duplicate` | dupliciranje praznog slajda |
| `DELETE /api/v1/materials/{materialId}/presentation/slides/{slideId}` | brisanje osim zadnjeg slajda |
| `PUT /api/v1/materials/{materialId}/presentation/slides/order` | potpuni novi redoslijed svih slajdova |

Missing, foreign-owner, archived, non-Presentation ili bez Draft verzije daju isti `404`.
Nevaljan command daje `400`; stale revision `409`; limit i pokušaj brisanja zadnjeg slajda
imaju zasebne stabilne `409` codeove. Teacher/owner ID nikad ne dolazi iz request bodyja.

## Frontend i visual contract

Route `/materials/:materialId/presentation` otvara start state ako Draft još nema dokument ili
canonical P3-derived editor nakon inicijalizacije. Editor prikazuje persisted prazne slajdove,
odabir, add/duplicate/delete te pristupačne up/down reorder kontrole. Canvas je stvarni 16:9
placeholder, a content/theme/layout/undo/redo/save/publish kontrole ostaju jasno disabled jer
pripadaju 7.3–7.8.

Desktop zadržava lijevu slide zonu, centralni canvas i desni properties rail. Mobile ih
responsivno slaže bez document horizontal overflowa; slide traka smije imati lokalni overflow.
Canonical odnos i stvarni runtime dokazi nalaze se u `visual-acceptance/phase-7.2/`.

## Security i failure behavior

- cookie session + Teacher policy + server-side owner provjera;
- svi writeovi zahtijevaju valjan antiforgery token;
- malformed, missing ili ne-Base64 rowversion završava kao kontrolirani `400`, ne `500`;
- write ne prima arbitrary JSON document ni executable markup;
- structured error odgovor ne izlaže SQL, storage ili ownership detalje;
- UI refresh nakon uspješnog writea čita novi autoritativni rowversion i redoslijed.

## Namjerno izvan scopea

- `PresentationElement`, tekst, slike, oblici, tablice, tema i layout handleri (7.3);
- teaching-specific elementi (7.4), Quick Check i Task veze (7.5–7.6);
- autosave/recovery i conflict merge UX (7.7);
- notes, preview, deterministic package, publish/export (7.8);
- AI assistance (7.9, BLOCKED), PPTX conversion i collaborative editing.
