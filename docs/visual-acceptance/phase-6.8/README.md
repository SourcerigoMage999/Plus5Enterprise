# Phase 6.8 visual acceptance — Material sharing

## Status

**PASS — stvarni login/API/SQL/storage share flow te desktop/mobile pregled.**

## Evidence

| Datoteka | Svrha |
|---|---|
| `canonical.png` | neizmijenjeni Screen 4.1 Materials/shell baseline; zaseban sharing PNG ne postoji |
| `material-sharing-desktop-1536x1024.png` | stvarni Shared Material s eksplicitnim Use grantom |
| `material-sharing-mobile-390x844.png` | responsive vrh sharing workspacea |
| `material-sharing-mobile-grant-390x844.png` | mobilni grant/access control prikaz nakon stvarnog scrolanja |
| `material-sharing-private-empty-desktop.png` | lokalni Private odabir i pošteno empty stanje prije savea |
| `measurements.json` | viewport/overflow, browser, security i runtime sharing evidence |

Canonical SHA-256:
`07677BA32CE7BCBBD7148B02BF903EB7D21667C295F5C2249CEAC33B99813D05`.

## Izvršeni gate

- jednokratni lokalni owner i recipient Teacher računi nastali su realnim registration/hash
  flowom te su aktivirani kao lokalni test fixture; credentiali nisu spremljeni u Git;
- stvarna prijava kroz `/auth/login`, bez auth bypassa;
- stvarni PDF import kroz object storage i malware scan;
- stvarni owner-only sharing GET/PUT: Private → Shared + jedan `Use` grant;
- persisted workspace nakon writea vraća Shared, jedan grant i `Use`;
- anonymous sharing GET vraća `401`, authenticated PUT bez CSRF-a `400`;
- desktop `scrollWidth/clientWidth = 1536/1536`;
- mobile `scrollWidth/clientWidth = 390/390`;
- browser errors: `0`;
- tijekom screenshot capturea nema business writeova, API interceptiona ni DOM mutationa.

## Canonical odnos i dokumentirana odstupanja

Screen 4.1 canonical određuje prihvaćeni Materials shell, brand, spacing, card/control tretman i
visual hierarchy. Source paket nema poseban PNG za permission management, pa Phase 6.8 ne tvrdi
pixel-identičnost nedokumentiranom ekranu. Layout koristi isti PLUS 5 shell i Material detail
navigation boundary, s centralnim visibility/grant radnim prostorom i desnim permission
objašnjenjima izvedenima iz zaključanog `MATERIAL_FOUNDATION.md` contracta.

Ne postoji user directory, avatar ili recipient profile jer bi otkrivanje drugih računa proširilo
privacy scope. Nema Edit/re-share/public/link/org kontrola, implicitnog Student/Evidence pristupa,
notificationa ili Lesson Builder akcije. Exact-email unos i View/Use labele vjerno predstavljaju
jedini odobreni v1 permission model.

Ponovljivi gate je `frontend/tests/visual/phase68.mjs`. Traži lokalne review credentiale,
recipient e-mail, bundled Playwright put i Screen 4.1 canonical PNG kroz environment varijable;
lozinke se ne spremaju u repozitorij.
