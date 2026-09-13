import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD) {
  throw new Error('Provide local review environment variables.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-4.4', import.meta.url))
await mkdir(output, { recursive: true })
const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = { screens: [], errors: [], checks: [], data: {} }

try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.on('pageerror', error => evidence.errors.push(`${size}: ${error.message}`))

    await page.goto('http://localhost:8081/auth/login')
    await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
    await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
    await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
    await page.waitForURL('http://localhost:8081/')

    const discovery = await page.evaluate(async () => {
      const calendar = await fetch('/api/v1/schedule?from=2026-09-13&to=2026-10-14')
      const body = await calendar.json()
      for (const session of body.items ?? []) {
        const response = await fetch(`/api/v1/schedule/${session.id}/edit`)
        if (!response.ok) continue
        const edit = await response.json()
        if (edit.canEditFutureSeries && edit.detail.status === 1) {
          return { status: calendar.status, sessionId: session.id, contextName: edit.detail.contextName }
        }
      }
      return { status: calendar.status, sessionId: null, contextName: null }
    })
    if (discovery.status !== 200 || !discovery.sessionId) throw new Error('No editable active-series Session was available')
    evidence.data = { sessionId: discovery.sessionId, contextName: discovery.contextName, writes: 0 }

    await page.goto(`http://localhost:8081/schedule/${discovery.sessionId}/edit`)
    await page.getByRole('heading', { name: '3.4 Uredi termin' }).waitFor()
    await page.getByText('✓ Nema konflikata').waitFor()
    if (!(await page.getByRole('radio', { name: /Samo ovaj termin/ }).isChecked())) throw new Error('Safe one-occurrence scope is not the default')
    const future = page.getByRole('radio', { name: /Svi budući termini/ })
    if (!(await future.isEnabled())) throw new Error('Future-series scope is unavailable for a regular occurrence')
    await future.click()
    if (await page.getByLabel(/Datum/).isEnabled()) throw new Error('Future-series scope must lock the recurrence day')
    if (await page.getByLabel(/Naziv termina/).isEnabled()) throw new Error('Future-series scope must not imply unsupported series title persistence')
    await page.getByRole('radio', { name: /Samo ovaj termin/ }).click()
    await page.getByText('✓ Nema konflikata').waitFor()

    const dimensions = await page.evaluate(() => {
      const basic = document.querySelector('.schedule-edit-basic')?.getBoundingClientRect()
      const center = document.querySelector('.schedule-edit-center-top')?.getBoundingClientRect()
      const aside = document.querySelector('.schedule-edit-layout aside')?.getBoundingClientRect()
      return {
        viewport: innerWidth,
        document: document.documentElement.scrollWidth,
        basic: basic ? { x: Math.round(basic.x), y: Math.round(basic.y + scrollY), width: Math.round(basic.width) } : null,
        center: center ? { x: Math.round(center.x), y: Math.round(center.y + scrollY), width: Math.round(center.width) } : null,
        aside: aside ? { x: Math.round(aside.x), y: Math.round(aside.y + scrollY), width: Math.round(aside.width) } : null,
      }
    })
    if (dimensions.document > width) throw new Error(`Document overflow at ${size}: ${dimensions.document}/${width}`)
    if (size === 'desktop' && (!dimensions.basic || !dimensions.center || !dimensions.aside
      || !(dimensions.basic.x < dimensions.center.x && dimensions.center.x < dimensions.aside.x))) {
      throw new Error('Desktop does not preserve the canonical three-column edit hierarchy')
    }
    for (const label of ['Boja termina', 'Podsjetnik za učitelja', 'Podsjetnik za učenike']) {
      if (await page.getByLabel(label).isEnabled()) throw new Error(`${label} must remain disabled until its contract is locked`)
    }
    await page.evaluate(async () => {
      await document.fonts.ready
      await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
      window.scrollTo(0, 0)
    })
    await page.screenshot({ path: `${output}/edit-session-${size}.png`, fullPage: true, animations: 'disabled' })
    evidence.screens.push({ name: 'edit-session', size, ...dimensions })
    evidence.checks.push(`${size}: canonical hierarchy, real active-series Session, safe scope switch, live conflict preview, future-control boundaries, no overflow`)
    await context.close()
  }
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 4.4 canonical desktop/mobile edit checks; no writes.')
} finally {
  await browser.close()
}
