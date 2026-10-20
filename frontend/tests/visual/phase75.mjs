import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_REVIEW_MATERIAL_ID',
  'PLUS5_PRESENTATION_QUICK_CHECK_CANONICAL',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.5', import.meta.url))
const appOrigin = 'http://127.0.0.1:8081'
const apiOrigin = 'http://127.0.0.1:8080'
const materialId = process.env.PLUS5_REVIEW_MATERIAL_ID
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_PRESENTATION_QUICK_CHECK_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '4.3 P4.png',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeQuickCheck: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  setupPage.setDefaultTimeout(60_000)
  setupPage.on('pageerror', error => evidence.errors.push(`setup: ${error.message}`))
  await login(setupPage)
  await setupPage.goto(`${appOrigin}/materials/${materialId}/presentation`)
  const start = setupPage.getByRole('button', { name: 'Započni prezentaciju' })
  await Promise.race([
    start.waitFor(),
    setupPage.getByRole('heading', { level: 1, name: '4.3 Novi materijal – Prezentacija' }).waitFor(),
  ])
  if (await start.isVisible()) {
    const response = setupPage.waitForResponse(item => item.url().endsWith(`/api/v1/materials/${materialId}/presentation`) && item.request().method() === 'POST')
    await start.click()
    if ((await response).status() !== 200) throw new Error('Presentation initialization did not return HTTP 200.')
  }
  await editorReady(setupPage)

  let workspace = await readWorkspace(setupPage)
  let quickChecks = workspace.body.document.slides
    .flatMap(slide => slide.elements)
    .filter(element => element.elementTypeCode === 'QUICK_CHECK')
  if (quickChecks.length === 0) {
    const createResponse = setupPage.waitForResponse(
      item => item.url().includes('/quick-checks') && item.request().method() === 'POST',
    )
    await setupPage.getByRole('toolbar', { name: 'Alati prezentacije' }).getByRole('button', { name: 'Quick Check' }).click()
    if ((await createResponse).status() !== 200) throw new Error('Quick Check create did not return HTTP 200.')
    await setupPage.getByText('Quick Check je dodan na slajd.').waitFor()
    workspace = await readWorkspace(setupPage)
    quickChecks = workspace.body.document.slides
      .flatMap(slide => slide.elements)
      .filter(element => element.elementTypeCode === 'QUICK_CHECK')
  }
  if (workspace.status !== 200 || quickChecks.length !== 1) {
    throw new Error(`Unexpected Quick Check workspace: ${JSON.stringify(workspace)}`)
  }
  const quickCheck = quickChecks[0].quickCheck
  if (
    !quickCheck?.assessableTaskVersionId ||
    quickCheck.options.map(option => option.key).join(',') !== 'A,B,C' ||
    quickCheck.correctOptionKey !== 'B' ||
    quickCheck.difficulty !== 2 ||
    quickCheck.evidenceType !== 1 ||
    quickCheck.maxPoints !== 1 ||
    quickCheck.knowledgeMappingPending !== true
  ) {
    throw new Error(`Quick Check contract mismatch: ${JSON.stringify(quickCheck)}`)
  }
  evidence.runtimeQuickCheck = quickCheck
  evidence.checks.push('runtime: real single-choice Quick Check with server keys A–C and one correct answer')
  evidence.checks.push('runtime: exact TaskVersion link, MaxPoints 1 and Knowledge mapping pending')
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
    await page.goto(`${appOrigin}/materials/${materialId}/presentation`)
    await editorReady(page)
    await page.getByRole('navigation', { name: 'Elementi slajda' }).getByRole('button', { name: 'Quick Check' }).click()
    if (size === 'mobile') {
      await page.locator('.presentation-canvas-element--quick_check').scrollIntoViewIfNeeded()
    } else {
      await page.evaluate(() => window.scrollTo(0, 0))
    }
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/presentation-quick-check-${size}-${width}x${height}.png`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'quick-check', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real Quick Check card, selected layer, property panel and no document overflow`)
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.5 real Quick Check flow plus desktop/mobile visual gate.')
} catch (error) {
  console.log(`FAIL: ${error instanceof Error ? error.stack : String(error)}`)
  process.exitCode = 1
} finally {
  await browser.close()
}

async function login(page) {
  await page.goto(`${appOrigin}/auth/login`)
  await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
  await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
  await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
  await page.waitForURL(`${appOrigin}/`)
}

async function editorReady(page) {
  await page.getByRole('heading', { level: 1, name: '4.3 Novi materijal – Prezentacija' }).waitFor()
  await page.locator('.presentation-editor__slides li').first().waitFor()
}

async function readWorkspace(page) {
  return page.evaluate(async ({ api, id }) => {
    const response = await fetch(`${api}/api/v1/materials/${id}/presentation`, { credentials: 'include' })
    return { status: response.status, body: await response.json() }
  }, { api: apiOrigin, id: materialId })
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
