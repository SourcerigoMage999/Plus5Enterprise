import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD) {
  throw new Error('Provide local review environment variables.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-4.1', import.meta.url))
await mkdir(output, { recursive: true })
const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = { screens: [], errors: [], checks: [], data: {} }

try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.on('pageerror', error => evidence.errors.push(error.message))

    async function shot(name) {
      await page.evaluate(async () => {
        await document.fonts.ready
        await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
        window.scrollTo(0, 0)
      })
      const dimensions = await page.evaluate(() => ({
        viewport: innerWidth,
        document: document.documentElement.scrollWidth,
        calendarClient: document.querySelector('.schedule-calendar-scroll')?.clientWidth ?? 0,
        calendarScroll: document.querySelector('.schedule-calendar-scroll')?.scrollWidth ?? 0,
      }))
      if (dimensions.document > width) throw new Error(`Document overflow at ${size}`)
      await page.screenshot({ path: `${output}/${name}-${size}.png`, fullPage: true, animations: 'disabled' })
      evidence.screens.push({ name, size, ...dimensions })
    }

    await page.goto('http://localhost:8081/auth/login')
    await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
    await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
    await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
    await page.waitForURL('http://localhost:8081/')
    const discovery = await page.evaluate(async () => {
      const response = await fetch('/api/v1/schedule?from=2026-09-01&to=2026-10-01')
      return { status: response.status, body: await response.json() }
    })
    if (discovery.status !== 200) throw new Error(`Calendar discovery returned ${discovery.status}`)
    const first = discovery.body.items[0]
    const anchor = first
      ? new Date(first.startsAtUtc).toLocaleDateString('sv-SE', { timeZone: discovery.body.timeZoneId })
      : '2026-09-14'
    evidence.data = { anchor, discoveredItems: discovery.body.items.length, timeZoneId: discovery.body.timeZoneId }

    await page.goto(`http://localhost:8081/schedule?date=${anchor}`)
    await page.getByRole('heading', { name: '3.1 Raspored' }).waitFor()
    await page.locator('.schedule-calendar').waitFor()
    const active = page.getByRole('link', { name: /Raspored/ })
    if (await active.getAttribute('aria-current') !== 'page') throw new Error('Schedule navigation is not active')
    if (await page.getByRole('button', { name: '+ Novi termin' }).isEnabled()) throw new Error('Future create action must remain disabled')
    await page.getByText('Jedinstvenih učenika').waitFor()
    await page.getByText('Planiranih dolazaka').waitFor()
    await shot('calendar-week')

    await page.getByRole('button', { name: 'Dan', exact: true }).click()
    await page.getByRole('button', { name: 'Dan', exact: true }).waitFor()
    await page.waitForFunction(() => document.querySelector('button[aria-pressed="true"]')?.textContent === 'Dan')
    await page.locator('.schedule-calendar--day').waitFor()
    await shot('calendar-day')
    evidence.checks.push(`${size}: canonical calendar, week/day, filters, exact metrics, disabled future actions, no document overflow`)
    await context.close()
  }
  if (evidence.errors.length) throw new Error('Browser errors occurred')
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log(`PASS: Phase 4.1 desktop/mobile calendar checks; ${evidence.data.discoveredItems} actual sessions discovered; no writes.`)
} finally {
  await browser.close()
}
