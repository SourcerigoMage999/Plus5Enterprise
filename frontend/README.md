# PLUS 5 frontend

React + TypeScript + Vite SPA s Phase 1.6 application/auth foundationom:

- responsive učiteljski app shell
- React Router centralni route registry
- neutralni placeholderi bez fake business podataka
- centralni CSS design tokeni
- Vitest + Testing Library component testovi
- Teacher registracija, verifikacija, login, recovery i change-password flowovi
- centralna cookie-session obrada `401`/`403`, route guard i CSRF API client
- bez bearer tokena u `localStorage` ili `sessionStorage`

```powershell
npm ci
npm run lint
npm run typecheck
npm run test
npm run build
```

Canonical contract nalazi se u `../docs/FRONTEND_FOUNDATION.md`, `../docs/AUTHENTICATION_REQUIREMENTS.md` i `../docs/AUTHENTICATION_ARCHITECTURE.md`, a root `README.md` opisuje potpuni repository workflow.
