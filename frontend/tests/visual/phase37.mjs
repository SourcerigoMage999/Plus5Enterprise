import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_REVIEW_GROUP_ID) {
  throw new Error('Provide local review environment variables.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-3.7', import.meta.url))
await mkdir(output, { recursive: true })
const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = { screens: [], errors: [], checks: [] }

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
      const dimensions = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth }))
      if (dimensions.document > width) throw new Error(`Document overflow at ${size}`)
      await page.screenshot({ path: `${output}/${name}-${size}.png`, fullPage: true, animations: 'disabled' })
      evidence.screens.push({ name, size, ...dimensions })
    }

    await page.goto('http://localhost:8081/auth/login')
    await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
    await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
    await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
    await page.waitForURL('http://localhost:8081/')
    await page.goto(`http://localhost:8081/students/groups/${process.env.PLUS5_REVIEW_GROUP_ID}/edit`)
    await page.getByRole('heading', { name: '2.9 Uredi grupu' }).waitFor()
    const program = page.getByLabel('Program / fokus', { exact: false })
    if (await program.isEnabled()) throw new Error('Program must be locked for a group with an active member')
    await page.getByText(/Program nije moguće promijeniti dok grupa ima aktivne učenike/).waitFor()
    await shot('edit')

    const name = page.getByLabel('Naziv grupe', { exact: false })
    await name.fill(`${await name.inputValue()} pregled`)
    await page.getByRole('link', { name: 'Odustani', exact: true }).click()
    await page.getByRole('dialog').waitFor()
    await shot('unsaved')
    await page.getByRole('button', { name: 'Ostani na obrascu' }).click()
    if (!page.url().endsWith('/edit')) throw new Error('Dirty navigation protection failed')
    evidence.checks.push(`${size}: canonical edit, program lock, dirty-navigation modal, no overflow`)
    await context.close()
  }
  if (evidence.errors.length) throw new Error('Browser errors occurred')
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 3.7 desktop/mobile visual and navigation checks; no business writes.')
} finally {
  await browser.close()
}
