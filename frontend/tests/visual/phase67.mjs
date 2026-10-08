import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_MATERIAL_EDIT_CANONICAL) {
  throw new Error('Provide local review environment variables and the canonical PNG path.')
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-6.7', import.meta.url))
const sample = join(tmpdir(), `plus5-phase67-${process.pid}.pdf`)
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_MATERIAL_EDIT_CANONICAL, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  realObjectStorage: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeVersionFlow: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  setupPage.on('pageerror', error => evidence.errors.push(`setup: ${error.message}`))
  await login(setupPage)
  const materialId = await importFixture(setupPage, sample)
  await setupPage.goto(`http://localhost:8081/materials/${materialId}/edit`)
  await setupPage.getByRole('heading', { level: 1, name: '4.5 Uredi materijal' }).waitFor().catch(async error => {
    console.error(`Edit workspace failed at ${setupPage.url()}: ${await setupPage.locator('body').innerText()}`)
    console.error(evidence.errors)
    throw error
  })
  await setupPage.getByLabel('Naziv *').fill('Phase 6.7 — uređena verzija materijala')
  await setupPage.getByLabel('Opis').fill('Stvarni metadata edit preko nove neobjavljene MaterialVersion skice.')
  const saveResponse = setupPage.waitForResponse(response => response.url().endsWith(`/api/v1/materials/${materialId}/draft`) && response.request().method() === 'PUT')
  await setupPage.getByRole('button', { name: /Spremi skicu/ }).click()
  if ((await saveResponse).status() !== 200) throw new Error('Save Draft did not return HTTP 200.')
  await setupPage.getByText(/v2 · Skica/).waitFor()
  const publishResponse = setupPage.waitForResponse(response => response.url().endsWith(`/api/v1/materials/${materialId}/draft/publish`) && response.request().method() === 'POST')
  await setupPage.getByRole('button', { name: /Objavi novu verziju/ }).click()
  if ((await publishResponse).status() !== 200) throw new Error('Publish did not return HTTP 200.')
  await setupPage.getByText(/v2 · Aktivna/).waitFor()
  await setupPage.getByRole('button', { name: /Povijest/ }).click()
  await setupPage.getByRole('button', { name: /v1/ }).click()
  await setupPage.getByRole('heading', { name: 'Verzija 1' }).waitFor()
  const restoreResponse = setupPage.waitForResponse(response => response.url().includes(`/api/v1/materials/${materialId}/versions/`) && response.url().endsWith('/restore') && response.request().method() === 'POST')
  await setupPage.getByRole('button', { name: /Vrati kao novu skicu/ }).click()
  if ((await restoreResponse).status() !== 200) throw new Error('Restore did not return HTTP 200.')
  await setupPage.getByText(/v3 · Skica/).waitFor()
  const workspace = await setupPage.evaluate(async id => {
    const response = await fetch(`/api/v1/materials/${id}/edit`)
    return { status: response.status, body: await response.json() }
  }, materialId)
  const statuses = workspace.body.history.map(item => `${item.versionNumber}:${item.status}`)
  if (workspace.status !== 200 || workspace.body.editableVersion.versionNumber !== 3
    || workspace.body.editableVersion.status !== 'draft'
    || !statuses.includes('2:active') || !statuses.includes('1:superseded')) {
    throw new Error(`Version flow is inconsistent: ${JSON.stringify(workspace)}`)
  }
  evidence.runtimeVersionFlow = {
    materialId,
    importStatus: 201,
    saveDraftStatus: 200,
    publishStatus: 200,
    restoreStatus: 200,
    history: statuses,
    editableVersion: '3:draft',
  }
  const missingCsrfStatus = await setupPage.evaluate(async ({ id, rowVersion, draftVersionId }) => {
    const response = await fetch(`/api/v1/materials/${id}/draft/publish`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ expectedRowVersion: rowVersion, draftVersionId }),
    })
    return response.status
  }, { id: materialId, rowVersion: workspace.body.rowVersion, draftVersionId: workspace.body.editableVersion.id })
  const anonymousContext = await browser.newContext()
  const anonymousStatus = (await anonymousContext.request.get(`http://localhost:8081/api/v1/materials/${materialId}/edit`)).status()
  await anonymousContext.close()
  if (missingCsrfStatus !== 400 || anonymousStatus !== 401) {
    throw new Error(`Security gate failed: anonymous ${anonymousStatus}, missing CSRF ${missingCsrfStatus}.`)
  }
  evidence.runtimeVersionFlow.anonymousEditStatus = anonymousStatus
  evidence.runtimeVersionFlow.missingCsrfPublishStatus = missingCsrfStatus
  evidence.checks.push('runtime: import → metadata draft v2 → publish v2 → immutable v1 restore as new draft v3')
  evidence.checks.push('security: anonymous edit 401 and authenticated publish without CSRF 400')
  await setupContext.close()

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
    await login(page)
    captureStarted = true
    await page.goto(`http://localhost:8081/materials/${materialId}/edit`)
    await page.getByRole('heading', { level: 1, name: '4.5 Uredi materijal' }).waitFor()
    await page.getByText(/v3 · Skica/).waitFor()
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({ path: `${output}/material-edit-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'edit', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real login/edit API, metadata workspace, Phase 7 boundary and no document overflow`)
    if (size === 'desktop') {
      await page.getByRole('button', { name: /Povijest/ }).click()
      await settle(page)
      await page.screenshot({ path: `${output}/material-version-history-desktop.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'history', size, capture: 'viewport', ...await measure(page) })
    }
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 6.7 real version lifecycle plus desktop/mobile visual gate.')
} finally {
  await browser.close()
  await unlink(sample).catch(() => {})
}

async function login(page) {
  await page.goto('http://localhost:8081/auth/login')
  await page.getByLabel('E-mail', { exact: true }).fill(process.env.PLUS5_REVIEW_EMAIL)
  await page.getByLabel('Lozinka', { exact: true }).fill(process.env.PLUS5_REVIEW_PASSWORD)
  await page.getByRole('button', { name: 'Prijavi se', exact: true }).click()
  await page.waitForURL('http://localhost:8081/')
}

async function importFixture(page, samplePath) {
  await page.goto('http://localhost:8081/materials/import')
  await page.locator('input[type="file"]').setInputFiles(samplePath)
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel(/Naziv/).fill(`Phase 6.7 izvor ${new Date().toISOString()}`)
  await page.getByLabel(/Vrsta materijala/).selectOption('PRESENTATION')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel('Cilj učenja').fill('Učenik primjenjuje ciljanu jezičnu strukturu u kontekstu.')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/v1/materials/import') && response.request().method() === 'POST')
  await page.getByRole('button', { name: /Potvrdi i uvezi/ }).click()
  const response = await responsePromise
  if (response.status() !== 201) throw new Error(`Fixture import returned HTTP ${response.status()}: ${await response.text()}`)
  return (await response.json()).materialId
}

async function settle(page) {
  await page.evaluate(async () => {
    await document.fonts.ready
    await Promise.all([...document.images].map(image => image.decode().catch(() => {})))
  })
}
async function measure(page) { return page.evaluate(() => ({ viewportWidth: innerWidth, viewportHeight: innerHeight, clientWidth: document.documentElement.clientWidth, scrollWidth: document.documentElement.scrollWidth, scrollHeight: document.documentElement.scrollHeight })) }
function assertNoOverflow(size, dimensions) { if (dimensions.scrollWidth > dimensions.clientWidth) throw new Error(`Document overflow at ${size}: ${dimensions.scrollWidth}/${dimensions.clientWidth}`) }
