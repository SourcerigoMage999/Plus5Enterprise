import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_MATERIAL_TASK_CANONICAL) {
  throw new Error('Provide local review environment variables and the canonical PNG path.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-6.5', import.meta.url))
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_MATERIAL_TASK_CANONICAL, `${output}/canonical.png`)

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
}

try {
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

    await page.goto('http://localhost:8081/auth/login')
    await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
    await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
    await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
    await page.waitForURL('http://localhost:8081/')
    captureStarted = true

    const api = await page.evaluate(async () => {
      const library = await fetch('/api/v1/materials?page=1&pageSize=24&ownership=1&sort=1')
      const body = await library.json()
      const selected = body.items.find(item => item.title === 'Present Perfect – procjenjivi zadaci')
      if (!selected) return { libraryStatus: library.status, selected: null }
      const detail = await fetch(`/api/v1/materials/${selected.id}`)
      return { libraryStatus: library.status, selected, detailStatus: detail.status, detailBody: await detail.json() }
    })
    if (api.libraryStatus !== 200 || api.detailStatus !== 200 || !api.selected) {
      throw new Error(`Real task-aware material API did not return the expected fixture: ${JSON.stringify(api)}`)
    }
    if (api.detailBody.tasks?.length !== 2
      || api.detailBody.tasks.some(task => task.knowledgeComponents.length !== 1)
      || api.detailBody.tasks[0].evidenceType !== 'recognition'
      || api.detailBody.tasks[1].evidenceType !== 'production') {
      throw new Error(`Assessable task payload is inconsistent: ${JSON.stringify(api.detailBody.tasks)}`)
    }

    await page.goto(`http://localhost:8081/materials/${api.selected.id}`)
    await page.getByRole('heading', { level: 1, name: '4.2 Pregled materijala' }).waitFor()
    await page.getByRole('heading', { level: 2, name: '◇ Zadaci i procjena' }).waitFor()
    await page.getByText('I ____ London twice.', { exact: true }).waitFor()
    await page.getByText('Napiši dvije rečenice o iskustvima koristeći Present Perfect.', { exact: true }).waitFor()
    await page.getByText('Sam materijal nije automatski dokaz znanja.', { exact: false }).waitFor()

    await page.getByRole('heading', { level: 2, name: '◇ Zadaci i procjena' }).scrollIntoViewIfNeeded()
    await page.evaluate(() => window.scrollBy(0, -16))
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({ path: `${output}/material-tasks-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'material-tasks', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real login/API, two versioned assessable tasks, evidence metadata, leaf mappings and no document overflow`)
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 6.5 real-login, real-API desktop/mobile assessable-task visual gate; no business writes during capture.')
} finally {
  await browser.close()
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
