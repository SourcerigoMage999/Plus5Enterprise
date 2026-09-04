# DS 001 Design System MVP

## Status izvora

`DS-001.docx`, verzija 1.0, u dokumentu označen `ODOBRENO`, datum 2026-06-29.
UI kit PNG ostaje canonical vizualni prilog. Ovo je source snapshot, ne retroaktivna
promjena prihvaćenog frontend foundationa bez alignment audita.

## Svrha i načela

Jedinstven vizualni identitet i konzistentno korisničko iskustvo kroz platformu.
Načela su jednostavnost, preglednost, brzina korištenja, dosljednost i minimalan broj
klikova. Korisnik mora u prve tri sekunde razumjeti gdje se nalazi i svrhu ekrana.

## Boje

| Uloga | Naziv | Vrijednost | Uporaba iz izvora |
|---|---|---|---|
| primarna | PLUS 5 Yellow | `#f8b91b` | primarni gumbi, aktivni elementi/izbornik, ključne akcije |
| sekundarna | PLUS 5 Blue | `#0f4d80` | zaglavlja, naslovi, navigacija, ikone i tekst |
| tercijarna | PLUS 5 Orange | `#e84a1c` | upozorenja, važne preporuke i pažnja |

Neutralne boje UI kita su `#2c2c2c`, `#6b7280`, `#e5e7eb`, `#f7f8fa` i `#ffffff`.
Statusi su Spreman (`#22c55e`), Potrebna provjera (`#f8b91b`) i Potrebna intervencija
(`#e84a1c`). Boja ne smije biti jedini signal statusa zbog postojećeg accessibility contracta.

## Komponente i hijerarhija

- jedna tipografija kroz platformu; UI kit prikazuje H1 32 px Bold, H2 20 px SemiBold,
  H3 16 px SemiBold u `#0f4d80`, Body 14 px Regular u `#2c2c2c` i Caption 12 px Regular
  u `#6b7280`
- bijele kartice s radiusom 12 px, paddingom 20 px i sjenom
  `0 4px 12px rgba(15, 77, 128, 0.08)`
- primarna akcija žuta; sekundarna bijela s plavim obrubom; danger crvena samo za radnje
  koje mogu uzrokovati gubitak podataka
- jedan outline stil ikona, potez 2 px, zaobljeni rubovi i `#0f4d80`; ikone pomažu
  tekstu i ne zamjenjuju ga
- tablice imaju `#f1f5f9` header, lijevo poravnan tekst i približno 8 px između redaka
- stalna lijeva navigacija širine 240 px, collapsed 72 px, s jasno označenim aktivnim ekranom
- 3–5 kontekstualnih brzih akcija kada ih stvarni use case zahtijeva; UI kit pokazuje
  visinu 48 px, radius 12 px, ikonu lijevo i strelicu desno
- vizualna urednost ima prednost nad količinom informacija

## Pravila prihvata iz izvora

Svaki novi ekran mora jasno odgovoriti koji problem rješava, biti razumljiv u manje od tri
sekunde, omogućiti glavnu radnju bez nepotrebnog razmišljanja i prikazati samo trenutačno
potrebne informacije.

## Alignment gate prema postojećem kodu

Postojeći frontend koristi `#ffc51b` za accent 400 i obitelj `#064b96` / `#063c78` /
`#003773` za brand plavu. Dovršeni visual acceptance dokazi također imaju pojedine plave
primarne akcije prema screen PNG-ovima. DS-001 traži `#f8b91b` i `#0f4d80`, žutu primarnu
akciju te uvodi narančastu.

Prije sljedećeg poslovnog UI ekrana treba izvršiti Phase 1.7 audit: usporediti UI kit,
canonical screen PNG-ove i postojeće tokene; odlučiti jesu li DS vrijednosti globalna
zamjena ili semantičko proširenje. Ne raditi masovni restyle dovršenih ekrana bez novog
visual regression pregleda. Security, accessibility i destructive-action semantika imaju
prednost nad čistom bojom gumba.
