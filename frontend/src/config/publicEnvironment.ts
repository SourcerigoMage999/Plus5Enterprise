export interface PublicEnvironment {
  readonly apiBaseUrl: string
}

export interface PublicEnvironmentSource {
  readonly VITE_API_BASE_URL?: string
}

const defaultApiBaseUrl = '/api/v1'

export function readPublicEnvironment(
  source: PublicEnvironmentSource,
): PublicEnvironment {
  return {
    apiBaseUrl: normalizeApiBaseUrl(
      source.VITE_API_BASE_URL ?? defaultApiBaseUrl,
    ),
  }
}

function normalizeApiBaseUrl(value: string): string {
  const candidate = value.trim()

  if (candidate.startsWith('/') && !candidate.startsWith('//')) {
    return withoutTrailingSlash(candidate)
  }

  let url: URL

  try {
    url = new URL(candidate)
  } catch {
    throw new Error(
      'VITE_API_BASE_URL must be a root-relative path or an absolute HTTP(S) URL.',
    )
  }

  if (
    (url.protocol !== 'http:' && url.protocol !== 'https:') ||
    url.username !== '' ||
    url.password !== '' ||
    url.search !== '' ||
    url.hash !== ''
  ) {
    throw new Error(
      'VITE_API_BASE_URL must not contain credentials, a query, or a fragment and must use HTTP(S).',
    )
  }

  return withoutTrailingSlash(url.toString())
}

function withoutTrailingSlash(value: string): string {
  return value.length > 1 ? value.replace(/\/$/, '') : value
}
