import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { BellRing, CheckCircle2, CircleAlert, Clock3, Layers3, RadioTower, RefreshCw, Send, Settings2, ShieldCheck, Sparkles, TriangleAlert } from 'lucide-react'
import { api, type NotificationPolicy, type NotificationPolicyInput, type NotificationRecord, type NotificationSummary, type Project } from './api'

const statusLabel: Record<string, string> = { sent: 'Entregada (simulada)', failed: 'Fallida', queued: 'En cola', grouped: 'Agrupada', suppressed: 'Omitida por política' }
const severityLabel: Record<string, string> = { info: 'Informativa', warning: 'Advertencia', critical: 'Crítica' }

export function NotificationsPage({ project }: { project: Project | null }) {
  const [policy, setPolicy] = useState<NotificationPolicy | null>(null)
  const [items, setItems] = useState<NotificationRecord[]>([])
  const [summary, setSummary] = useState<NotificationSummary | null>(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    if (!project) return
    setLoading(true); setError('')
    try {
      const [loadedPolicy, loadedItems, loadedSummary] = await Promise.all([
        api.getNotificationPolicy(project.id), api.getNotifications(project.id), api.getNotificationSummary(project.id),
      ])
      setPolicy(loadedPolicy); setItems(loadedItems); setSummary(loadedSummary)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo cargar el centro de notificaciones.') }
    finally { setLoading(false) }
  }, [project])

  useEffect(() => { void load() }, [load])

  async function act(action: () => Promise<unknown>, message: string) {
    setError(''); setNotice('')
    try { await action(); setNotice(message); await load() }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo completar la acción.') }
  }

  if (!project) return <ProjectRequired />
  return <div className="page notifications-page">
    <section className="page-heading">
      <div><div className="eyebrow"><span /> Fase 6 · canal simulado</div><h1>Alertas y recuperación</h1><p>Configura señales útiles, evita el ruido y recupera fallos sin conectar aún un bot real.</p></div>
      <div className="heading-actions"><span className="demo-pill"><Sparkles size={14} /> Telegram no conectado</span><button className="primary-button" onClick={() => act(() => api.processQueuedNotifications(project.id), 'Cola revisada por el simulador.')}><RefreshCw size={16} /> Procesar cola</button></div>
    </section>
    <section className="notification-safety"><ShieldCheck size={19} /><div><strong>Modo simulado y sin secretos</strong><span>“Entregada” solo confirma una prueba local. No se guardó token, chat ID ni se envió información a Telegram.</span></div></section>
    {error && <div className="global-error notification-message" role="alert">{error}</div>}
    {notice && <div className="success-banner notification-message" role="status">{notice}</div>}
    {loading && <div className="panel operations-empty">Cargando alertas…</div>}
    {!loading && policy && summary && <><NotificationMetrics summary={summary} /><div className="notification-layout"><PolicyPanel policy={policy} projectId={project.id} onAct={act} /><SimulationPanel projectId={project.id} onAct={act} /></div><NotificationHistory items={items} projectId={project.id} onAct={act} /></>}
  </div>
}

function NotificationMetrics({ summary }: { summary: NotificationSummary }) {
  return <section className="notification-kpis"><article><span><CheckCircle2 size={16} /></span><small>Entregadas</small><strong>{summary.delivered}</strong><p>pruebas locales</p></article><article className="notification-kpi--amber"><span><Clock3 size={16} /></span><small>En cola</small><strong>{summary.pending}</strong><p>por horario silencioso</p></article><article className="notification-kpi--red"><span><CircleAlert size={16} /></span><small>Fallidas</small><strong>{summary.failed}</strong><p>con recuperación manual</p></article><article className="notification-kpi--violet"><span><Layers3 size={16} /></span><small>Eventos recibidos</small><strong>{summary.totalOccurrences}</strong><p>{summary.grouped} {summary.grouped === 1 ? 'registro agrupado evita ruido' : 'registros agrupados evitan ruido'}</p></article></section>
}

