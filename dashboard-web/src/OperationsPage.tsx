import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { AlertTriangle, CheckCircle2, CircleDollarSign, Clock3, Gauge, History, PauseCircle, PlayCircle, Plus, RotateCcw, ShieldCheck, Sparkles, XCircle } from 'lucide-react'
import { api, type Budget, type BudgetInput, type ExecutionRecord, type ExecutionSummary, type Project, type RatePlan } from './api'

type Tab = 'executions' | 'approvals' | 'costs' | 'rates'

const statusLabels: Record<string, string> = {
  awaiting_approval: 'Pendiente de aprobación', approved: 'Aprobada', succeeded: 'Completada',
  failed: 'Fallida', blocked: 'Bloqueada', cancelled: 'Cancelada',
}

const flowLabels: Record<string, string> = {
  seo_brief: 'Brief SEO', seo_draft: 'Borrador SEO', content_analysis: 'Análisis de contenido', content_draft: 'Borrador de contenido', seo_analysis: 'Análisis SEO',
}

const usd = new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'USD', minimumFractionDigits: 4, maximumFractionDigits: 4 })
const gtq = new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ', minimumFractionDigits: 2, maximumFractionDigits: 4 })
const integer = new Intl.NumberFormat('es-GT')

export function OperationsPage({ project, initialTab }: { project: Project | null; initialTab: 'executions' | 'costs' }) {
  const [tab, setTab] = useState<Tab>(initialTab)
  const [executions, setExecutions] = useState<ExecutionRecord[]>([])
  const [rates, setRates] = useState<RatePlan[]>([])
  const [budget, setBudget] = useState<Budget | null>(null)
  const [summary, setSummary] = useState<ExecutionSummary | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [showPlan, setShowPlan] = useState(false)

  useEffect(() => setTab(initialTab), [initialTab])

  const load = useCallback(async () => {
    if (!project) return
    setLoading(true); setError('')
    try {
      const [loadedExecutions, loadedRates, loadedBudget, loadedSummary] = await Promise.all([
        api.getExecutions(project.id), api.getRatePlans(project.id), api.getBudget(project.id), api.getExecutionSummary(project.id),
      ])
      setExecutions(loadedExecutions); setRates(loadedRates); setBudget(loadedBudget); setSummary(loadedSummary)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No fue posible cargar el control de ejecuciones.')
    } finally { setLoading(false) }
  }, [project])

  useEffect(() => { void load() }, [load])

  async function act(action: () => Promise<unknown>, success: string) {
    setError(''); setNotice('')
    try { await action(); setNotice(success); await load() }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo completar la acción.') }
  }

  if (!project) return <ProjectRequired />

  const pending = executions.filter((item) => item.status === 'awaiting_approval')
  return (
    <div className="page operations-page">
      <section className="page-heading operations-heading">
        <div><div className="eyebrow"><span /> Fase 5 · control local</div><h1>Ejecuciones y costos</h1><p>Planifica, aprueba y audita cada intento antes de conectar proveedores reales.</p></div>
        <div className="heading-actions"><span className="demo-pill"><Sparkles size={14} /> Simulación sin consumo real</span><button className="primary-button" onClick={() => setShowPlan(true)}><Plus size={16} /> Nueva ejecución</button></div>
      </section>

      <section className="operations-safety"><ShieldCheck size={19} /><div><strong>Modo seguro activo</strong><span>Los importes son estimaciones basadas en unidades planificadas, tarifa registrada y tipo de cambio configurable. No son facturas del proveedor.</span></div>{summary?.isPaused && <b><PauseCircle size={15} /> Ejecuciones pausadas</b>}</section>
      {error && <div className="global-error" role="alert">{error}</div>}
      {notice && <div className="success-banner" role="status">{notice}</div>}

      <nav className="module-tabs" aria-label="Secciones de operaciones">
        <button className={tab === 'executions' ? 'active' : ''} onClick={() => setTab('executions')}>Ejecuciones <span>{executions.length}</span></button>
        <button className={tab === 'approvals' ? 'active' : ''} onClick={() => setTab('approvals')}>Aprobaciones <span>{pending.length}</span></button>
        <button className={tab === 'costs' ? 'active' : ''} onClick={() => setTab('costs')}>Costos y límites</button>
        <button className={tab === 'rates' ? 'active' : ''} onClick={() => setTab('rates')}>Tarifas</button>
      </nav>

      {loading ? <div className="panel operations-empty">Cargando controles…</div> : null}
      {!loading && tab === 'executions' && <ExecutionsView items={executions} onAct={act} projectId={project.id} />}
      {!loading && tab === 'approvals' && <ApprovalsView items={pending} onAct={act} projectId={project.id} />}
      {!loading && tab === 'costs' && budget && summary && <CostsView budget={budget} summary={summary} projectId={project.id} onAct={act} />}
      {!loading && tab === 'rates' && <RatesView rates={rates} projectId={project.id} onAct={act} />}
      {showPlan && <PlanDialog rates={rates.filter((item) => item.isActive)} projectId={project.id} onClose={() => setShowPlan(false)} onCreated={async () => { setShowPlan(false); setNotice('Ejecución planificada y costo estimado registrado.'); await load() }} />}
    </div>
  )
}

