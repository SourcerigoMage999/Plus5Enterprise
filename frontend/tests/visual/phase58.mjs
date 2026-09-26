import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD) {
  throw new Error('Provide local review environment variables.')
}

const evidenceStudentId = '56000000-0000-0000-0000-000000000010'
const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-5.8', import.meta.url))
await mkdir(output, { recursive: true })
const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = { realApi: true, authBypass: false, apiIntercept: false, screens: [], checks: [], errors: [], businessWritesDuringCapture: [] }

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
    if (api.status !== 200 || api.body.models.length < 2) throw new Error('Real knowledge API did not return separate model versions')
    const published = api.body.models.find(model => model.status === 'Published')
    if (!published || published.areas.length < 2) throw new Error('Published model does not contain selectable Areas')

    await page.goto(`http://localhost:8081/students/${evidenceStudentId}/knowledge`)
    await page.getByRole('heading', { level: 1, name: 'Detalj znanja učenika' }).waitFor()
    const publishedCard = page.locator('.knowledge-model').filter({ hasText: `${published.code} v${published.version}` })
    const firstTab = publishedCard.getByRole('tab').first()
    await firstTab.click()
    const selectedRowButton = publishedCard.locator('.knowledge-table tbody button').first()
    await selectedRowButton.click()
    if (await selectedRowButton.getAttribute('aria-pressed') !== 'true') throw new Error('Component selection is not exposed accessibly')
    const selectedName = await selectedRowButton.innerText()
    const detailHeading = publishedCard.locator('.knowledge-component-detail h4')
    if (await detailHeading.innerText() !== selectedName) throw new Error('Selected component did not update the detail panel')
    if (!await publishedCard.getByText(`${published.code} v${published.version}`, { exact: true }).last().isVisible()) {
      throw new Error('Exact KnowledgeModel version is missing from component detail')
    }
    const detailText = await publishedCard.locator('.knowledge-component-detail').innerText()
    for (const label of ['Pouzdanost', 'Lanci dokaza', 'Efektivna težina', 'Izračunato', 'Algoritam']) {
      if (!detailText.includes(label)) throw new Error(`Component explainability is missing ${label}`)
    }

    await page.evaluate(async () => {
      await document.fonts.ready
      await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
      window.scrollTo(0, 0)
    })
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({ path: `${output}/grammar-detail-${size}-${width}x${height}.png`, fullPage: true, animations: 'disabled' })
    evidence.screens.push({ name: 'grammar-detail', size, ...dimensions, selectedName, model: `${published.code} v${published.version}` })
    evidence.checks.push(`${size}: real login/API, model-defined Area, selected component, detail panel, exact model version, explainability, no overflow`)

    if (size === 'desktop') {
      const noDataArea = published.areas.find(area => area.components.some(component => component.score === null))
      if (!noDataArea) throw new Error('Real API has no component-level no-data state')
      await publishedCard.getByRole('tab', { name: new RegExp(noDataArea.name) }).click()
      const noDataButton = publishedCard.locator('.knowledge-table tbody button').first()
      await noDataButton.click()
      await publishedCard.getByText('Nema dovoljno podataka za rezultat', { exact: true }).waitFor()
      await page.evaluate(() => window.scrollTo(0, 0))
      const noDataDimensions = await measure(page)
      assertNoOverflow('no-data-desktop', noDataDimensions)
      await page.screenshot({ path: `${output}/grammar-detail-no-data-desktop.png`, fullPage: true, animations: 'disabled' })
      evidence.screens.push({ name: 'grammar-detail-no-data', size, ...noDataDimensions, area: noDataArea.name })
      evidence.checks.push('desktop component no-data: real API null score, no synthetic percentage, detail panel and no overflow')
    }

    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 5.8 real-login, real-API desktop/mobile/component-no-data visual gate; no business writes during capture.')
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
