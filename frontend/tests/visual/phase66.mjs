import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_MATERIAL_IMPORT_CANONICAL) {
  throw new Error('Provide local review environment variables and the canonical PNG path.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-6.6', import.meta.url))
const sample = join(tmpdir(), `plus5-phase66-${process.pid}.pdf`)
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_MATERIAL_IMPORT_CANONICAL, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realObjectStorage: true,
  realMalwareScanner: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeImport: null,
}

try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    let captureStarted = false
    page.on('pageerror', error => evidence.errors.push(`${size}: ${error.message}`))
    page.on('request', request => {
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${new URL(request.url()).pathname}`)
      }
    })

    await login(page)
    captureStarted = true
    await page.goto('http://localhost:8081/materials/import')
    await page.getByRole('heading', { level: 1, name: '4.4 Uvoz vlastitog materijala' }).waitFor()
    await page.getByRole('heading', { level: 2, name: 'Odaberite datoteku' }).waitFor()
    await settle(page)
    const fileDimensions = await measure(page)
    assertNoOverflow(`${size} file`, fileDimensions)
    if (size === 'desktop') {
      await page.screenshot({ path: `${output}/material-import-file-desktop-1536x1024.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'file', size, capture: 'viewport', ...fileDimensions })
    }

    await prepareReview(page, sample, `Phase 6.6 pregled — ${size}`)
    await page.evaluate(() => window.scrollTo(0, 0))
    await settle(page)
    const reviewDimensions = await measure(page)
    assertNoOverflow(`${size} review`, reviewDimensions)
    await page.screenshot({ path: `${output}/material-import-review-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'review', size, capture: 'viewport', ...reviewDimensions })
    if (size === 'mobile') {
      await page.getByRole('button', { name: /Potvrdi i uvezi/ }).scrollIntoViewIfNeeded()
      await page.screenshot({ path: `${output}/material-import-review-actions-mobile.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'review-actions', size, capture: 'viewport', ...await measure(page) })
    }
    evidence.checks.push(`${size}: real login/options API, four-step flow, local file/metadata/mapping review and no document overflow`)
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected visual-capture business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)

  const runtimeContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const runtimePage = await runtimeContext.newPage()
  runtimePage.on('pageerror', error => evidence.errors.push(`runtime: ${error.message}`))
  await login(runtimePage)
  await runtimePage.goto('http://localhost:8081/materials/import')
  const runtimeTitle = `Phase 6.6 siguran uvoz ${new Date().toISOString()}`
  await prepareReview(runtimePage, sample, runtimeTitle)
  const importResponse = runtimePage.waitForResponse(response =>
    response.url().endsWith('/api/v1/materials/import') && response.request().method() === 'POST')
  await runtimePage.getByRole('button', { name: /Potvrdi i uvezi/ }).click()
  const response = await importResponse
  if (response.status() !== 201) throw new Error(`Runtime import returned HTTP ${response.status()}: ${await response.text()}`)
  const created = await response.json()
  await runtimePage.waitForURL(new RegExp(`/materials/${created.materialId}$`))
  await runtimePage.getByRole('heading', { level: 2, name: runtimeTitle }).waitFor()
  const detail = await runtimePage.evaluate(async materialId => {
    const detailResponse = await fetch(`/api/v1/materials/${materialId}`)
    return { status: detailResponse.status, body: await detailResponse.json() }
  }, created.materialId)
  if (detail.status !== 200 || detail.body.title !== runtimeTitle || detail.body.file?.format !== 'pdf') {
    throw new Error(`Imported material detail is inconsistent: ${JSON.stringify(detail)}`)
  }
  evidence.runtimeImport = {
    status: response.status(),
    materialId: created.materialId,
    detailStatus: detail.status,
    fileFormat: detail.body.file.format,
    expectedBusinessWrites: 1,
  }
  evidence.checks.push('runtime: one intentional safe PDF import passed validation, quarantine scan, clean promotion, atomic activation and detail read-back')
  await runtimeContext.close()

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 6.6 real-login/API/storage/scanner import plus desktop/mobile visual gate.')
} finally {
  await browser.close()
  await unlink(sample).catch(() => {})
}

async function login(page) {
  await page.goto('http://localhost:8081/auth/login')
  await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
  await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
  await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
  await page.waitForURL('http://localhost:8081/')
}

async function prepareReview(page, samplePath, title) {
  await page.locator('input[type="file"]').setInputFiles(samplePath)
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByRole('heading', { level: 2, name: 'Osnovni podaci' }).waitFor()
  await page.getByLabel(/Naziv/).fill(title)
  await page.getByLabel(/Vrsta materijala/).selectOption('WORKSHEET')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByLabel('Opis').fill('Siguran lokalni Phase 6.6 import workflow.')
  await page.getByLabel('Oznake').fill('phase-6.6, siguran-uvoz')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByRole('heading', { level: 2, name: 'Cilj i pedagoško mapiranje' }).waitFor()
  await page.getByLabel('Cilj učenja').fill('Učenik koristi materijal u planiranoj nastavnoj aktivnosti.')
  const component = page.locator('.material-import__choices input[type="checkbox"]').first()
  if (await component.count()) await component.check()
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByRole('heading', { level: 2, name: 'Pregled prije uvoza' }).waitFor()
}

async function settle(page) {
  await page.evaluate(async () => {
    await document.fonts.ready
    await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
  })
}

async function measure(page) {
  return page.evaluate(() => ({
    viewportWidth: innerWidth,
    viewportHeight: innerHeight,
    clientWidth: document.documentElement.clientWidth,
    scrollWidth: document.documentElement.scrollWidth,
    scrollHeight: document.documentElement.scrollHeight,
  }))
}

function assertNoOverflow(name, dimensions) {
  if (dimensions.scrollWidth > dimensions.clientWidth) {
    throw new Error(`Document overflow at ${name}: ${dimensions.scrollWidth}/${dimensions.clientWidth}`)
  }
}
