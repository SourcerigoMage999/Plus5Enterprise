# Presentation AI assistance

## Status

**Phase 7.9 — IMPLEMENTED / REVIEW READY — 2026-10-25.**

Ovaj contract zaključava opcionalnu AI pomoć unutar postojećeg Presentation Editora. AI je
isključivo bounded suggestion servis: ne posjeduje domenski state, ne piše izravno u bazu, ne
objavljuje prezentaciju i ne stvara Evidence. Ručni editor uvijek ostaje potpuno funkcionalan.

## Trošak i uključivanje

AI integracija je defaultno isključena:

```text
PresentationAi:Enabled = false
```

U tom stanju aplikacija ne šalje OpenAI request, ne koristi API ključ i ne stvara vanjski AI
trošak. PLUS 5 nema in-app kupnju, pretplatu, naplatu tokena ni automatsko uključivanje AI-ja.
Samo vlasnik deploymenta smije ga eksplicitno uključiti serverskom konfiguracijom i vlastitim
OpenAI API ključem. Ključ ostaje u environment/secret storeu; nikad nije `VITE_*`, browser,
baza, Git ni log.

Jedna eksplicitna Teacher akcija stvara najviše jedan logički AI request. Nema skrivenog
prefetcha, autoregeneracije ni automatskog retry loopa. Dedicated Teacher limit je 20 AI
requestova u 10 minuta, body je najviše 8 KiB, a provider timeout najviše 30 sekundi.

## Provider i regionalna granica

- provider: OpenAI API;
- API: Responses API;
- production model: `gpt-6.1-sol`;
- base URL: `https://eu.api.openai.com/v1/`;
- `store = false`;
- provider tools, web/file search, computer use i arbitrary functions: isključeni;
- background mode, Assistants/Threads, vector storeovi i provider file storage: zabranjeni;
- organization/project data-sharing i training opt-in: moraju ostati isključeni;
- Zero Data Retention se preferira kada je dostupan, ali nije funkcionalni preduvjet.

Application sloj poznaje samo `IPresentationAiAssistant`; provider, model, endpoint i prompt
template dolaze iz validirane serverske konfiguracije. Nema tihog fallbacka na drugi model.

## Dopušteni use caseovi

V1 podržava četiri suggestion kinda:

1. `PRESENTATION_DRAFT` — početna struktura s naslovima i postojećim tekstualnim elementima;
2. `ELEMENT_CONTENT` — sadržaj za `TEXT`, `RULE`, `EXAMPLE`, `VOCABULARY` ili instructional
   `QUESTION`;
3. `QUICK_CHECK` — pitanje, 2–8 odgovora, jedan točan odgovor, Difficulty i EvidenceType;
4. `KNOWLEDGE_MAPPING` — izbor samo iz bounded skupa server-provided Published/Active/leaf
   Knowledge Component kandidata.

AI ne generira IMAGE/AUDIO/VIDEO binary, HTML/CSS, geometry automation, novi Knowledge
Component ni weight. Quick Check `MaxPoints = 1` ostaje domensko pravilo. Mapping se ne širi
na parent i ne sprema automatski.

## Data-minimization granica

Server prompt smije sadržavati samo podatke potrebne za konkretan zahtjev:

- version-bound Material naslov, cilj, predmet, jezik, Program, SchoolGrade,
  ProficiencyLevel i najviše 20 tagova;
- aktualni Draft document, slide order i najviše 100 postojećih tekstualnih elemenata;
- podržane element typeove;
- za mapping najviše 100 eligible leaf kandidata s ID-jem, nazivom, KnowledgeArea te
  KnowledgeModel code/version vrijednostima.

Phase 7.9 request nikad ne smije sadržavati Student/Guardian identitet ili kontakt, članstva,
attendance, StudentAttempt, EvidenceEvent, mastery/readiness, ocjene/rezultate, Teacherove
privatne bilješke o Studentu, session cookie/token, storage credential/key/signed URL,
quarantine/scan metadata ni binary assete. PDF/DOCX/PPTX, slike, MP4 i MP3 ne šalju se
provideru. Presentation AI assistance nije student personalization.

Teacher free text i Material sadržaj tretiraju se kao nepouzdani podaci, ne kao system
instruction. Prompt template je server-owned, versioned i zabranjuje promjenu pravila kroz
sadržaj materijala. Model nema alate ni pristup zabranjenim podacima.

## Structured output i potvrda

