# Popis svih ekrana koje vidi u#U010ditelj

> Izvor: `Za programera - novo/Popis svih ekrana koje vidi u#U010ditelj.docx`
> Status: automatski pretvoreno iz DOCX-a radi AI čitljivosti. Izvorni vizual/PNG ostaje mjerodavan za izgled.

PLUS 5 – KOMPLETNA MAPA KORISNIČKIH EKRANA

Važna napomena: ne računam popup, dropdown, tooltip, potvrdu brisanja i slične elemente kao zaseban ekran. Računam ekran kada korisnik dolazi na novu radnu površinu/stranicu s vlastitom funkcijom. Ako kasnije utvrdimo da neki modal mora biti kompleksan, možemo ga posebno nacrtati.

0. Stalni okvir aplikacije

Na svakom glavnom ekranu postoji isti lijevi sidebar. To je važno jer iz bilo kojeg od tih ekrana korisnik može izravno otvoriti:

Radni stol → Učenici → Raspored → Materijali → Priprema sata → PLUS 5 Ploča → Domaće zadaće → Poruke → Izvještaji → Financije → Postavke

Ne bih dopustio da različiti postojeći mockupovi imaju različite glavne menije. To trebamo sada zaključati u jednu V1 navigaciju. To je i u skladu s pravilom Design Systema da glavne stavke ostaju iste između ekrana.

1. RADNI STOL

1.1 Radni stol učitelja – svi klikabilni elementi

2. UČENICI

2.1 Popis učenika

→ učenik → 2.2 Digitalni dosje učenika
→ „Dodaj učenika” → 2.3 Novi učenik
→ naziv grupe → 2.8 Detalj grupe
→ „Pogledaj sve grupe” → 2.7 Grupe
→ poruka uz učenika → 9.2 Razgovor

2.2 Digitalni dosje učenika

→ „Učenici” / breadcrumb → 2.1 Popis učenika

→ „Uredi učenika” → 2.6 Uredi učenika

→ naziv grupe „Grammar 8A” → 2.8 Detalj grupe

→ „Pogledaj detalje spremnosti” → 2.4 Procjena spremnosti učenika

→ „Otvori plan sata” → 5.3 Plan sata

→ „Pogledaj detaljno” u Napretku po područjima → 2.5 Detalj znanja učenika

→ pojedini korišteni materijal → 4.2 Pregled materijala

→ „Pogledaj sve” kod korištenih materijala → 4.1 Biblioteka materijala, filtrirana na tog učenika

→ pojedina nedavna aktivnost → detalj aktivnosti / održanog sata (broj ekrana još nije definiran)

→ „Pogledaj sve” kod Nedavnih aktivnosti → Povijest aktivnosti učenika (broj ekrana još nije definiran)

→ „Pogledaj sve” kod Posljednjeg sata → Povijest sati učenika (broj ekrana još nije definiran)

→ „Pošalji poruku roditelju” → 9.2 Razgovor

→ posljednja komunikacija / „Pogledaj sve” → 9.2 Razgovor

→ „Zakaži termin” → 3.3 Novi termin

→ bilješke učitelja / „Pogledaj sve” → Bilješke učenika (broj ekrana još nije definiran)

2.3 Novi učenik

→ „Učenici” / breadcrumb → 2.1 Popis učenika

→ „+ Novi program” iz polja Program → 12.x Novi program

→ „Spremi učenika” → 2.2 Digitalni dosje učenika

→ „Odustani” → 2.1 Popis učenika

2.4 Procjena spremnosti učenika

→ „Učenici” u breadcrumbu → 2.1 Popis učenika

→ ime učenika u breadcrumbu / „Natrag na dosje” → 2.2 Digitalni dosje učenika

→ „Pogledaj detalje znanja” → 2.5 Detalj znanja učenika

→ pojedino područje znanja (Grammar, Vocabulary, Reading, Listening, Speaking, Writing) → 2.5 Detalj znanja učenika, s unaprijed otvorenim odabranim područjem

→ pojedina tema u detaljnoj analizi (npr. Present Perfect, Irregular Verbs, Reading – Main Idea...) → 2.5 Detalj znanja učenika, s unaprijed otvorenom odabranom komponentom/temom

→ „Pripremi sat prema procjeni” → 5.1 Priprema sata, s prenesenim učenikom i područjima preporučenima za rad

→ „Izvezi izvještaj (PDF)” → generiranje/preuzimanje PDF izvještaja; ne otvara novi ekran

Plus, kao i na ostalim Teacher ekranima, stalni sidebar ostaje dostupan i vodi na zaključane glavne module aplikacije.

2.5 Detalj znanja učenika

→ Pregled po područjima → ostaje na 2.5, mijenja sadržaj taba
→ Grammar → ostaje na 2.5, otvara Grammar prikaz
→ Vocabulary → ostaje na 2.5, otvara Vocabulary prikaz
→ Reading → ostaje na 2.5, otvara Reading prikaz
→ Listening → ostaje na 2.5, otvara Listening prikaz
→ Speaking → ostaje na 2.5, otvara Speaking prikaz
→ Writing → ostaje na 2.5, otvara Writing prikaz

→ pojedina komponenta znanja/vještine → ostaje na 2.5, otvara detalj odabrane komponente
→ „Pogledaj sve komponente/vještine” → ostaje na 2.5, proširuje popis
→ „Pogledaj detalje komponente/vještine” → ostaje na 2.5, prikazuje detalj odabrane komponente

→ pojedina aktivnost / dokaz → 6.3 Detalj aktivnosti / rezultata učenika
→ „Pogledaj sve aktivnosti” → 6.2 Aktivnosti učenika

