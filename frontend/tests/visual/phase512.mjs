import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_SPEAKING_CANONICAL) {
  throw new Error('Provide local review environment variables and the canonical PNG path.')
}

const evidenceStudentId = '56000000-0000-0000-0000-000000000010'
const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-5.12', import.meta.url))
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_SPEAKING_CANONICAL, `${output}/canonical.png`)

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

    const api = await page.evaluate(async studentId => {
      const response = await fetch(`/api/v1/students/${studentId}/knowledge`)
      return { status: response.status, body: await response.json() }
    }, evidenceStudentId)
    if (api.status !== 200) throw new Error('Real knowledge API did not return 200')
    const published = api.body.models.find(model => model.status === 'Published')
    const speaking = published?.areas.find(area => /speaking|govor/i.test(area.name))
    if (!published || !speaking || speaking.components.length === 0) {
      throw new Error('Published model does not contain a real Speaking area with components')
    }

    await page.goto(`http://localhost:8081/students/${evidenceStudentId}/knowledge?areaId=${speaking.knowledgeAreaId}`)
    await page.getByRole('heading', { level: 1, name: 'Detalj znanja učenika' }).waitFor()
    const publishedCard = page.locator('.knowledge-model').filter({ hasText: `${published.code} v${published.version}` })
    const speakingTab = publishedCard.getByRole('tab', { name: new RegExp(speaking.name) })
    if (await speakingTab.getAttribute('aria-selected') !== 'true') throw new Error('Speaking deep-link did not select the real model Area')

    const selectedRowButton = publishedCard.locator('.knowledge-table tbody button').first()
    await selectedRowButton.click()
    if (await selectedRowButton.getAttribute('aria-pressed') !== 'true') throw new Error('Speaking component selection is not exposed accessibly')
    const selectedName = (await selectedRowButton.innerText()).trim()
    if (await publishedCard.locator('.knowledge-component-detail h4').innerText() !== selectedName) {
      throw new Error('Selected Speaking component did not update the detail panel')
    }
    const detailText = await publishedCard.locator('.knowledge-component-detail').innerText()
    for (const label of ['Pouzdanost', 'Lanci dokaza', 'Efektivna težina', 'Izračunato', 'Algoritam', `${published.code} v${published.version}`]) {
      if (!detailText.includes(label)) throw new Error(`Speaking explainability is missing ${label}`)
    }

    await page.evaluate(async () => {
      await document.fonts.ready
      await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
    })
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)

    await publishedCard.evaluate(element => element.scrollIntoView({ block: 'start' }))
    await page.screenshot({ path: `${output}/speaking-detail-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({
      name: 'speaking-detail', size, capture: 'viewport', ...dimensions,
      area: speaking.name, selectedName, model: `${published.code} v${published.version}`,
    })
    evidence.checks.push(`${size}: real login/API, Speaking Area selected, component/detail synchronized, exact model version, explainability, no document overflow`)

    if (size === 'desktop') {
      await page.evaluate(() => window.scrollTo(0, 0))
      await page.screenshot({ path: `${output}/speaking-detail-overview-desktop.png`, fullPage: true, animations: 'disabled' })
      evidence.screens.push({ name: 'speaking-detail-overview', size, capture: 'full-page supplementary', ...dimensions })
    }

    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 5.12 real-login, real-API desktop/mobile Speaking visual gate; no business writes during capture.')
} finally {
  await browser.close()
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
