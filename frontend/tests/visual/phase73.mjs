import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_PRESENTATION_CANONICAL',
  'PLUS5_PRESENTATION_IMAGE',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.3', import.meta.url))
const sample = join(tmpdir(), `plus5-phase73-${process.pid}.pdf`)
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_PRESENTATION_CANONICAL, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  realObjectStorage: true,
  realMalwareScan: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '4.3 P3.png',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeElementFlow: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const page = await setupContext.newPage()
  page.on('pageerror', error => evidence.errors.push(`setup: ${error.message}`))
  await login(page)
  const materialId = await importFixture(page, sample)
  await createDraft(page, materialId)
  await page.getByRole('link', { name: 'Otvori editor prezentacije' }).click()
  await page.getByRole('button', { name: 'Započni prezentaciju' }).click()
  await editorReady(page)

  for (const name of ['Tekst', 'Oblik', 'Tablica']) {
    const response = page.waitForResponse(item => item.url().includes('/elements') && item.request().method() === 'POST')
    await page.getByRole('button', { name, exact: true }).click()
    if ((await response).status() !== 200) throw new Error(`${name} create did not return HTTP 200.`)
    await page.getByText(`${name} je dodan na slajd.`).waitFor()
  }

  const imageResponse = page.waitForResponse(item => item.url().includes('/images') && item.request().method() === 'POST')
  await page.locator('input[type="file"][accept*="image/png"]').setInputFiles(process.env.PLUS5_PRESENTATION_IMAGE)
  if ((await imageResponse).status() !== 200) throw new Error('Image upload did not return HTTP 200.')
  await page.getByText('Slika je sigurno provjerena i dodana na slajd.').waitFor()

  await page.getByRole('navigation', { name: 'Elementi slajda' }).getByRole('button', { name: /Tekst/ }).click()
  await page.getByLabel('Sadržaj').fill('Present Perfect — pravilo i primjer')
  const updateResponse = page.waitForResponse(item => item.url().includes('/elements/') && item.request().method() === 'PUT')
  await page.getByRole('button', { name: 'Spremi element' }).click()
  if ((await updateResponse).status() !== 200) throw new Error('Element update did not return HTTP 200.')
  await page.getByText('Element je spremljen.').waitFor()

  const workspace = await readWorkspace(page, materialId)
  const firstSlide = workspace.body.document.slides[0]
  if (workspace.status !== 200 || firstSlide.elements.length !== 4
    || firstSlide.elements.map(element => element.elementTypeCode).sort().join(',') !== 'IMAGE,SHAPE,TABLE,TEXT') {
    throw new Error(`Unexpected core element workspace: ${JSON.stringify(workspace)}`)
  }
  if (firstSlide.elements.some(element => 'objectKey' in element || 'sha256Checksum' in element)) {
    throw new Error('Storage internals were exposed in the editor DTO.')
  }
  const imageElement = firstSlide.elements.find(element => element.elementTypeCode === 'IMAGE')
  const imageStatus = (await page.request.get(`http://localhost:8081/api/v1${imageElement.asset.readPath}`)).status()
  const anonymous = await browser.newContext()
  const anonymousAssetStatus = (await anonymous.request.get(`http://localhost:8081/api/v1${imageElement.asset.readPath}`)).status()
  await anonymous.close()
  if (imageStatus !== 200 || anonymousAssetStatus !== 401) {
    throw new Error(`Asset access gate failed: owner ${imageStatus}, anonymous ${anonymousAssetStatus}.`)
  }
  evidence.runtimeElementFlow = {
    materialId,
    elementTypes: firstSlide.elements.map(element => element.elementTypeCode),
    contiguousZIndex: firstSlide.elements.every((element, index) => element.zIndex === index),
    ownerAssetStatus: imageStatus,
    anonymousAssetStatus,
    storageInternalsExposed: false,
  }
  evidence.checks.push('runtime: TEXT + IMAGE + SHAPE + TABLE create, server-normalized update and real Clean image read')
  evidence.checks.push('security: owner asset 200, anonymous asset 401, no storage key/checksum in DTO')
  await setupContext.close()

  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const capturePage = await context.newPage()
    let captureStarted = false
    capturePage.on('pageerror', error => evidence.errors.push(`${size}: ${error.message}`))
    capturePage.on('request', request => {
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${new URL(request.url()).pathname}`)
      }
    })
    await login(capturePage)
    captureStarted = true
    await capturePage.goto(`http://localhost:8081/materials/${materialId}/presentation`)
    await editorReady(capturePage)
    await capturePage.getByRole('navigation', { name: 'Elementi slajda' }).getByRole('button', { name: /Tekst/ }).click()
    await capturePage.evaluate(() => window.scrollTo(0, 0))
    await settle(capturePage)
    const dimensions = await measure(capturePage)
    assertNoOverflow(size, dimensions)
    await capturePage.screenshot({ path: `${output}/presentation-core-elements-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'core-elements', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real editor query and image stream, four core elements, selected property panel, no document overflow`)
    if (size === 'mobile') {
      await capturePage.locator('.presentation-editor__workspace').scrollIntoViewIfNeeded()
      await settle(capturePage)
      await capturePage.screenshot({ path: `${output}/presentation-core-elements-mobile-workspace-390x844.png`, fullPage: false, animations: 'disabled' })
    }
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.3 real core element/storage flow plus desktop/mobile visual gate.')
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
  await page.getByLabel(/Naziv/).fill(`Phase 7.3 core elementi ${new Date().toISOString()}`)
  await page.getByLabel(/Vrsta materijala/).selectOption('PRESENTATION')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel('Cilj učenja').fill('Učenik razumije pravilo i primjer kroz strukturirane elemente.')
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
  await page.getByLabel('Naziv *').fill('Phase 7.3 — core elementi')
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
