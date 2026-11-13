import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = [
  'PLUS5_REVIEW_PLAYWRIGHT',
  'PLUS5_REVIEW_EMAIL',
  'PLUS5_REVIEW_PASSWORD',
  'PLUS5_LESSON_CONFIRMATION_CANONICAL',
  'PLUS5_LESSON_PLAN_ID',
]
if (required.some(name => !process.env[name]))
  throw new Error(`Provide local review values: ${required.join(', ')}.`)

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-9.9', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://127.0.0.1:5173'
const lessonPlanId = process.env.PLUS5_LESSON_PLAN_ID

await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_LESSON_CONFIRMATION_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  phase: '9.9',
  capturedAt: '2026-11-12',
  realApi: true,
  realSql: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: '5.6 Potvrđena priprema.png',
  setupBusinessWrites: ['POST /api/v1/lesson-plans/{id}/confirmation/confirm'],
  businessWritesDuringCapture: [],
  screens: [],
  apiResponses: [],
  errors: [],
}

try {
  for (const [size, width, height] of [['desktop', 1536, 1024], ['mobile', 390, 844]]) {
    const context = await browser.newContext({ viewport: { width, height }, locale: 'hr-HR', deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.setDefaultTimeout(60_000)
    let captureStarted = false

    page.on('request', request => {
      if (captureStarted && request.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(request.method()))
        evidence.businessWritesDuringCapture.push(`${size}: ${request.method()} ${new URL(request.url()).pathname}`)
    })
    page.on('response', response => {
      const path = new URL(response.url()).pathname
      if (path.endsWith('/confirmation/')) evidence.apiResponses.push({ size, path, status: response.status() })
    })

    await login(page)
    page.on('pageerror', error => evidence.errors.push(`${size}: ${error.message}`))
    page.on('console', message => {
      if (message.type() === 'error') evidence.errors.push(`${size} console: ${message.text()}`)
    })

    captureStarted = true
    await page.goto(`${appOrigin}/lesson-plans/${lessonPlanId}/confirmation`)
    await page.getByRole('heading', { name: 'Sat je spreman', exact: true }).waitFor()
    await page.getByText('Spreman', { exact: true }).waitFor()
    const boardButton = page.getByRole('button', { name: /Pokreni sat na PLUS 5 Ploči/ })
    await boardButton.waitFor()
    if (await boardButton.isEnabled()) throw new Error('Phase 10 Board action must remain disabled.')
    await settle(page)

    const dimensions = await page.evaluate(() => {
      const profile = document.querySelector('.app-shell-global-profile')?.getBoundingClientRect() ?? null
      const actions = document.querySelector('.ds-page-actions')?.getBoundingClientRect() ?? null
      return {
        viewportWidth: innerWidth,
        viewportHeight: innerHeight,
        clientWidth: document.documentElement.clientWidth,
        scrollWidth: document.documentElement.scrollWidth,
        scrollHeight: document.documentElement.scrollHeight,
        actionsProfileOverlap: Boolean(profile && actions && actions.top < profile.bottom && actions.bottom > profile.top),
        errorStateVisible: Boolean(document.querySelector('[role="alert"]')),
      }
    })
    if (dimensions.scrollWidth > dimensions.clientWidth)
      throw new Error(`Document overflow at ${size}: ${dimensions.scrollWidth}/${dimensions.clientWidth}`)
    if (dimensions.actionsProfileOverlap)
      throw new Error(`Global profile overlaps page actions at ${size}.`)

    await page.screenshot({
      path: `${output}/confirmed-lesson-plan-${size}-${width}x${height}.jpg`,
      fullPage: false,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'confirmed-lesson-plan-ready', size, capture: 'viewport', ...dimensions })
    await context.close()
  }

  for (const size of ['desktop', 'mobile']) {
    if (!evidence.apiResponses.some(result => result.size === size && result.status === 200))
      throw new Error(`${size} confirmation API did not return HTTP 200.`)
  }
  if (evidence.businessWritesDuringCapture.length)
    throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  if (evidence.errors.length)
    throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)

  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log(`PASS: Phase 9.9 Ready desktop/mobile visual gate for ${lessonPlanId}.`)
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

async function settle(page) {
  await page.evaluate(async () => {
    await document.fonts.ready
    await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
  })
}
