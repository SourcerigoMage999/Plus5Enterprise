import { copyFile, mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_MATERIAL_LIBRARY_CANONICAL) {
  throw new Error('Provide local review environment variables and the canonical PNG path.')
}

const { chromium } = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-6.3', import.meta.url))
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_MATERIAL_LIBRARY_CANONICAL, `${output}/canonical.png`)

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
      const [materials, overview] = await Promise.all([
        fetch('/api/v1/materials?page=1&pageSize=24&ownership=1&sort=1'),
        fetch('/api/v1/materials/overview?ownership=1'),
      ])
      return {
        materialStatus: materials.status,
        materialBody: await materials.json(),
        overviewStatus: overview.status,
        overviewBody: await overview.json(),
      }
    })
    if (api.materialStatus !== 200 || api.overviewStatus !== 200) {
      throw new Error(`Real material APIs did not return 200 (${api.materialStatus}/${api.overviewStatus}): ${JSON.stringify(api)}`)
    }
    if (!api.materialBody.items.some(item => item.title === 'Present Perfect – pravila')) throw new Error('Owned material fixture is missing')
    if (api.overviewBody.materialTypeCounts.length < 2) throw new Error('Material overview facets are missing')

    await page.goto('http://localhost:8081/materials')
    await page.getByRole('heading', { level: 1, name: '4.1 Biblioteka materijala' }).waitFor()
    await page.getByRole('heading', { level: 2, name: 'Present Perfect – pravila' }).waitFor()
    if (await page.getByRole('tab', { name: 'Moji materijali' }).getAttribute('aria-selected') !== 'true') {
      throw new Error('Owned materials tab is not selected')
    }
    if (await page.getByRole('button', { name: /Novi materijal/ }).isEnabled()) throw new Error('Future create workflow is enabled')
    if (await page.getByRole('button', { name: 'Akcije za Present Perfect – pravila' }).isEnabled()) throw new Error('Future item actions are enabled')

    await settle(page)
    const ownedDimensions = await measure(page)
    assertNoOverflow(size, ownedDimensions)
    await page.screenshot({ path: `${output}/material-library-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'owned-material-library', size, capture: 'viewport', ...ownedDimensions })

    await page.getByRole('tab', { name: 'Dijeljeni sa mnom' }).click()
    await page.getByRole('heading', { level: 2, name: 'Conversation starters' }).waitFor()
    if (await page.getByRole('tab', { name: 'Dijeljeni sa mnom' }).getAttribute('aria-selected') !== 'true') {
      throw new Error('Shared materials tab is not selected')
    }
    const sharedDimensions = await measure(page)
    assertNoOverflow(size, sharedDimensions)
    if (size === 'desktop') {
      await page.screenshot({ path: `${output}/material-library-shared-desktop.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'shared-material-library', size, capture: 'viewport', ...sharedDimensions })
    }

    await page.getByRole('tab', { name: 'Moji materijali' }).click()
    await page.getByRole('heading', { level: 2, name: 'Present Perfect – pravila' }).waitFor()
    await page.getByRole('button', { name: 'Lista' }).click()
    if (await page.getByRole('button', { name: 'Lista' }).getAttribute('aria-pressed') !== 'true') throw new Error('List view did not activate')
    await page.getByRole('button', { name: 'Mreža' }).click()

    if (size === 'desktop') {
      await page.getByRole('searchbox', { name: 'Pretraži materijale' }).fill('nema-ovakvog-materijala')
      await page.getByRole('button', { name: 'Pretraži', exact: true }).click()
      await page.getByRole('heading', { level: 2, name: 'Nema materijala za odabrane filtre' }).waitFor()
      const emptyDimensions = await measure(page)
      assertNoOverflow(size, emptyDimensions)
      await page.screenshot({ path: `${output}/material-library-no-results-desktop.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'no-results', size, capture: 'viewport', ...emptyDimensions })
    }

    evidence.checks.push(`${size}: real login/API, owned/shared scopes, grid/list controls, disabled future actions, no document overflow`)
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected business writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 6.3 real-login, real-API desktop/mobile material library visual gate; no business writes during capture.')
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
