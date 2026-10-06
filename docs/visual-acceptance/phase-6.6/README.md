# Phase 6.6 visual acceptance — Import own material

## Status

**PASS — stvarni login, stvarni options API, desktop/mobile wizard i stvarni persistentni clean import.**

## Evidence

| Datoteka | Svrha |
|---|---|
| `canonical.png` | neizmijenjeni canonical `4.4 Uvoz vlastitog materijala.png` |
| `material-import-file-desktop-1536x1024.png` | korak odabira datoteke, desktop viewport |
| `material-import-review-desktop-1536x1024.png` | stvarni review, desktop viewport |
| `material-import-review-mobile-390x844.png` | vrh responsive reviewa, mobile viewport |
| `material-import-review-actions-mobile.png` | review potvrda i sigurnosna zona na mobitelu |
| `measurements.json` | viewport/overflow, browser i runtime import evidence |

Canonical SHA-256:
`B2034958DBE6E8BB1380E7A69B7414D9C9FEEB01B723B66E9F972EB29CE07E38`.

## Izvršeni gate

- stvarna prijava kroz `/auth/login`, bez auth bypassa;
- stvarni `GET /api/v1/materials/import/options`, bez API interceptiona ili DOM mutationa;
- četverokoračni file → metadata → mapping → review flow;
- tijekom desktop/mobile capturea nema dovršenog business writea;
- jedan zaseban namjerni runtime PDF import: HTTP 201, private quarantine, stvarni ClamAV
  scan, clean promocija, SQL activation i detail HTTP 200;
- clean bucket sadrži importirani opaque-key objekt, quarantine bucket je nakon promocije
  prazan, a objekt preživljava restart pinned Adobe S3Mock 5.1.0 containera;
- desktop `scrollWidth/clientWidth = 1536/1536`;
- mobile `scrollWidth/clientWidth = 390/390`;
- browser errors: `0`.

## Canonical odnos i odstupanja

Zadržani su Screen 4.4 naslov, četverokoračna hijerarhija, dominantni file-drop prostor,
jasna potvrda, sigurnosna poruka i PLUS 5 responsive shell. Implementacija koristi zaključane
format-specifične limite (PDF 50 MB, DOCX 25 MB, PPTX/ZIP 100 MB, MP4 250 MB), umjesto
canonical generalizacije na 100 MB.

Canonical desna help zona i lista nedavno uvezenih datoteka nisu duplicirane unutar wizarda:
postojeća Material Library ostaje stvarni izvor biblioteke, a help/notification modul nema
zaključan backend contract. Nema AI prijedloga, thumbnaila ni fake upload podataka.

Stvarni browser gate otkrio je i ispravio preuranjeni submit pri zadnjem `Nastavi` prijelazu;
review sada ostaje lokalno stanje sve dok Teacher eksplicitno ne klikne `Potvrdi i uvezi`.

Ponovljivi gate je `frontend/tests/visual/phase66.mjs`. Skripta traži lokalne review
credentiale, put do bundled Playwrighta i canonical PNG kroz environment varijable; lozinka
se ne sprema u Git. Skripta stvara privremeni minimalni PDF izvan repozitorija i briše ga po
završetku.
