# Presentation teaching-specific elements

## Status

**Phase 7.4 — FINAL LOCKED, odobreno 2026-10-18.**

Ovaj contract proširuje zaključani ADR-0026 i Phase 7.3 editor nastavnim elementima. Ne
mijenja slide lifecycle, document concurrency authority ni assessable/Evidence granicu.

## Scope i semantika

Draft `PRESENTATION` MaterialVersion podržava još šest zatvorenih v1 tipova:

- `RULE`: naslov, tekst pravila i naglasna boja;
- `EXAMPLE`: primjer, opcionalno objašnjenje i naglasna boja;
- `VOCABULARY`: pojam, definicija i opcionalni primjer uporabe;
- `QUESTION`: nastavno pitanje i opcionalna napomena za raspravu;
- `VIDEO`: private Clean MP4 asset i naslov, s vidljivim kontrolama;
- `AUDIO`: private Clean MP3 asset, s vidljivim kontrolama i bez autoplayja.

Payload registry dopušta samo poznata polja, normalizira tekst i odbija nepoznata polja.
`QUESTION` je isključivo instructional sadržaj: nije `AssessableTaskVersion`, ne stvara
`StudentAttempt` ni `EvidenceEvent`, nema bodove, correct answer, scoring ili Knowledge
mapping. Ti contracti ostaju Phase 7.5–7.6.

## AUDIO v1 — zaključana SA odluka

- dopušten je samo MP3: `.mp3` + deklarirani `audio/mpeg`;
- maksimalna veličina je 25 MiB; to nije ograničenje trajanja;
- server provjerava sigurnu ekstenziju, MIME, stvarnu veličinu, SHA-256 i strukturu MP3-a;
- prihvaća se MP3 s opcionalnim valjanim ID3v2 zaglavljem nakon kojeg slijede valjani MPEG
  audio frameovi te MP3 bez ID3v2 taga koji počinje valjanim MPEG frameom;
- malformed, truncated i neprepoznat sadržaj se odbija;
- payload je točno `schemaVersion = 1`, `autoplay = false`, `showControls = true`;
- browser nikad ne smije automatski pokrenuti zvuk;
- WAV, M4A/AAC, OGG/Opus i svi drugi audio formati nisu dio v1;
- trim, waveform editing, volume automation, fade, playback regions, transcript, speech-to-
  text, AI analiza i pronunciation scoring nisu dio Phase 7.4.

## VIDEO v1

Phase 7.4 ponovno koristi postojeći zaključani MP4 contract: `.mp4`, `video/mp4`, MP4
signature/structure provjera i najviše 250 MiB. Ne uvodi novi format policy ni vanjski video
URL. Kontrole reprodukcije moraju biti vidljive.

## Asset security lifecycle

IMAGE/VIDEO/AUDIO asseti su private i version-bound:

```text
upload
→ extension/MIME/structure/size validation
→ private quarantine
→ fail-closed ClamAV scan
→ SHA-256
→ Clean promote
→ version-bound PresentationAsset
```

Scanner failure završava kontroliranim `503`. Detektirani malware ostaje kao nereferencirani
private `Quarantined` asset radi audit traila i nikada se ne promovira niti postaje dostupan.
Samo `Clean` asset smije biti referenciran. `PresentationElement.TypeCode` mora odgovarati
`PresentationAsset.AssetKindCode`, oba moraju pripadati istoj MaterialVersion, a SQL trigger
`51245` ponovno provjerava kind/version/Clean granicu.

Payload i javni DTO ne sadrže storage key, checksum, signed/public URL, vanjski URL, base64
ili raw binary. Owner-only read endpoint streama private Clean objekt uz range podršku.

## API i concurrency

Uz Phase 7.3 endpointove uvedeni su:

- `POST /api/v1/materials/{materialId}/presentation/slides/{slideId}/videos`;
- `POST /api/v1/materials/{materialId}/presentation/slides/{slideId}/audio`.

Video multipart body je ograničen na 251 MiB, audio na 26 MiB. Oba writea zahtijevaju
Teacher session, CSRF, Presentation rate limit, ownership, Draft status i aktualni document
rowversion. Stale write vraća `409`. Browser filename i MIME nisu authority.

## Duplicate i restore

- duplicate slajda unutar iste MaterialVersion smije reuseati isti Clean asset;
- restore/nova Draft verzija server-side kopira Clean bytes u novi PresentationAsset, novi
  ID i novi opaque object key bez ponovnog scaniranja nepromijenjenih bytesa;
- zamjena datoteke, kada dobije zaseban workflow, mora ponovno proći puni quarantine/scan
  pipeline.

## Editor UX

Toolbar nudi Pravilo, Primjer, Vokabular, Pitanje, Video i Audio. Strukturirani property
panel ne izlaže HTML/URL/scoring polja. Video i audio prikazuju native kontrole; audio nema
`autoplay` atribut. Responsive editor zadržava prihvaćenu P3 hijerarhiju.

## Out of scope

- Quick Check i sve assessable ponašanje (7.5);
- Knowledge mapping (7.6);
- autosave/recovery (7.7);
- preview, publish, package/export (7.8);
- AI pomoć (7.9, blokirano zasebnim gateom).

## Evidence

Implementacijski i testni rezultat je u
`summaries/PHASE_7.4_PRESENTATION_TEACHING_ELEMENTS_SUMMARY.md`. Runtime visual evidence
nalazi se u `visual-acceptance/phase-7.4/`.
