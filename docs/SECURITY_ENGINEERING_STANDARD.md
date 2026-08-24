# SECURITY_ENGINEERING_STANDARD

## Status

**MANDATORY v1.0 — 2026-08-23**

Security pravila vrijede od prve faze. Security review nije događaj samo pred release.

## 1. Security model

- deny by default
- least privilege
- server-side authorization za svaki zaštićeni resurs/akciju
- client se smatra nepouzdanim
- svaki external input je nepouzdan dok nije validiran

## 2. Authentication

Detaljni auth flow zaključan je za ROADMAP Phase 1.6 u `AUTHENTICATION_REQUIREMENTS.md` i `AUTHENTICATION_ARCHITECTURE.md`. Implementacija mora slijediti:

- standardne ASP.NET Core/Identity/kriptografske primitive; ne izmišljati vlastitu kriptografiju
- password hashing kroz provjerenu framework implementaciju
- email/account recovery tokene tretirati kao secrets
- session/token revocation mora biti moguća kada business flow to zahtijeva
- cookie/token transport mora biti siguran za odabrani model
- auth/CSRF Data Protection key ring mora biti dijeljen između API instanci i trajan preko restarta; key material mora biti šifriran certifikatom izvan Developmenta

## 3. Authorization

- policy/role/ownership provjera na serveru
- endpoint nije siguran zato što ga UI ne prikazuje
- object-level authorization obavezna kada korisnik pristupa Student/Group/Session/Material resursu koji ne pripada njegovom scopeu
- ne vjerovati `userId`, `teacherId` ili ownership vrijednosti iz requesta bez provjere identity contexta

## 4. Web/API security

Produkcija mora imati:

- HTTPS only
- siguran HSTS kada topology to dopušta
- restriktivan CORS s eksplicitnim originima; bez credentialed wildcarda
- standardizirano sigurno error ponašanje bez stack tracea
- request/body size limite prema use caseu
- rate limiting posebno za login, reset, verification i druge abuse-prone endpointove kada se uvedu

## 5. Injection / serialization

- parametrizirani queryji/EF Core
- raw SQL mora koristiti parametre i review
- nikada spajanje user inputa u SQL string
- sigurna JSON serializacija
- ne podržavati polymorphic/deserialization feature bez potrebe i security reviewa

## 6. XSS/CSRF

- React escaping se ne zaobilazi bez sanitizacije
- untrusted HTML nije dopušten bez jasno odabranog sanitizer pristupa
- CSRF zaštita mora odgovarati auth transport modelu; cookie-based auth zahtijeva eksplicitnu procjenu CSRF-a

## 7. Secrets

Secrets uključuju najmanje:

- DB credentials
- JWT/signing/encryption ključeve
- email/API keys
- object storage keys
- AI provider keys

Pravila:

- nikada u Git repozitoriju
- nikada hardcoded u sourceu
- nikada u frontend bundleu
- Development: user-secrets ili lokalni necommitani `.env`
- Production/VPS: environment/secrets mehanizam s ograničenim filesystem permissionsima ili dedicated secret store ako se kasnije uvede
- rotacija mora biti moguća bez promjene source koda

## 8. Logging & privacy

Ne logirati:

- passwords
- auth/refresh/reset/verification tokene
- API keys
- cijele authorization headere
- nepotreban PII

Security događaji trebaju biti dovoljno auditabilni za istragu bez stvaranja nove baze osjetljivih podataka.

## 9. File uploads

Kada se uvedu:

- allowlist tipova/ekstenzija prema business zahtjevu
- limit veličine
- server-generated storage name/key
- ne vjerovati MIME-u/filenameu klijenta
- zaštita od path traversal
- privatni fileovi nisu javni samo zato što URL izgleda teško pogodiv
- malware scanning procijeniti prema tipu sadržaja i riziku prije produkcije

## 10. Dependencies / supply chain

CI mora uključiti:

- restore/install iz lockanih dependency definicija gdje ecosystem to podržava
- vulnerability audit
- zabranu knowingly critical ranjive dependency verzije bez eksplicitnog risk acceptancea

## 11. Data protection

- collect minimum necessary data
- encryption in transit obavezna u produkciji
- backup sadrži isti klasificirani podatak kao baza i mora biti jednako zaštićen
- encryption at rest/topology odluka zaključava se prije produkcijskog releasea

## 12. Abuse / availability

Za javne endpointove razmotriti:

- rate limiting
- bounded pagination
- payload size limits
- timeout/cancellation
- zaštitu od nekontroliranih expensive queryja

## 13. Security release gates

Prije produkcije mora biti potvrđeno:

- nema secreta u repo/historyju
- TLS konfiguriran
- DB nije javno dostupna
- backup/restore testiran
- auth/session flows security-reviewani
- authorization integration testovi postoje za kritične resurse
- dependency audit bez neprihvaćenih critical/high nalaza
- osnovni OWASP web/API threat review dovršen