→ „Pripremi aktivnosti za sljedeći sat” → 5.1 Priprema sata, s prenesenim učenikom, područjem i identificiranom potrebom

→ „Izvezi izvještaj (PDF)” → generira/izvozi izvještaj, ne otvara novi glavni ekran
→ „Natrag na procjenu” → 2.4 Procjena spremnosti učenika
→ „Saznaj više o procjeni” → otvara informativno objašnjenje metodologije procjene, bez napuštanja 2.5

2.6 Uredi učenika

→ „Promijeni fotografiju” → otvara odabir/uploada fotografije, ostaje na 2.6

→ Program → dropdown, ostaje na 2.6
→ Način rada: Individualno / Grupa → dropdown, ostaje na 2.6
→ Grupa → dropdown, ostaje na 2.6
→ Status: Aktivan / Na čekanju / Neaktivan → dropdown, ostaje na 2.6

→ „Dodaj kontakt” → otvara obrazac za dodavanje roditelja/skrbnika, ostaje na 2.6
→ postojeći roditelj/skrbnik → uređivanje kontakta, ostaje na 2.6

→ postavke privatnosti i vidljivosti → prekidači, ostaje na 2.6

→ „Spremi promjene” → 2.2 Digitalni dosje učenika
→ „Odustani” → 2.2 Digitalni dosje učenika

→ „Izbriši učenika” → modal za potvrdu brisanja/arhiviranja → nakon potvrde 2.1 Popis učenika

2.7 Grupe

→ „+ Nova grupa” → 2.8 Nova grupa
→ „Uredi grupu” → 2.9 Uredi grupu

→ pojedina grupa → ostaje na 2.7, prikazuje detalj odabrane grupe u desnom panelu
→ pretraživanje grupa → ostaje na 2.7
→ filter programa/statusa → ostaje na 2.7

Tab „Učenici u grupi”

→ pojedini učenik → 2.2 Digitalni dosje učenika
→ „+ Dodaj učenika u grupu” → otvara odabir postojećih učenika, ostaje na 2.7
→ ••• uz učenika → otvara akcije za učenika u grupi, ostaje na 2.7
→ „Ukloni iz grupe” → potvrda uklanjanja, ostaje na 2.7

Tab „Raspored”

→ Raspored → ostaje na 2.7, mijenja sadržaj taba
→ pojedini termin grupe → 3.2 Detalj termina
→ „Pogledaj cijeli raspored” → 3.1 Raspored

Tab „Materijali”

→ Materijali → ostaje na 2.7, mijenja sadržaj taba
→ pojedini materijal → 4.2 Pregled materijala
→ „Pogledaj sve materijale” → 4.1 Biblioteka materijala

Tab „Bilješke”

→ Bilješke → ostaje na 2.7, mijenja sadržaj taba
→ „Uredi bilješku” → uređivanje bilješke grupe, ostaje na 2.7

Ostale akcije

→ „Izvezi izvještaj (PDF)” → generira/preuzima izvještaj grupe, ne otvara novi glavni ekran

2.8 Nova grupa

→ Naziv grupe → unos, ostaje na 2.8
→ Program → dropdown postojećih programa, ostaje na 2.8
→ Razred / razina → dropdown, ostaje na 2.8
→ Opis grupe → unos, ostaje na 2.8
→ Maksimalan broj učenika → unos, ostaje na 2.8
→ Lokacija / učionica → dropdown, ostaje na 2.8

Raspored grupe

→ Dan održavanja → dropdown, ostaje na 2.8
→ Vrijeme početka / završetka → odabir vremena, ostaje na 2.8
→ „+ Dodaj termin” → dodaje novi termin, ostaje na 2.8
→ ikona za uklanjanje termina → uklanja termin, ostaje na 2.8

Učenici u grupi

→ pretraživanje učenika → ostaje na 2.8
→ odabir učenika → checkbox, ostaje na 2.8
→ „Pogledaj sve učenike” → 2.1 Popis učenika

Materijali i plan rada

→ odabir početnog materijala → dropdown, ostaje na 2.8
→ „+ Dodaj materijal” → otvara odabir materijala, ostaje na 2.8
→ pojedini materijal → 4.2 Pregled materijala

Ciljevi grupe

→ odabir/dodavanje ciljeva grupe → ostaje na 2.8
→ ciljana razina → dropdown, ostaje na 2.8

Završne akcije

→ „Kreiraj grupu” → stvara grupu → 2.7 Grupe
→ „Odustani” → 2.7 Grupe

2.9 Uredi grupu

→ Naziv grupe → uređivanje, ostaje na 2.9
→ Program / fokus → dropdown postojećih programa, ostaje na 2.9
→ Razred / razina → dropdown, ostaje na 2.9
→ Opis grupe → uređivanje, ostaje na 2.9
→ Boja grupe → odabir, ostaje na 2.9
→ Maksimalan broj učenika → uređivanje, ostaje na 2.9
→ Minimalan broj za održavanje → uređivanje, ostaje na 2.9
→ Lokacija / učionica → dropdown, ostaje na 2.9
→ Status grupe → Aktivan / Na čekanju / Neaktivan, ostaje na 2.9

Raspored grupe

→ Dan održavanja → dropdown, ostaje na 2.9
→ Vrijeme početka / završetka → uređivanje vremena, ostaje na 2.9
→ „+ Dodaj još jedan termin” → dodaje termin, ostaje na 2.9
→ ikona za uklanjanje termina → uklanja termin, ostaje na 2.9
→ Trajanje sata → dropdown, ostaje na 2.9
→ Datum početka → odabir datuma, ostaje na 2.9
→ Datum završetka → odabir datuma, ostaje na 2.9

