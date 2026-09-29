# Material foundation

## Status

Phase 6.1 contract je zaključan SA/Product odlukom od 2026-09-29. Implementacija je
**FINAL LOCKED — odobreno 2026-09-29.** Ovaj dokument je source of truth za Material identitet,
verzioniranje, fizičku datoteku, ownership/share granicu i storage/security strategiju.

## Granica agregata

`Material` je stabilni Teacher-owned identitet. Owner dolazi isključivo iz autentificirane
serverske sesije i ne mijenja se dijeljenjem. `MaterialVersion` je immutable snapshot nakon
aktivacije, a `MaterialFile` pripada točno jednoj verziji i opisuje točno jedan fizički objekt.

```text
Material:        Active -> Archived
MaterialVersion: Draft -> Active -> Superseded
MaterialFile:    PendingUpload -> Uploaded -> Scanning -> Clean | Quarantined | Failed
                PendingUpload | Uploaded -> Rejected
                Failed -> Scanning, najviše tri scan pokušaja ukupno
```

- samo jedna verzija Materiala smije biti `Active`;
- aktivacija zahtijeva točno jednu vlastitu `Clean` datoteku i postavlja current version;
- superseding uklanja stari current pointer; replacement treba dovršiti u jednoj transakciji;
- `Active` i `Superseded` snapshoti te njihove datoteke ne mijenjaju se in-place;
- restore kopira povijesni snapshot u novi Draft s većim version brojem;
- user-visible delete je arhiviranje; destructive historical delete i retention nisu 6.1.

Snapshot sadrži title, description, material type, subject/language, file referencu i checksum.
Curriculum/Knowledge/CEFR mapping dolazi u 6.2 i mora biti version-bound. `FileFormat` nije isto
što i pedagoški `MaterialType`.

## Storage topology

Business/application kod ovisi o `IMaterialObjectStorage`; početni production provider je
Cloudflare R2 preko privatnog S3-compatible adaptera. SQL sprema samo metadata/lifecycle, nikad
binary. API filesystem nije persistent storage. Credentiali su server-side secrets.

Server generira opaque key semantički oblika:

```text
materials/{teacherId}/{materialId}/{materialVersionId}/{fileId}
```

Originalni sanitized filename služi samo za prikaz. Klijent ne bira bucket, path, owner ni
object key. Canonical lokacija je `StorageProvider + StorageContainer + ObjectKey`; permanentni
javni URL ne sprema se. Nakon autorizacije dopušten je server-streaming ili signed access s
TTL-om najviše pet minuta, i to samo za `Clean` sadržaj.

Quarantine i clean objekt moraju biti stvarna access granica. Pending, Uploaded, Scanning,
Quarantined, Rejected i Failed objekt nema korisnički download/preview/share/AI/Lesson pristup.

## Format i upload security contract

| Format | MIME v1 | Maksimum |
|---|---|---:|
| PDF | `application/pdf` | 50 MB |
| DOCX | OOXML Word MIME | 25 MB |
| PPTX | OOXML Presentation MIME | 100 MB |
| MP4 | `video/mp4` | 250 MB |
| ZIP | `application/zip` ili `application/x-zip-compressed` | 100 MB compressed |

Sve ostalo, uključujući generički `application/octet-stream`, odbija se. Upload implementation
iz 6.6 mora fail-closed provjeriti extension, declared MIME, magic/signature i strukturu formata.
DOCX/PPTX moraju imati očekivanu OOXML strukturu.

ZIP dodatno zabranjuje executable sadržaj, nested/encrypted archive, symlink, traversal i
absolute path. Dopušteno je najviše 500 entryja, ukupno 500 MB expanded i compression ratio
najviše 100:1 po entryju. Ne postoji best-effort extraction nesigurnog arhiva.

Svaki uspješno uploadani objekt dobiva SHA-256. Hash služi za integrity/audit i buduću
detekciju duplikata; 6.1 ne radi cross-owner deduplication.

## Scanning i validation portovi

- `IMaterialFileValidator` predstavlja signature/structure/archive validaciju;
- `IMalwareScanner` je engine-neutralan, uz početni ClamAV-compatible deployment;
- svaki upload mora biti skeniran prije aktivacije/korištenja;
- scanner outage je `Failed`, nikad implicitni `Clean`; najviše tri pokušaja s budućim
  exponential backoffom;
- malware rezultat ide izravno u `Quarantined` bez transient retryja;
- audit smije sadržavati ID-eve, result category, timestamp i correlation/trace ID, ali ne
  file payload ili osjetljivi scanner output.

6.1 definira portove i persistence lifecycle. Stvarni upload orchestration, endpoint rate/body
limit, CSRF i ClamAV/R2 deployment pripadaju 6.6/deployment konfiguraciji.

## Ownership, visibility i share

Visibility v1 je samo `Private` ili `Shared`. `MaterialShare` je eksplicitni jedinstveni grant
prema drugom Teacher accountu:

- `View`: metadata i clean content read/preview;
- `Use`: View plus buduća uporaba u vlastitom Lesson workflowu ili eksplicitno dupliciranje;
- nema Edit, re-share, ownership transfer, public/marketplace/link/org scopea;
- owner se ne može shareati sam sebi;
- `Private` Material ne može imati share grant; grantovi se moraju ukloniti prije povratka iz
  `Shared` u `Private`;
- Material share ne daje pristup Studentu, Groupi, Evidenceu, readinessu ili Lesson historyju;
- missing/foreign resource ostaje indistinguishable `404` kada API surface bude uveden.

## AI granica

`IMaterialAnalysisService` je opcionalni port. AI smije dobiti samo prethodno validiran i `Clean`
sadržaj, od odobrenog providera uz dokumentiranu svrhu, payload, retention/privacy politiku i
server-side credential. Student, Guardian, Evidence/readiness i drugi Teacher podaci ne šalju se
ako nisu nužni.

AI rezultat je samo `Suggested`. Canonical metadata nastaje tek nakon Teacher `Accept`, `Edit`
ili `Reject`; AI nikad izravno ne potvrđuje pedagoški metadata. Suggestion provenance mora nositi
provider, model, generated time, kind i status bez nepotrebne kopije prompta/file sadržaja.
Nedostupan ili neodobren AI ne blokira ručni Material workflow. Phase 6.1 ne uvodi production AI
provider niti suggestion persistence/UI.

## Budući povijesni invariant

Phase 6.1 ne implementira Task domenu, ali zaključava da budući pedagoški značajan Task edit
stvara novi `TaskVersion`. Attempt, EvidenceEvent, Lesson history i MaterialVersion moraju
referencirati točan povijesni TaskVersion; prompt, options, correct answer, scoring, Knowledge
mapping, difficulty, EvidenceType i evaluation criterion ne mijenjaju se retroaktivno.

## Persistence zaštita

SQL Server štiti ownership FK-ove, unique version number, najviše jednu Active verziju,
current/active/clean konzistentnost, format/size/lifecycle kodove, immutable published snapshot,
unique object key, unique share, self-share zabranu i restrictive deleteove. `Material.RowVersion`
je optimistic-concurrency granica mutable aggregate roota. Migracija nema seed ni backfill.

## Izvan Phase 6.1

Nisu implementirani metadata mapping 6.2, Library/detail UI, upload API/wizard, R2/ClamAV
deployment adapteri, Task/TaskVersion aggregate, AI suggestion persistence/UI, preview conversion,
thumbnail/transcoding, quotas/billing, hard delete/retention, shared-with-me ekran ni Lesson
Builder integracija.
