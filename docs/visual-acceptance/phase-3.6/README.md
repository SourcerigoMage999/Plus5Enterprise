# Phase 3.6 — canonical desktop/mobile visual acceptance

**2026-09-11 — interni visual acceptance PASS uz navedene iznimke; završni SA review čeka.**

## Izvori i stvarni pregled
Canonical: Za programera - novo/UČITELJ/2.0 Učenici/2.8 Nova grupa/2.8 Nova grupa.png.
Slika je otvorena i uspoređena sa stvarnim Edge prikazom aplikacije, ne generiranim mockupom.
Referenca DS-001/ADR-0014 i postojeći shell ostaju prihvaćena osnova.

| Provjera | Rezultat |
|---|---|
| Canonical PNG visual comparison | PASS za zaključani 3.6 scope, uz iznimke ispod |
| Desktop screenshot comparison 1536×1024 | PASS |
| Mobile adaptation 390×844 | PASS, document width 390 |
| Spremanje učenika/otvorene serije | 201, 12 početnih Sessiona |
| Conflict / prazna grupa bez rasporeda | 409 / 201, 0 Sessiona |
| Link i Back, Escape ostaje na obrascu | PASS na oba viewporta |
| Browser page errors | 0 |

## Dokazi
- [Desktop forma](create-desktop.png), [mobilna forma](create-mobile.png)
- [Desktop potvrda odlaska](unsaved-desktop.png), [mobilna potvrda](unsaved-mobile.png)
- [Desktop spremljena grupa](created-desktop.png), [mobilna prazna grupa](created-mobile.png)
- [Mobilni konflikt](conflict-mobile.png)
- [Full create mjerenja i ID-jevi fixturea](final-measurements.json)
- [Završna read-only vizualna provjera](visual-measurements.json)

Create/unsaved snimke osvježene su nakon poravnanja panela i DS modalnih akcija.
Created/conflict slike su iz izvršenog full write journeyja prije tih presentation-only
korekcija; nisu predstavljene kao nove poslovne transakcije. Full-page modal backdrop
pokriva viewport, a ne cijelu visinu dokumenta; to je način snimanja, ne vidljiva rupa
u aktivnom viewportu.

## Usporedba i iznimke
Provjereni su položaj sidebar/profila, žuti Učenici, breadcrumb/naslov/subtitle,
poravnanje gornjih triju panela, lokacija u srednjem panelu, redovi rasporeda i
akcije, donji učenici/sažetak, razmaci, čitljivost, tanki obrubi i plava save hijerarhija.
Tijekom pregleda lokacija je vraćena u srednji panel, sr-only nazivi više ne zauzimaju
mjesto iznad slotova, info zona je usklađena s plavim source treatmentom, modal koristi
DS secondary/danger akcije. Obrazac i kontrole ne uzrokuju document overflow.

Ne preslikavaju se rejected minimum/automatska deaktivacija, mode toggle, lažne razine,
fotografije, boja, notes/goals/materials ili permissions koje approved priprema nije
implementirala. Summary je iz tekstualnog sourcea. Opis ima foundation limit 1000.
Broj kandidata/slotova i prazna lokacija ovise o stvarnom demo sadržaju. Native datum/
vrijeme može biti prikazano u OS formatu; nije zamijenjeno vlastitim date pickerom.

## Reprodukcija i demo podaci
frontend/tests/visual/phase36.mjs zahtijeva Edge, lokalni Playwright index.mjs u
PLUS5_REVIEW_PLAYWRIGHT i odobrene credentials samo kroz PLUS5_REVIEW_EMAIL/PASSWORD.
Defaultni full test stvara označeni demo fixture: učenika, grupu s rasporedom i praznu
grupu. Provjerava konflikt istog slota. Nije sigurno slijepo ponavljati isti raspored
na bazi koja ga već sadrži: koristite čisti odobreni testni fixture/prozor.
PLUS5_REVIEW_CAPTURE_ONLY=true snima formu i navigaciju bez novih upisa.

U lokalnoj aplikaciji ostale su dvije demo grupe, 12 termina i dva označena demo učenika
(jedan iz prvog pokušaja, jedan iz uspješnog testa). Nisu mijenjani postojeći učenici.
SQL integration testovi koriste zasebne nasumične baze i uklanjaju samo vlastite baze.
Credentials nisu u screenshotovima ili repozitoriju.

## Povijesni pripremni dokazi
preparation-desktop.png, preparation-mobile.png i measurements.json pripadaju ranijem
PARTIAL stanju. Ne dokazuju završni write niti se koriste kao finalni acceptance.
