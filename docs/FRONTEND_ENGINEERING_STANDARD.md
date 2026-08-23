# FRONTEND_ENGINEERING_STANDARD

## Status

**MANDATORY v1.0 — 2026-08-23**

## 1. Platform

- React
- TypeScript
- Vite
- TypeScript strict mode

## 2. Core rule

Frontend je presentation/client sloj. Server je autoritet za sigurnost i trajna poslovna pravila.

## 3. Component design

- komponente moraju imati jasnu odgovornost
- velike stranice razbijati prema stvarnim UI/use-case granicama
- reusable komponenta ne nastaje samo zato što se dva elementa slučajno slično izgledaju
- business logiku ne skrivati u JSX-u
- side-effecte držati eksplicitnima i kontroliranima

## 4. State

Razlikovati:

- local UI state
- form state
- server state/cache
- URL/navigation state

Ne uvoditi global store za sve. Global state library zahtijeva stvarnu cross-cutting potrebu i odluku.

## 5. API access

- jedan standardizirani API client/boundary
- ne raspršivati `fetch`/HTTP konfiguraciju kroz komponente
- tipizirani request/response modeli
- loading, empty, error i retry ponašanje mora biti namjerno
- auth handling ne smije oslabiti server-side security

## 6. Forms

- client validation radi UX-a
- server validation je konačni autoritet
- greške servera moraju biti prikazive korisniku na konzistentan način
- spriječiti accidental duplicate submit kada operacija nije idempotentna

## 7. Security

- ne spremati dugovječne osjetljive auth tokene u `localStorage` ako auth dizajn predviđa sigurniji cookie/session mehanizam
- ne renderirati untrusted HTML bez sanitizacije
- ne skrivati authorization samo CSS-om/UI-em i smatrati to zaštitom
- secrets nikada ne ulaze u Vite client bundle

## 8. Accessibility

- semantički HTML prvo
- tipkovnica za interaktivne funkcije
- vidljiv focus state
- label/aria naming gdje native semantics nisu dovoljne
- kontrast i statusi ne smiju ovisiti samo o boji

## 9. Performance

- ne prefetchati/učitavati ogromne liste bez pagination/virtualization razloga
- route/code splitting uvoditi tamo gdje bundle mjerenje pokaže potrebu
- optimizacije rendera (`memo`, `useMemo`, `useCallback`) nisu default dekoracija; koristiti kada postoji razlog

## 10. Dependency policy

Nova npm biblioteka mora imati jasan benefit i security/maintenance review. Ne uvoditi više biblioteka za isti problem.

## 11. Frontend code review blockers

- business/security pravilo postoji samo na klijentu
- `any` kao trajno zaobilaženje tipova u važnom contractu
- secret u frontend env varijabli/bundleu
- XSS risk kroz nesanitizirani HTML
- ekran nema loading/error/empty state gdje su potrebni
- komponenta radi direktne ad-hoc API pozive mimo standardnog boundaryja
- scope pripada budućoj ROADMAP fazi
