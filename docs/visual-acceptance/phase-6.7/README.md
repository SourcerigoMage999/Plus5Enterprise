# Phase 6.7 visual acceptance — Edit material and version history

## Status

**PASS — stvarni login/API/SQL/storage version lifecycle te canonical desktop/mobile pregled.**

## Evidence

| Datoteka | Svrha |
|---|---|
| `canonical.png` | neizmijenjeni canonical `4.5 Uredi materijal.png` |
| `material-edit-desktop-1536x1024.png` | stvarni metadata/version editor, desktop viewport |
| `material-edit-mobile-390x844.png` | responsive editor, mobile viewport |
| `material-version-history-desktop.png` | stvarni v3 Draft / v2 Active / v1 Superseded history drawer |
| `measurements.json` | viewport/overflow, browser, security i runtime lifecycle evidence |

Canonical SHA-256:
`DCC716FE75C7A4F8972BC4FD8BC4F00DE142C3A291D41EA38DF9AAB8AD243DD7`.

## Izvršeni gate

- stvarna prijava kroz `/auth/login`, bez auth bypassa;
- stvarni import fixturea i stvarni owner-scoped edit/history API;
- stvarni SQL/private-storage flow: import Active v1 → Save Draft v2 → publish v2 → restore v1
  kao novi Draft v3;
- nakon flowa history je `v3 Draft`, `v2 Active/current`, `v1 Superseded`;
- anonymous edit vraća `401`, authenticated publish bez CSRF-a `400`;
- desktop `scrollWidth/clientWidth = 1536/1536`;
- mobile `scrollWidth/clientWidth = 390/390`;
- browser errors: `0`;
- tijekom screenshot capturea nema business writeova.

## Canonical odnos i dokumentirana odstupanja

Očuvani su Screen 4.5 naslov, version/history/save/publish hijerarhija, četverokoračni indikator,
dominantni centralni content prostor i desni metadata/cilj/mapiranje panel. Responsive varijanta
slaže akcije i editor u jednu čitljivu kolonu bez document overflowa.

Canonical puni slide editor, slide thumbnails, content toolbar, notes, preview/fullscreen/export,
quick actions, AI suggestioni i destructive delete nisu implementirani jer njihovi domain i
runtime contracti pripadaju Phase 7 ili kasnijim fazama. Centralni prostor to jasno kaže umjesto
da simulira slide podatke. Visibility je read-only do Phase 6.8, a Duplicate je disabled.

Ponovljivi gate je `frontend/tests/visual/phase67.mjs`. Traži lokalne review credentiale,
bundled Playwright put i canonical PNG kroz environment varijable; lozinka se ne sprema u Git.
