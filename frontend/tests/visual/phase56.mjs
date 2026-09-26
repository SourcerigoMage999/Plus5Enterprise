import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD) {
  throw new Error('Provide local review environment variables.')
}

const evidenceStudentId = '56000000-0000-0000-0000-000000000010'
const noDataStudentId = '56000000-0000-0000-0000-000000000011'
const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-5.6', import.meta.url))
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
      const response = await fetch(`/api/v1/students/${studentId}/readiness`)
      return { status: response.status, body: await response.json() }
    }, evidenceStudentId)
    if (api.status !== 200 || api.body.areas.length < 2) throw new Error('Real readiness API did not return projection data')
    const versions = [...new Set(api.body.areas.map(area => `${area.knowledgeModelCode}/${area.knowledgeModelVersion}`))]
    if (versions.length < 2) throw new Error('Real API data does not contain two KnowledgeModel versions')

    await page.goto(`http://localhost:8081/students/${evidenceStudentId}/readiness`)
    await page.getByRole('heading', { level: 1, name: 'Ana Anić' }).waitFor()
    await page.getByRole('link', { name: 'Ana Anić' }).waitFor()
    const dossierHref = await page.getByRole('link', { name: /Natrag na dosje/ }).getAttribute('href')
    const breadcrumbHref = await page.getByRole('link', { name: 'Ana Anić' }).getAttribute('href')
    if (dossierHref !== `/students/${evidenceStudentId}` || breadcrumbHref !== dossierHref) {
      throw new Error('Breadcrumb and dossier return do not target the owned dossier')
    }
    const modelCount = await page.locator('.readiness-model').count()
    if (modelCount !== versions.length) throw new Error('KnowledgeModel versions are visually merged')

    await page.evaluate(async () => {
      await document.fonts.ready
      await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
      window.scrollTo(0, 0)
    })
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({
      path: `${output}/readiness-${size}-${width}x${height}.png`,
      fullPage: true,
      animations: 'disabled',
    })
    evidence.screens.push({ name: 'readiness', size, ...dimensions, versions, areaCount: api.body.areas.length })
    evidence.checks.push(`${size}: real login/API, breadcrumb, dossier return, separate KnowledgeModel versions, no overflow`)

    if (size === 'desktop') {
      await page.getByRole('link', { name: /Natrag na dosje/ }).click()
      await page.getByRole('heading', { level: 1, name: 'Ana Anić' }).waitFor()

      const noDataApi = await page.evaluate(async studentId => {
        const response = await fetch(`/api/v1/students/${studentId}/readiness`)
        return { status: response.status, body: await response.json() }
      }, noDataStudentId)
      if (noDataApi.status !== 200 || noDataApi.body.areas.length !== 0) throw new Error('Real no-data API state is not empty')
      await page.goto(`http://localhost:8081/students/${noDataStudentId}/readiness`)
      await page.getByRole('heading', { name: 'Nema dovoljno podataka za procjenu spremnosti' }).waitFor()
      const noDataDimensions = await measure(page)
      assertNoOverflow('no-data-desktop', noDataDimensions)
      await page.screenshot({
        path: `${output}/readiness-no-data-desktop.png`,
        fullPage: true,
        animations: 'disabled',
      })
      evidence.screens.push({ name: 'readiness-no-data', size, ...noDataDimensions, areaCount: 0 })
      evidence.checks.push('desktop no-data: real owned Student and API empty projection state, no synthetic score, no overflow')
    }

    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 5.6 real-login, real-API desktop/mobile/no-data visual gate; no business writes during capture.')
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