Učenici u grupi

→ „Članovi grupe” → ostaje na 2.9, prikazuje trenutačne članove
→ „Dostupni učenici” → ostaje na 2.9, prikazuje učenike koje je moguće dodati
→ pretraživanje učenika → ostaje na 2.9
→ filter učenika → ostaje na 2.9
→ pojedini učenik / ime učenika → 2.2 Digitalni dosje učenika
→ dodaj učenika u grupu → ostaje na 2.9
→ ••• uz učenika → otvara akcije člana grupe, ostaje na 2.9
→ „Ukloni iz grupe” → potvrda uklanjanja, ostaje na 2.9
→ „Pogledaj sve učenike” → 2.1 Popis učenika

Materijali grupe

→ početni materijal → odabir, ostaje na 2.9
→ „+ Dodaj materijal” → odabir iz postojeće biblioteke, ostaje na 2.9
→ pojedini materijal → 4.2 Pregled materijala
→ ukloni materijal → ostaje na 2.9

Ciljevi grupe

→ Ciljana razina → dropdown, ostaje na 2.9
→ postojeći cilj → uređivanje/uklanjanje, ostaje na 2.9
→ „Dodaj cilj” → dodaje novi cilj grupe, ostaje na 2.9

Bilješke i postavke

→ Bilješke o grupi → uređivanje, ostaje na 2.9
→ „Grupu prikaži u rasporedu i izvještajima” → toggle, ostaje na 2.9
→ „Dopušteno korištenje na PLUS 5 Ploči” → toggle, ostaje na 2.9
→ „Vidljiva drugim nastavnicima” → toggle, ostaje na 2.9

Završne akcije

→ „Spremi promjene” → sprema promjene → 2.7 Grupe
→ „Odustani” → 2.7 Grupe
→ „Izbriši grupu / Arhiviraj grupu” → modal za potvrdu → nakon potvrde 2.7 Grupe

3. RASPORED

3.1 Raspored / Kalendar

Glavni kalendar

→ pojedini termin → 3.2 Detalj termina
→ „Tjedan” → ostaje na 3.1, prikazuje tjedni raspored
→ „Dan” → ostaje na 3.1, prikazuje dnevni raspored
→ „Danas” → ostaje na 3.1, vraća prikaz na današnji datum
→ strelica prethodno / sljedeće → ostaje na 3.1, mijenja prikazani tjedan ili dan

Novi termin

→ „+ Novi termin” → 3.3 Novi termin

Mini kalendar

→ pojedini datum → ostaje na 3.1, prikazuje odabrani datum/tjedan
→ prethodni / sljedeći mjesec → ostaje na 3.1

Filteri

→ Grupa → filter, ostaje na 3.1
→ Program → filter, ostaje na 3.1
→ Učionica → filter, ostaje na 3.1
→ „Prikaži samo moje termine” → toggle, ostaje na 3.1

Sažetak tjedna

→ „Pogledaj detaljan izvještaj” → 10.1 Izvještaji, s unaprijed odabranim podacima za raspored/radne sate i prikazani period

Podsjetnici

→ pojedini podsjetnik na termin → 3.2 Detalj termina
→ „Pogledaj sve podsjetnike” → ostaje na 3.1, otvara prošireni prikaz podsjetnika

3.2 Detalj termina

Osnovni podaci termina

→ Grupa / naziv grupe „Grammar 8A” → 2.7 Grupe, s otvorenom odabranom grupom
→ Program „Grammar Focus” → ostaje na 3.2
→ Datum termina → ostaje na 3.2
→ Vrijeme termina → ostaje na 3.2
→ Učionica → ostaje na 3.2
→ Status termina → ostaje na 3.2

Glavne akcije

→ „Uredi termin” → 3.4 Uredi termin
→ „Pokreni sat” → 6.1 PLUS 5 Ploča

Ako plan sata još nije napravljen:

→ „Pripremi sat” → 5.1 Priprema sata

Ako plan već postoji:

→ „Otvori plan sata” → 5.3 Plan sata

Učenici

→ ime / fotografija pojedinog učenika → 2.2 Digitalni dosje učenika
→ Status dolaska → Prisutan / Odsutan, ostaje na 3.2
→ ••• uz učenika → otvara dodatne akcije učenika, ostaje na 3.2
→ „Dodaj učenika u termin” → odabir učenika, ostaje na 3.2

Tema i cilj sata

→ Tema sata → ostaje na 3.2
→ Cilj sata → ostaje na 3.2
→ Grammar → ostaje na 3.2
→ Speaking → ostaje na 3.2
→ Vocabulary → ostaje na 3.2

Ovi elementi ovdje prvenstveno prikazuju podatke povezane s planom sata i ne otvaraju zasebne ekrane.

Materijali za ovaj sat

→ pojedini materijal → 4.2 Pregled materijala
→ „+ Dodaj materijal” → odabir iz 4.1 Biblioteke materijala, zatim povratak na 3.2

Domaća zadaća

Ako domaća zadaća postoji:

→ pojedina domaća zadaća → detalj domaće zadaće u modulu 8. Domaće zadaće
→ „Uredi domaću zadaću” → uređivanje postojeće domaće zadaće

Ako domaća ne postoji:

→ „+ Dodaj domaću zadaću” → 8.2 Nova domaća zadaća, s unaprijed odabranim učenikom/grupom i terminom

Napomene učitelja

→ polje Napomene učitelja → unos/uređivanje napomene, ostaje na 3.2

Akcije termina

