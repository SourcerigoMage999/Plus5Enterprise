# Phase 6.4 visual acceptance — Material detail

## Status

**PASS — stvarni login, stvarni API i canonical desktop/mobile usporedba.**

## Evidence

| Datoteka | Svrha |
|---|---|
| `canonical.png` | neizmijenjeni canonical `4.2 Pregled materijala.png` |
| `material-detail-desktop-1536x1024.png` | stvarni owner detail, desktop viewport |
| `material-detail-mobile-390x844.png` | stvarni owner detail, mobile viewport |
| `material-detail-not-found-desktop.png` | indistinguishable missing/foreign stanje |
| `measurements.json` | viewport/overflow i runtime evidence |

Canonical SHA-256:
`FE87227511E863D6A62B0349066CF082E3DA89365666E5BB697F534C4679C456`.

## Izvršeni gate

- stvarna prijava kroz `/auth/login`, bez auth bypassa;
- stvarni `GET /api/v1/materials` za odabir i stvarni
  `GET /api/v1/materials/{materialId}`;
- desktop `scrollWidth/clientWidth = 1536/1536`;
- mobile `scrollWidth/clientWidth = 390/390`;
- browser errors: `0`;
- business writeovi tijekom capturea: `0`;
- `Otvori`/`Preuzmi` ostaju disabled bez storage adaptera;
- `Kopiraj link` ostaje sigurna aktivna lokalna akcija;
- canonical header/card/preview/right-rail hijerarhija i responsive stacking pregledani su.

## Canonical odstupanja

- centralni prikaz je pošten format preview, ne lažirani PPTX/PDF sadržaj; production binary
  read i conversion adapter nisu dio 6.4;
- lokalni lifecycle-valid Material fixture nema Knowledge/Curriculum mapping, pa screenshot
  prikazuje stvarna no-data stanja. Popunjene mapping projekcije pokrivene su ciljanim backend i
  frontend testovima bez mijenjanja objavljenog snapshot-a radi snimke;
- task list, difficulty, Evidence vrsta i scoring iz task canonical varijante pripadaju 6.5;
- readiness, usage history, recommendations, edit i Add-to-Lesson nisu simulirani bez contracta.

Ponovljivi gate je `frontend/tests/visual/phase64.mjs`. Skripta traži lokalne review
credentiale, put do bundled Playwrighta i canonical PNG kroz environment varijable; ne sprema
lozinku u Git.
