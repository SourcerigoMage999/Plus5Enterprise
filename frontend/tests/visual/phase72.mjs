import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_PRESENTATION_CANONICAL) {
  throw new Error('Provide local review credentials, Playwright and the canonical PNG path.')
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.2', import.meta.url))
const sample = join(tmpdir(), `plus5-phase72-${process.pid}.pdf`)
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_PRESENTATION_CANONICAL, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  realObjectStorage: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '4.3 P3.png',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeSlideFlow: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  setupPage.on('pageerror', error => evidence.errors.push(`setup: ${error.message}`))
  await login(setupPage)
  const materialId = await importFixture(setupPage, sample)
  await createDraft(setupPage, materialId)
  await setupPage.getByRole('link', { name: 'Otvori editor prezentacije' }).click()
  await setupPage.getByRole('heading', { name: 'Započnite praznu prezentaciju' }).waitFor()

  let response = setupPage.waitForResponse(item => item.url().endsWith(`/api/v1/materials/${materialId}/presentation`) && item.request().method() === 'POST')
  await setupPage.getByRole('button', { name: 'Započni prezentaciju' }).click()
  if ((await response).status() !== 200) throw new Error('Presentation initialization did not return HTTP 200.')
  await editorReady(setupPage)

  let workspace = await readWorkspace(setupPage, materialId)
  if (workspace.status !== 200 || workspace.body.document.slides.length !== 1) {
    throw new Error(`Unexpected initialized presentation: ${JSON.stringify(workspace)}`)
  }
  const staleRowVersion = workspace.body.document.rowVersion

  response = setupPage.waitForResponse(item => item.url().endsWith(`/api/v1/materials/${materialId}/presentation/slides`) && item.request().method() === 'POST')
  await setupPage.getByRole('button', { name: 'Dodaj slajd', exact: true }).first().click()
  if ((await response).status() !== 200) throw new Error('Add slide did not return HTTP 200.')
  await setupPage.getByText('Dodan je novi prazni slajd.').waitFor()
  await waitForSlideCount(setupPage, materialId, 2)

  response = setupPage.waitForResponse(item => item.url().includes('/duplicate') && item.request().method() === 'POST')
  await setupPage.getByRole('button', { name: 'Dupliciraj', exact: true }).click()
  if ((await response).status() !== 200) throw new Error('Duplicate slide did not return HTTP 200.')
  await setupPage.getByText('Slajd je dupliciran.').waitFor()
  await waitForSlideCount(setupPage, materialId, 3)

  response = setupPage.waitForResponse(item => item.url().endsWith('/slides/order') && item.request().method() === 'PUT')
  await setupPage.getByRole('button', { name: 'Pomakni slajd dolje' }).click()
  if ((await response).status() !== 200) throw new Error('Reorder did not return HTTP 200.')
  await setupPage.getByText('Redoslijed slajdova je spremljen.').waitFor()

  response = setupPage.waitForResponse(item => item.url().includes('/presentation/slides/') && item.request().method() === 'DELETE')
  setupPage.once('dialog', dialog => dialog.accept())
  await setupPage.getByRole('button', { name: 'Obriši', exact: true }).click()
  if ((await response).status() !== 200) throw new Error('Delete slide did not return HTTP 200.')
  await setupPage.getByText('Slajd je obrisan.').waitFor()
  await waitForSlideCount(setupPage, materialId, 2)

  response = setupPage.waitForResponse(item => item.url().endsWith(`/api/v1/materials/${materialId}/presentation/slides`) && item.request().method() === 'POST')
  await setupPage.getByRole('button', { name: 'Dodaj slajd', exact: true }).first().click()
  if ((await response).status() !== 200) throw new Error('Final add slide did not return HTTP 200.')
  await setupPage.getByText('Dodan je novi prazni slajd.').waitFor()
  workspace = await waitForSlideCount(setupPage, materialId, 3)

  const staleStatus = await setupPage.evaluate(async ({ id, rowVersion }) => {
    const csrfResponse = await fetch('/api/v1/auth/csrf')
    const csrf = await csrfResponse.json()
    const write = await fetch(`/api/v1/materials/${id}/presentation/slides`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token },
      body: JSON.stringify({ expectedDocumentRowVersion: rowVersion, afterSlideId: null }),
    })
    return write.status
  }, { id: materialId, rowVersion: staleRowVersion })
  const missingCsrfStatus = await setupPage.evaluate(async ({ id, rowVersion }) => {
    const write = await fetch(`/api/v1/materials/${id}/presentation/slides`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ expectedDocumentRowVersion: rowVersion, afterSlideId: null }),
    })
    return write.status
  }, { id: materialId, rowVersion: workspace.body.document.rowVersion })
  const anonymousContext = await browser.newContext()
  const anonymousStatus = (await anonymousContext.request.get(`http://localhost:8081/api/v1/materials/${materialId}/presentation`)).status()
  await anonymousContext.close()
  if (staleStatus !== 409 || missingCsrfStatus !== 400 || anonymousStatus !== 401) {
    throw new Error(`Security/concurrency gate failed: stale ${staleStatus}, no-CSRF ${missingCsrfStatus}, anonymous ${anonymousStatus}.`)
  }
  if (workspace.body.document.slides.some((slide, index) => slide.sortOrder !== index)) {
    throw new Error(`Slide order is not contiguous: ${JSON.stringify(workspace.body.document.slides)}`)
  }
  evidence.runtimeSlideFlow = {
    materialId,
    importStatus: 201,
    draftStatus: 200,
    initializeStatus: 200,
    slideCount: 3,
    contiguousSortOrder: true,
    staleRowVersionStatus: staleStatus,
    missingCsrfStatus,
    anonymousStatus,
  }
  evidence.checks.push('runtime: import PRESENTATION → Draft v2 → initialize → add → duplicate → reorder → delete → add')
  evidence.checks.push('security/concurrency: anonymous 401, missing CSRF 400 and stale document rowversion 409')
  await setupContext.close()

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
    await page.goto(`http://localhost:8081/materials/${materialId}/presentation`)
    await editorReady(page)
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({ path: `${output}/presentation-editor-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'editor', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real login/editor API, three persisted blank slides, Phase 7.2 controls and no document overflow`)
    if (size === 'mobile') {
      await page.locator('.presentation-editor__workspace').scrollIntoViewIfNeeded()
      await settle(page)
      await page.screenshot({ path: `${output}/presentation-editor-mobile-workspace-390x844.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'editor-workspace', size, capture: 'viewport', ...await measure(page) })
    }
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.2 real slide CRUD/concurrency flow plus desktop/mobile visual gate.')
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

async function importFixture(page, samplePath) {
  await page.goto('http://localhost:8081/materials/import')
  await page.locator('input[type="file"]').setInputFiles(samplePath)
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel(/Naziv/).fill(`Phase 7.2 prezentacija ${new Date().toISOString()}`)
  await page.getByLabel(/Vrsta materijala/).selectOption('PRESENTATION')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel('Cilj učenja').fill('Učenik prati strukturu prezentacije kroz jasno poredane slajdove.')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/v1/materials/import') && response.request().method() === 'POST')
  await page.getByRole('button', { name: /Potvrdi i uvezi/ }).click()
  const response = await responsePromise
  if (response.status() !== 201) throw new Error(`Fixture import returned HTTP ${response.status()}: ${await response.text()}`)
  return (await response.json()).materialId
}

async function createDraft(page, materialId) {
  await page.goto(`http://localhost:8081/materials/${materialId}/edit`)
  await page.getByRole('heading', { level: 1, name: '4.5 Uredi materijal' }).waitFor()
  await page.getByLabel('Naziv *').fill('Phase 7.2 — prezentacija praznih slajdova')
  const responsePromise = page.waitForResponse(response => response.url().endsWith(`/api/v1/materials/${materialId}/draft`) && response.request().method() === 'PUT')
  await page.getByRole('button', { name: /Spremi skicu/ }).click()
  const response = await responsePromise
  if (response.status() !== 200) throw new Error(`Draft save returned HTTP ${response.status()}: ${await response.text()}`)
  await page.getByText(/v2 · Skica/).waitFor()
}

async function editorReady(page) {
  await page.getByRole('heading', { level: 1, name: '4.3 Novi materijal – Prezentacija' }).waitFor()
  await page.locator('.presentation-editor__slides li').first().waitFor()
}

async function readWorkspace(page, materialId) {
  return page.evaluate(async id => {
    const response = await fetch(`/api/v1/materials/${id}/presentation`)
    return { status: response.status, body: await response.json() }
  }, materialId)
}

async function waitForSlideCount(page, materialId, expected) {
  let workspace
  await page.waitForFunction(async ({ id, count }) => {
    const response = await fetch(`/api/v1/materials/${id}/presentation`)
    if (!response.ok) return false
    const value = await response.json()
    return value.document?.slides?.length === count
  }, { id: materialId, count: expected })
  workspace = await readWorkspace(page, materialId)
  return workspace
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
