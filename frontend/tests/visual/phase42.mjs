import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD) {
  throw new Error('Provide local review environment variables.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-4.2', import.meta.url))
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
      const response = await fetch('/api/v1/schedule?from=2026-09-01&to=2026-10-01')
      return { status: response.status, body: await response.json() }
    })
    if (discovery.status !== 200 || !discovery.body.items.length) throw new Error(`Calendar discovery returned ${discovery.status} without sessions`)
    const first = discovery.body.items[0]
    evidence.data = { sessionId: first.id, contextName: first.contextName, discoveredItems: discovery.body.items.length, timeZoneId: discovery.body.timeZoneId }

    await page.goto(`http://localhost:8081/schedule/${first.id}`)
    await page.getByRole('heading', { name: '3.2 Detalj termina' }).waitFor()
    await page.locator('.session-detail-columns').waitFor()
    if (await page.getByRole('link', { name: 'Raspored', exact: true }).first().getAttribute('href') === null) throw new Error('Calendar breadcrumb is missing')
    for (const name of [/Uredi termin/, /Pokreni sat/, /Pripremi sat/, /Otkaži termin/]) {
      if (await page.getByRole('button', { name }).isEnabled()) throw new Error(`Future action ${name} must remain disabled`)
    }
    const dimensions = await page.evaluate(() => ({
      viewport: innerWidth,
      document: document.documentElement.scrollWidth,
      participantRegionClient: document.querySelector('.session-student-table')?.clientWidth ?? 0,
      participantRegionScroll: document.querySelector('.session-student-table')?.scrollWidth ?? 0,
      participantCount: document.querySelectorAll('.session-student-table tbody tr').length,
    }))
    if (dimensions.document > width) throw new Error(`Document overflow at ${size}: ${dimensions.document}/${width}`)
    await page.evaluate(async () => {
      await document.fonts.ready
      await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
      window.scrollTo(0, 0)
    })
    await page.screenshot({ path: `${output}/session-detail-${size}.png`, fullPage: true, animations: 'disabled' })
    evidence.screens.push({ name: 'session-detail', size, ...dimensions })
    evidence.checks.push(`${size}: canonical hierarchy, real session/context/participants, disabled future writes, no document overflow`)
    await context.close()
  }
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log(`PASS: Phase 4.2 desktop/mobile detail checks; ${evidence.data.discoveredItems} actual sessions discovered; no writes.`)
} finally {
  await browser.close()
}
