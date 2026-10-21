# Phase 7.6 visual acceptance

## Status

**PASS — 2026-10-21.**

## Canonical source

- izvor: `4.3 P4.png`;
- lokalna neizmijenjena kopija: `canonical.png`;
- SHA-256: `8365CD11CD94DB8573472E627B510A6576DB4735ABB279E775B5F42766F6D394`.

## Minimalni stvarni capture

- `presentation-knowledge-mapping-desktop-1536x1024.png`;
- `presentation-knowledge-mapping-mobile-390x844.png`;
- `measurements.json`.

Capture koristi stvarni Teacher cookie login, lokalni SQL Server i stvarni Presentation API.
Prikazan je postojeći Quick Check povezan s `Present Continuous` targetom iz objavljenog
`ENG-7 / 2026` modela te otvoreni bounded picker s dostupnim aktivnim leaf kandidatima.
Nisu korišteni auth bypass, API interception ni DOM mutation. Tijekom capturea nema
Presentation business writeova ni browser/page grešaka.

## Measurements

| Prikaz | clientWidth | scrollWidth | Rezultat |
|---|---:|---:|---|
| desktop 1536×1024 | 1536 | 1536 | PASS |
| mobile 390×844 | 390 | 390 | PASS |

Oba prikaza imaju exactly jedan spremljeni mapping, otvoreni picker i uspješne stvarne GET
responseove za editor workspace i candidate katalog.

## Canonical usporedba

Očuvani su canonical P4 centralni Quick Check, desni property panel, žuto naglašena
`Povezanost sa znanjem` zona, selected target i akcija `Odaberi komponentu`. Implementacija
eksplicitno prikazuje KnowledgeArea i KnowledgeModel code/version jer različite verzije ne
smiju biti vizualno stopljene. Mobile prikaz fokusira isti feature state u čitljivom
jednostupčanom rasporedu bez globalnog horizontalnog overflowa.

Canonical budući autosave, preview/publish i AI kontrole nisu simulirane; pripadaju fazama
7.7–7.9.
