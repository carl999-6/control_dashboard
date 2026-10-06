import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { BellRing, Bot, CheckCircle2, CircleAlert, Clock3, Database, ExternalLink, Globe2, KeyRound, Layers3, Play, RadioTower, RefreshCw, Save, Send, Settings2, ShieldCheck, Sparkles, TriangleAlert, Unplug, Workflow } from 'lucide-react'
import { api, type AutomationRun, type AutomationSchedule, type AutomationScheduleInput, type GeminiSettings, type GeminiSettingsInput, type NotificationPolicy, type NotificationPolicyInput, type NotificationRecord, type NotificationSummary, type Project, type SearchConsoleStatus, type SearchConsoleSummary, type WordPressSettings, type WordPressSettingsInput } from './api'

const statusLabel: Record<string, string> = { sent: 'Entregada', failed: 'Fallida', queued: 'En cola', grouped: 'Agrupada', suppressed: 'Omitida por política' }
const severityLabel: Record<string, string> = { info: 'Informativa', warning: 'Advertencia', critical: 'Crítica' }
const frequencyLabel: Record<string, string> = { manual: 'Solo manual', interval: 'Por intervalo', daily: 'Diaria', weekly: 'Semanal' }
const dayLabel = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado']

export function NotificationsPage({ project }: { project: Project | null }) {
  const [policy, setPolicy] = useState<NotificationPolicy | null>(null)
  const [items, setItems] = useState<NotificationRecord[]>([])
  const [summary, setSummary] = useState<NotificationSummary | null>(null)
  const [schedules, setSchedules] = useState<AutomationSchedule[]>([])
  const [runs, setRuns] = useState<AutomationRun[]>([])
  const [searchConsole, setSearchConsole] = useState<SearchConsoleStatus | null>(null)
  const [searchSummary, setSearchSummary] = useState<SearchConsoleSummary | null>(null)
  const [gemini, setGemini] = useState<GeminiSettings | null>(null)
  const [wordPress, setWordPress] = useState<WordPressSettings | null>(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    if (!project) return
    setLoading(true); setError('')
    try {
      const [loadedPolicy, loadedItems, loadedSummary, loadedSchedules, loadedRuns, loadedSearchConsole, loadedSearchSummary, loadedGemini, loadedWordPress] = await Promise.all([
        api.getNotificationPolicy(project.id), api.getNotifications(project.id), api.getNotificationSummary(project.id),
        api.getAutomationSchedules(project.id), api.getAutomationRuns(project.id), api.getSearchConsoleStatus(project.id),
        api.getSearchConsoleSummary(project.id), api.getGeminiSettings(project.id), api.getWordPressSettings(project.id),
      ])
      setPolicy(loadedPolicy); setItems(loadedItems); setSummary(loadedSummary); setSchedules(loadedSchedules); setRuns(loadedRuns)
      setSearchConsole(loadedSearchConsole); setSearchSummary(loadedSearchSummary)
      setGemini(loadedGemini)
      setWordPress(loadedWordPress)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo cargar el centro de automatizaciones.') }
    finally { setLoading(false) }
  }, [project])

  useEffect(() => { void load() }, [load])

  async function act(action: () => Promise<unknown>, message: string) {
    setError(''); setNotice('')
    try { await action(); setNotice(message); await load() }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo completar la acción.') }
  }

  async function connectSearchConsole() {
    if (!project) return
    setError(''); setNotice('')
    try {
      const result = await api.authorizeSearchConsole(project.id)
      window.location.assign(result.authorizationUrl)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo iniciar la conexión con Google.') }
  }

  if (!project) return <ProjectRequired />
  const telegramConnected = policy?.deliveryMode === 'telegram'
  return <div className="page notifications-page">
    <section className="page-heading">
      <div><div className="eyebrow"><span /> Fase 7 · automatización local</div><h1>Automatizaciones y alertas</h1><p>Programa flujos por proyecto, ejecútalos manualmente y conserva un historial verificable.</p></div>
      <div className="heading-actions"><span className="demo-pill"><Sparkles size={14} /> {telegramConnected ? 'Telegram conectado' : 'Telegram simulado'}</span><button className="primary-button" onClick={() => act(() => api.processQueuedNotifications(project.id), 'Cola de notificaciones procesada.')}><RefreshCw size={16} /> Procesar cola</button></div>
    </section>
    <section className="notification-safety"><ShieldCheck size={19} /><div><strong>{telegramConnected ? 'Credenciales cargadas desde el entorno local' : 'Modo seguro sin credenciales activas'}</strong><span>{telegramConnected ? 'El token y el chat ID no se muestran ni se guardan en la base de datos. Las pruebas enviarán un mensaje real.' : 'Configura TELEGRAM_BOT_TOKEN y TELEGRAM_CHAT_ID en .env para activar envíos reales; el historial sigue funcionando en modo simulado.'}</span></div></section>
    {error && <div className="global-error notification-message" role="alert">{error}</div>}
    {notice && <div className="success-banner notification-message" role="status">{notice}</div>}
    {loading && <div className="panel operations-empty">Cargando automatizaciones…</div>}
    {!loading && policy && summary && searchConsole && searchSummary && gemini && wordPress && <>
      <SearchConsolePanel status={searchConsole} summary={searchSummary} projectId={project.id} onConnect={connectSearchConsole} onAct={act} />
      <GeminiPanel settings={gemini} projectId={project.id} onAct={act} />
      <WordPressPanel settings={wordPress} projectId={project.id} onAct={act} />
      <AutomationSchedules schedules={schedules} runs={runs} projectId={project.id} onAct={act} />
      <NotificationMetrics summary={summary} />
      <div className="notification-layout"><PolicyPanel policy={policy} projectId={project.id} onAct={act} /><TestPanel projectId={project.id} real={telegramConnected} onAct={act} /></div>
      <NotificationHistory items={items} projectId={project.id} onAct={act} />
    </>}
  </div>
}

function WordPressPanel({ settings, projectId, onAct }: { settings: WordPressSettings; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [form, setForm] = useState<WordPressSettingsInput>({ isEnabled: settings.isEnabled, baseUrl: settings.baseUrl, username: settings.username })
  useEffect(() => setForm({ isEnabled: settings.isEnabled, baseUrl: settings.baseUrl, username: settings.username }), [settings])
  const connected = settings.status === 'connected'
  return <section className="panel integration-panel wordpress-panel">
    <div className="integration-heading"><span><Globe2 size={21} /></span><div><small>Fase 7D · destino real controlado</small><h2>WordPress</h2><p>Recibe borradores SEO validados. El backend fija siempre <code>status: draft</code>; publicar requiere entrar a WordPress.</p></div><b className={`integration-state ${connected ? 'integration-state--connected' : ''}`}>{settings.status === 'paused' ? 'Pausado' : connected ? 'Conectado' : settings.passwordConfigured ? 'Por comprobar' : 'Falta contraseña'}</b></div>
    {!settings.passwordConfigured && <div className="integration-guidance"><KeyRound size={18} /><div><strong>Falta WORDPRESS_APPLICATION_PASSWORD</strong><p>Guárdala únicamente en <code>.env</code> y reinicia la API. No se almacena ni se muestra en el dashboard.</p></div></div>}
    <div className="gemini-safety"><ShieldCheck size={17} /><div><strong>Publicación automática deshabilitada por diseño</strong><span>La integración no acepta otro estado que <code>draft</code> y evita crear dos entradas para la misma pieza local.</span></div><b>{settings.lastCheckedAt ? `Comprobado ${new Date(settings.lastCheckedAt).toLocaleDateString('es-GT')}` : 'Sin comprobar'}</b></div>
    {settings.lastError && <div className="integration-error"><TriangleAlert size={16} /> {settings.lastError}</div>}
    <form className="gemini-settings" onSubmit={event => { event.preventDefault(); void onAct(() => api.updateWordPressSettings(projectId, form), 'Configuración de WordPress actualizada.') }}>
      <label className="pause-control"><input type="checkbox" checked={form.isEnabled} onChange={event => setForm({ ...form, isEnabled: event.target.checked })} /><span><Globe2 size={18} /><b>{form.isEnabled ? 'Envío de borradores habilitado' : 'Envío pausado'}</b><small>Al pausar, los borradores locales se conservan y no se llama a WordPress.</small></span></label>
      <label>URL del sitio<input type="url" required value={form.baseUrl} onChange={event => setForm({ ...form, baseUrl: event.target.value })} /></label>
      <label>Usuario de aplicación<input required value={form.username} onChange={event => setForm({ ...form, username: event.target.value })} /></label>
      <div className="integration-actions"><button className="secondary-button" type="submit"><Save size={14} /> Guardar configuración</button><button className="primary-button" type="button" disabled={!settings.passwordConfigured || !form.isEnabled} onClick={() => void onAct(() => api.testWordPressConnection(projectId), 'WordPress confirmó las credenciales y el acceso de edición.')}><RefreshCw size={14} /> Probar conexión</button></div>
    </form>
  </section>
}

function GeminiPanel({ settings, projectId, onAct }: { settings: GeminiSettings; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [generating, setGenerating] = useState(false)
  const generationPending = useRef(false)

  async function generateDraft() {
    if (generationPending.current) return
    generationPending.current = true
    setGenerating(true)
    try {
      await onAct(() => runAutomationOrThrow(projectId, 'seo_content_pipeline'), 'Borrador SEO creado en WordPress para revisión manual.')
    } finally {
      generationPending.current = false
      setGenerating(false)
    }
  }
  const [form, setForm] = useState<GeminiSettingsInput>({
    isEnabled: settings.isEnabled, model: settings.model, minimumImpressions: settings.minimumImpressions,
    minimumPosition: settings.minimumPosition, maximumPosition: settings.maximumPosition,
    maximumCtrPercent: settings.maximumCtrPercent, maximumSeoDraftsPerDay: settings.maximumSeoDraftsPerDay,
    maximumXProposalsPerDay: settings.maximumXProposalsPerDay,
    maximumTotalGeminiRunsPerDay: settings.maximumTotalGeminiRunsPerDay,
    minimumDraftWords: settings.minimumDraftWords, maximumOutputTokens: settings.maximumOutputTokens, owner: settings.owner,
  })
  useEffect(() => setForm({
    isEnabled: settings.isEnabled, model: settings.model, minimumImpressions: settings.minimumImpressions,
    minimumPosition: settings.minimumPosition, maximumPosition: settings.maximumPosition,
    maximumCtrPercent: settings.maximumCtrPercent, maximumSeoDraftsPerDay: settings.maximumSeoDraftsPerDay,
    maximumXProposalsPerDay: settings.maximumXProposalsPerDay,
    maximumTotalGeminiRunsPerDay: settings.maximumTotalGeminiRunsPerDay,
    minimumDraftWords: settings.minimumDraftWords, maximumOutputTokens: settings.maximumOutputTokens, owner: settings.owner,
  }), [settings])

  return <section className="panel integration-panel gemini-panel">
    <div className="integration-heading"><span><Bot size={21} /></span><div><small>Fase 7C–7E · generación asistida y revisable</small><h2>Gemini para SEO y X</h2><p>Genera borradores SEO y propuestas de respuesta para X bajo una cuota compartida. Nunca publica automáticamente.</p></div><b className={`integration-state ${settings.apiKeyConfigured && settings.isEnabled ? 'integration-state--connected' : ''}`}>{!settings.apiKeyConfigured ? 'Falta API key' : settings.isEnabled ? 'Activo' : 'Pausado'}</b></div>
    {!settings.apiKeyConfigured && <div className="integration-guidance"><KeyRound size={18} /><div><strong>Falta GEMINI_API_KEY en el entorno local</strong><p>Crea una clave en un proyecto de Google AI Studio sin facturación, guárdala únicamente en <code>.env</code> y reinicia la API.</p></div></div>}
    <div className="gemini-safety"><ShieldCheck size={17} /><div><strong>Política gratuita y revisión humana</strong><span>Un error 429 detiene el flujo y avisa por Telegram; no existe proveedor pagado alternativo. SEO y X comparten el límite total.</span></div><b>{settings.totalGeminiRunsToday}/{settings.maximumTotalGeminiRunsPerDay} hoy</b></div>
    <form className="gemini-settings" onSubmit={event => { event.preventDefault(); void onAct(() => api.updateGeminiSettings(projectId, form), 'Configuración de Gemini actualizada.') }}>
      <label className="pause-control"><input type="checkbox" checked={form.isEnabled} onChange={event => setForm({ ...form, isEnabled: event.target.checked })} /><span><Sparkles size={18} /><b>{form.isEnabled ? 'Generación habilitada' : 'Generación pausada'}</b><small>Al pausar, no se consume cuota aunque la programación siga activa.</small></span></label>
      <label>Modelo gratuito<input value={form.model} onChange={event => setForm({ ...form, model: event.target.value })} /></label>
      <label>Responsable de revisión<input value={form.owner} onChange={event => setForm({ ...form, owner: event.target.value })} /></label>
      <label>Impresiones mínimas<input type="number" min="1" max="1000000" value={form.minimumImpressions} onChange={event => setForm({ ...form, minimumImpressions: Number(event.target.value) })} /></label>
      <label>CTR máximo (%)<input type="number" min="0" max="100" step="0.1" value={form.maximumCtrPercent} onChange={event => setForm({ ...form, maximumCtrPercent: Number(event.target.value) })} /></label>
      <label>Posición mínima<input type="number" min="1" max="100" step="0.1" value={form.minimumPosition} onChange={event => setForm({ ...form, minimumPosition: Number(event.target.value) })} /></label>
      <label>Posición máxima<input type="number" min="1" max="100" step="0.1" value={form.maximumPosition} onChange={event => setForm({ ...form, maximumPosition: Number(event.target.value) })} /></label>
      <label>Borradores SEO por día<input type="number" min="1" max="10" value={form.maximumSeoDraftsPerDay} onChange={event => setForm({ ...form, maximumSeoDraftsPerDay: Number(event.target.value) })} /></label>
      <label>Propuestas para X por día<input type="number" min="1" max="10" value={form.maximumXProposalsPerDay} onChange={event => setForm({ ...form, maximumXProposalsPerDay: Number(event.target.value) })} /></label>
      <label>Ejecuciones totales por día<input type="number" min="1" max="20" value={form.maximumTotalGeminiRunsPerDay} onChange={event => setForm({ ...form, maximumTotalGeminiRunsPerDay: Number(event.target.value) })} /></label>
      <label>Extensión mínima (palabras)<input type="number" min="300" max="2500" value={form.minimumDraftWords} onChange={event => setForm({ ...form, minimumDraftWords: Number(event.target.value) })} /></label>
      <label>Máximo de tokens de salida<input type="number" min="512" max="8192" value={form.maximumOutputTokens} onChange={event => setForm({ ...form, maximumOutputTokens: Number(event.target.value) })} /></label>
      <div className="integration-actions"><button className="secondary-button" type="submit" disabled={generating}><Save size={14} /> Guardar configuración</button><button className="primary-button" type="button" disabled={generating || !settings.apiKeyConfigured || !form.isEnabled} aria-busy={generating} onClick={() => void generateDraft()}>{generating ? <RefreshCw size={14} className="generation-spinner" /> : <Sparkles size={14} />}{generating ? 'Generando borrador…' : 'Generar siguiente borrador'}</button></div>
      {generating && <div className="generation-progress" role="status"><strong>El flujo SEO está en curso.</strong><span>Estamos esperando el resultado de la generación, validación y creación del borrador en WordPress. Puede tardar unos minutos. El resultado aparecerá al terminar.</span></div>}
    </form>
  </section>
}

function SearchConsolePanel({ status, summary, projectId, onConnect, onAct }: { status: SearchConsoleStatus; summary: SearchConsoleSummary; projectId: string; onConnect: () => Promise<void>; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [lookbackDays, setLookbackDays] = useState(status.lookbackDays)
  const [rowLimit, setRowLimit] = useState(status.rowLimit)
  const [confirmingDisconnect, setConfirmingDisconnect] = useState(false)
  const [disconnectText, setDisconnectText] = useState('')
  useEffect(() => { setLookbackDays(status.lookbackDays); setRowLimit(status.rowLimit) }, [status])

  const closeDisconnectConfirmation = () => {
    setConfirmingDisconnect(false)
    setDisconnectText('')
  }

  const disconnect = (event: FormEvent) => {
    event.preventDefault()
    if (disconnectText.trim() !== 'desconectar') return
    void onAct(() => api.disconnectSearchConsole(projectId, disconnectText), 'Search Console fue desconectado del proyecto.')
    closeDisconnectConfirmation()
  }

  return <section className="panel integration-panel">
    <div className="integration-heading">
      <span><Globe2 size={21} /></span>
      <div><small>Fase 7B · fuente real de solo lectura</small><h2>Google Search Console</h2><p>Importa consultas y páginas del proyecto. El dashboard nunca puede modificar ni publicar contenido en Google.</p></div>
      <b className={`integration-state ${status.connected ? 'integration-state--connected' : ''}`}>{status.connected ? 'Conectado' : status.clientConfigured ? 'Sin conectar' : 'Falta configurar OAuth'}</b>
    </div>
    {!status.clientConfigured && <div className="integration-guidance"><CircleAlert size={18} /><div><strong>Faltan las credenciales OAuth locales</strong><p>Agrega GOOGLE_SEARCH_CONSOLE_CLIENT_ID y GOOGLE_SEARCH_CONSOLE_CLIENT_SECRET en <code>.env</code>, reinicia la API y vuelve a esta pantalla.</p></div></div>}
    {status.clientConfigured && !status.connected && <div className="integration-connect"><div><strong>Autoriza la propiedad de este proyecto</strong><p>Google mostrará la pantalla de consentimiento. Solo se solicita el permiso <code>webmasters.readonly</code>.</p></div><button className="primary-button" onClick={() => void onConnect()}><ExternalLink size={15} /> Conectar con Google</button></div>}
    {status.connected && <>
      <div className="integration-resource"><Database size={17} /><div><small>Propiedad seleccionada automáticamente</small><strong>{status.property}</strong><span>Permiso en Google: {status.permissionLevel || 'lectura'} · última sincronización: {status.lastSyncAt ? new Date(status.lastSyncAt).toLocaleString('es-GT') : 'todavía no ejecutada'}</span></div></div>
      {status.lastError && <div className="integration-error"><TriangleAlert size={16} /> {status.lastError}</div>}
      <div className="integration-metrics">
        <article><small>Clics</small><strong>{formatNumber(summary.clicks)}</strong></article>
        <article><small>Impresiones</small><strong>{formatNumber(summary.impressions)}</strong></article>
        <article><small>CTR</small><strong>{formatPercent(summary.ctr)}</strong></article>
        <article><small>Posición media</small><strong>{summary.averagePosition ? summary.averagePosition.toFixed(1) : '—'}</strong></article>
      </div>
      <div className="integration-body">
        <form className="integration-settings" onSubmit={event => { event.preventDefault(); void onAct(() => api.updateSearchConsoleSettings(projectId, lookbackDays, rowLimit), 'Configuración de Search Console actualizada.') }}>
          <div><h3>Sincronización</h3><p>Google suele consolidar los datos con retraso; se consulta hasta dos días antes de la fecha actual.</p></div>
          <label>Días a importar<input type="number" min="7" max="90" value={lookbackDays} onChange={event => setLookbackDays(Number(event.target.value))} /></label>
          <label>Máximo de filas<input type="number" min="1" max="25000" value={rowLimit} onChange={event => setRowLimit(Number(event.target.value))} /></label>
          <div className="integration-actions"><button className="secondary-button" type="submit"><Save size={14} /> Guardar límites</button><button className="primary-button" type="button" onClick={() => void onAct(() => runAutomationOrThrow(projectId, 'search_console_sync'), 'Search Console se sincronizó correctamente.') }><RefreshCw size={14} /> Sincronizar ahora</button><button className="danger-button" type="button" onClick={() => setConfirmingDisconnect(true)}><Unplug size={14} /> Desconectar</button></div>
        </form>
        <div className="search-query-list"><div><h3>Consultas con más impresiones</h3><p>{summary.startDate && summary.endDate ? `${summary.startDate} al ${summary.endDate}` : 'Aparecerán después de la primera sincronización.'}</p></div>{summary.topQueries.length ? summary.topQueries.slice(0, 5).map(item => <article key={`${item.query}-${item.page}`}><div><strong>{item.query || '(consulta anonimizada por Google)'}</strong><span>{item.page}</span></div><b>{formatNumber(item.impressions)} imp.</b><small>{formatNumber(item.clicks)} clics</small></article>) : <div className="search-query-empty">Aún no hay métricas reales importadas.</div>}</div>
      </div>
    </>}
    {confirmingDisconnect && <div className="modal-backdrop"><section className="operations-modal disconnect-confirmation" role="dialog" aria-modal="true" aria-labelledby="disconnect-title"><span className="modal-kicker">ACCIÓN DELICADA</span><h2 id="disconnect-title">Desconectar Google Search Console</h2><p>Se eliminarán las métricas importadas de este proyecto y se pausará la sincronización automática. La propiedad podrá conectarse nuevamente más adelante.</p><form onSubmit={disconnect}><label>Escribe <strong>desconectar</strong> para continuar<input autoFocus autoComplete="off" spellCheck={false} value={disconnectText} onChange={event => setDisconnectText(event.target.value)} placeholder="desconectar" /></label><div className="modal-actions"><button className="secondary-button" type="button" onClick={closeDisconnectConfirmation}>Cancelar</button><button className="danger-button" type="submit" disabled={disconnectText.trim() !== 'desconectar'}><Unplug size={14} /> Confirmar desconexión</button></div></form></section></div>}
  </section>
}

function formatNumber(value: number) { return new Intl.NumberFormat('es-GT', { maximumFractionDigits: 1 }).format(value) }
function formatPercent(value: number) { return new Intl.NumberFormat('es-GT', { style: 'percent', maximumFractionDigits: 1 }).format(value) }

function AutomationSchedules({ schedules, runs, projectId, onAct }: { schedules: AutomationSchedule[]; runs: AutomationRun[]; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  return <section className="automation-section"><div className="section-toolbar"><div><h2>Programaciones del proyecto</h2><p>Las horas usan la zona horaria configurada en el proyecto. Los límites se aplican también a ejecuciones manuales.</p></div></div><div className="automation-grid">{schedules.map(schedule => <AutomationCard key={schedule.id} schedule={schedule} latestRun={runs.find(run => run.automationScheduleId === schedule.id)} projectId={projectId} onAct={onAct} />)}</div></section>
}

function AutomationCard({ schedule, latestRun, projectId, onAct }: { schedule: AutomationSchedule; latestRun?: AutomationRun; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [form, setForm] = useState<AutomationScheduleInput>({ isEnabled: schedule.isEnabled, frequency: schedule.frequency, intervalMinutes: schedule.intervalMinutes, localTime: schedule.localTime, dayOfWeek: schedule.dayOfWeek, maxRunsPerDay: schedule.maxRunsPerDay })
  useEffect(() => setForm({ isEnabled: schedule.isEnabled, frequency: schedule.frequency, intervalMinutes: schedule.intervalMinutes, localTime: schedule.localTime, dayOfWeek: schedule.dayOfWeek, maxRunsPerDay: schedule.maxRunsPerDay }), [schedule])
  function save(event: FormEvent) { event.preventDefault(); void onAct(() => api.updateAutomationSchedule(projectId, schedule.workflow, form), `Programación “${schedule.displayName}” actualizada.`) }
  return <article className="panel automation-card"><div className="automation-card__heading"><span><Workflow size={18} /></span><div><h3>{schedule.displayName}</h3><small>{schedule.workflow}</small></div><label className="automation-toggle"><input type="checkbox" checked={form.isEnabled} onChange={event => setForm({ ...form, isEnabled: event.target.checked })} /><span>{form.isEnabled ? 'Activa' : 'Pausada'}</span></label></div><form onSubmit={save}><label>Frecuencia<select value={form.frequency} onChange={event => setForm({ ...form, frequency: event.target.value })}>{Object.entries(frequencyLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>{form.frequency === 'interval' && <label>Intervalo (min)<input type="number" min="5" max="10080" value={form.intervalMinutes ?? 60} onChange={event => setForm({ ...form, intervalMinutes: Number(event.target.value) })} /></label>}{form.frequency !== 'manual' && form.frequency !== 'interval' && <label>Hora local<input type="time" value={form.localTime} onChange={event => setForm({ ...form, localTime: event.target.value })} /></label>}{form.frequency === 'weekly' && <label>Día<select value={form.dayOfWeek ?? 1} onChange={event => setForm({ ...form, dayOfWeek: Number(event.target.value) })}>{dayLabel.map((label, index) => <option value={index} key={label}>{label}</option>)}</select></label>}<label>Máximo diario<input type="number" min="1" max="100" value={form.maxRunsPerDay} onChange={event => setForm({ ...form, maxRunsPerDay: Number(event.target.value) })} /></label><div className="automation-card__actions"><button className="secondary-button" type="submit"><Save size={14} /> Guardar</button><button className="primary-button" type="button" onClick={() => void onAct(() => runAutomationOrThrow(projectId, schedule.workflow), `“${schedule.displayName}” terminó correctamente.`)}><Play size={14} /> Ejecutar ahora</button></div></form><div className="automation-card__status"><span>Próxima: {schedule.nextRunAt ? new Date(schedule.nextRunAt).toLocaleString('es-GT') : 'sin programación'}</span><span>Última: {latestRun ? `${latestRun.status}${latestRun.errorCode ? ` · ${latestRun.errorCode}` : ''}` : 'sin ejecuciones'}</span>{latestRun?.errorMessage && <em>{latestRun.errorMessage}</em>}</div></article>
}

async function runAutomationOrThrow(projectId: string, workflow: string) {
  const run = await api.runAutomation(projectId, workflow)
  if (run.status !== 'succeeded') {
    const code = run.errorCode ? ` (${run.errorCode})` : ''
    throw new Error(`${run.errorMessage || 'La automatización no pudo completarse.'}${code}`)
  }
  return run
}

function NotificationMetrics({ summary }: { summary: NotificationSummary }) {
  return <section className="notification-kpis"><article><span><CheckCircle2 size={16} /></span><small>Entregadas</small><strong>{summary.delivered}</strong><p>reales o simuladas</p></article><article className="notification-kpi--amber"><span><Clock3 size={16} /></span><small>En cola</small><strong>{summary.pending}</strong><p>por política o reintento</p></article><article className="notification-kpi--red"><span><CircleAlert size={16} /></span><small>Fallidas</small><strong>{summary.failed}</strong><p>con recuperación manual</p></article><article className="notification-kpi--violet"><span><Layers3 size={16} /></span><small>Eventos recibidos</small><strong>{summary.totalOccurrences}</strong><p>{summary.grouped} agrupados para evitar ruido</p></article></section>
}

function PolicyPanel({ policy, projectId, onAct }: { policy: NotificationPolicy; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [form, setForm] = useState<NotificationPolicyInput>({ isEnabled: policy.isEnabled, minimumSeverity: policy.minimumSeverity, groupWindowMinutes: policy.groupWindowMinutes, quietHoursStart: policy.quietHoursStart, quietHoursEnd: policy.quietHoursEnd })
  useEffect(() => setForm({ isEnabled: policy.isEnabled, minimumSeverity: policy.minimumSeverity, groupWindowMinutes: policy.groupWindowMinutes, quietHoursStart: policy.quietHoursStart, quietHoursEnd: policy.quietHoursEnd }), [policy])
  function save(event: FormEvent) { event.preventDefault(); void onAct(() => api.updateNotificationPolicy(projectId, form), 'Política de alertas actualizada.') }
  return <section className="panel notification-policy"><div className="panel-title"><div><h2>Política por proyecto</h2><p>Canal actual: {policy.deliveryMode === 'telegram' ? 'Telegram real' : 'simulado'}.</p></div><Settings2 size={20} /></div><form onSubmit={save}><label className="pause-control"><input type="checkbox" checked={form.isEnabled} onChange={event => setForm({ ...form, isEnabled: event.target.checked })} /><span><BellRing size={18} /><b>{form.isEnabled ? 'Alertas habilitadas' : 'Alertas detenidas'}</b><small>Al detenerlas, se conserva el registro como omitido.</small></span></label><label>Severidad mínima<select value={form.minimumSeverity} onChange={event => setForm({ ...form, minimumSeverity: event.target.value })}>{Object.entries(severityLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><label>Agrupar durante (minutos)<input type="number" min="1" max="1440" value={form.groupWindowMinutes} onChange={event => setForm({ ...form, groupWindowMinutes: Number(event.target.value) })} /></label><label>Silencio desde<input type="number" min="0" max="23" value={form.quietHoursStart} onChange={event => setForm({ ...form, quietHoursStart: Number(event.target.value) })} /></label><label>Silencio hasta<input type="number" min="0" max="23" value={form.quietHoursEnd} onChange={event => setForm({ ...form, quietHoursEnd: Number(event.target.value) })} /></label><button className="primary-button" type="submit">Guardar política</button></form></section>
}

function TestPanel({ projectId, real, onAct }: { projectId: string; real: boolean; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [severity, setSeverity] = useState('warning')
  const send = (simulateFailure: boolean) => onAct(() => api.dispatchNotification(projectId, { deduplicationKey: simulateFailure ? `simulated-failure-${crypto.randomUUID()}` : `telegram-test-${crypto.randomUUID()}`, category: 'operations', severity: simulateFailure ? 'critical' : severity, title: simulateFailure ? 'Fallo de entrega de prueba' : 'Prueba del centro de control', message: simulateFailure ? 'El sistema registró un fallo controlado para comprobar su recuperación.' : 'La configuración de notificaciones respondió correctamente.', simulateFailure }), simulateFailure ? 'Fallo controlado registrado.' : real ? 'Mensaje enviado a Telegram.' : 'Entrega simulada registrada.')
  return <section className="panel notification-simulator"><div className="panel-title"><div><h2>Probar canal</h2><p>{real ? 'Envía una prueba real al chat configurado.' : 'Valida el flujo local sin llamar a Telegram.'}</p></div><RadioTower size={20} /></div><div className="simulation-actions"><label>Severidad<select value={severity} onChange={event => setSeverity(event.target.value)}>{Object.entries(severityLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><button className="primary-button" onClick={() => void send(false)}><Send size={15} /> {real ? 'Enviar prueba' : 'Simular alerta'}</button><button className="danger-button" onClick={() => void send(true)}><TriangleAlert size={15} /> Simular fallo</button></div></section>
}

function NotificationHistory({ items, projectId, onAct }: { items: NotificationRecord[]; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  return <section className="notification-history"><div className="section-toolbar"><div><h2>Historial de entregas</h2><p>Estados persistentes y separados por proyecto.</p></div></div>{items.length ? <div className="panel notification-table">{items.map(item => <article className="notification-row" key={item.id}><span className={`notification-severity notification-severity--${item.severity}`}><BellRing size={16} /></span><div><div className="notification-row__title"><span className={`notification-status notification-status--${item.status}`}>{statusLabel[item.status] ?? item.status}</span><small>{severityLabel[item.severity] ?? item.severity} · {item.category}</small></div><h3>{item.title}</h3><p>{item.message}</p>{item.flow && <small>{item.flow}{item.provider ? ` · ${item.provider}` : ''}{item.model ? ` · ${item.model}` : ''}</small>}{item.errorMessage && <em><TriangleAlert size={13} /> {item.errorMessage}</em>}</div><div className="notification-row__meta"><span>{item.groupCount} {item.groupCount === 1 ? 'evento' : 'eventos'}</span><small>{item.attemptCount} {item.attemptCount === 1 ? 'intento' : 'intentos'}</small><time>{new Date(item.updatedAt).toLocaleString('es-GT')}</time>{(item.status === 'failed' || item.status === 'queued') && <button className="secondary-button" onClick={() => onAct(() => api.retryNotification(projectId, item.id), 'Notificación recuperada.')}><RefreshCw size={14} /> Recuperar</button>}</div></article>)}</div> : <div className="panel operations-empty"><span><BellRing size={25} /></span><h2>Aún no hay alertas</h2><p>Prueba el canal para crear la primera entrega.</p></div>}</section>
}

function ProjectRequired() { return <div className="page module-page"><div className="panel operations-empty"><span><BellRing size={25} /></span><h2>Selecciona un proyecto</h2><p>Las programaciones y alertas se mantienen aisladas por proyecto.</p></div></div> }
