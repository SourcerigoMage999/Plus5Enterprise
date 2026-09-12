import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD) {
  throw new Error('Provide local review environment variables.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-4.3', import.meta.url))
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

    const futureDate = new Date(Date.now() + 21 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
    await page.goto(`http://localhost:8081/schedule/new?date=${futureDate}`)
    await page.getByRole('heading', { name: '3.3 Novi termin' }).waitFor()
    const group = page.getByLabel('Grupa', { exact: true })
    await group.locator('option:not([value=""])').first().waitFor({ state: 'attached' })
    const groupId = await group.locator('option:not([value=""])').first().getAttribute('value')
    if (!groupId) throw new Error('No active group was available for visual acceptance')
    await group.selectOption(groupId)
    await page.getByLabel('Naziv termina (opcionalno)').fill('Dodatna priprema za ispit')
    await page.getByLabel('Opis / napomena (opcionalno)').fill('Ponoviti ključne zadatke i provjeriti domaću zadaću.')
    await page.getByLabel('Početak').fill('17:00')
    await page.getByLabel('Završetak').fill('18:00')

    const dimensions = await page.evaluate(() => {
      const cards = [...document.querySelectorAll('.schedule-create-card')].map(element => {
        const rect = element.getBoundingClientRect()
        return { x: Math.round(rect.x), y: Math.round(rect.y + scrollY), width: Math.round(rect.width), height: Math.round(rect.height) }
      })
      const summary = document.querySelector('.schedule-create-summary')?.getBoundingClientRect()
      return {
        viewport: innerWidth,
        document: document.documentElement.scrollWidth,
        cards,
        summary: summary ? { x: Math.round(summary.x), y: Math.round(summary.y + scrollY), width: Math.round(summary.width), height: Math.round(summary.height) } : null,
      }
    })
    if (dimensions.document > width) throw new Error(`Document overflow at ${size}: ${dimensions.document}/${width}`)
    if (size === 'desktop') {
      if (!dimensions.summary || dimensions.cards.length !== 5) throw new Error('Canonical cards or summary are missing')
      if (!(dimensions.cards[0].x < dimensions.cards[1].x && dimensions.cards[1].x < dimensions.summary.x)) {
        throw new Error('Desktop top row does not preserve the canonical three-column hierarchy')
      }
      if (!(dimensions.cards[2].y > dimensions.cards[0].y && dimensions.cards[3].y > dimensions.cards[1].y
        && dimensions.cards[4].y > dimensions.summary.y)) {
        throw new Error('Desktop lower cards do not preserve the canonical column hierarchy')
      }
    }
    if (await page.getByLabel('Redovno – svaki tjedan').isEnabled()) throw new Error('Group recurrence must remain disabled')
    for (const label of ['Boja termina', 'Podsjetnik sebi', 'Podsjetnik učenicima']) {
      if (await page.getByLabel(label).isEnabled()) throw new Error(`${label} must remain disabled until its contract is locked`)
    }
    if (!(await page.getByRole('button', { name: 'Spremi termin' }).isEnabled())) throw new Error('Valid create action should be enabled')
    await page.evaluate(async () => {
      await document.fonts.ready
      await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
      window.scrollTo(0, 0)
    })
    await page.screenshot({ path: `${output}/create-session-${size}.png`, fullPage: true, animations: 'disabled' })
    evidence.screens.push({ name: 'create-session', size, ...dimensions })
    evidence.checks.push(`${size}: canonical hierarchy, real owner-scoped group, responsive form, intentional future-control boundaries, no document overflow`)
    evidence.data = { contextId: groupId, date: futureDate, writes: 0 }
    await context.close()
  }
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 4.3 canonical desktop/mobile form checks; no writes.')
} finally {
  await browser.close()
}
