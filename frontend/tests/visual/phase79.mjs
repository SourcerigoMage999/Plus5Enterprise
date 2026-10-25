import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_REVIEW_MATERIAL_ID',
  'PLUS5_PRESENTATION_AI_CANONICAL',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-7.9', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://127.0.0.1:5173'
const materialId = process.env.PLUS5_REVIEW_MATERIAL_ID
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_PRESENTATION_AI_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  paidProviderCall: false,
  canonicalSource: '4.3 P5.png',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
}

try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.setDefaultTimeout(60_000)
    let captureStarted = false
    page.on('request', request => {
      const url = new URL(request.url())
      if (url.hostname === 'api.openai.com' || url.hostname === 'eu.api.openai.com') {
        evidence.paidProviderCall = true
      }
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${url.pathname}`)
      }
    })

    await login(page)
    observeErrors(page, size)
    await page.goto(`${appOrigin}/materials/${materialId}/presentation`)
    await page.getByRole('heading', { level: 1, name: '4.3 Novi materijal – Prezentacija' }).waitFor()
    await page.getByRole('button', { name: /AI pomoć/ }).click()
    const disabled = page.getByText('AI pomoć je isključena', { exact: true })
    await disabled.waitFor()
    captureStarted = true
    await page.getByText(/Nema automatskih OpenAI poziva ni dodatnog troška/).waitFor()
    await page.getByText(/PLUS 5 nema naplatu unutar aplikacije/).waitFor()
    if (size === 'mobile') await disabled.scrollIntoViewIfNeeded()
    else await page.evaluate(() => window.scrollTo(0, 0))
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/presentation-ai-disabled-${size}-${width}x${height}.png`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'ai-disabled-no-cost', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: actual editor, explicit AI-disabled/no-cost state and no document overflow`)
    await context.close()
  }

  if (evidence.paidProviderCall) throw new Error('A browser request reached the paid OpenAI provider.')
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  evidence.checks.push('default-disabled runtime: zero generation requests and zero paid provider calls')
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 7.9 real default-disabled desktop/mobile visual gate.')
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
