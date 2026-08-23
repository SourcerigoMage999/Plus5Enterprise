# DOCKER_DEPLOYMENT_STANDARD

## Status

**MANDATORY v1.0 — 2026-08-23**

Docker se koristi tijekom razvoja i kao standardni deployment artifact. VPS produkcijski deployment dolazi kasnije prema ROADMAP-u.

## 1. Docker image standard

Backend image mora:

- koristiti multi-stage build
- buildati iz zaključanog SDK targeta
- final runtime image sadržavati samo runtime artefakte
- raditi kao non-root user
- imati eksplicitni listening port
- imati health endpoint integriran s container healthcheckom kada je prikladno
- ne sadržavati source secrets, `.git`, lokalne certifikate ni dev artefakte
- biti reproducibilan iz clean checkouta

Frontend se može:

- buildati u zasebnom stageu i servirati iza web/reverse-proxy sloja, ili
- deployati odvojeno ako se kasnije tako odluči.

Promjena topologyja zahtijeva ADR ako mijenja production architecture.

## 2. `.dockerignore`

Mora isključiti najmanje:

- `.git`
- build outpute (`bin`, `obj`, frontend dist/node_modules gdje nisu potrebni kao context)
- IDE metadata
- test artifacts koji nisu potrebni buildu
- `.env` i secrets
- lokalne certifikate/ključeve

## 3. Configuration

Image je environment-agnostic.

- nema baked-in production connection stringa
- nema API ključeva u image layerima
- runtime konfiguracija kroz environment/secrets
- Development i Production koriste isti kod/image gdje je praktično, različitu konfiguraciju

## 4. Docker Compose — local development

Compose smije orkestrirati:

- API
- frontend po potrebi
- SQL Server
- kasnije samo one infrastructure servise koje ROADMAP stvarno uvede

DB port lokalno može biti bindan na loopback za dev. Produkcijski DB port ne smije biti javno izložen internetu.

Named volume koristiti za lokalne DB podatke kada persistence treba preživjeti restart containera.

## 5. Health checks

Razlikovati:

- **liveness** — proces/aplikacija radi
- **readiness** — aplikacija može posluživati stvarni promet i ključne dependencyje

Ne stavljati svaki vanjski provider u liveness i time uzrokovati restart storm.

## 6. Database migrations

- migration nije side effect svake API instance na startupu u production multi-instance scenariju
- deployment pipeline/runbook izvršava migracije kontrolirano prije/promišljeno uz rollout
- migration mora završiti prije nego nova verzija ovisi o novoj shemi
- destruktivne promjene zahtijevaju kompatibilan migration plan

## 7. Initial VPS topology

Kada ROADMAP dođe do produkcijskog deploymenta, prihvatljiv početni topology može biti:

```text
Internet
  |
Cloudflare/DNS (ako se odabere)
  |
VPS firewall
  |
Reverse proxy (TLS)
  |
Docker network
  |-- PLUS 5 API container
  |-- frontend/static web container ili proxy-served build
  |-- SQL Server container/service (private only) *
```

`*` SQL Server na istom VPS-u je dopušten kao inicijalna odluka samo ako sizing, persistence i backup/restore zadovolje produkcijske zahtjeve. Arhitektura mora omogućiti kasnije preseljenje baze na odvojeni host bez promjene business koda.

## 8. VPS security baseline

Prije produkcije:

- SSH key auth
- password/root remote login ograničen/isključen prema runbooku
- firewall allowlist samo nužnih portova
- HTTPS TLS certifikat i automatska obnova
- DB port nije javno otvoren
- automatic/security OS updates politika definirana
- Docker daemon nije remote-public
- application containers non-root
- secrets s minimalnim permissionsima

## 9. Persistent data

- container filesystem nije mjesto za trajne korisničke podatke
- SQL data na persistent volume/disk
- uploaded materials u object storage strategiji kada se uvede; ne oslanjati se na ephemeral API container disk

## 10. Backup / restore

Produkcija nije odobrena dok nije definirano i TESTIRANO:

- DB backup schedule
- retention
- off-server/offsite kopija
- encryption/access control
- restore postupak
- dokaz da se backup stvarno može vratiti

RPO/RTO se zaključavaju prije production releasea.

## 11. Observability

Minimalno za VPS release:

- structured application logs
- disk/memory/CPU monitoring
- container restart status
- health endpoint monitoring
- DB storage capacity alert
- backup failure alert

## 12. Scaling path

Ne graditi ga unaprijed, ali arhitektura mora omogućiti:

1. veći VPS (vertical scaling)
2. odvajanje SQL Servera/storagea
3. više stateless API instanci iza reverse proxy/load balancera
4. cache/background infrastructure tek ako profiling pokaže potrebu

Za prelazak na više API instanci mora se provjeriti session/state, distributed locking i background-worker ownership.

## 13. Performance proof

Prije tvrdnje o određenom broju istovremenih korisnika:

- definirati reprezentativne scenarije
- load test API + DB
- pratiti p95/p99 latency, error rate, CPU, memory, DB waits/connections
- identificirati bottleneck
- optimizirati na temelju mjerenja

10.000+ ukupnih korisnika je arhitekturni cilj; konkretan concurrency nije obećanje bez testa i odgovarajućeg VPS sizinga.
