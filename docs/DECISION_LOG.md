# DECISION_LOG

Ovdje se zapisuju arhitekturne i trajne implementacijske odluke.

## Format

### ADR-0001 — <Naziv odluke>
- **Datum:** YYYY-MM-DD
- **Status:** Proposed / Accepted / Superseded
- **Kontekst:** ...
- **Odluka:** ...
- **Razlozi:** ...
- **Posljedice:** ...
- **Alternative:** ...

---

Trenutačno nema zaključanih tehničkih ADR-ova izvedenih samo iz ovog ZIP-a.

## Accepted decisions

### ADR-0001 — React + TypeScript frontend
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** PLUS 5 zahtijeva bogat interaktivni web UI i fazni razvoj odvojen od server business logike.
- **Odluka:** Frontend koristi React + TypeScript + Vite.
- **Razlozi:** snažan ecosystem, tipizirani contracti, modularan UI, prikladno za kompleksne interaktivne ekrane.
- **Posljedice:** frontend je API klijent; server ostaje autoritet za business/security pravila.
- **Alternative:** Blazor i drugi frontend frameworkovi nisu odabrani.

### ADR-0002 — ASP.NET Core / .NET backend
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** Potreban je siguran, testabilan backend s kompleksnom poslovnom logikom i SQL persistenceom.
- **Odluka:** Backend koristi C# + ASP.NET Core na .NET 10 baselineu.
- **Razlozi:** stabilan web stack, dobar performance, security tooling, EF Core i kvalitetna testabilnost.
- **Posljedice:** nove backend komponente moraju slijediti `BACKEND_ENGINEERING_STANDARD.md`.
- **Alternative:** nisu odabrane u trenutnom baselineu.

### ADR-0003 — SQL Server + EF Core migrations
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** PLUS 5 ima izrazito relacijske domene: korisnici, učenici, grupe, raspored, kurikulum, evidence i materijali.
- **Odluka:** Primarna OLTP baza je Microsoft SQL Server; schema evolution kroz verzionirane EF Core migracije.
- **Razlozi:** transakcijska konzistentnost, constrainti, relacijski upiti, zreo .NET integration.
- **Posljedice:** 3NF je default; ručne production schema izmjene nisu normalan workflow.
- **Alternative:** NoSQL nije odabran kao primarna baza.

### ADR-0004 — Modularni monolit prije mikroservisa
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** Cilj je siguran fazni razvoj i 10.000+ korisnika bez nepotrebne operativne složenosti.
- **Odluka:** PLUS 5 započinje kao modularni monolit s jasnim domenskim granicama i mogućnošću horizontalnog skaliranja API-ja.
- **Razlozi:** niži razvojni i operativni trošak; 10.000+ korisnika ne zahtijeva mikroservise po defaultu.
- **Posljedice:** mikroservisi/message broker/Kubernetes zahtijevaju novi ADR i izmjeren razlog.
- **Alternative:** microservices-first je odbijen.

### ADR-0005 — Docker kao deployment artifact; VPS kasnije
- **Datum:** 2026-08-23
- **Status:** Accepted
- **Kontekst:** Razvoj koristi Docker, a produkcija će kasnije biti deployana na VPS.
- **Odluka:** Aplikacija se pakira u Docker image; Docker Compose je dopušten za lokalni razvoj i inicijalni single-VPS topology. Produkcijski VPS detalji zaključavaju se u release/deployment fazi.
- **Razlozi:** reproducibilan environment, jednostavniji deployment i kasniji scaling path.
- **Posljedice:** image mora biti non-root, bez secreta; DB mora ostati private; backup/restore i TLS su production gateovi.
- **Alternative:** manual host deployment bez containera nije standardni put.