→ „Otkaži termin” → modal potvrde → ostaje na 3.2, status postaje Otkazan
→ „Dupliciraj termin” → 3.3 Novi termin, s unaprijed popunjenim podacima postojećeg termina
→ „Pošalji podsjetnik učenicima” → modal/potvrda slanja, ostaje na 3.2

Povezano

→ Grupa „Grammar 8A” → 2.7 Grupe, otvorena Grammar 8A
→ Program „Grammar Focus” → ostaje na 3.2
→ Učionica → ostaje na 3.2

Povijest termina

→ prikaz događaja vezanih uz termin → ostaje na 3.2

Ne otvaramo novi ekran samo zbog povijesti termina.

Nakon održanog sata

Kada je status termina Održan, 3.2 prikazuje sažetak održanog sata.

→ „Pogledaj rezultate sata” → odgovarajući detaljni izvještaj održanog sata u modulu 10. Izvještaji
→ pojedini učenik u rezultatima → 2.2 Digitalni dosje učenika

Breadcrumb

→ „Raspored” → 3.1 Raspored
→ „Grammar 8A” → 2.7 Grupe, otvorena Grammar 8A
→ datum termina → ostaje na 3.2

3.3 Novi termin

Osnovni podaci

→ „Grupni sat” → odabir načina rada, ostaje na 3.3
→ „Individualni sat” → odabir načina rada, ostaje na 3.3

Ako je odabran Grupni sat:

→ Grupa → dropdown postojećih grupa, ostaje na 3.3
→ odabrana grupa / „Grammar 8A” → podaci grupe automatski se učitavaju, ostaje na 3.3

Ako je odabran Individualni sat:

→ Učenik → dropdown/pretraživanje postojećih učenika, ostaje na 3.3

→ Naziv termina → unos, ostaje na 3.3
→ Opis / Napomena → unos, ostaje na 3.3

Datum i vrijeme

→ Datum → odabir datuma, ostaje na 3.3
→ Vrijeme početka → odabir vremena, ostaje na 3.3
→ Vrijeme završetka → odabir vremena, ostaje na 3.3
→ Trajanje → automatski izračun, ostaje na 3.3

Ponavljanje

→ „Ne ponavlja se” → jednokratni termin, ostaje na 3.3
→ „Redovno – svaki tjedan” → uključuje postavke ponavljanja, ostaje na 3.3
→ Dan u tjednu → odabir, ostaje na 3.3
→ Broj ponavljanja / datum završetka → odabir, ostaje na 3.3

Ako odabrana grupa već ima redoviti raspored:

→ „Dodaj dodatni termin” → nastavlja stvaranje pojedinačnog termina na 3.3
→ „Promijeni redoviti raspored grupe” → 2.9 Uredi grupu

Lokacija

→ Učionica / lokacija → dropdown, ostaje na 3.3
→ Online sat → unos poveznice za online nastavu, ostaje na 3.3

Dodatne postavke

→ Boja termina → odabir, ostaje na 3.3
→ Podsjetnik za učitelja → dropdown, ostaje na 3.3
→ Podsjetnik za učenike → dropdown, ostaje na 3.3

Obavijesti

→ „Automatski obavijesti učenike / roditelje” → uključivanje/isključivanje, ostaje na 3.3
→ Poruka → uređivanje automatski pripremljene poruke, ostaje na 3.3

Sažetak termina

→ Sažetak termina → samo prikaz podataka, ostaje na 3.3

Sažetak se automatski ažurira prema podacima unesenima u obrazac.

Završne akcije

→ „Spremi termin” → provjera konflikata → stvara termin → 3.2 Detalj termina

→ „Odustani” → 3.1 Raspored

Ako postoje nespremljene promjene:

→ potvrda napuštanja → modal, ostaje na 3.3
→ „Napusti bez spremanja” → 3.1 Raspored
→ „Nastavi uređivati” → ostaje na 3.3

Provjera konflikata

Ako PLUS 5 pronađe konflikt termina:

→ upozorenje o konfliktu → ostaje na 3.3
→ „Promijeni vrijeme” → ostaje na 3.3
→ „Promijeni učionicu” → ostaje na 3.3
→ postojeći konfliktni termin → 3.2 Detalj termina tog termina

Breadcrumb

→ „Raspored” → 3.1 Raspored
→ „Novi termin” → ostaje na 3.3

3.4 Uredi termin

Osnovni podaci

→ „Grupni sat” → odabir načina rada, ostaje na 3.4
→ „Individualni sat” → odabir načina rada, ostaje na 3.4

Ako je grupni sat:

→ Grupa → dropdown postojećih grupa, ostaje na 3.4
→ odabrana grupa / „Grammar 8A” → ostaje na 3.4

Ako je individualni sat:

→ Učenik → dropdown/pretraživanje učenika, ostaje na 3.4

→ Naziv termina → uređivanje, ostaje na 3.4
→ Opis / Napomena → uređivanje, ostaje na 3.4

Datum i vrijeme

→ Datum → promjena datuma, ostaje na 3.4
→ Vrijeme početka → promjena vremena, ostaje na 3.4
→ Vrijeme završetka → promjena vremena, ostaje na 3.4
→ Trajanje → automatski izračun, ostaje na 3.4

Lokacija

→ Učionica → dropdown, ostaje na 3.4
→ Online sat / poveznica → unos ili uređivanje poveznice, ostaje na 3.4

Opseg promjene

Ako je termin dio ponavljajućeg rasporeda:

→ „Samo ovaj termin” → promjena vrijedi samo za odabrani termin, ostaje na 3.4

→ „Svi budući termini ove grupe” → promjena se primjenjuje na buduće termine serije, ostaje na 3.4