function ExecutionsView({ items, projectId, onAct }: { items: ExecutionRecord[]; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  if (!items.length) return <Empty title="Aún no hay ejecuciones" detail="Registra una tarifa y planifica el primer intento. Nada se enviará a servicios externos." />
  return <section className="execution-list">{items.map((item) => <article className="panel execution-card" key={item.id}>
    <div className="execution-card__main"><span className={`execution-status execution-status--${item.status}`}>{statusLabels[item.status] ?? item.status}</span><div><h2>{flowLabels[item.flow] ?? item.flow}</h2><p>{item.provider} · {item.model} · intento {item.attemptNumber}</p></div></div>
    <div className="execution-card__numbers"><span><small>Unidades</small><strong>{integer.format(item.inputUnits + item.outputUnits)}</strong></span><span><small>Estimado</small><strong>{usd.format(item.estimatedCostUsd)}</strong><em>{gtq.format(item.estimatedCostGtq)}</em></span><span><small>Creada</small><strong>{new Date(item.createdAt).toLocaleDateString('es-GT')}</strong></span></div>
    {item.errorMessage && <p className="execution-error"><AlertTriangle size={15} /> {item.errorMessage}</p>}
    <div className="execution-card__footer"><code>{item.idempotencyKey}</code><div>
      {item.status === 'approved' && <><button onClick={() => onAct(() => api.completeExecution(projectId, item.id, true), 'Simulación completada correctamente.')}><CheckCircle2 size={14} /> Simular éxito</button><button className="danger-text" onClick={() => onAct(() => api.completeExecution(projectId, item.id, false), 'Fallo simulado registrado.')}><XCircle size={14} /> Simular fallo</button></>}
      {item.status === 'failed' && <button onClick={() => onAct(() => api.retryExecution(projectId, item.id), 'Reintento creado con una nueva clave idempotente.')}><RotateCcw size={14} /> Reintentar</button>}
    </div></div>
    <details><summary><History size={14} /> Ver auditoría ({item.audit.length})</summary><ol>{item.audit.map((entry) => <li key={entry.id}><span>{new Date(entry.occurredAt).toLocaleString('es-GT')}</span><strong>{statusLabels[entry.toStatus] ?? entry.toStatus}</strong><p>{entry.note} · {entry.actor}</p></li>)}</ol></details>
  </article>)}</section>
}

function ApprovalsView({ items, projectId, onAct }: { items: ExecutionRecord[]; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  if (!items.length) return <Empty title="No hay aprobaciones pendientes" detail="Las ejecuciones que requieran revisión humana aparecerán aquí." />
  return <section className="approval-grid">{items.map((item) => <article className="panel approval-card" key={item.id}><span className="approval-card__icon"><Clock3 size={22} /></span><small>APROBACIÓN REQUERIDA</small><h2>{flowLabels[item.flow] ?? item.flow}</h2><p>{item.provider} · {item.model}</p><dl><div><dt>Entrada</dt><dd>{integer.format(item.inputUnits)}</dd></div><div><dt>Salida</dt><dd>{integer.format(item.outputUnits)}</dd></div><div><dt>Costo estimado</dt><dd>{usd.format(item.estimatedCostUsd)}</dd></div></dl><div className="approval-card__actions"><button className="primary-button" onClick={() => onAct(() => api.approveExecution(projectId, item.id, 'Revisada desde el centro de control.'), 'Ejecución aprobada y lista para simulación.')}><CheckCircle2 size={15} /> Aprobar</button><button className="secondary-button" onClick={() => onAct(() => api.cancelExecution(projectId, item.id), 'Ejecución cancelada.')}><XCircle size={15} /> Cancelar</button></div></article>)}</section>
}

function CostsView({ budget, summary, projectId, onAct }: { budget: Budget; summary: ExecutionSummary; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [form, setForm] = useState<BudgetInput>({ dailyLimitUsd: budget.dailyLimitUsd, monthlyLimitUsd: budget.monthlyLimitUsd, warningPercent: budget.warningPercent, exchangeRateGtqPerUsd: budget.exchangeRateGtqPerUsd, isPaused: budget.isPaused })
  useEffect(() => setForm({ dailyLimitUsd: budget.dailyLimitUsd, monthlyLimitUsd: budget.monthlyLimitUsd, warningPercent: budget.warningPercent, exchangeRateGtqPerUsd: budget.exchangeRateGtqPerUsd, isPaused: budget.isPaused }), [budget])
  return <div className="cost-layout"><section className="cost-metrics"><article className="metric-card"><small>Estimado hoy</small><strong>{usd.format(summary.todayCostUsd)}</strong><span>Reserva planificada, no cobro real</span></article><article className="metric-card metric-card--violet"><small>Estimado del mes</small><strong>{gtq.format(summary.monthCostGtq)}</strong><span>{usd.format(summary.monthCostUsd)} al cambio registrado</span></article><article className="metric-card metric-card--amber"><small>Presupuesto usado</small><strong>{summary.budgetUsedPercent.toFixed(2)}%</strong><div className="progress-track"><span style={{ width: `${Math.min(summary.budgetUsedPercent, 100)}%` }} /></div></article></section>
    <section className="cost-columns"><div className="panel budget-panel"><div className="panel-title"><div><h2>Límites y pausa global</h2><p>Todo cálculo se compara antes de permitir una simulación.</p></div><Gauge size={20} /></div><form onSubmit={(event) => { event.preventDefault(); void onAct(() => api.updateBudget(projectId, form), 'Política de presupuesto actualizada.') }}><label>Límite diario (USD)<input type="number" min="0.0001" step="0.0001" value={form.dailyLimitUsd} onChange={(event) => setForm({ ...form, dailyLimitUsd: Number(event.target.value) })} /></label><label>Límite mensual (USD)<input type="number" min="0.0001" step="0.0001" value={form.monthlyLimitUsd} onChange={(event) => setForm({ ...form, monthlyLimitUsd: Number(event.target.value) })} /></label><label>Alerta al (%)<input type="number" min="1" max="100" value={form.warningPercent} onChange={(event) => setForm({ ...form, warningPercent: Number(event.target.value) })} /></label><label>GTQ por USD<input type="number" min="0.0001" step="0.0001" value={form.exchangeRateGtqPerUsd} onChange={(event) => setForm({ ...form, exchangeRateGtqPerUsd: Number(event.target.value) })} /></label><label className="pause-control"><input type="checkbox" checked={form.isPaused} onChange={(event) => setForm({ ...form, isPaused: event.target.checked })} /><span>{form.isPaused ? <PauseCircle size={18} /> : <PlayCircle size={18} />}<b>{form.isPaused ? 'Nuevas ejecuciones pausadas' : 'Planificación habilitada'}</b><small>La pausa bloquea nuevos intentos, sin borrar el historial.</small></span></label><button className="primary-button" type="submit">Guardar política</button></form></div>
      <div className="panel provider-panel"><div className="panel-title"><div><h2>Desglose por proveedor</h2><p>Estimaciones acumuladas durante el mes.</p></div><CircleDollarSign size={20} /></div>{summary.providers.length ? summary.providers.map((provider) => <div className="provider-row" key={provider.provider}><span><strong>{provider.provider}</strong><small>{provider.executions} ejecuciones</small></span><span><strong>{usd.format(provider.costUsd)}</strong><small>{gtq.format(provider.costGtq)}</small></span></div>) : <p className="muted-copy">Sin estimaciones en este periodo.</p>}<div className="cost-disclaimer"><AlertTriangle size={16} /><p>La precisión depende de la tarifa y del tipo de cambio registrados. Al conectar APIs reales se conciliará con el consumo reportado por cada proveedor.</p></div></div></section></div>
}

function RatesView({ rates, projectId, onAct }: { rates: RatePlan[]; projectId: string; onAct: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [form, setForm] = useState({ provider: 'gemini', model: '', inputUsdPerMillion: 0.1, outputUsdPerMillion: 0.4, effectiveFrom: null as string | null })
  return <div className="rates-layout"><section className="panel rate-form"><div className="panel-title"><div><h2>Nueva versión de tarifa</h2><p>La versión vigente anterior se conserva para auditar cálculos históricos.</p></div><Plus size={20} /></div><form onSubmit={(event) => { event.preventDefault(); void onAct(() => api.createRatePlan(projectId, form), 'Nueva versión de tarifa registrada.') }}><label>Proveedor<input required value={form.provider} onChange={(event) => setForm({ ...form, provider: event.target.value })} /></label><label>Modelo<input required placeholder="Ej. gemini-flash" value={form.model} onChange={(event) => setForm({ ...form, model: event.target.value })} /></label><label>Entrada / millón (USD)<input required type="number" min="0" step="0.0001" value={form.inputUsdPerMillion} onChange={(event) => setForm({ ...form, inputUsdPerMillion: Number(event.target.value) })} /></label><label>Salida / millón (USD)<input required type="number" min="0" step="0.0001" value={form.outputUsdPerMillion} onChange={(event) => setForm({ ...form, outputUsdPerMillion: Number(event.target.value) })} /></label><button className="primary-button" type="submit">Registrar versión</button></form></section><section className="panel rate-history"><div className="panel-title"><div><h2>Historial de tarifas</h2><p>Cada ejecución conserva la versión usada.</p></div><History size={20} /></div>{rates.length ? rates.map((rate) => <div className={`rate-row ${rate.isActive ? 'rate-row--active' : ''}`} key={rate.id}><span><b>{rate.provider}</b><strong>{rate.model}</strong><small>Desde {new Date(rate.effectiveFrom).toLocaleDateString('es-GT')}</small></span><span><small>Entrada</small><b>{usd.format(rate.inputUsdPerMillion)} / M</b></span><span><small>Salida</small><b>{usd.format(rate.outputUsdPerMillion)} / M</b></span><em>{rate.isActive ? 'Vigente' : 'Histórica'}</em></div>) : <p className="muted-copy">Registra la primera tarifa para poder planificar una ejecución.</p>}</section></div>
}

function PlanDialog({ rates, projectId, onClose, onCreated }: { rates: RatePlan[]; projectId: string; onClose: () => void; onCreated: () => Promise<void> }) {
  const first = rates[0]
  const [form, setForm] = useState({ idempotencyKey: `manual-${crypto.randomUUID()}`, provider: first?.provider ?? '', model: first?.model ?? '', flow: 'seo_brief', inputUnits: 10000, outputUnits: 2500, approvalRequired: true })
  const [error, setError] = useState('')
  const selected = rates.find((rate) => rate.provider === form.provider && rate.model === form.model)
  const estimate = selected ? form.inputUnits / 1_000_000 * selected.inputUsdPerMillion + form.outputUnits / 1_000_000 * selected.outputUsdPerMillion : 0
  async function submit(event: FormEvent) { event.preventDefault(); setError(''); try { await api.planExecution(projectId, form); await onCreated() } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo planificar la ejecución.') } }
  return <div className="modal-backdrop" role="presentation" onMouseDown={onClose}><section className="modal operations-modal" role="dialog" aria-modal="true" aria-labelledby="plan-title" onMouseDown={(event) => event.stopPropagation()}><button className="modal__close" onClick={onClose} aria-label="Cerrar"><XCircle size={20} /></button><span className="modal-kicker">SIMULACIÓN CONTROLADA</span><h2 id="plan-title">Planificar ejecución</h2><p>Se registrará el intento y su estimación; no se llamará a ninguna API externa.</p>{error && <div className="global-error">{error}</div>}{!rates.length ? <div className="empty-inline"><AlertTriangle size={18} /> Registra una tarifa activa antes de continuar.</div> : <form onSubmit={submit}><label>Tarifa<select value={`${form.provider}/${form.model}`} onChange={(event) => { const rate = rates.find((item) => `${item.provider}/${item.model}` === event.target.value); if (rate) setForm({ ...form, provider: rate.provider, model: rate.model }) }}>{rates.map((rate) => <option key={rate.id} value={`${rate.provider}/${rate.model}`}>{rate.provider} · {rate.model}</option>)}</select></label><label>Flujo<select value={form.flow} onChange={(event) => setForm({ ...form, flow: event.target.value })}>{Object.entries(flowLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><div className="form-split"><label>Unidades de entrada<input type="number" min="1" value={form.inputUnits} onChange={(event) => setForm({ ...form, inputUnits: Number(event.target.value) })} /></label><label>Unidades de salida<input type="number" min="0" value={form.outputUnits} onChange={(event) => setForm({ ...form, outputUnits: Number(event.target.value) })} /></label></div><label className="check-control"><input type="checkbox" checked={form.approvalRequired} onChange={(event) => setForm({ ...form, approvalRequired: event.target.checked })} /><span><b>Requerir aprobación humana</b><small>La ejecución quedará detenida hasta aprobarla.</small></span></label><div className="estimate-box"><span>Costo estimado</span><strong>{usd.format(estimate)}</strong><small>según la tarifa activa seleccionada</small></div><div className="modal-actions"><button type="button" className="secondary-button" onClick={onClose}>Cancelar</button><button className="primary-button" type="submit">Registrar planificación</button></div></form>}</section></div>
}

function Empty({ title, detail }: { title: string; detail: string }) { return <div className="panel operations-empty"><span><History size={25} /></span><h2>{title}</h2><p>{detail}</p></div> }
function ProjectRequired() { return <div className="page module-page"><div className="panel operations-empty"><span><ShieldCheck size={25} /></span><h2>Selecciona un proyecto</h2><p>Las ejecuciones, tarifas y presupuestos están aislados por proyecto.</p></div></div> }
