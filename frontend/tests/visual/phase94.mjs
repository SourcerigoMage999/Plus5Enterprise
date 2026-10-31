import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_LESSON_GOAL_CANONICAL',
]
if (required.some(name => !process.env[name])) {
  throw new Error(`Provide local review values: ${required.join(', ')}.`)
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-9.4', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://localhost:5173'
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_LESSON_GOAL_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '5.1 Početak pripreme - cilj sata.png',
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
      const path = new URL(response.url()).pathname
      if (path.startsWith('/api/v1/lesson-plans/')) evidence.apiResponses.push({ size, path, status: response.status() })
    })

    await login(page)
    observeErrors(page, size)
    captureStarted = true
    await page.goto(`${appOrigin}/lesson-plans`)
    await page.getByRole('heading', { level: 1, name: '5.1 Početak pripreme sata — Cilj sata' }).waitFor()
    const participant = page.locator('#lesson-participant')
    await page.waitForFunction(() => document.querySelectorAll('#lesson-participant option').length > 1)
    await participant.selectOption({ index: 1 })
    await page.getByLabel('Unesite vlastiti cilj sata').fill('Učenik će primijeniti odabrane komponente znanja u vođenom zadatku.')
    const firstComponent = page.locator('.lesson-goal-component input[type="checkbox"]').first()
    await firstComponent.check()
    await page.getByRole('complementary', { name: 'Sažetak pripreme' }).getByText('Odabrano znanje').waitFor()
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/lesson-goal-${size}-${width}x${height}.png`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'lesson-goal', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: actual participant, goal and Knowledge Component selection; no document overflow`)
    await context.close()
  }

  const requiredPaths = ['/api/v1/lesson-plans/participants', '/api/v1/lesson-plans/knowledge-components']
  for (const size of ['desktop', 'mobile']) {
    for (const path of requiredPaths) {
      if (!evidence.apiResponses.some(result => result.size === size && result.path === path && result.status === 200)) {
        throw new Error(`${size} did not receive HTTP 200 from ${path}.`)
      }
    }
  }
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 9.4 real goal-selection desktop/mobile visual gate.')
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
