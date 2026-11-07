# Phase 9.8 visual acceptance

## Status

**PASS — 2026-11-07.**

Canonical source: `5.5 Detalji - uređivanje aktivnosti/5.5 Detalji - uređivanje aktivnosti.png`.

- canonical SHA-256: `F94BCFD41AEF109C5E863693FB72C5D9E852472077D3CB9A8449A8FE9A118511`;
- copied unchanged as `canonical.png`;
- desktop evidence: `lesson-activity-detail-desktop-1536x1024.jpg`;
- mobile evidence: `lesson-activity-detail-mobile-390x844.jpg`.

## Runtime flow

Evidence je nastao kroz stvarni lokalni browser/runtime:

1. stvarna Teacher cookie prijava bez auth bypassa;
2. stvarni SQL-backed Phase 9.5–9.7 Draft LessonPlan i aktivnost;
3. stvarni Phase 9.8 detail GET;
4. AI integracija stvarno isključena, bez provider poziva i bez troška;
5. desktop i mobile viewport capture bez poslovnog writea.

Nije korišten API interception, mock DOM, hardkodirani screenshot state ili browser-side
Lesson/AI logika.

## Vizualna usporedba

PASS:

- trajni PLUS 5 shell i aktivna `Priprema sata` navigacija;
- breadcrumb, Screen 5.5 naslov, Knowledge Block i plan context;
- canonical edit actions, core fields, upute, Board flags i Notes zona;
- page-level akcije spuštene su ispod globalne notification/profile zone prema obaveznoj
  `ds-page-actions--below-global-profile` granici;
- jasno vidljiva default-off AI zona bez lažnog Generate CTA-a;
- desktop content/sidebar hijerarhija i mobile single-column stacking;
- bez globalnog horizontalnog overflowa ili preklapanja kontrola.

Namjerna odstupanja od canonicala:

- nema izmišljenog assessment/task attachment editora jer njegov authoring contract još nije
  zaključan;
- AI zona je prikazana kao disabled/no-cost state jer je provider defaultno isključen;
- verzionirani Knowledge Block i rowversion-safe akcije prikazani su eksplicitnije radi
  authoring sigurnosti.

## Mjerenja

- desktop viewport: `1536×1024`, `scrollWidth 1521 <= clientWidth 1521`;
- desktop global profile/action overlap: `false`; vertikalni razmak: `57.22 px`;
- mobile viewport: `390×844`, `scrollWidth 375 <= clientWidth 375`;
- mobile global profile/action overlap: `false`; vertikalni razmak: `164.59 px`;
- browser/page errors tijekom konačnog capturea: `0`;
- error state vidljiv tijekom konačnog capturea: `false`;
- business writeovi tijekom capturea: `0`;
- activity detail API: HTTP `200`.

Strojno čitljivi detalji su u `measurements.json`.
