import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_DASHBOARD_CANONICAL',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-8.2', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://127.0.0.1:5173'
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_DASHBOARD_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: 'Radni stol učitelja.png',
  screens: [],
  checks: [],
  apiResponses: [],
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
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${new URL(request.url()).pathname}`)
      }
    })
    page.on('response', response => {
      if (response.url().endsWith('/api/v1/dashboard')) {
        evidence.apiResponses.push({ size, status: response.status() })
      }
    })

    await login(page)
    observeErrors(page, size)
    captureStarted = true
    await page.getByRole('heading', { level: 1, name: /Dobro došli/ }).waitFor()
    await page.getByRole('region', { name: 'Današnji raspored' }).waitFor()
    await page.getByRole('region', { name: 'Učenici — pregled' }).waitFor()
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/teacher-dashboard-${size}-${width}x${height}.png`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'teacher-dashboard', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: actual Teacher dashboard and no document overflow`)
    await context.close()
  }

  if (evidence.apiResponses.length !== 2 || evidence.apiResponses.some(result => result.status !== 200)) {
    throw new Error(`Dashboard API did not return HTTP 200 twice: ${JSON.stringify(evidence.apiResponses)}`)
  }
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 8.2 real Teacher dashboard desktop/mobile visual gate.')
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
