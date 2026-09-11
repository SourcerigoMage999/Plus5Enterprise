import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'
if (!process.env.PLUS5_REVIEW_PLAYWRIGHT) throw new Error('Set PLUS5_REVIEW_PLAYWRIGHT to an installed Playwright index.mjs path.')
const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)

const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-1.7', import.meta.url))
const base = 'http://localhost:8081'
if (!process.env.PLUS5_REVIEW_EMAIL || !process.env.PLUS5_REVIEW_PASSWORD) throw new Error('Provide local review password in environment.')
await mkdir(output, { recursive: true })
const browser = await chromium.launch({ channel: 'msedge', headless: true })
const results = { browser: browser.version(), screens: [], interactions: [], errors: [] }
try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: 1, locale: 'hr-HR' })
    const page = await context.newPage()
    page.on('pageerror', error => results.errors.push(error.message))
    async function shot(name) {
      await page.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(i => i.decode().catch(() => {}))); window.scrollTo(0, 0) })
      const dimensions = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth, heading: document.querySelector('h1')?.textContent, css: [...document.querySelectorAll('link[rel=stylesheet]')].map(l => l.getAttribute('href')) }))
      await page.screenshot({ path: `${output}/${name}-${size}.png`, fullPage: true, animations: 'disabled' })
      results.screens.push({ name, size, ...dimensions })
      if (dimensions.document > width) {
        console.log(await page.evaluate(() => [...document.querySelectorAll('body *')].filter(el => { const r=el.getBoundingClientRect(); if(r.right <= innerWidth) return false; for(let p=el.parentElement;p;p=p.parentElement) {if(['auto','scroll','hidden','clip'].includes(getComputedStyle(p).overflowX))return false} return true }).map(el => ({ tag:el.tagName, cls:el.className, width:el.getBoundingClientRect().width, right:el.getBoundingClientRect().right,position:getComputedStyle(el).position }))))
        throw new Error(`Overflow on ${name} ${size}: ${dimensions.document}`)
      }
    }
    await page.goto(`${base}/auth/login`, { waitUntil: 'networkidle' })
    await shot('login')
    await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
    await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
    await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
    await page.waitForURL(`${base}/`)
    await page.goto(`${base}/students`, { waitUntil: 'networkidle' })
    await page.getByRole('table').waitFor()
    await shot('students')
    if (size === 'mobile') {
      await page.getByRole('link', { name: 'Uredi učenika', exact: true }).focus()
      const visibleAction = await page.getByRole('link', { name: 'Uredi učenika', exact: true }).evaluate(el => { const r=el.getBoundingClientRect(); return r.left >= 0 && r.right <= innerWidth })
      if (!visibleAction) throw new Error('Student actions cannot be reached by keyboard')
      await shot('students-actions')
    }
    const dossier = await page.getByRole('link', { name: 'Otvori dosje', exact: true }).getAttribute('href')
    if (!dossier) throw new Error('Missing existing demo student')
    await page.getByRole('button', { name: 'Kartice', exact: true }).click()
    await page.waitForURL(/view=cards/)
    await page.locator('.student-cards').waitFor({ state: 'visible' })
    await shot('student-cards')
    await page.goto(`${base}${dossier}`, { waitUntil: 'networkidle' })
    await page.locator('h1').waitFor()
    await shot('dossier')
    if (size === 'mobile') {
      await page.getByRole('link', { name: /Uredi učenika/ }).focus()
      const visibleAction = await page.getByRole('link', { name: /Uredi učenika/ }).evaluate(el => { const r=el.getBoundingClientRect(); return r.left >= 0 && r.right <= innerWidth })
      if (!visibleAction) throw new Error('Dossier edit link cannot be reached by keyboard')
      await shot('dossier-actions')
    }
    await page.goto(`${base}${dossier}/edit`, { waitUntil: 'networkidle' })
    await page.getByRole('heading', { name: '2.6 Uredi učenika', exact: true }).waitFor()
    await shot('edit')
    await page.getByRole('button', { name: 'Arhiviraj učenika', exact: true }).click()
    await page.getByRole('dialog').waitFor()
    await shot('archive-confirmation')
    await page.getByRole('dialog').getByRole('button', { name: 'Odustani', exact: true }).click()
    await page.goto(`${base}/students/new`, { waitUntil: 'networkidle' })
    await page.getByRole('heading', { name: 'Novi učenik', exact: true }).waitFor()
    await shot('create')
    await page.getByRole('button', { name: 'Spremi učenika', exact: true }).click()
    const invalid = await page.locator('input:invalid').count()
    if (!invalid) throw new Error('Required field validation missing')
    results.interactions.push(`${size}: empty create native validation; no save; archive cancelled`)
    await page.goto(`${base}/students/groups`, { waitUntil: 'networkidle' })
    await page.getByRole('heading', { name: '2.7 Grupe', exact: true }).waitFor()
    await page.getByRole('tab', { name: /Učenici/ }).waitFor()
    await shot('groups')
    await page.getByRole('tab', { name: /Učenici/ }).focus()
    await page.keyboard.press('ArrowRight')
    if (await page.getByRole('tab', { name: 'Raspored', exact: true }).getAttribute('aria-selected') !== 'true') throw new Error('Tab keyboard failed')
    await page.getByRole('heading', { name: 'Redoviti raspored', exact: true }).waitFor()
    await shot('schedule-empty')
    await page.keyboard.press('Home')
    await page.getByRole('region', { name: /Članovi grupe/ }).waitFor()
    if (size === 'mobile') {
      const region = page.getByRole('region', { name: /Članovi grupe/ })
      await region.focus()
      await region.evaluate(el => { el.scrollLeft = el.scrollWidth })
      await shot('groups-actions')
    }
    results.interactions.push(`${size}: group tab keyboard navigation PASS`)
    await page.getByRole('button', { name: /^Ukloni / }).first().click()
    await shot('membership-confirmation')
    await page.getByRole('button', { name: 'Odustani', exact: true }).click()
    results.interactions.push(`${size}: membership confirmation cancelled; no write`)
    await page.getByRole('combobox', { name: 'Status grupe', exact: true }).selectOption('3')
    await page.getByRole('heading', { name: 'Nema grupa za odabrane filtre', exact: true }).waitFor()
    await shot('groups-empty')
    results.interactions.push(`${size}: empty group filter PASS`)
    await context.close()
  }
} finally {
  await writeFile(`${output}/measurements.json`, JSON.stringify(results, null, 2))
  await browser.close()
}
if (results.errors.length) throw new Error('Browser page errors')
console.log(`PASS: ${results.screens.length} screenshots; ${results.interactions.length} interaction checks; no page errors.`)
