import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = ['PLUS5_REVIEW_PLAYWRIGHT', 'PLUS5_REVIEW_EMAIL', 'PLUS5_REVIEW_PASSWORD', 'PLUS5_LESSON_STRUCTURE_CANONICAL']
if (required.some(name => !process.env[name])) throw new Error(`Provide local review values: ${required.join(', ')}.`)

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-9.5', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://localhost:5173'
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_LESSON_STRUCTURE_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true, realSql: true, authBypass: false, apiIntercept: false, domMutation: false,
  canonicalSource: '5.2 Prijedlog strukture i sadržaj sata.png', policyCode: 'SYSTEM_BASELINE_V1',
  setupBusinessWrites: ['POST /api/v1/lesson-plans/'], businessWritesDuringCapture: [],
  screens: [], apiResponses: [], errors: [],
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  await login(setupPage)
  const lessonPlanId = await createDraft(setupPage)
  await setupContext.close()

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
      if (path.endsWith('/structure/')) evidence.apiResponses.push({ size, path, status: response.status() })
    })
    await login(page)
    page.on('pageerror', error => evidence.errors.push(`${size}: ${error.message}`))
    page.on('console', message => { if (message.type() === 'error') evidence.errors.push(`${size} console: ${message.text()}`) })
    captureStarted = true
    await page.goto(`${appOrigin}/lesson-plans/${lessonPlanId}/structure`)
    await page.getByRole('heading', { name: /Plan sata za/ }).waitFor()
    await page.getByText('SYSTEM_BASELINE_V1', { exact: false }).waitFor()
    await page.getByText('Relevantnost materijala').waitFor()
    await settle(page)
    const dimensions = await page.evaluate(() => ({ viewportWidth: innerWidth, viewportHeight: innerHeight,
      clientWidth: document.documentElement.clientWidth, scrollWidth: document.documentElement.scrollWidth,
      scrollHeight: document.documentElement.scrollHeight }))
    if (dimensions.scrollWidth > dimensions.clientWidth) throw new Error(`Document overflow at ${size}: ${dimensions.scrollWidth}/${dimensions.clientWidth}`)
    await page.screenshot({ path: `${output}/lesson-structure-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'lesson-structure-preview', size, capture: 'viewport', ...dimensions })
    await context.close()
  }

  for (const size of ['desktop', 'mobile']) {
    if (!evidence.apiResponses.some(result => result.size === size && result.status === 200))
      throw new Error(`${size} structure API did not return HTTP 200.`)
  }
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log(`PASS: Phase 9.5 real preview desktop/mobile visual gate for ${lessonPlanId}.`)
} catch (error) {
  console.log(`FAIL: ${error instanceof Error ? error.stack : String(error)}`)
  process.exitCode = 1
} finally { await browser.close() }

async function login(page) {
  await page.goto(`${appOrigin}/auth/login`)
  await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
  await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
  await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
  await page.waitForURL(`${appOrigin}/`)
}

async function createDraft(page) {
  return page.evaluate(async () => {
    const base = 'http://localhost:5270/api/v1'
    const request = async path => {
      const response = await fetch(`${base}${path}`, { credentials: 'include' })
      if (!response.ok) throw new Error(`${path} returned ${response.status}`)
      return response.json()
    }
    const participants = await request('/lesson-plans/participants?type=student&page=1&pageSize=1')
    const knowledge = await request('/lesson-plans/knowledge-components?page=1&pageSize=1')
    if (!participants.items?.length || !knowledge.items?.length) throw new Error('Review data needs one student and one published Knowledge Component.')
    const csrf = await request('/auth/csrf')
    const response = await fetch(`${base}/lesson-plans/`, { method: 'POST', credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token },
      body: JSON.stringify({ participantType: 'student', participantId: participants.items[0].id,
        sessionId: null, goal: 'Učenik će primijeniti odabrane komponente znanja u vođenom zadatku.',
        knowledgeComponentIds: [knowledge.items[0].id] }) })
    if (!response.ok) throw new Error(`Lesson create returned ${response.status}`)
    return (await response.json()).lessonPlanId
  })
}

async function settle(page) {
  await page.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(image => image.decode().catch(() => {}))) })
}
