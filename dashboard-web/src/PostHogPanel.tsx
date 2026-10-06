import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { BarChart3, Check, ExternalLink, RefreshCw, Save } from 'lucide-react'
import { api, type Campaign, type PostHogEventDetail, type PostHogSettingsInput, type PostHogStatus, type PostHogSummary, type XReplyProposal } from './api'

const initialSettings: PostHogSettingsInput = {
  region: 'us', externalProjectId: 0, publicToken: '',
  apiKeyEnvironmentVariable: 'POSTHOG_PERSONAL_API_KEY',
  lookbackDays: 7, rowLimit: 5000, isEnabled: true,
}

const eventName: Record<string, string> = {
  '$pageview': 'Página vista', fyr_quote_request: 'Solicitud de cotización', fyr_whatsapp_click: 'Clic en WhatsApp',
}
const socialName: Record<string, string> = { x: 'X', twitter: 'X', ig: 'Instagram', instagram: 'Instagram', facebook: 'Facebook', linkedin: 'LinkedIn', direct: 'Directo', unattributed: 'Sin atribución' }
const readable = (value: string) => value || 'No disponible'

export function PostHogPanel({ projectId, campaigns, proposals }: { projectId: string; campaigns: Campaign[]; proposals: XReplyProposal[] }) {
  const [status, setStatus] = useState<PostHogStatus | null>(null)
  const [form, setForm] = useState<PostHogSettingsInput>(initialSettings)
  const [summary, setSummary] = useState<PostHogSummary | null>(null)
  const [days, setDays] = useState(28)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [eventSelection, setEventSelection] = useState<{ label: string; source?: string; campaign?: string; content?: string } | null>(null)
  const [events, setEvents] = useState<PostHogEventDetail[]>([])
  const [eventsBusy, setEventsBusy] = useState(false)
  const [eventsError, setEventsError] = useState('')
  const [expandedEvent, setExpandedEvent] = useState<number | null>(null)
  const eventRequest = useRef(0)

  const load = useCallback(async () => {
    const [nextStatus, nextSummary] = await Promise.all([api.getPostHogStatus(projectId), api.getPostHogSummary(projectId, days)])
    setStatus(nextStatus)
    setForm({ region: nextStatus.region, externalProjectId: nextStatus.externalProjectId,
      publicToken: nextStatus.publicToken, apiKeyEnvironmentVariable: nextStatus.apiKeyEnvironmentVariable,
      lookbackDays: nextStatus.lookbackDays, rowLimit: nextStatus.rowLimit, isEnabled: nextStatus.isEnabled })
    setSummary(nextSummary)
  }, [projectId, days])

  useEffect(() => { void load().catch(reason => setError(reason instanceof Error ? reason.message : 'No se pudo cargar PostHog.')) }, [load])
  useEffect(() => {
    eventRequest.current += 1
    setEventSelection(null); setEvents([]); setExpandedEvent(null); setEventsError('')
  }, [projectId, days])

  async function showEvents(selection: { label: string; source?: string; campaign?: string; content?: string }) {
    const requestId = ++eventRequest.current
    setEventSelection(selection); setEvents([]); setExpandedEvent(null); setEventsError(''); setEventsBusy(true)
    try {
      const result = await api.getPostHogEvents(projectId, days, selection)
      if (requestId === eventRequest.current) setEvents(result)
    } catch (reason) {
      if (requestId === eventRequest.current) setEventsError(reason instanceof Error ? reason.message : 'No se pudieron consultar los eventos.')
    } finally { if (requestId === eventRequest.current) setEventsBusy(false) }
  }

  async function save(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('')
    try {
      const replacing = status?.isConfigured && (status.region !== form.region || status.externalProjectId !== form.externalProjectId)
      if (replacing && !window.confirm('Cambiar región o proyecto eliminará los agregados importados del dashboard. Los datos originales de PostHog no se borran. ¿Continuar?')) return
      await api.updatePostHogSettings(projectId, { ...form, confirmReplaceData: Boolean(replacing) })
      await load()
      setNotice('Configuración guardada. La clave de consulta se toma únicamente de .env.')
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo guardar PostHog.') }
    finally { setBusy(false) }
  }

  async function sync() {
    setBusy(true); setError(''); setNotice('')
    try {
      const result = await api.runAutomation(projectId, 'posthog_sync')
      if (result.status !== 'succeeded') throw new Error(result.errorMessage || `La sincronización terminó con estado ${result.status}.`)
      await load()
      setNotice('PostHog sincronizado. Las cifras se reemplazaron para el rango importado, sin duplicados.')
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo sincronizar PostHog.') }
    finally { setBusy(false) }
  }

  const proposalById = useMemo(() => new Map(proposals.map(item => [item.id, item])), [proposals])
  const campaignById = useMemo(() => new Map(campaigns.map(item => [item.id, item])), [campaigns])
  const tracked = summary?.attribution ?? []

  return <section className="posthog-layout">
    <article className="panel posthog-panel">
      <header className="x-phase-heading"><span><BarChart3 size={20} /></span><div><small>FASE 7F · ANALÍTICA REAL</small><h2>PostHog Web Analytics</h2><p>Visitas y conversiones desde FyrStudios, agrupadas por fecha y parámetros UTM.</p></div><b className={`integration-state ${status?.isConfigured && status.keyAvailable && status.isEnabled ? 'integration-state--connected' : ''}`}>{status?.isConfigured && status.keyAvailable && status.isEnabled ? 'Configurado' : 'Pendiente'}</b></header>
      <p className="marketing-disclaimer">La clave personal debe tener solo permiso <code>query:read</code>. El token público <code>phc_...</code> identifica el sitio y no es una contraseña. Se guardan conteos agregados, sin nombres, correos, IP ni identificadores de sesión.</p>
      <form className="project-form" onSubmit={save}>
        <div className="form-row"><label className="form-field"><span>Región de PostHog Cloud</span><select value={form.region} onChange={event => setForm({ ...form, region: event.target.value as 'us' | 'eu' })}><option value="us">Estados Unidos</option><option value="eu">Unión Europea</option></select></label><label className="form-field"><span>ID numérico del proyecto</span><input type="number" required min="1" value={form.externalProjectId || ''} onChange={event => setForm({ ...form, externalProjectId: Number(event.target.value) })} placeholder="12345" /></label></div>
        <div className="form-row"><label className="form-field"><span>Token público del proyecto</span><input required value={form.publicToken} onChange={event => setForm({ ...form, publicToken: event.target.value })} placeholder="phc_..." /></label><label className="form-field"><span>Nombre de variable en .env</span><input required value={form.apiKeyEnvironmentVariable} onChange={event => setForm({ ...form, apiKeyEnvironmentVariable: event.target.value.toUpperCase() })} /></label></div>
        <div className="form-row"><label className="form-field"><span>Días a recuperar en cada sincronización</span><input type="number" min="1" max="31" value={form.lookbackDays} onChange={event => setForm({ ...form, lookbackDays: Number(event.target.value) })} /></label><label className="form-field"><span>Máximo de filas</span><input type="number" min="100" max="5000" value={form.rowLimit} onChange={event => setForm({ ...form, rowLimit: Number(event.target.value) })} /></label></div>
        <label className="pause-control"><input type="checkbox" checked={form.isEnabled} onChange={event => setForm({ ...form, isEnabled: event.target.checked })} /><span><b>{form.isEnabled ? 'Captura importable activada' : 'Integración pausada'}</b><small>La frecuencia automática se ajusta en Automatizaciones; puedes sincronizar manualmente aquí.</small></span></label>
        <div className="x-connection-state"><span className={status?.keyAvailable ? 'ok' : ''}>{status?.keyAvailable ? '✓ Clave personal detectada en .env' : '— Falta la variable de clave personal en .env'}</span>{status?.lastSyncAt && <span>Última sincronización: {new Date(status.lastSyncAt).toLocaleString('es-GT')}</span>}</div>
        {summary && summary.pageviews > 0 && summary.sessions === 0 && <p className="form-error">Hay páginas vistas, pero la sincronización anterior no importó las sesiones. Reinicia la API actualizada y pulsa «Sincronizar ahora».</p>}
        {status?.lastError && <p className="form-error">Último error: {status.lastError}</p>}
        {error && <p className="form-error" role="alert">{error}</p>}{notice && <p className="form-success"><Check size={14} /> {notice}</p>}
        <div className="form-actions"><button className="secondary-button" disabled={busy}><Save size={14} /> Guardar configuración</button><button className="primary-button" type="button" disabled={busy || !status?.isEnabled || !status.keyAvailable} onClick={() => void sync()}><RefreshCw size={14} /> {busy ? 'Procesando…' : 'Sincronizar ahora'}</button></div>
      </form>
    </article>
    <section className="marketing-kpis">
      <article><span>Páginas vistas</span><strong>{summary?.pageviews.toLocaleString('es-GT') ?? '0'}</strong><small>Evento $pageview</small></article>
      <article><span>Sesiones diarias</span><strong>{summary?.sessions.toLocaleString('es-GT') ?? '0'}</strong><small>Sesiones únicas dentro de cada día</small></article>
      <article><span>Solicitudes de cotización</span><strong>{summary?.quoteRequests.toLocaleString('es-GT') ?? '0'}</strong><small>Formulario enviado correctamente</small></article>
      <article><span>Clics en WhatsApp</span><strong>{summary?.whatsAppClicks.toLocaleString('es-GT') ?? '0'}</strong><small>Interés, no cotización confirmada</small></article>
    </section>
    <article className="panel posthog-panel"><div className="section-toolbar"><div><h2>Resultados de enlaces UTM</h2><p>Una visita es una sesión con ese UTM; las páginas vistas pueden ser varias dentro de la misma visita.</p></div><label className="form-field"><span>Periodo</span><select value={days} onChange={event => setDays(Number(event.target.value))}><option value={7}>7 días</option><option value={28}>28 días</option><option value={90}>90 días</option></select></label></div>
      <div className="posthog-results">{tracked.slice(0, 40).map((item, index) => {
        const proposal = item.xReplyProposalId ? proposalById.get(item.xReplyProposalId) : null
        const campaign = item.campaignId ? campaignById.get(item.campaignId) : null
        return <div key={`${item.content}-${item.campaign}-${index}`}><div><strong>{proposal ? 'Respuesta de X' : ((campaign?.name ?? item.campaign) || item.source)}</strong><small>{socialName[item.source] ?? item.source}{item.campaign && ` · campaña ${item.campaign}`}{item.content && ` · contenido ${item.content}`}</small>{proposal?.publishedReplyUrl && <a href={proposal.publishedReplyUrl} target="_blank" rel="noreferrer">Ver respuesta <ExternalLink size={12} /></a>}</div><div><strong>{item.visits.toLocaleString('es-GT')} visitas</strong><small>{item.pageviews} páginas vistas · {item.quoteRequests} cotizaciones · {item.whatsAppClicks} clics WhatsApp</small><button className="posthog-detail-button" type="button" onClick={() => void showEvents({ label: item.campaign || item.source, source: item.source === 'unattributed' ? undefined : item.source, campaign: item.campaign, content: item.content })}>Ver eventos</button></div></div>
      })}{tracked.length === 0 && <p className="empty-copy">Aún no hay visitas con UTM sincronizadas. Instala el rastreador en WordPress, abre un enlace UTM de prueba y sincroniza.</p>}</div>
      <div className="posthog-event-toolbar"><div><h3>Detalle de eventos</h3><p>Consulta puntual a PostHog: navegador, ubicación aproximada, dispositivo y origen. No se guardan eventos individuales en el dashboard.</p></div><button className="secondary-button" type="button" onClick={() => void showEvents({ label: 'Todos los eventos recientes' })}>Ver eventos recientes</button></div>
      {eventSelection && <div className="posthog-event-list"><h4>{eventSelection.label} · últimos 30 eventos del periodo</h4>
        {eventsBusy && <p className="empty-copy">Consultando eventos…</p>}{eventsError && <p className="form-error" role="alert">{eventsError}</p>}
        {!eventsBusy && !eventsError && events.length === 0 && <p className="empty-copy">No hay eventos para este filtro y periodo.</p>}
        {events.map((item, index) => <article className="posthog-event" key={`${item.occurredAt}-${item.event}-${index}`}>
          <button type="button" className="posthog-event-heading" aria-expanded={expandedEvent === index} onClick={() => setExpandedEvent(expandedEvent === index ? null : index)}><span><strong>{eventName[item.event] ?? item.event}</strong><small>{new Date(item.occurredAt).toLocaleString('es-GT')} · {item.path}</small></span><span>{socialName[item.source] ?? item.source} · {expandedEvent === index ? 'Ocultar' : 'Ver detalle'}</span></button>
          {expandedEvent === index && <dl className="posthog-event-fields">
            <div><dt>Origen</dt><dd>{socialName[item.source] ?? item.source} ({item.attribution === 'utm' ? 'UTM' : item.attribution === 'referrer' ? 'referencia' : item.attribution === 'direct' ? 'directo' : 'sin atribución'})</dd></div>
            <div><dt>Campaña / contenido</dt><dd>{item.campaign || item.content ? `${item.campaign || '—'} / ${item.content || '—'}` : 'No disponible'}</dd></div>
            <div><dt>Dominio de referencia</dt><dd>{item.referrerDomain === '$direct' ? 'Directo' : readable(item.referrerDomain)}</dd></div>
            <div><dt>Navegador</dt><dd>{readable(item.browser)}</dd></div>
            <div><dt>Ciudad</dt><dd>{readable(item.city)}</dd></div>
            <div><dt>País</dt><dd>{readable(item.country)}</dd></div>
            <div><dt>Dispositivo</dt><dd>{readable(item.deviceType)}</dd></div>
            <div><dt>Sistema operativo</dt><dd>{readable(item.os)}{item.osVersion && ` ${item.osVersion}`}</dd></div>
          </dl>}
        </article>)}
      </div>}
    </article>
  </section>
}
