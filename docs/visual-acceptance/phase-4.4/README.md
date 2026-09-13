# Phase 4.4 — Edit session visual acceptance

## Rezultat

**PASS — 2026-09-13.** Stvarni `/schedule/:sessionId/edit` Screen 3.4 uspoređen je s canonical
`3.4 Uredi termin.png` (SHA-256
`384CE2E9DAC1D7B657AEABE12F81FD21A32197D92BE92A4DAB3F4CEF7AEFBAD6`) iz odobrenog
teacher source paketa. Usporedba potvrđuje high visual fidelity, ne matematički pixel-perfect
rezultat.

Provjera je izvedena na rebuilt Docker aplikaciji normalnom lokalnom prijavom i stvarnim
owner-scoped aktivnim Sessionom iz serije. Lozinka, cookie i token nisu spremljeni. Headless
Edge koristio je desktop 1536×1024 i mobile 390×844; snimke su full-page. Provjera je sigurno
mijenjala scope i kontrolirala polja, ali nije spremala, otkazivala ni napravila drugi write.

## Dokazi

- [Desktop](edit-session-desktop.png)
- [Mobile](edit-session-mobile.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| Canonical layout i hijerarhija | PASS — desktop čuva tri stupca; mobile ih slaže u jedan |
| Shell i aktivni modul | PASS — stalni PLUS 5 shell, žuti Raspored, profil i notification prostor |
| Prefill i zaključani kontekst | PASS — stvarni Group Session, način rada, program i broj učenika |
| Datum, vrijeme i lokacija | PASS — stvarne vrijednosti, owner-scoped lokacije i jasno odvojeni načini |
| Scope promjene | PASS — zadana jedna instanca i sigurna buduća serija s vidljivim objašnjenjem |
| Conflict zona | PASS — live server provjera i blokirajuća hijerarhija |
| Trenutni termin i akcije | PASS — sažetak, cancel i duplicate imaju canonical prioritet |
| Budući contracti | PASS — boje i reminders su vidljivi, ali stvarno disabled |
| Desktop overflow | PASS — document/viewport 1536/1536 |
| Mobile adaptation | PASS — document/viewport 390/390 |
| Browser greške | PASS — 0 `pageerror` događaja |
| Writeovi tijekom gatea | PASS — 0 |

## Namjerna odstupanja

- Za future-series scope datum, naziv i napomena ostaju zaključani: zaključani recurrence model
  Screen 3.4 dopušta jednostavnu promjenu vremena/lokacije, dok se dan/struktura grupnog
  rasporeda mijenja u 2.9.
- Notification prompt/slanje nije aktivirano jer notification contract još nije zaključan.
- Boja i reminders nisu perzistirani i ne predstavljaju lažni client-only uspjeh.
- Demo koristi stvarnu grupu i inicijale; nisu dodani lažni avatar/storage podaci radi slike.
- Postojeći prihvaćeni DS-001 shell, logo, tokeni i responsive navigacija imaju prednost nad
  doslovnim kopiranjem pojedinačnih piksela.

## Ponovljiva provjera

Pokrenuti lokalni Compose, prijaviti se odobrenim demo računom bez spremanja lozinke, otvoriti
Raspored, odabrati budući termin aktivne serije i zatim `Uredi termin`. Potvrditi stvarni
prefill, zadani one-occurrence scope, future-series zaključavanje, server conflict signal,
cancel potvrdu i disabled buduće kontrole. Usporediti 1536×1024 i 390×844 s canonical PNG-om
te potvrditi da nema document overflowa. Funkcionalne writeove zasebno pokrivaju component,
API i stvarni SQL testovi.