→ „Otvori 2.9 Uredi grupu” → 2.9 Uredi grupu

Dodatne postavke

→ Boja termina → odabir boje, ostaje na 3.4
→ Podsjetnik za učitelja → dropdown, ostaje na 3.4
→ Podsjetnik za učenike → dropdown, ostaje na 3.4

Mogući konflikti

Ako nema konflikta:

→ „Nema konflikata” → informativni prikaz, ostaje na 3.4

Ako postoji konflikt:

→ konfliktni termin → 3.2 Detalj termina konfliktnog termina
→ „Promijeni vrijeme” → ostaje na 3.4
→ „Promijeni učionicu” → ostaje na 3.4

Trenutni termin

→ Trenutni termin → informativni prikaz postojećih podataka, ostaje na 3.4

Spremanje

→ „Spremi promjene” → provjera podataka i konflikata

Ako promjena ne zahtijeva obavještavanje:

→ spremanje → 3.2 Detalj termina

Ako je promijenjen datum, vrijeme, lokacija ili drugi podatak važan učenicima:

→ modal „Želite li obavijestiti učenike/roditelje?”

Iz modala:

→ „Spremi i pošalji obavijest” → spremanje + slanje obavijesti → 3.2 Detalj termina

→ „Spremi bez obavijesti” → spremanje → 3.2 Detalj termina

→ „Odustani” → zatvara modal, ostaje na 3.4

Otkaži termin

→ „Otkaži termin” → modal potvrde

Iz modala:

→ „Potvrdi otkazivanje” → status termina postaje Otkazan → 3.2 Detalj termina

→ „Otkaži i obavijesti učenike/roditelje” → otkazivanje + obavijest → 3.2 Detalj termina

→ „Odustani” → ostaje na 3.4

Dupliciraj termin

→ „Dupliciraj termin” → 3.3 Novi termin, s unaprijed popunjenim podacima postojećeg termina

Odustani od uređivanja

→ „Odustani” → 3.2 Detalj termina

Ako postoje nespremljene promjene:

→ modal potvrde napuštanja

→ „Napusti bez spremanja” → 3.2 Detalj termina

→ „Nastavi uređivati” → ostaje na 3.4

Breadcrumb

→ „Raspored” → 3.1 Raspored
→ „Detalj termina” → 3.2 Detalj termina
→ „Uredi termin” → ostaje na 3.4

4. MATERIJALI

4.1 Biblioteka materijala

Zaglavlje

→ „+ Novi materijal” → 4.3 Novi materijal

Glavni prikazi biblioteke

→ „Moji materijali” → mijenja prikaz, ostaje na 4.1
→ „Dijeljeni sa mnom” → mijenja prikaz, ostaje na 4.1

Pretraživanje i filtriranje

→ Pretraži materijale → filtrira rezultate, ostaje na 4.1
→ „Filteri” → otvara/zatvara filtere, ostaje na 4.1

Filteri:

→ Predmet → odabir, ostaje na 4.1
→ Program → odabir, ostaje na 4.1
→ Razred → odabir, ostaje na 4.1
→ Vrsta materijala → odabir, ostaje na 4.1
→ Oznake → odabir, ostaje na 4.1
→ „Prikaži samo moje materijale” → uključivanje/isključivanje, ostaje na 4.1
→ „Poništi sve” → uklanja filtere, ostaje na 4.1

Sortiranje i način prikaza

→ „Sortiraj prema” → dropdown, ostaje na 4.1
→ Kartice / Grid → mijenja način prikaza, ostaje na 4.1
→ Lista → mijenja način prikaza, ostaje na 4.1

Pojedini materijal

→ fotografija / preview materijala → 4.2 Pregled materijala
→ naziv materijala → 4.2 Pregled materijala

Primjer:

→ „Present Perfect – pravila” → 4.2 Pregled materijala

„•••” uz pojedini materijal

→ „Otvori” → 4.2 Pregled materijala
→ „Uredi” → 4.4 Uredi materijal
→ „Dupliciraj” → stvara kopiju, ostaje na 4.1
→ „Dodaj u pripremu sata” → modal za odabir pripreme, ostaje na 4.1
→ „Podijeli” → modal dijeljenja, ostaje na 4.1
→ „Arhiviraj” → modal/potvrda → ostaje na 4.1
→ „Obriši” → modal/potvrda → ostaje na 4.1

Dodaj u pripremu sata

Nakon klika „Dodaj u pripremu sata”:

→ odabir pripreme/sata → ostaje u modalu na 4.1
→ „Dodaj” → povezuje materijal s odabranom pripremom → ostaje na 4.1
→ „Odustani” → zatvara modal → ostaje na 4.1

Dijeljenje materijala

→ odabir učitelja / korisnika → ostaje u modalu na 4.1
→ „Podijeli” → dijeli materijal → ostaje na 4.1
→ „Odustani” → zatvara modal → ostaje na 4.1

Vrste materijala

Klik na pojedinu vrstu primjenjuje filter:

→ Prezentacije → filtrirani 4.1
→ Radni listovi → filtrirani 4.1
→ Kartice za razgovor → filtrirani 4.1
→ Interaktivne vježbe → filtrirani 4.1
→ Video → filtrirani 4.1
→ Audio → filtrirani 4.1
→ Kvizovi → filtrirani 4.1
→ Slike → filtrirani 4.1
→ Plakati → filtrirani 4.1
→ Mape → filtrirani 4.1

→ „Pogledaj sve oznake” → prošireni prikaz oznaka/filtera, ostaje na 4.1

Nedavno dodano

