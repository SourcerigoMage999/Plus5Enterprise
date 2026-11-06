import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

const required = ['PLUS5_REVIEW_PLAYWRIGHT', 'PLUS5_REVIEW_EMAIL', 'PLUS5_REVIEW_PASSWORD', 'PLUS5_ACTIVITY_SELECTION_CANONICAL']
if (required.some(name => !process.env[name])) throw new Error(`Provide local review values: ${required.join(', ')}.`)

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-9.7', import.meta.url))
const appOrigin = process.env.PLUS5_REVIEW_APP_ORIGIN ?? 'http://localhost:5173'
const apiOrigin = process.env.PLUS5_REVIEW_API_ORIGIN ?? 'http://localhost:5270/api/v1'
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_ACTIVITY_SELECTION_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true, realSql: true, authBypass: false, apiIntercept: false, domMutation: false,
  canonicalSource: '5.4 odabir aktivnosti  knowledge blocka.png',
  setupBusinessWrites: [
    'POST /api/v1/lesson-plans/',
    'POST /api/v1/lesson-plans/{id}/structure/apply',
    'POST /api/v1/lesson-plans/{id}/editor/activities',
  ],
  businessWritesDuringCapture: [], screens: [], apiResponses: [], errors: [],
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  await login(setupPage)
  const lessonPlanId = await preparePlan(setupPage)
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
      if (path.endsWith('/editor/activity-candidates')) evidence.apiResponses.push({ size, path, status: response.status() })
    })
    await login(page)
    page.on('pageerror', error => evidence.errors.push(`${size}: ${error.message}`))
    page.on('console', message => { if (message.type() === 'error') evidence.errors.push(`${size} console: ${message.text()}`) })
    captureStarted = true
    await page.goto(`${appOrigin}/lesson-plans/${lessonPlanId}/activity-selection`)
    await page.getByRole('heading', { name: 'Odabir aktivnosti / Knowledge Blocka' }).waitFor()
    await page.getByText('KB-005', { exact: true }).first().waitFor()
    await page.getByText('Već u planu: 2', { exact: true }).waitFor()
    if (size === 'mobile') await page.locator('.activity-selection-results').scrollIntoViewIfNeeded()
    await settle(page)
    const dimensions = await page.evaluate(() => ({
      viewportWidth: innerWidth, viewportHeight: innerHeight,
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
      scrollHeight: document.documentElement.scrollHeight,
    }))
    if (dimensions.scrollWidth > dimensions.clientWidth)
      throw new Error(`Document overflow at ${size}: ${dimensions.scrollWidth}/${dimensions.clientWidth}`)
    await page.screenshot({ path: `${output}/activity-selection-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'activity-selection', size, capture: 'viewport', ...dimensions })
    await context.close()
  }

  for (const size of ['desktop', 'mobile']) {
    if (!evidence.apiResponses.some(result => result.size === size && result.status === 200))
      throw new Error(`${size} candidate API did not return HTTP 200.`)
  }
  if (evidence.businessWritesDuringCapture.length)
    throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log(`PASS: Phase 9.7 real selection desktop/mobile visual gate for ${lessonPlanId}.`)
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

async function preparePlan(page) {
  return page.evaluate(async apiBase => {
    const request = async path => {
      const response = await fetch(`${apiBase}${path}`, { credentials: 'include' })
      if (!response.ok) throw new Error(`${path} returned ${response.status}`)
      return response.json()
    }
    const write = async (path, body) => {
      const csrf = await request('/auth/csrf')
      const response = await fetch(`${apiBase}${path}`, {
        method: 'POST', credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token },
        body: JSON.stringify(body),
      })
      if (!response.ok) throw new Error(`${path} returned ${response.status}`)
      return response.json()
    }
    const participants = await request('/lesson-plans/participants?type=student&page=1&pageSize=1')
    const knowledge = await request('/lesson-plans/knowledge-components?page=1&pageSize=1')
    if (!participants.items?.length || !knowledge.items?.length)
      throw new Error('Review fixture needs one student and one Published Knowledge Component.')
    const draft = await write('/lesson-plans/', {
      participantType: 'student', participantId: participants.items[0].id, sessionId: null,
      goal: 'Učenik će primijeniti odabrane komponente znanja u vođenom zadatku.',
      knowledgeComponentIds: [knowledge.items[0].id],
    })
    const structure = await request(`/lesson-plans/${draft.lessonPlanId}/structure/`)
    const applied = await write(`/lesson-plans/${draft.lessonPlanId}/structure/apply`, { rowVersion: structure.rowVersion })
    const candidates = await request(`/lesson-plans/${draft.lessonPlanId}/editor/activity-candidates`)
    const candidate = candidates.find(item => item.code === 'KB-005')
    if (!candidate) throw new Error('KB-005 is not available to the Phase 9.7 review fixture.')
    await write(`/lesson-plans/${draft.lessonPlanId}/editor/activities`, {
      knowledgeBlockVersionId: candidate.knowledgeBlockVersionId,
      rowVersion: applied.rowVersion,
    })
    return draft.lessonPlanId
  }, apiOrigin)
}

async function settle(page) {
  await page.evaluate(async () => {
    await document.fonts.ready
    await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
  })
}