Provider mora vratiti strogi JSON-schema output. Server ga ponovno normalizira i validira
prije stvaranja `PresentationAiSuggestion`. Provider odgovor nije domenski authority.

Suggestion lifecycle je:

```text
Pending
→ Accepted | Edited | Rejected | Expired
```

Pending suggestion istječe nakon 24 sata. UI ga jasno označava kao `AI prijedlog · nije
spremljeno` i nudi eksplicitni `Prihvati`, `Uredi` ili `Odbij`. Zatvaranje panela, timeout ili
uspješan provider response nisu prihvaćanje.

Tek Accept/Edit request ponovno provjerava Teacher ownership, Draft/version target,
document rowversion, schema i postojeća domenska pravila te poziva normalne typed presentation
servise. AI adapter nema EF/database write put i ne može pozvati publish. Reject i expiry ne
mijenjaju canonical graph. Resolve čisti generirani sadržaj; canonical Teacher-confirmed Draft
ostaje normalan version-bound snapshot.

## Provenance i retention

SQL čuva samo minimalni audit metadata:

- suggestion, Teacher, Material, MaterialVersion i PresentationDocument ID;
- optional target Slide/Element;
- suggestion kind, provider, model i prompt-template version;
- generated/expiry/resolved vrijeme i status.

Ne čuvaju se raw system/user prompt, cijeli provider response, chain-of-thought ni Student
podaci. `GeneratedContentJson` postoji samo dok je suggestion Pending i briše se pri
Accept/Edit/Reject/Expired. SQL constrainti i exact same-version FK-ovi štite lifecycle,
ownership i target granice.

Normalni application log sadrži samo correlation ID, suggestion kind, provider/model,
trajanje, token usage i failure kategoriju. Prompt, odgovor, presentation text, Teacher free
text, Student podaci i API key se ne logiraju.

## Failure fallback

Timeout, 429, provider 4xx/5xx, network failure ili malformed/business-invalid structured
output vraćaju kontrolirano unavailable/invalid stanje. Dopušten je najviše jedan retry samo
za transient network/5xx. Nema retryja za 429 ili malformed output, nema model fallbacka i
nema utjecaja na ručni editor.

## API i UI

```http
GET  /api/v1/materials/{materialId}/presentation/ai/status
POST /api/v1/materials/{materialId}/presentation/ai/suggestions
GET  /api/v1/materials/{materialId}/presentation/ai/suggestions/{suggestionId}
POST /api/v1/materials/{materialId}/presentation/ai/suggestions/{suggestionId}/resolve
```

Endpointi su Teacher-only i owner-scoped. Write putovi imaju CSRF, bounded body i rate limit.
Missing/foreign/archived/non-Draft resurs ne otkriva tuđe podatke. Frontend poziva isključivo
PLUS 5 API; browser nikad ne kontaktira OpenAI.

Default-disabled panel eksplicitno objašnjava da nema automatskih OpenAI poziva, dodatnog
troška ni naplate u aplikaciji te da ručni editor ostaje dostupan. Enabled panel ne šalje
request do eksplicitnog klika Teachera.

## Izvan scopea

- Student personalization, grading, Attempt, Evidence, mastery i readiness;
- automatic publish i autonomous presentation generation/save;
- OCR, document extraction, image understanding/generation, transcription i audio/video
  analiza;
- web/tool/agent access, provider file storage i conversation history;
- streaming canonical write i token-by-token persistence;
- AI billing, credits, subscription, quota purchase ili in-app payment;
- Lesson Builder AI contract i model-improvement/learning iz Teacher feedbacka.

## Validation budget

Klasifikacija: `SCHEMA + WRITE_API + EXTERNAL_PROVIDER + SECURITY + UI + DOCS_ONLY`.

- Release build i targeted Phase 7.9 domain/provider/API testovi;
- stvarni SQL migration/lifecycle gate i EF drift provjera;
- targeted frontend AI panel testovi, lint, TypeScript i production build;
- dependency-boundary review za novi provider-neutral application port;
- jedan desktop i jedan mobile screenshot default-disabled stvarnog UI-ja, document overflow
  assertion i browser/page errors;
- live paid OpenAI call: N/A — integracija je defaultno isključena i acceptance ne smije
  stvarati vanjski trošak;
- full regression: N/A — targeted phase validation je dovoljna;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni;
- Docker non-root/health/runtime identity: N/A — zaključana infrastruktura nije promijenjena.
