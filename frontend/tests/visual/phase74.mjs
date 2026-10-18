import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = ['PLUS5_REVIEW_PLAYWRIGHT', 'PLUS5_REVIEW_EMAIL', 'PLUS5_REVIEW_PASSWORD']
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.4', import.meta.url))
const canonical = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.3/canonical.png', import.meta.url))
const sample = join(tmpdir(), `plus5-phase74-${process.pid}.pdf`)
const video = join(tmpdir(), `plus5-phase74-${process.pid}.mp4`)
const audio = join(tmpdir(), `plus5-phase74-${process.pid}.mp3`)
await mkdir(output, { recursive: true })
await copyFile(canonical, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')
await writeFile(video, Buffer.from([0, 0, 0, 20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6f, 0x6d]))
await writeFile(audio, createMp3Frames())

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '4.3 P3.png',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeElementTypes: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const page = await setupContext.newPage()
  page.setDefaultTimeout(60_000)
  page.on('pageerror', error => evidence.errors.push(`setup: ${error.message}`))
  await login(page)
  const materialId = await importFixture(page, sample)
  await createDraft(page, materialId)
  await page.getByRole('link', { name: 'Otvori editor prezentacije' }).click()
  await page.getByRole('button', { name: 'Započni prezentaciju' }).click()
  await editorReady(page)

  for (const name of ['Pravilo', 'Primjer', 'Vokabular', 'Pitanje']) {
    const response = page.waitForResponse(item => item.url().includes('/elements') && item.request().method() === 'POST')
    await page.getByRole('button', { name, exact: true }).click()
    if ((await response).status() !== 200) throw new Error(`${name} create did not return HTTP 200.`)
    await page.getByText(`${name} je dodan na slajd.`).waitFor()
  }

  await page.getByRole('button', { name: /Dodaj slajd/ }).click()
  await page.getByText('Dodan je novi prazni slajd.').waitFor()
  await page.locator('.presentation-editor__slides li').nth(1).waitFor()
  await page.locator('.presentation-editor__slides li').last().getByRole('button').click()
  await page.waitForTimeout(250)

  const videoResponse = page.waitForResponse(
    item => item.url().includes('/videos') && item.request().method() === 'POST',
    { timeout: 60_000 },
  )
  await page.locator('input[type="file"][accept*="video/mp4"]').setInputFiles(video)
  if ((await videoResponse).status() !== 200) throw new Error('Video upload did not return HTTP 200.')
  await page.getByText('MP4 video je sigurno provjeren i dodan na slajd.').waitFor()

  const audioResponse = page.waitForResponse(
    item => item.url().includes('/audio') && item.request().method() === 'POST',
    { timeout: 60_000 },
  )
  await page.locator('input[type="file"][accept*="audio/mpeg"]').setInputFiles(audio)
  if ((await audioResponse).status() !== 200) throw new Error('Audio upload did not return HTTP 200.')
  await page.getByText('MP3 audio je sigurno provjeren i dodan na slajd.').waitFor()

  const workspace = await readWorkspace(page, materialId)
  const elements = workspace.body.document.slides.flatMap(slide => slide.elements)
  const types = elements.map(element => element.elementTypeCode).sort()
  if (workspace.status !== 200 || types.join(',') !== 'AUDIO,EXAMPLE,QUESTION,RULE,VIDEO,VOCABULARY') {
    throw new Error(`Unexpected teaching element workspace: ${JSON.stringify(workspace)}`)
  }
  const audioElement = elements.find(element => element.elementTypeCode === 'AUDIO')
  const videoElement = elements.find(element => element.elementTypeCode === 'VIDEO')
  const audioPayload = JSON.parse(audioElement.payloadJson)
  if (audioPayload.autoplay !== false || audioPayload.showControls !== true || audioElement.asset.assetKindCode !== 'AUDIO') {
    throw new Error(`AUDIO contract mismatch: ${JSON.stringify(audioElement)}`)
  }
  if (videoElement.asset.assetKindCode !== 'VIDEO') throw new Error('VIDEO asset kind mismatch.')
  const audioAutoplayAttribute = await page.locator('audio').getAttribute('autoplay')
  if (audioAutoplayAttribute !== null) throw new Error('Rendered audio unexpectedly has autoplay.')
  evidence.runtimeElementTypes = types
  evidence.checks.push('runtime: RULE + EXAMPLE + VOCABULARY + instructional QUESTION + Clean MP4 VIDEO + Clean MP3 AUDIO')
  evidence.checks.push('audio: controls present and no autoplay attribute')
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
    await capturePage.getByRole('navigation', { name: 'Elementi slajda' }).getByRole('button', { name: /Pravilo/ }).click()
    await capturePage.evaluate(() => window.scrollTo(0, 0))
    await settle(capturePage)
    const dimensions = await measure(capturePage)
    assertNoOverflow(size, dimensions)
    await capturePage.screenshot({ path: `${output}/presentation-teaching-elements-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'teaching-elements', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real editor data, teaching toolbar/cards/property panel and no document overflow`)

    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.4 real teaching/media flow plus desktop/mobile visual gate.')
} finally {
  await browser.close()
  await Promise.all([sample, video, audio].map(path => unlink(path).catch(() => {})))
}

function createMp3Frames() {
  const frameLength = 417
  const bytes = Buffer.alloc(frameLength * 2)
  for (const offset of [0, frameLength]) {
    bytes[offset] = 0xff
    bytes[offset + 1] = 0xfb
    bytes[offset + 2] = 0x90
    bytes[offset + 3] = 0
  }
  return bytes
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
  await page.getByLabel(/Naziv/).fill(`Phase 7.4 nastavni elementi ${new Date().toISOString()}`)
  await page.getByLabel(/Vrsta materijala/).selectOption('PRESENTATION')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel('Cilj učenja').fill('Učenik uči kroz pravilo, primjer, vokabular, raspravu i privatne medije.')
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
  await page.getByLabel('Naziv *').fill('Phase 7.4 — nastavni elementi')
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