→ pojedini nedavno dodani materijal → 4.2 Pregled materijala
→ „Pogledaj sve nedavne” → filtrirani/sortirani prikaz na 4.1

Materijali preporučeni za konkretan sat

Kada je 4.1 otvoren iz 5.1 Pripreme sata:

→ preporučeni materijal → 4.2 Pregled materijala
→ „Dodaj u sat” → dodaje materijal u aktivnu pripremu → povratak na 5.1 Priprema sata
→ „Natrag na pripremu sata” → 5.1 Priprema sata

4.2 Pregled materijala

Ovaj sitemap vrijedi i za obične nastavne materijale i za interaktivne zadatke, uz dodatne funkcije procjene kada materijal može generirati podatke o znanju učenika.

Zaglavlje materijala

→ „Uredi materijal” → 4.4 Uredi materijal
→ „Dodaj u pripremu sata” → modal odabira pripreme, ostaje na 4.2
→ „Više akcija” → dropdown, ostaje na 4.2

Iz „Više akcija”:

→ „Dupliciraj” → stvara kopiju materijala → 4.2 Pregled kopiranog materijala
→ „Podijeli” → modal dijeljenja, ostaje na 4.2
→ „Arhiviraj” → potvrda → 4.1 Biblioteka materijala
→ „Obriši” → potvrda → 4.1 Biblioteka materijala

Rad s materijalom

→ „Otvori zadatak” / „Otvori materijal” → otvara materijal u punom prikazu, ostaje unutar 4.2

→ „Preuzmi” → preuzimanje materijala

→ „Kopiraj link” → kopira poveznicu, ostaje na 4.2

→ „•••” → dodatne akcije, ostaje na 4.2

Tabovi

→ „Pregled zadatka” → ostaje na 4.2, mijenja sadržaj

→ „Zadaci i procjena” → ostaje na 4.2, prikazuje strukturu procjenjivih zadataka

→ „Povezanost s učenjem” → ostaje na 4.2, prikazuje Knowledge Components, ciljeve i kurikularnu povezanost

→ „Korištenje” → ostaje na 4.2, prikazuje gdje je materijal korišten ili planiran

Pregled interaktivnog zadatka

Ako materijal sadrži više zadataka:

→ Zadatak 1 → prikazuje Zadatak 1, ostaje na 4.2
→ Zadatak 2 → prikazuje Zadatak 2, ostaje na 4.2
→ Zadatak 3 → prikazuje Zadatak 3, ostaje na 4.2
→ ...
→ posljednji zadatak → prikazuje odgovarajući zadatak, ostaje na 4.2

→ „Prethodni zadatak” → ostaje na 4.2
→ „Sljedeći zadatak” → ostaje na 4.2

→ Desktop preview → mijenja preview, ostaje na 4.2
→ Tablet preview → mijenja preview, ostaje na 4.2
→ Mobile preview → mijenja preview, ostaje na 4.2

Rezultati učenika

→ „Rezultati učenika” → prikaz rezultata učenika za ovaj materijal

Ovdje bih predvidio zaseban ekran:

→ 4.5 Rezultati materijala

jer to više nije samo svojstvo materijala nego zasebna analiza svih pokušaja učenika.

Iz 4.5 će se kasnije moći ići prema konkretnom učeniku, pokušaju i njegovu Knowledge Modelu.

Zadaci i procjena

Za svaki procjenjivi zadatak prikazuju se njegova metadata.

Klik na pojedini zadatak:

→ Zadatak → detalji zadatka unutar taba, ostaje na 4.2

U detalju:

→ Knowledge Component → prikaz/povezanost komponente, ostaje na 4.2
→ Težina zadatka → informativno
→ Vrsta zadatka → informativno
→ Vrsta dokaza → informativno
→ Bodovanje / kriterij vrednovanja → informativno
→ Točan odgovor / kriterij → informativno

Promjena tih podataka ne radi se ovdje, nego:

→ „Uredi” → 4.4 Uredi materijal

Povezanost s učenjem

Meta / cilj učenja

→ Meta (cilj učenja) → prikaz cilja, ostaje na 4.2

Knowledge Components

→ pojedina Knowledge Component → prikaz detalja/povezanosti komponente, ostaje na 4.2

Primjer:

→ Present Perfect – affirmative → ostaje na 4.2
→ Present Perfect – negative → ostaje na 4.2
→ Irregular verbs – past participle → ostaje na 4.2
→ Time expressions → ostaje na 4.2

→ „Dodaj komponentu” → uređivanje metadata → 4.4 Uredi materijal

Tagovi

→ pojedini tag → filtriranje/povezani materijali, ostaje na 4.2 ili otvara filtrirani 4.1 Biblioteka materijala

→ „Dodaj tag” → 4.4 Uredi materijal

Važno ostaje zaključano:

tag ≠ Knowledge Component

Tagovi služe organizaciji i pretraživanju, dok Knowledge Components sudjeluju u modelu znanja.

Kurikulum i standardi

U tabu Povezanost s učenjem:

→ Razred → informativno
→ CEFR razina → informativno
→ Kurikularni ishod → detalj povezanog ishoda unutar 4.2

Uređivanje:

→ „Uredi povezanost” → 4.4 Uredi materijal

Procjena spremnosti učenika

Ako materijal ima Knowledge Components relevantne za učenike:

→ pojedini učenik → 2.4 Procjena spremnosti učenika

Primjer:

→ Petar Horvat – 84 % → 2.4 Procjena spremnosti učenika – Petar Horvat

→ Sara Perić – 38 % → 2.4 Procjena spremnosti učenika – Sara Perić

→ „Pogledaj detalje spremnosti učenika” → 2.4 Procjena spremnosti učenika

