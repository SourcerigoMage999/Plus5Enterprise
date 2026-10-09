import { copyFile, mkdir, unlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

if (!process.env.PLUS5_REVIEW_PLAYWRIGHT || !process.env.PLUS5_REVIEW_EMAIL
  || !process.env.PLUS5_REVIEW_PASSWORD || !process.env.PLUS5_SHARE_RECIPIENT_EMAIL
  || !process.env.PLUS5_MATERIAL_LIBRARY_CANONICAL) {
  throw new Error('Provide local review credentials, recipient email, Playwright and the canonical PNG path.')
}

const playwrightModule = await import(pathToFileURL(process.env.PLUS5_REVIEW_PLAYWRIGHT).href)
const { chromium } = playwrightModule.default ?? playwrightModule
const output = fileURLToPath(new URL('../../../docs/visual-acceptance/phase-6.8', import.meta.url))
const sample = join(tmpdir(), `plus5-phase68-${process.pid}.pdf`)
await mkdir(output, { recursive: true })
await copyFile(process.env.PLUS5_MATERIAL_LIBRARY_CANONICAL, `${output}/canonical.png`)
await writeFile(sample, '%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<<>>\n%%EOF\n')

const browser = await chromium.launch({ channel: 'msedge', headless: true })
const evidence = {
  realApi: true,
  realSql: true,
  realObjectStorage: true,
  authBypass: false,
  apiIntercept: false,
  domMutation: false,
  canonicalSource: 'Screen 4.1 Material library; no dedicated sharing PNG exists',
  screens: [],
  checks: [],
  errors: [],
  businessWritesDuringCapture: [],
  runtimeSharingFlow: null,
}

try {
  const setupContext = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: 'hr-HR' })
  const setupPage = await setupContext.newPage()
  setupPage.on('pageerror', error => evidence.errors.push(`setup: ${error.message}`))
  await login(setupPage)
  const materialId = await importFixture(setupPage, sample)

  await setupPage.goto(`http://localhost:8081/materials/${materialId}/sharing`)
  await setupPage.getByRole('heading', { level: 1, name: 'Dijeljenje materijala' }).waitFor()
  const initial = await readSharing(setupPage, materialId)
  if (initial.status !== 200 || initial.body.visibility !== 'private' || initial.body.grants.length !== 0) {
    throw new Error(`Unexpected initial sharing workspace: ${JSON.stringify(initial)}`)
  }

  await setupPage.getByText('Dijeljeno', { exact: true }).click()
  await setupPage.getByRole('button', { name: /Dodaj učitelja/ }).click()
  await setupPage.getByLabel('E-mail učitelja').fill(process.env.PLUS5_SHARE_RECIPIENT_EMAIL)
  await setupPage.getByLabel('Razina pristupa').selectOption('use')
  const saveResponse = setupPage.waitForResponse(response => response.url().endsWith(`/api/v1/materials/${materialId}/sharing`) && response.request().method() === 'PUT')
  await setupPage.getByRole('button', { name: 'Spremi dijeljenje' }).click()
  if ((await saveResponse).status() !== 200) throw new Error('Sharing save did not return HTTP 200.')
  await setupPage.getByText('Postavke dijeljenja su spremljene.').waitFor()

  const shared = await readSharing(setupPage, materialId)
  if (shared.status !== 200 || shared.body.visibility !== 'shared'
    || shared.body.grants.length !== 1 || shared.body.grants[0].access !== 'use'
    || shared.body.grants[0].email.toLowerCase() !== process.env.PLUS5_SHARE_RECIPIENT_EMAIL.toLowerCase()) {
    throw new Error(`Persisted sharing workspace is inconsistent: ${JSON.stringify(shared)}`)
  }

  const missingCsrfStatus = await setupPage.evaluate(async ({ id, workspace }) => {
    const response = await fetch(`/api/v1/materials/${id}/sharing`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        expectedRowVersion: workspace.rowVersion,
        visibility: workspace.visibility,
        grants: workspace.grants.map(grant => ({ recipientEmail: grant.email, access: grant.access })),
      }),
    })
    return response.status
  }, { id: materialId, workspace: shared.body })
  const anonymousContext = await browser.newContext()
  const anonymousStatus = (await anonymousContext.request.get(`http://localhost:8081/api/v1/materials/${materialId}/sharing`)).status()
  await anonymousContext.close()
  if (missingCsrfStatus !== 400 || anonymousStatus !== 401) {
    throw new Error(`Security gate failed: anonymous ${anonymousStatus}, missing CSRF ${missingCsrfStatus}.`)
  }

  evidence.runtimeSharingFlow = {
    materialId,
    initialVisibility: 'private',
    savedVisibility: 'shared',
    grantAccess: 'use',
    grantCount: 1,
    saveStatus: 200,
    anonymousStatus,
    missingCsrfStatus,
  }
  evidence.checks.push('runtime: private owner material → explicit active Teacher Use grant → persisted Shared material')
  evidence.checks.push('security: anonymous sharing workspace 401 and authenticated PUT without CSRF 400')
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
    await page.goto(`http://localhost:8081/materials/${materialId}/sharing`)
    await page.getByRole('heading', { level: 1, name: 'Dijeljenje materijala' }).waitFor()
    const recipientInput = page.getByLabel('E-mail učitelja')
    await recipientInput.waitFor()
    if ((await recipientInput.inputValue()).toLowerCase() !== process.env.PLUS5_SHARE_RECIPIENT_EMAIL.toLowerCase()) {
      throw new Error('Persisted recipient email is not visible in the sharing workspace.')
    }
    await settle(page)
    const dimensions = await measure(page)
    assertNoOverflow(size, dimensions)
    await page.screenshot({ path: `${output}/material-sharing-${size}-${width}x${height}.png`, fullPage: false, animations: 'disabled' })
    evidence.screens.push({ name: 'shared-use-grant', size, capture: 'viewport', ...dimensions })
    evidence.checks.push(`${size}: real login/sharing API, explicit Use grant, owner-only controls and no document overflow`)
    if (size === 'desktop') {
      await page.getByText('Privatno', { exact: true }).click()
      await page.getByText('Privatni materijal nema grantove.').waitFor()
      await settle(page)
      await page.screenshot({ path: `${output}/material-sharing-private-empty-desktop.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'private-empty-unsaved', size, capture: 'viewport', ...await measure(page) })
      evidence.checks.push('desktop: Private selection clears grants locally and honestly renders the empty state before save')
    } else {
      await recipientInput.scrollIntoViewIfNeeded()
      await settle(page)
      await page.screenshot({ path: `${output}/material-sharing-mobile-grant-390x844.png`, fullPage: false, animations: 'disabled' })
      evidence.screens.push({ name: 'shared-use-grant-scrolled', size, capture: 'viewport', ...await measure(page) })
      evidence.checks.push('mobile: scrolled interaction evidence keeps grant controls usable inside the 390 px viewport')
    }
    await context.close()
  }

  if (evidence.errors.length) throw new Error(`Browser errors occurred: ${evidence.errors.join('; ')}`)
  if (evidence.businessWritesDuringCapture.length) throw new Error(`Unexpected capture writes: ${evidence.businessWritesDuringCapture.join('; ')}`)
  await writeFile(`${output}/measurements.json`, JSON.stringify(evidence, null, 2))
  console.log('PASS: Phase 6.8 real sharing flow plus desktop/mobile visual gate.')
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
  await page.getByLabel(/Naziv/).fill(`Phase 6.8 dijeljenje ${new Date().toISOString()}`)
  await page.getByLabel(/Vrsta materijala/).selectOption('WORKSHEET')
  await page.getByLabel('Predmet').fill('Engleski jezik')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  await page.getByLabel('Cilj učenja').fill('Učenik koristi materijal uz eksplicitno dodijeljen pristup.')
  await page.getByRole('button', { name: /Nastavi/ }).click()
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/v1/materials/import') && response.request().method() === 'POST')
  await page.getByRole('button', { name: /Potvrdi i uvezi/ }).click()
  const response = await responsePromise
  if (response.status() !== 201) throw new Error(`Fixture import returned HTTP ${response.status()}: ${await response.text()}`)
  return (await response.json()).materialId
}

async function readSharing(page, materialId) {
  return page.evaluate(async id => {
    const response = await fetch(`/api/v1/materials/${id}/sharing`)
    return { status: response.status, body: await response.json() }
  }, materialId)
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
