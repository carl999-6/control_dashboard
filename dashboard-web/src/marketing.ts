import type { MarketingEventInput } from './api'

const stages = new Set(['visit', 'interest', 'contact', 'quote'])

export function buildUtmUrl(baseUrl: string, source: string, medium: string, campaign: string, content = '') {
  const normalized = /^https?:\/\//i.test(baseUrl.trim()) ? baseUrl.trim() : `https://${baseUrl.trim()}`
  const url = new URL(normalized)
  url.searchParams.set('utm_source', source.trim().toLowerCase())
  url.searchParams.set('utm_medium', medium.trim().toLowerCase())
  url.searchParams.set('utm_campaign', campaign.trim().toLowerCase())
  if (content.trim()) url.searchParams.set('utm_content', content.trim().toLowerCase())
  else url.searchParams.delete('utm_content')
  return url.toString()
}

function parseCsvLine(line: string) {
  const values: string[] = []
  let current = ''
  let quoted = false
  for (let index = 0; index < line.length; index += 1) {
    const char = line[index]
    if (char === '"' && quoted && line[index + 1] === '"') { current += '"'; index += 1 }
    else if (char === '"') quoted = !quoted
    else if (char === ',' && !quoted) { values.push(current.trim()); current = '' }
    else current += char
  }
  if (quoted) throw new Error('Hay una comilla sin cerrar en el CSV.')
  values.push(current.trim())
  return values
}

function normalizeRow(value: Record<string, unknown>, source: 'import_csv' | 'import_json'): MarketingEventInput {
  const stage = String(value.stage ?? '').trim().toLowerCase()
  const eventSource = String(value.source ?? '').trim().toLowerCase()
  const count = Number(value.count)
  if (!stages.has(stage)) throw new Error(`Etapa no válida: ${stage || '(vacía)'}.`)
  if (!eventSource) throw new Error('Cada fila necesita una fuente.')
  if (!Number.isInteger(count) || count < 1) throw new Error('Cada fila necesita un conteo entero mayor que cero.')
  const occurredAt = String(value.occurredAt ?? value.occurred_at ?? '').trim()
  if (occurredAt && Number.isNaN(Date.parse(occurredAt))) throw new Error(`Fecha no válida: ${occurredAt}.`)
  return {
    stage,
    source: eventSource,
    medium: String(value.medium ?? 'unknown').trim().toLowerCase(),
    landingPath: String(value.landingPath ?? value.landing_path ?? '').trim(),
    count,
    occurredAt: occurredAt || null,
    campaignId: String(value.campaignId ?? value.campaign_id ?? '').trim() || null,
    socialPostId: String(value.socialPostId ?? value.social_post_id ?? '').trim() || null,
    dataSource: source,
  }
}

export function parseMarketingImport(text: string, format: 'csv' | 'json') {
  if (!text.trim()) throw new Error('El archivo o texto está vacío.')
  if (format === 'json') {
    const parsed: unknown = JSON.parse(text)
    if (!Array.isArray(parsed)) throw new Error('El JSON debe contener una lista de eventos.')
    return parsed.map((row) => normalizeRow(row as Record<string, unknown>, 'import_json'))
  }

  const lines = text.replace(/^\uFEFF/, '').split(/\r?\n/).filter((line) => line.trim())
  if (lines.length < 2) throw new Error('El CSV debe incluir encabezados y al menos una fila.')
  const headers = parseCsvLine(lines[0])
  for (const required of ['stage', 'source', 'count']) {
    if (!headers.includes(required)) throw new Error(`Falta la columna obligatoria ${required}.`)
  }
  return lines.slice(1).map((line) => {
    const values = parseCsvLine(line)
    const row = Object.fromEntries(headers.map((header, index) => [header, values[index] ?? '']))
    return normalizeRow(row, 'import_csv')
  })
}