Ovdje prikazani postotak nije rezultat samo ovog materijala, nego rezultat relevantnog Knowledge Modela učenika.

Sažetak procjene materijala

→ broj zadataka → informativno
→ vrste dokaza → informativno
→ težina zadataka → informativno
→ Knowledge Components koje materijal procjenjuje → informativno / prikaz detalja na 4.2

Dokazno povezivanje — Evidence

→ „Evidence / Dokazno povezivanje” → informativni prikaz načina na koji rezultati ulaze u Knowledge Model

Za svaki učenikov pokušaj sustav može generirati:

Task → Attempt → Evidence Event → Knowledge Component

Ovaj proces odvija se automatski i učitelj ga na 4.2 ne uređuje ručno.

Povezano s pripremama i satovima

→ pojedina priprema sata → odgovarajući ekran 5.3 Plan sata

Primjer:

→ „Grammar 8A – 25.8.2026.” → 5.3 Plan sata

Ako je riječ o već održanom terminu:

→ pojedini održani sat/termin → 3.2 Detalj termina

→ „Pogledaj sve korištene/planirane” → tab Korištenje na 4.2

Dodaj u pripremu sata

→ „Dodaj u pripremu sata” → modal

Iz modala:

→ odabir pripreme sata → ostaje u modalu
→ „Dodaj” → povezuje materijal s pripremom → ostaje na 4.2
→ „Odustani” → zatvara modal → ostaje na 4.2

Breadcrumb

→ „Materijali” → 4.1 Biblioteka materijala
→ „Biblioteka materijala” → 4.1 Biblioteka materijala
→ naziv trenutnog materijala → ostaje na 4.2

4.3 Novi materijal

Za ručno stvaranje materijala.

4.4 Uvoz vlastitog materijala

Upload PDF/DOCX/slike itd.

4.5 Uredi materijal

→ spremi → 4.2 Pregled materijala

5. PRIPREMA SATA / LESSON BUILDER

Ovo je najveće stablo.

Dokumentacija kaže da Lesson Builder mora krenuti od cilja, koristiti podatke učenika, odabrati KB-ove, njihov redoslijed, trajanje, metode i materijale te učitelju omogućiti izmjene.

5.1 Početak pripreme – Cilj sata

Učitelj vidi učenika i upisuje/odabire cilj.

To je jedini obavezni novi unos prema FS-004.

→ Nastavi → 5.2 Prijedlog strukture sata

5.2 Prijedlog strukture sata

Sustav slaže potrebne KB-ove.

→ prihvati → 5.3 Plan sata
→ dodaj KB → 5.4 Odabir aktivnosti/Knowledge Blocka

5.3 Plan sata / Uređivanje pripreme

Vizual imamo.

Ovo je glavni Lesson Builder ekran.

→ aktivnost → 5.5 Detalj aktivnosti
→ materijali → 5.6 Materijali za pripremu
→ Dodaj aktivnost → 5.4 Odabir KB-a
→ Spremi i nastavi / potvrdi → 5.7 Potvrđena priprema

5.4 Odabir aktivnosti / Knowledge Blocka

Ovo je učiteljski izbor KB-a, ne administratorski ekran KB-a.

Učitelj bira npr. Reading, Speaking, Igru, Izazov, Priču, Misiju...

→ odabir → povratak na 5.3 Plan sata

5.5 Detalj / uređivanje aktivnosti

Vrlo važan ekran.

KB dokumentacija predviđa da učitelj može pregledavati, uređivati, mijenjati aktivnosti, dodavati komponente i preskakati blokove.

Tu uređujemo:

trajanje, cilj, metodu, zadatke, težinu, materijale itd.

→ materijal → 4.2
→ spremi → 5.3

5.6 Materijali za pripremu

To je upravo ekran Biblioteke koji si nacrtao s desnim stupcem „Materijali za sat”.

→ materijal → 4.2
→ dodaj → ostaje ovdje
→ natrag → 5.3

5.7 Potvrđena priprema / Sat je spreman

Ovo nam nedostaje.

FS-004 kaže da nakon potvrde plan mora biti spremljen uz učenika, materijali dostupni Ploči i priprema spremna za izvođenje.

→ Pokreni sat → 6.1 PLUS 5 Ploča

6. PLUS 5 PLOČA

6.1 Ploča učitelja

Imamo više vizuala. Trebamo zaključati jedan.

→ aktivnost u timelineu → 6.2 Aktivnost na Ploči
→ materijal → prikazuje se na Ploči
→ učenik → 6.3 Pregled rada učenika
→ Sažetak sata → 6.4 Sažetak tijekom sata
→ Završi sat → 6.5 Završetak sata

6.2 Aktivnost na Ploči

Ne mora biti potpuno novi layout, ali predstavlja radno stanje Ploče za pojedini KB.

Tu se izvršava Reading, Grammar, Game, Challenge, Story, Mission itd.

Knowledge Blockovi zato ne znače 25 potpuno različitih glavnih stranica. Oni koriste zajednički okvir Ploče, ali imaju različite sadržaje i kontrole.

6.3 Pregled rada pojedinog učenika

Posebno važno za grupne instrukcije.

Klik na npr. Saru 2/5 otvara detaljnije njezine odgovore.

→ natrag → 6.1

6.4 Sažetak sata tijekom izvođenja

Što je odrađeno, što ostaje, vrijeme, rezultati.

6.5 Završetak sata

Ovo moramo nacrtati.

Učitelj potvrđuje:

što je odrađeno, rezultat, poteškoće, bilješku, domaću zadaću i sljedeći korak.

→ Spremi i završi → 7.2 Detalj održanog sata

