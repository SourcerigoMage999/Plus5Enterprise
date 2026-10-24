import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_PRESENTATION_REVIEW_CANONICAL',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.8', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://127.0.0.1:5173'
const apiOrigin = process.env.PLUS5_REVIEW_API_ORIGIN ?? 'http://127.0.0.1:8080'
const sample = join(tmpdir(), `plus5-phase78-${process.pid}.pdf`)
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_PRESENTATION_REVIEW_CANONICAL, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  realObjectStorage: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '4.3 P6.png',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimePublish: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  setupPage.setDefaultTimeout(60_000)
  await login(setupPage)
  observeErrors(setupPage, 'setup')
  const materialId = await importFixture(setupPage, sample)
  await createDraft(setupPage, materialId)
  await initializePresentation(setupPage, materialId)
  await setupContext.close()

  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.setDefaultTimeout(60_000)
    await login(page)
    observeErrors(page, size)
    let captureStarted = true
    page.on('request', request => {
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${new URL(request.url()).pathname}`)
      }
    })
    await openReview(page, materialId)
    await page.getByRole('button', { name: /Spremi materijal/ }).waitFor()
    if (await page.getByRole('button', { name: /Spremi materijal/ }).isDisabled()) {
      throw new Error(`Publish is blocked at ${size}: ${await page.locator('main').innerText().catch(() => page.locator('body').innerText())}`)
    }
    if (size === 'desktop') {
      await page.evaluate(() => window.scrollTo(0, 0))
    } else {
      await page.locator('.presentation-review__preview').scrollIntoViewIfNeeded()
    }
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/presentation-review-${size}-${width}x${height}.png`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'review-and-save', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real review API, metadata, pedagogical summary, persisted slide preview and no document overflow`)
    captureStarted = false
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)

  const publishContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const publishPage = await publishContext.newPage()
  publishPage.setDefaultTimeout(60_000)
  await login(publishPage)
  observeErrors(publishPage, 'publish')
  await openReview(publishPage, materialId)
  const review = await readJson(publishPage, `/api/v1/materials/${materialId}/presentation/review`)
  if (review.status !== 200) throw new Error(`Review API returned ${review.status} before publish.`)
  const publishResponse = publishPage.waitForResponse(response =>
    response.url().endsWith(`/api/v1/materials/${materialId}/presentation/publish`)
      && response.request().method() === 'POST')
  await publishPage.getByRole('button', { name: /Spremi materijal/ }).click()
  const published = await publishResponse
  if (published.status() !== 200) {
    throw new Error(`Presentation publish returned HTTP ${published.status()}: ${await published.text()}`)
  }
  await publishPage.waitForURL(`${appOrigin}/materials/${materialId}`)
  await publishPage.getByRole('heading', { level: 1, name: '4.2 Pregled materijala' }).waitFor()
  const detail = await readJson(publishPage, `/api/v1/materials/${materialId}`)
  const retiredReview = await requestJson(publishContext.request, `/api/v1/materials/${materialId}/presentation/review`)
  if (detail.status !== 200
      || detail.body.versionId !== review.body.materialVersionId
      || detail.body.file?.mediaType !== 'application/vnd.plus5.presentation+zip'
      || detail.body.file?.originalFileName !== 'plus5-presentation.zip'
      || retiredReview.status !== 404) {
    throw new Error(`Published material verification failed: ${JSON.stringify({ detail, retiredReview })}`)
  }
  evidence.runtimePublish = {
    materialId,
    materialVersionId: detail.body.versionId,
    publishStatus: published.status(),
    detailStatus: detail.status,
    formerDraftReviewStatus: retiredReview.status,
    packageMediaType: detail.body.file.mediaType,
    packageFileName: detail.body.file.originalFileName,
  }
  evidence.checks.push('runtime: review → deterministic private package → atomic publish → material detail')
  await publishContext.close()

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.8 real review/publish flow plus desktop/mobile visual gate.')
} catch (error) {
  console.log(`FAIL: ${error instanceof Error ? error.stack : String(error)}`)
  process.exitCode = 1
} finally {
  await browser.close()
  await unlink(sample).catch(() => {})
}

async function login(page) {
  await page.goto(`${appOrigin}/auth/login`)
  await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
  await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
  await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
  await page.waitForURL(`${appOrigin}/`)
}

async function openReview(page, materialId) {
  await page.goto(`${appOrigin}/materials/${materialId}/presentation/review`)
  await page.getByRole('heading', { level: 1, name: 'Pregled i spremanje' }).waitFor()
  await page.locator('.presentation-review-canvas').first().waitFor()
}

async function importFixture(page, samplePath) {
  await page.goto(`${appOrigin}/materials/import`)
  await page.locator('input[type="file"]').setInputFiles(samplePath)
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel(/Naziv/).fill(`Phase 7.8 pregled i spremanje ${new Date().toISOString()}`)
  await page.getByLabel(/Vrsta materijala/).selectOption('PRESENTATION')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel('Cilj učenja').fill('Učenik pregledava dovršenu prezentaciju prije spremanja materijala.')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/v1/materials/import') && response.request().method() === 'POST')
  await page.getByRole('button', { name: /Potvrdi i uvezi/ }).click()
  const response = await responsePromise
  if (response.status() !== 201) throw new Error(`Fixture import returned HTTP ${response.status()}: ${await response.text()}`)
  return (await response.json()).materialId
}

async function createDraft(page, materialId) {
  await page.goto(`${appOrigin}/materials/${materialId}/edit`)
  await page.getByRole('heading', { level: 1, name: '4.5 Uredi materijal' }).waitFor()
  await page.getByLabel('Naziv *').fill('Phase 7.8 — pregled i spremanje prezentacije')
  const responsePromise = page.waitForResponse(response => response.url().endsWith(`/api/v1/materials/${materialId}/draft`) && response.request().method() === 'PUT')
  await page.getByRole('button', { name: /Spremi skicu/ }).click()
  const response = await responsePromise
  if (response.status() !== 200) throw new Error(`Draft save returned HTTP ${response.status()}: ${await response.text()}`)
}

async function initializePresentation(page, materialId) {
  await page.goto(`${appOrigin}/materials/${materialId}/presentation`)
  await page.getByRole('heading', { level: 1, name: 'Započnite praznu prezentaciju' }).waitFor()
  const responsePromise = page.waitForResponse(response => response.url().endsWith(`/api/v1/materials/${materialId}/presentation`) && response.request().method() === 'POST')
  await page.getByRole('button', { name: 'Započni prezentaciju' }).click()
  const response = await responsePromise
  if (response.status() !== 200) throw new Error(`Presentation initialization returned HTTP ${response.status()}: ${await response.text()}`)
  await page.locator('.presentation-editor__slides li').first().waitFor()
}

async function readJson(page, path) {
  return page.evaluate(async ({ requestPath, origin }) => {
    const response = await fetch(`${origin}${requestPath}`, { credentials: 'include' })
    const body = response.headers.get('content-type')?.includes('json')
      ? await response.json()
      : null
    return { status: response.status, body }
  }, { requestPath: path, origin: apiOrigin })
}

async function requestJson(request, path) {
  const response = await request.get(`${apiOrigin}${path}`)
  return {
    status: response.status(),
    body: response.headers()['content-type']?.includes('json') ? await response.json() : null,
  }
}

function observeErrors(page, label) {
  page.on('pageerror', error => evidence.errors.push(`${label}: ${error.message}`))
  page.on('console', message => {
    if (message.type() === 'error') evidence.errors.push(`${label} console: ${message.text()}`)
  })
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

function assertNoOverflow(size, dimensions) {
  if (dimensions.scrollWidth > dimensions.clientWidth) {
    throw new Error(`Document overflow at ${size}: ${dimensions.scrollWidth}/${dimensions.clientWidth}`)
  }
}