function PolicyPanel({ policy, projectId, onAct }: { policy: NotificationPolicy; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [form, setForm] = useState<NotificationPolicyInput>({ isEnabled: policy.isEnabled, minimumSeverity: policy.minimumSeverity, groupWindowMinutes: policy.groupWindowMinutes, quietHoursStart: policy.quietHoursStart, quietHoursEnd: policy.quietHoursEnd })
  useEffect(() => setForm({ isEnabled: policy.isEnabled, minimumSeverity: policy.minimumSeverity, groupWindowMinutes: policy.groupWindowMinutes, quietHoursStart: policy.quietHoursStart, quietHoursEnd: policy.quietHoursEnd }), [policy])
  function save(event: FormEvent) { event.preventDefault(); void onAct(() => api.updateNotificationPolicy(projectId, form), 'Política de alertas actualizada.') }
  return <section className="panel notification-policy"><div className="panel-title"><div><h2>Política por proyecto</h2><p>El canal está desacoplado y fijado en modo simulado.</p></div><Settings2 size={20} /></div><form onSubmit={save}><label className="pause-control"><input type="checkbox" checked={form.isEnabled} onChange={(event) => setForm({ ...form, isEnabled: event.target.checked })} /><span><BellRing size={18} /><b>{form.isEnabled ? 'Alertas habilitadas' : 'Alertas detenidas'}</b><small>Al detenerlas, se conserva el registro como omitido.</small></span></label><label>Severidad mínima<select value={form.minimumSeverity} onChange={(event) => setForm({ ...form, minimumSeverity: event.target.value })}>{Object.entries(severityLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><label>Agrupar durante (minutos)<input type="number" min="1" max="1440" value={form.groupWindowMinutes} onChange={(event) => setForm({ ...form, groupWindowMinutes: Number(event.target.value) })} /></label><label>Silencio desde (hora local)<input type="number" min="0" max="23" value={form.quietHoursStart} onChange={(event) => setForm({ ...form, quietHoursStart: Number(event.target.value) })} /></label><label>Silencio hasta (hora local)<input type="number" min="0" max="23" value={form.quietHoursEnd} onChange={(event) => setForm({ ...form, quietHoursEnd: Number(event.target.value) })} /></label><button className="primary-button" type="submit">Guardar política</button></form></section>
}

function SimulationPanel({ projectId, onAct }: { projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [severity, setSeverity] = useState('warning')
  const send = (simulateFailure: boolean) => onAct(() => api.dispatchNotification(projectId, { deduplicationKey: simulateFailure ? `simulated-failure-${crypto.randomUUID()}` : 'simulated-budget-warning', category: simulateFailure ? 'operations' : 'budget', severity: simulateFailure ? 'critical' : severity, title: simulateFailure ? 'Fallo de entrega de prueba' : 'Aviso de presupuesto de prueba', message: simulateFailure ? 'El simulador registrará un fallo recuperable. No se llama a Telegram.' : 'Esta señal puede agruparse al repetirla para evitar alertas redundantes.', simulateFailure }), simulateFailure ? 'Fallo simulado registrado; puedes recuperarlo desde el historial.' : 'Alerta simulada registrada.')
  return <section className="panel notification-simulator"><div className="panel-title"><div><h2>Probar sin Telegram</h2><p>Genera registros locales para validar política, agrupación y recuperación.</p></div><RadioTower size={20} /></div><div className="simulation-actions"><label>Severidad<select value={severity} onChange={(event) => setSeverity(event.target.value)}>{Object.entries(severityLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><button className="primary-button" onClick={() => void send(false)}><Send size={15} /> Simular alerta</button><button className="danger-button" onClick={() => void send(true)}><TriangleAlert size={15} /> Simular fallo</button></div><div className="simulation-note"><Layers3 size={16} /><p>Repite “Simular alerta” dentro de la ventana configurada para verificar que el sistema agrupa el evento, en vez de crear otro envío.</p></div></section>
}

function NotificationHistory({ items, projectId, onAct }: { items: NotificationRecord[]; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  return <section className="notification-history"><div className="section-toolbar"><div><h2>Historial de entregas</h2><p>Estados persistentes y separados por proyecto.</p></div></div>{items.length ? <div className="panel notification-table">{items.map((item) => <article className="notification-row" key={item.id}><span className={`notification-severity notification-severity--${item.severity}`}><BellRing size={16} /></span><div><div className="notification-row__title"><span className={`notification-status notification-status--${item.status}`}>{statusLabel[item.status] ?? item.status}</span><small>{severityLabel[item.severity] ?? item.severity} · {item.category}</small></div><h3>{item.title}</h3><p>{item.message}</p>{item.errorMessage && <em><TriangleAlert size={13} /> {item.errorMessage}</em>}</div><div className="notification-row__meta"><span>{item.groupCount} {item.groupCount === 1 ? 'evento' : 'eventos'}</span><small>{item.attemptCount} {item.attemptCount === 1 ? 'intento' : 'intentos'}</small><time>{new Date(item.updatedAt).toLocaleString('es-GT')}</time>{(item.status === 'failed' || item.status === 'queued') && <button className="secondary-button" onClick={() => onAct(() => api.retryNotification(projectId, item.id), 'Notificación recuperada por el simulador.')}><RefreshCw size={14} /> Recuperar</button>}</div></article>)}</div> : <div className="panel operations-empty"><span><BellRing size={25} /></span><h2>Aún no hay alertas</h2><p>Usa el simulador para probar una entrega local sin exponer información.</p></div>}</section>
}

function ProjectRequired() { return <div className="page module-page"><div className="panel operations-empty"><span><BellRing size={25} /></span><h2>Selecciona un proyecto</h2><p>Las políticas y el historial de alertas se mantienen aislados por proyecto.</p></div></div> }