7. SATI / POVIJEST

Ne mora nužno biti zasebna sidebar stavka. Može biti dostupna kroz učenika i izvještaje.

7.1 Povijest sati učenika

→ pojedini sat → 7.2 Detalj održanog sata

7.2 Detalj održanog sata

Prikazuje:

planirano vs. izvedeno, aktivnosti, trajanja, rezultate, korištene materijale, domaću zadaću, bilješke.

→ materijal → 4.2
→ izvještaj → 10.2
→ pripremi sljedeći sat → 5.1

8. DOMAĆE ZADAĆE

8.1 Pregled domaćih zadaća

→ domaća → 8.3 Detalj domaće zadaće
→ Nova domaća → 8.2

8.2 Nova domaća zadaća

→ materijal/zadatak → 4.1/4.2
→ dodijeli → 8.3

8.3 Detalj domaće zadaće

Rezultat, status, odgovori, komentar.

→ učenik → 2.2

9. PORUKE

9.1 Poruke / Inbox

→ razgovor → 9.2

9.2 Razgovor s roditeljem

Isti razgovor mora biti dostupan i iz Digitalnog dosjea.

→ učenik → 2.2
→ eventualno izvještaj u poruci → 10.2

10. IZVJEŠTAJI

10.1 Izvještaji – odabir učenika / pregled

→ učenik → 10.2 Izvještaj učenika

10.2 Izvještaj učenika – Pregled

Vizual imamo.

Na mockupu već imamo tabove koji stvaraju daljnje prikaze:

10.3 Znanje

Detaljni Knowledge Model.

10.4 Aktivnosti

10.5 Zadaci

10.6 Sati

10.7 Ponašanje i angažman

To bih tretirao kao pod-ekrane istog modula, jer svaki ima dovoljno drugačiji sadržaj da ga trebamo zasebno opisati i vjerojatno zasebno nacrtati.

10.8 Izvoz / Izvještaj za roditelja

Klik Izvezi izvještaj.

Tu učitelj bira razdoblje i sadržaj.

→ pregled prije slanja → 10.9 Pregled izvještaja

10.9 Pregled izvještaja roditelju

→ Pošalji roditelju → 9.2 / potvrda slanja

11. FINANCIJE

11.1 Pregled financija

11.2 Pregled održanih/naplaćenih sati

11.3 Detalj financijske stavke

Ovaj modul zasad imamo najmanje dokumentiran, pa ne bih izmišljao dodatnu dubinu dok ga ne definiramo.

12. POSTAVKE

12.1 Postavke

Iz njega najmanje:

12.2 Moj profil

12.3 Postavke nastave

Standardno trajanje sata, način rada itd.

12.4 Cijene

12.5 Obavijesti

12.6 Račun / sigurnost

Za ovo trenutno nemamo dovoljno funkcionalne dokumentacije, pa je ovo moja izvedena struktura, a ne nešto što dokumenti već eksplicitno definiraju.

13. OBAVIJESTI

Na vrhu aplikacije imamo zvono.

13.1 Centar obavijesti

Klik na pojedinu obavijest ne treba stvarati još jedan ekran nego vodi na objekt:

novi materijal → Materijal
poruka → Razgovor
ispit → Učenik/ispit
termin → Termin
domaća → Domaća zadaća.

14. KORISNIČKI PROFIL

Klik na avatar gore desno.

14.1 Korisnički izbornik

Ako je samo dropdown, ne crtamo ga kao ekran.

Ali iz njega:

→ Moj profil → 12.2
→ Postavke → 12.1
→ Odjava → Login.

14.2 Prijava

Ovo je također pravi korisnički ekran koji nam je do sada nedostajao u mapi.

14.3 Zaboravljena lozinka

## Tablica 1

| # | Što učitelj klikne | Vodi na |
| --- | --- | --- |
| 1 | Učenik u današnjem rasporedu | 2.2 Digitalni dosje učenika |
| 2 | Današnji termin | 3.2 Detalj termina |
| 3 | „Pogledaj cijeli raspored” | 3.1 Raspored |
| 4 | Brza akcija „Materijali” | 4.1 Biblioteka materijala |
| 5 | Pojedini materijal u „Korišteni materijali” | 4.2 Pregled materijala |
| 6 | „Idi u biblioteku materijala” | 4.1 Biblioteka materijala |
| 7 | „Otvori plan sata” | 5.3 Plan sata |
| 8 | „Pokreni sat” | 6.1 PLUS 5 Ploča učitelja |
| 9 | Brza akcija „Dodaj učenika” | 2.3 Novi učenik |
| 10 | Brza akcija „Pošalji domaću” | 8.2 Nova domaća zadaća |
| 11 | Brza akcija „Kreiraj test/ispit” | 5.1 Priprema sata – unaprijed odabran cilj procjene/provjere |
| 12 | Brza akcija „Odgovori roditelju” | 9.2 Razgovor |
| 13 | Brza akcija „Financije” | 11.1 Financije |
| 14 | Pojedina obavijest | Ekran na koji se konkretna obavijest odnosi |
| 15 | „Pogledaj sve obavijesti” | 13.1 Centar obavijesti |
| 16 | Pojedina nedavna aktivnost | Ekran objekta na koji se aktivnost odnosi |
| 17 | „Pogledaj sve aktivnosti” | Trebamo još odrediti odredišni ekran |
| 18 | „Pogledaj sve učenike” | 2.1 Popis učenika |
| 19 | Zvono za obavijesti gore desno | 13.1 Centar obavijesti |
| 20 | Profil/avatar učitelja | 14.1 Korisnički izbornik |
