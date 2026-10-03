import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_MATERIAL_DETAIL_CANONICAL) {
  throw new Error('Provide local review environment variables and the canonical PNG path.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-6.4', import.meta.url))
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_MATERIAL_DETAIL_CANONICAL, `${output}/canonical.png`)

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
      const selected = body.items.find(item => item.title === 'Present Perfect – pravila')
      if (!selected) return { libraryStatus: library.status, selected: null }
      const detail = await fetch(`/api/v1/materials/${selected.id}`)
      return { libraryStatus: library.status, selected, detailStatus: detail.status, detailBody: await detail.json() }
    })
    if (api.libraryStatus !== 200 || api.detailStatus !== 200 || !api.selected) {
      throw new Error(`Real material detail API did not return the expected fixture: ${JSON.stringify(api)}`)
    }
    if (api.detailBody.title !== 'Present Perfect – pravila' || api.detailBody.file.format !== 'pptx') {
      throw new Error(`Material detail payload is inconsistent: ${JSON.stringify(api.detailBody)}`)
    }

    await page.goto(`http://localhost:8081/materials/${api.selected.id}`)
    await page.getByRole('heading', { level: 1, name: '4.2 Pregled materijala' }).waitFor()
    await page.getByRole('heading', { level: 2, name: 'Present Perfect – pravila' }).waitFor()
    if (await page.getByRole('button', { name: 'Otvori' }).isEnabled()) throw new Error('Open action is enabled without a storage-read adapter')
    if (await page.getByRole('button', { name: 'Preuzmi' }).isEnabled()) throw new Error('Download action is enabled without a storage-read adapter')
    if (!await page.getByRole('button', { name: 'Kopiraj link' }).isEnabled()) throw new Error('Safe copy-link action is disabled')
    await page.getByText('Sam materijal nije automatski dokaz znanja.', { exact: false }).waitFor()

    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({ path: `${output}/material-detail-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'material-detail', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real login/API, owner detail, metadata, honest mapping/usage boundaries, disabled storage actions, no document overflow`)

    if (size === 'desktop') {
      await page.goto('http://localhost:8081/materials/00000000-0000-0000-0000-000000000001')
      await page.getByText('Materijal nije pronađen ili mu nemate pristup.').waitFor()
      const notFoundDimensions = await measure(page)
      assertNoOverflow('not-found', notFoundDimensions)
      await page.screenshot({ path: `${output}/material-detail-not-found-desktop.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'safe-not-found', size, capture: 'viewport', ...notFoundDimensions })
    }
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 6.4 real-login, real-API desktop/mobile material detail visual gate; no business writes during capture.')
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
