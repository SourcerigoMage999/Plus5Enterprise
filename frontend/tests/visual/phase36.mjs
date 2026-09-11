import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'
if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL || !process.env.PLUS5_REVIEW_PASSWORD) throw new Error('Provide local review environment variables.')
const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-3.6', import.meta.url))
await mkdir(output, { recursive: true })
const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = { screens: [], errors: [], created: [], checks: [] }
const run = Date.now().toString(36)
const captureOnly = process.env.PLUS5_REVIEW_CAPTURE_ONLY === 'true'
const date = new Date(); date.setDate(date.getDate() + 7)
const startDate = date.toISOString().slice(0, 10)
let scheduleGroup
try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.on('pageerror', error => evidence.errors.push(error.message))
    async function shot(name) {
      await page.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(i => i.decode().catch(() => {}))); window.scrollTo(0, 0) })
      const dimensions = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth }))
      if (dimensions.document > width) throw new Error('Document overflow')
      await page.screenshot({ path: output + '/' + name + '-' + size + '.png', fullPage: true, animations: 'disabled' })
      evidence.screens.push({ name, size, ...dimensions })
    }
    await page.goto('http://localhost:8081/auth/login')
    await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
    await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
    await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
    await page.waitForURL('http://localhost:8081/')
    await page.goto('http://localhost:8081/students/groups')
    await page.getByRole('link', { name: '+ Nova grupa' }).click()
    await page.waitForURL('**/students/groups/new')
    const program = page.getByLabel('Program / fokus', { exact: false })
    const grade = page.getByLabel('Razred / razina', { exact: false })
    await program.locator('option').nth(1).waitFor({ state: 'attached' })
    await program.selectOption({ index: 1 })
    await grade.selectOption({ index: 1 })
    await page.getByRole('navigation', { name: 'Stranice učenika' }).waitFor()
    if (size === 'desktop' && !captureOnly) {
      const gradeId = await grade.inputValue()
      const csrf = await (await context.request.get('http://localhost:8081/api/v1/auth/csrf')).json()
      const response = await context.request.post('http://localhost:8081/api/v1/students', { headers: { 'X-CSRF-TOKEN': csrf.token }, data: { firstName: 'Vizualni', lastName: 'Učenik ' + run, schoolGradeId: gradeId, status: 'active' } })
      if (response.status() !== 201) throw new Error('Fixture student create failed: ' + response.status())
      const student = await response.json()
      evidence.created.push({ type: 'student', id: student.id })
      await page.reload()
      await program.selectOption({ index: 1 }); await grade.selectOption({ index: 1 })
      await page.getByLabel('Pretraži učenike', { exact: true }).fill(run)
      await page.getByRole('checkbox', { name: /Vizualni/ }).waitFor()
    }
    await page.getByLabel('Naziv grupe', { exact: false }).fill('Grupa 3.6 ' + size + ' ' + run)
    await page.getByLabel('Maksimalan broj učenika', { exact: false }).fill('6')
    if (size === 'desktop') {
      const candidates = page.getByRole('checkbox', { name: /Vizualni/ })
      if (await candidates.count()) await candidates.first().check()
    }
    await page.getByRole('button', { name: '+ Dodaj termin', exact: true }).click()
    await page.getByRole('combobox', { name: /Dan 1\. termina/ }).selectOption('1')
    await page.getByLabel('Početak 1. termina', { exact: true }).fill('21:00')
    await page.getByLabel('Završetak 1. termina', { exact: true }).fill('21:45')
    await page.getByLabel('Datum početka', { exact: false }).fill(startDate)
    await shot('create')
    await page.getByRole('link', { name: 'Pogledaj sve učenike' }).click()
    await page.getByRole('dialog').waitFor()
    await shot('unsaved')
    await page.getByRole('button', { name: 'Ostani na obrascu' }).click()
    await page.goBack()
    await page.getByRole('dialog').waitFor()
    await page.getByRole('dialog').press('Escape')
    if (!page.url().endsWith('/students/groups/new')) throw new Error('Back protection failed')
    evidence.checks.push(size + ': link + Back + Escape retain form')
    if (captureOnly) { await context.close(); continue }
    const response = page.waitForResponse(r => r.url().endsWith('/api/v1/groups') && r.request().method() === 'POST')
    await page.getByRole('button', { name: 'Kreiraj grupu', exact: true }).click()
    const save = await response
    if (size === 'mobile') {
      if (save.status() !== 409) throw new Error('Expected conflict')
      await page.getByRole('alert').waitFor()
      await shot('conflict')
      await page.getByRole('button', { name: 'Ukloni 1. termin' }).click()
      const retry = page.waitForResponse(r => r.url().endsWith('/api/v1/groups') && r.request().method() === 'POST')
      await page.getByRole('button', { name: 'Kreiraj grupu', exact: true }).click()
      const empty = await retry
      if (empty.status() !== 201) throw new Error('Empty group create failed')
      evidence.created.push({ type: 'empty-group', ...(await empty.json()) })
      evidence.checks.push('mobile: conflict 409, then 0 members + 0 schedule create 201')
    } else {
      if (save.status() !== 201) throw new Error('Scheduled group create failed: ' + save.status())
      scheduleGroup = await save.json()
      evidence.created.push({ type: 'scheduled-group', ...scheduleGroup })
      if (scheduleGroup.sessionCount !== 12) throw new Error('Expected 12 weekly sessions')
      evidence.checks.push('desktop: selected member + open-ended schedule create 201; 12 sessions')
    }
    await page.waitForURL(/students\/groups\?group=/)
    await page.getByRole('status').filter({ hasText: 'uspješno je kreirana' }).waitFor()
    await page.getByRole('heading', { name: /Grupa 3.6/ }).waitFor()
    await shot('created')
    await context.close()
  }
  if (evidence.errors.length) throw new Error('Browser errors occurred')
  await writeFile(output + (captureOnly ? '/visual-measurements.json' : '/final-measurements.json'), JSON.stringify(evidence, null, 2))
  console.log(captureOnly ? 'PASS: final visual capture + desktop/mobile navigation; no new business writes.' : 'PASS: full create + conflict + empty group + desktop/mobile navigation. Demo fixture IDs recorded.')
} finally {
  await writeFile(output + '/run-' + run + '.json', JSON.stringify(evidence, null, 2))
  await browser.close()
}
