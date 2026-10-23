import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_REVIEW_MATERIAL_ID',
  'PLUS5_PRESENTATION_AUTOSAVE_CANONICAL',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.7', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://127.0.0.1:8081'
const materialId = process.env.PLUS5_REVIEW_MATERIAL_ID
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_PRESENTATION_AUTOSAVE_CANONICAL, `${output}/canonical.png`)

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
  businessWritesDuringSetup: [],
  businessWritesDuringCapture: [],
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  setupPage.setDefaultTimeout(60_000)
  await login(setupPage)
  observeErrors(setupPage, 'setup')
  setupPage.on('request', request => {
    if (request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
      evidence.businessWritesDuringSetup.push(`${request.method()} ${new URL(request.url()).pathname}`)
    }
  })
  await openQuickCheck(setupPage)

  const prompt = setupPage.locator('.presentation-quick-check-fields textarea').first()
  await prompt.waitFor({ timeout: 5_000 }).catch(async () => {
    const formText = await setupPage.locator('.presentation-element-form').innerText()
    throw new Error(`Quick Check editor fields are missing. Property form: ${formText}`)
  })
  const current = await prompt.inputValue()
  const original = current.replace(/(?: \[autosave provjera\])+$/, '')
  if (current !== original) {
    const interruptedRestore = setupPage.waitForResponse(response => isElementPut(response))
    await prompt.fill(original)
    if ((await interruptedRestore).status() !== 200) throw new Error('Interrupted-run restore did not return HTTP 200.')
    await expectSaved(setupPage)
  }
  const updateResponse = setupPage.waitForResponse(response => isElementPut(response))
  await prompt.fill(`${original} [autosave provjera]`)
  if ((await updateResponse).status() !== 200) throw new Error('Autosave update did not return HTTP 200.')
  await expectSaved(setupPage)

  const restoreResponse = setupPage.waitForResponse(response => isElementPut(response))
  await prompt.fill(original)
  if ((await restoreResponse).status() !== 200) throw new Error('Autosave restore did not return HTTP 200.')
  await expectSaved(setupPage)
  evidence.checks.push('runtime: 800 ms autosave completed through the real typed element endpoint')
  evidence.checks.push('runtime: original Quick Check prompt restored after the reversible autosave proof')
  await setupContext.close()

  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.setDefaultTimeout(60_000)
    await login(page)
    observeErrors(page, size)
    await openQuickCheck(page)
    let captureStarted = true
    page.on('request', request => {
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${new URL(request.url()).pathname}`)
      }
    })
    await expectSaved(page)
    if (size === 'mobile') {
      await page.locator('.presentation-element-form__save-state').scrollIntoViewIfNeeded()
    } else {
      await page.evaluate(() => window.scrollTo(0, 0))
    }
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/presentation-autosave-${size}-${width}x${height}.png`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'autosave-saved', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: saved status, selected Quick Check properties and no document overflow`)
    captureStarted = false
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.7 real autosave flow plus desktop/mobile visual gate.')
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

async function openQuickCheck(page) {
  await page.goto(`${appOrigin}/materials/${materialId}/presentation`)
  await page.getByRole('heading', { level: 1, name: '4.3 Novi materijal – Prezentacija' }).waitFor()
  await page.locator('.presentation-editor__slides li').first().waitFor()
  await page.getByRole('navigation', { name: 'Elementi slajda' }).getByRole('button', { name: 'Quick Check' }).click()
  await page.locator('.presentation-element-form__save-state').waitFor()
}

async function expectSaved(page) {
  await page.getByText('Sve promjene spremljene', { exact: true }).waitFor()
  await page.locator('.presentation-element-form__save-state').getByText('Spremljeno', { exact: true }).waitFor()
}

function isElementPut(response) {
  return response.request().method() === 'PUT'
    && response.url().includes(`/api/v1/materials/${materialId}/presentation/slides/`)
    && response.url().includes('/quick-checks/')
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
