import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  ArrowRight, CalendarDays, Check, CircleDot, ClipboardCheck, FileText, History,
  Lightbulb, Pencil, Plus, Save, Search, Send, Sparkles, Trash2, TrendingUp, X,
} from 'lucide-react'
import {
  api, type ContentPiece, type ContentPieceInput, type Project, type SeoOpportunity,
  type SeoOpportunityInput, type SeoSummary,
} from './api'

type SeoTab = 'pipeline' | 'opportunities' | 'calendar'
type Editor = { kind: 'opportunity'; item: SeoOpportunity | null } | { kind: 'content'; item: ContentPiece | null } | null

const emptyOpportunity: SeoOpportunityInput = {
  query: '', targetPage: '', evidence: '', hypothesis: '', status: 'detected', baselineImpressions: 0, baselineClicks: 0,
}
const emptyContent: ContentPieceInput = {
  seoOpportunityId: null, title: '', contentType: 'new', primaryKeyword: '', searchIntent: 'informational',
  hypothesis: '', baselineSummary: '', objective: '', owner: 'Carlos', brief: '', draftMarkdown: '',
  metaTitle: '', metaDescription: '', scheduledFor: null,
}

const statusLabels: Record<string, string> = {
  brief: 'Brief', draft: 'Borrador', pending_review: 'En revisión', approved: 'Aprobada',
  scheduled: 'Programada', sent_draft: 'Borrador enviado', published: 'Publicada', measured: 'Medida', discarded: 'Descartada',
  detected: 'Detectada', selected: 'Seleccionada', converted: 'Convertida', dismissed: 'Descartada',
}
const sourceLabels: Record<string, string> = { simulated: 'Simulada', manual: 'Manual', google_search_console: 'Google Search Console' }

export function SeoPage({ project }: { project: Project | null }) {
  const [tab, setTab] = useState<SeoTab>('pipeline')
  const [opportunities, setOpportunities] = useState<SeoOpportunity[]>([])
  const [content, setContent] = useState<ContentPiece[]>([])
  const [summary, setSummary] = useState<SeoSummary | null>(null)
  const [editor, setEditor] = useState<Editor>(null)
  const [opportunityForm, setOpportunityForm] = useState<SeoOpportunityInput>(emptyOpportunity)
  const [contentForm, setContentForm] = useState<ContentPieceInput>(emptyContent)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [workflowNote, setWorkflowNote] = useState('')
  const [scheduleDate, setScheduleDate] = useState('')
  const [measurement, setMeasurement] = useState({ impressions: 0, clicks: 0, notes: '' })

  const loadSeo = useCallback(async () => {
    if (!project) return
    setLoading(true); setError('')
    try {
      const [opportunityItems, contentItems, summaryData] = await Promise.all([
        api.getSeoOpportunities(project.id), api.getContentPieces(project.id), api.getSeoSummary(project.id),
      ])
      setOpportunities(opportunityItems); setContent(contentItems); setSummary(summaryData)
      setEditor((current) => {
        if (current?.kind !== 'content' || !current.item) return current
        const refreshed = contentItems.find((item) => item.id === current.item?.id)
        return refreshed ? { kind: 'content', item: refreshed } : null
      })
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No fue posible cargar SEO y contenido.') }
    finally { setLoading(false) }
  }, [project])

  useEffect(() => { void loadSeo() }, [loadSeo])

  if (!project) {
    return <div className="page module-page"><section className="page-heading"><div><div className="eyebrow"><span /> Fase 4</div><h1>SEO y contenido</h1><p>Oportunidades, revisión y calendario editorial.</p></div></section><div className="project-required"><Search size={30} /><h2>Selecciona un proyecto</h2><p>El flujo editorial y su historial se mantienen aislados dentro de cada proyecto.</p></div></div>
  }
  const activeProject = project

  function openOpportunity(item: SeoOpportunity | null) {
    setOpportunityForm(item ? {
      query: item.query, targetPage: item.targetPage, evidence: item.evidence, hypothesis: item.hypothesis,
      status: item.status, baselineImpressions: item.baselineImpressions, baselineClicks: item.baselineClicks,
    } : emptyOpportunity)
    setEditor({ kind: 'opportunity', item }); setError('')
  }

  function openContent(item: ContentPiece | null, opportunity?: SeoOpportunity) {
    setContentForm(item ? toContentInput(item) : opportunity ? {
      ...emptyContent, seoOpportunityId: opportunity.id, primaryKeyword: opportunity.query,
      hypothesis: opportunity.hypothesis,
      baselineSummary: `${opportunity.baselineImpressions.toLocaleString('es-GT')} impresiones y ${opportunity.baselineClicks.toLocaleString('es-GT')} clics. ${opportunity.evidence}`,
    } : emptyContent)
    setWorkflowNote(''); setScheduleDate(item?.scheduledFor?.slice(0, 16) ?? '')
    setMeasurement({ impressions: item?.resultImpressions ?? 0, clicks: item?.resultClicks ?? 0, notes: item?.resultNotes ?? '' })
    setEditor({ kind: 'content', item }); setError('')
  }

  async function saveOpportunity(event: FormEvent) {
    event.preventDefault(); if (!editor || editor.kind !== 'opportunity') return
    setSaving(true); setError('')
    try {
      if (editor.item) await api.updateSeoOpportunity(activeProject.id, editor.item.id, opportunityForm)
      else await api.createSeoOpportunity(activeProject.id, opportunityForm)
      await loadSeo(); setEditor(null)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo guardar la oportunidad.') }
    finally { setSaving(false) }
  }

  async function saveContent(event: FormEvent) {
    event.preventDefault(); if (!editor || editor.kind !== 'content') return
    setSaving(true); setError('')
    try {
      const payload = { ...contentForm, scheduledFor: contentForm.scheduledFor || null }
      if (editor.item) await api.updateContentPiece(activeProject.id, editor.item.id, payload)
      else await api.createContentPiece(activeProject.id, payload)
      await loadSeo(); setEditor(null)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo guardar la pieza.') }
    finally { setSaving(false) }
  }

  async function removeEditorItem() {
    if (!editor?.item) return
    const label = editor.kind === 'opportunity' ? 'la oportunidad' : 'la pieza editorial'
    if (!window.confirm(`¿Eliminar ${label}? Esta acción no se puede deshacer.`)) return
    setSaving(true)
    try {
      if (editor.kind === 'opportunity') await api.deleteSeoOpportunity(activeProject.id, editor.item.id)
      else await api.deleteContentPiece(activeProject.id, editor.item.id)
      await loadSeo(); setEditor(null)
    } catch (reason) { setError(reason instanceof Error ? reason.message : `No se pudo eliminar ${label}.`) }
    finally { setSaving(false) }
  }

  async function transition(target: string) {
    if (editor?.kind !== 'content' || !editor.item) return
    setSaving(true); setError('')
    try {
      await api.transitionContentPiece(activeProject.id, editor.item.id, target, workflowNote, target === 'scheduled' ? scheduleDate || null : null)
      setWorkflowNote(''); await loadSeo()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo cambiar el estado.') }
    finally { setSaving(false) }
  }

  async function sendWordPressDraft() {
    if (editor?.kind !== 'content' || !editor.item) return
    setSaving(true); setError('')
    try { await api.sendWordPressDraft(activeProject.id, editor.item.id); await loadSeo() }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo crear el borrador en WordPress.') }
    finally { setSaving(false) }
  }

  async function saveMeasurement(event: FormEvent) {
    event.preventDefault(); if (editor?.kind !== 'content' || !editor.item) return
    setSaving(true); setError('')
    try { await api.measureContentPiece(activeProject.id, editor.item.id, measurement.impressions, measurement.clicks, measurement.notes); await loadSeo() }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo guardar la medición.') }
    finally { setSaving(false) }
  }

  return (
    <div className="page seo-page">
      <section className="page-heading"><div><div className="eyebrow"><span /> Fase 7D · {project.name}</div><h1>SEO y contenido</h1><p>De la oportunidad al borrador real de WordPress, con publicación siempre manual.</p></div><span className="demo-pill"><Sparkles size={14} /> WordPress en modo draft</span></section>
      <div className="seo-disclaimer"><Check size={16} /><span>El sistema solo puede crear borradores en WordPress. Revisarlos y publicarlos seguirá siendo una decisión humana dentro de WordPress.</span></div>
      <nav className="module-tabs" aria-label="Secciones de SEO">
        {([['pipeline', 'Flujo editorial'], ['opportunities', 'Oportunidades'], ['calendar', 'Calendario']] as const).map(([value, label]) => <button key={value} className={tab === value ? 'active' : ''} onClick={() => setTab(value)}>{label}</button>)}
      </nav>
      {error && <p className="global-error marketing-error" role="alert">{error}</p>}
      {loading && <p className="loading-copy">Cargando flujo editorial…</p>}
      {!loading && tab === 'pipeline' && <PipelineView summary={summary} items={content} onAdd={() => openContent(null)} onEdit={(item) => openContent(item)} />}
      {!loading && tab === 'opportunities' && <OpportunitiesView items={opportunities} onAdd={() => openOpportunity(null)} onEdit={openOpportunity} onCreateContent={(item) => openContent(null, item)} />}
      {!loading && tab === 'calendar' && <EditorialCalendar items={content} onEdit={(item) => openContent(item)} />}

      {editor?.kind === 'opportunity' && <EditorDrawer title={editor.item ? 'Editar oportunidad' : 'Nueva oportunidad'} onClose={() => setEditor(null)}>
        <form className="project-form" onSubmit={saveOpportunity}>
          <FormField label="Consulta o tema"><input required maxLength={220} value={opportunityForm.query} onChange={(event) => setOpportunityForm({ ...opportunityForm, query: event.target.value })} /></FormField>
          <FormField label="Página objetivo"><input maxLength={500} value={opportunityForm.targetPage} onChange={(event) => setOpportunityForm({ ...opportunityForm, targetPage: event.target.value })} placeholder="/blog/tema" /></FormField>
          <FormField label="Evidencia"><textarea required rows={3} maxLength={1000} value={opportunityForm.evidence} onChange={(event) => setOpportunityForm({ ...opportunityForm, evidence: event.target.value })} /></FormField>
          <FormField label="Hipótesis"><textarea required rows={3} maxLength={1000} value={opportunityForm.hypothesis} onChange={(event) => setOpportunityForm({ ...opportunityForm, hypothesis: event.target.value })} /></FormField>
          <div className="form-row form-row--three"><NumberField label="Impresiones base" value={opportunityForm.baselineImpressions} onChange={(value) => setOpportunityForm({ ...opportunityForm, baselineImpressions: value })} /><NumberField label="Clics base" value={opportunityForm.baselineClicks} onChange={(value) => setOpportunityForm({ ...opportunityForm, baselineClicks: value })} /><FormField label="Estado"><select value={opportunityForm.status} onChange={(event) => setOpportunityForm({ ...opportunityForm, status: event.target.value })}><option value="detected">Detectada</option><option value="selected">Seleccionada</option><option value="converted">Convertida</option><option value="dismissed">Descartada</option></select></FormField></div>
          {error && <p className="form-error">{error}</p>}<div className="form-actions">{editor.item && <button type="button" className="danger-button" onClick={removeEditorItem}><Trash2 size={14} /> Eliminar</button>}<button className="primary-button" disabled={saving}><Save size={15} /> Guardar oportunidad</button></div>
        </form>
      </EditorDrawer>}

      {editor?.kind === 'content' && <EditorDrawer title={editor.item ? editor.item.title : 'Nueva pieza editorial'} onClose={() => setEditor(null)} wide>
        <div className="content-editor-layout"><form className="project-form content-form" onSubmit={saveContent}>
          <div className="form-row"><FormField label="Título"><input required maxLength={220} value={contentForm.title} onChange={(event) => setContentForm({ ...contentForm, title: event.target.value })} /></FormField><FormField label="Responsable"><input required maxLength={120} value={contentForm.owner} onChange={(event) => setContentForm({ ...contentForm, owner: event.target.value })} /></FormField></div>
          <div className="form-row form-row--three"><FormField label="Tipo"><select value={contentForm.contentType} onChange={(event) => setContentForm({ ...contentForm, contentType: event.target.value })}><option value="new">Nuevo contenido</option><option value="update">Actualizar</option><option value="merge">Fusionar</option></select></FormField><FormField label="Intención"><select value={contentForm.searchIntent} onChange={(event) => setContentForm({ ...contentForm, searchIntent: event.target.value })}><option value="informational">Informativa</option><option value="commercial">Comercial</option><option value="transactional">Transaccional</option><option value="navigational">Navegacional</option></select></FormField><FormField label="Oportunidad"><select value={contentForm.seoOpportunityId ?? ''} onChange={(event) => setContentForm({ ...contentForm, seoOpportunityId: event.target.value || null })}><option value="">Sin asociación</option>{opportunities.map((item) => <option value={item.id} key={item.id}>{item.query}</option>)}</select></FormField></div>
          <FormField label="Palabra clave principal"><input required value={contentForm.primaryKeyword} onChange={(event) => setContentForm({ ...contentForm, primaryKeyword: event.target.value })} /></FormField>
          <FormField label="Hipótesis"><textarea required rows={2} maxLength={1200} value={contentForm.hypothesis} onChange={(event) => setContentForm({ ...contentForm, hypothesis: event.target.value })} /></FormField>
          <FormField label="Línea base"><textarea required rows={2} maxLength={1200} value={contentForm.baselineSummary} onChange={(event) => setContentForm({ ...contentForm, baselineSummary: event.target.value })} /></FormField>
          <FormField label="Objetivo"><textarea required rows={2} maxLength={800} value={contentForm.objective} onChange={(event) => setContentForm({ ...contentForm, objective: event.target.value })} /></FormField>
          <FormField label="Brief"><textarea rows={5} maxLength={10000} value={contentForm.brief} onChange={(event) => setContentForm({ ...contentForm, brief: event.target.value })} placeholder="Estructura, preguntas, fuentes y CTA…" /></FormField>
          <FormField label="Borrador en Markdown"><textarea className="draft-editor" rows={10} maxLength={60000} value={contentForm.draftMarkdown} onChange={(event) => setContentForm({ ...contentForm, draftMarkdown: event.target.value })} placeholder="# Título…" /></FormField>
          <div className="form-row"><FormField label="Meta title"><input maxLength={180} value={contentForm.metaTitle} onChange={(event) => setContentForm({ ...contentForm, metaTitle: event.target.value })} /></FormField><FormField label="Meta description"><input maxLength={320} value={contentForm.metaDescription} onChange={(event) => setContentForm({ ...contentForm, metaDescription: event.target.value })} /></FormField></div>
          {error && <p className="form-error">{error}</p>}<div className="form-actions">{editor.item && <button type="button" className="danger-button" onClick={removeEditorItem}><Trash2 size={14} /> Eliminar</button>}<button className="primary-button" disabled={saving}><Save size={15} /> Guardar contenido</button></div>
        </form>{editor.item && <WorkflowPanel item={editor.item} note={workflowNote} setNote={setWorkflowNote} scheduleDate={scheduleDate} setScheduleDate={setScheduleDate} measurement={measurement} setMeasurement={setMeasurement} saving={saving} transition={transition} sendWordPressDraft={sendWordPressDraft} saveMeasurement={saveMeasurement} />}</div>
      </EditorDrawer>}
    </div>
  )
}

function PipelineView({ summary, items, onAdd, onEdit }: { summary: SeoSummary | null; items: ContentPiece[]; onAdd: () => void; onEdit: (item: ContentPiece) => void }) {
  const columns = ['brief', 'draft', 'pending_review', 'approved', 'scheduled', 'sent_draft', 'published', 'measured']
  return <><section className="seo-kpis"><Metric label="Oportunidades" value={summary?.opportunities ?? 0} detail="detectadas o registradas" /><Metric label="Piezas activas" value={summary?.activePieces ?? 0} detail="en el flujo editorial" /><Metric label="Por revisar" value={summary?.pendingReview ?? 0} detail="requieren decisión humana" /><Metric label="Medidas" value={summary?.measured ?? 0} detail="con resultado registrado" /></section><div className="section-toolbar"><div><h2>Flujo editorial</h2><p>Cada cambio queda registrado en el historial.</p></div><button className="primary-button" onClick={onAdd}><Plus size={15} /> Nueva pieza</button></div><section className="editorial-board">{columns.map((status) => <div className="editorial-column" key={status}><header><span>{statusLabels[status]}</span><b>{items.filter((item) => item.status === status).length}</b></header>{items.filter((item) => item.status === status).map((item) => <button className="content-ticket" key={item.id} onClick={() => onEdit(item)}><small>{item.contentType} · {item.owner}</small><strong>{item.title}</strong><span>{item.primaryKeyword}</span>{item.isDemoData && <em>SIMULADA</em>}</button>)}</div>)}</section></>
}

function OpportunitiesView({ items, onAdd, onEdit, onCreateContent }: { items: SeoOpportunity[]; onAdd: () => void; onEdit: (item: SeoOpportunity) => void; onCreateContent: (item: SeoOpportunity) => void }) {
  return <><div className="section-toolbar"><div><h2>Oportunidades SEO</h2><p>Evidencia, hipótesis y línea base antes de escribir.</p></div><button className="primary-button" onClick={onAdd}><Plus size={15} /> Nueva oportunidad</button></div><div className="seo-disclaimer"><Lightbulb size={16} /><span>Si Search Console aún no tiene datos, crea una oportunidad manual y déjala en estado <strong>Seleccionada</strong>. El flujo automático de Gemini podrá usarla, comprobar duplicados y crear únicamente un borrador de WordPress.</span></div><section className="opportunity-grid">{items.map((item) => { const ctr = item.baselineImpressions ? (item.baselineClicks / item.baselineImpressions) * 100 : 0; return <article className="opportunity-card" key={item.id}><header><span><Lightbulb size={17} /></span><div><small>{sourceLabels[item.dataSource] ?? item.dataSource}{item.isDemoData ? ' · DATOS DE PRUEBA' : ''}</small><h3>{item.query}</h3></div><span className={`seo-status seo-status--${item.status}`}>{statusLabels[item.status]}</span></header><p>{item.hypothesis}</p><div className="opportunity-metrics"><span><strong>{item.baselineImpressions.toLocaleString('es-GT')}</strong> impresiones</span><span><strong>{item.baselineClicks}</strong> clics</span><span><strong>{ctr.toFixed(1)}%</strong> CTR</span></div><footer><button className="secondary-button" onClick={() => onEdit(item)}><Pencil size={13} /> Editar</button><button className="primary-button" onClick={() => onCreateContent(item)}>Crear pieza <ArrowRight size={13} /></button></footer></article>})}{items.length === 0 && <EmptyState text="Aún no hay oportunidades SEO en este proyecto." />}</section></>
}

function EditorialCalendar({ items, onEdit }: { items: ContentPiece[]; onEdit: (item: ContentPiece) => void }) {
  const scheduled = useMemo(() => items.filter((item) => item.scheduledFor).sort((a, b) => new Date(a.scheduledFor!).getTime() - new Date(b.scheduledFor!).getTime()), [items])
  return <section className="calendar-panel panel"><header><span><CalendarDays size={20} /></span><div><h2>Calendario editorial</h2><p>Fechas previstas; no implica publicación automática.</p></div></header><div className="calendar-list">{scheduled.map((item) => { const date = new Date(item.scheduledFor!); return <button key={item.id} onClick={() => onEdit(item)}><time><strong>{date.toLocaleDateString('es-GT', { day: '2-digit' })}</strong><span>{date.toLocaleDateString('es-GT', { month: 'short' })}</span></time><div><small>{statusLabels[item.status]} · {item.owner}</small><strong>{item.title}</strong><span>{item.primaryKeyword}</span></div><ArrowRight size={16} /></button>})}{scheduled.length === 0 && <EmptyState text="No hay piezas programadas todavía." />}</div></section>
}

function WorkflowPanel({ item, note, setNote, scheduleDate, setScheduleDate, measurement, setMeasurement, saving, transition, sendWordPressDraft, saveMeasurement }: {
  item: ContentPiece; note: string; setNote: (value: string) => void; scheduleDate: string; setScheduleDate: (value: string) => void;
  measurement: { impressions: number; clicks: number; notes: string }; setMeasurement: (value: { impressions: number; clicks: number; notes: string }) => void;
  saving: boolean; transition: (target: string) => Promise<void>; sendWordPressDraft: () => Promise<void>; saveMeasurement: (event: FormEvent) => Promise<void>;
}) {
  const actions: Record<string, { target: string; label: string }[]> = {
    brief: [{ target: 'draft', label: 'Pasar a borrador' }], draft: [{ target: 'pending_review', label: 'Solicitar revisión' }],
    pending_review: [{ target: 'draft', label: 'Solicitar cambios' }, { target: 'approved', label: 'Aprobar' }],
    sent_draft: [{ target: 'published', label: 'Marcar publicada' }], discarded: [{ target: 'brief', label: 'Restaurar como brief' }],
  }
  const canMeasure = item.status === 'sent_draft' || item.status === 'published'
  const canDiscard = ['brief', 'draft', 'pending_review'].includes(item.status)
  const canSend = ['draft', 'pending_review', 'approved', 'scheduled'].includes(item.status) && !item.wordPressPostId
  return <aside className="workflow-panel"><div className="workflow-current"><small>ESTADO ACTUAL</small><strong>{statusLabels[item.status]}</strong><span>{item.isDemoData ? 'Datos simulados' : 'Registro del proyecto'}</span></div>{item.status === 'approved' && <div className="workflow-block"><h3>Programar</h3><input type="datetime-local" value={scheduleDate} onChange={(event) => setScheduleDate(event.target.value)} /><button className="secondary-button" disabled={!scheduleDate || saving} onClick={() => transition('scheduled')}><CalendarDays size={14} /> Programar</button></div>}{canSend && <div className="workflow-block workflow-simulation"><h3>WordPress</h3><p>Crea una entrada real con estado <code>draft</code>. Nunca la publica.</p><button className="primary-button" disabled={saving || !item.draftMarkdown} onClick={sendWordPressDraft}><Send size={14} /> Crear borrador en WordPress</button></div>}{actions[item.status]?.length > 0 && <div className="workflow-block"><h3>Decisión</h3><textarea rows={2} placeholder="Nota opcional para el historial" value={note} onChange={(event) => setNote(event.target.value)} />{actions[item.status].map((action) => <button key={action.target} className="secondary-button" disabled={saving} onClick={() => transition(action.target)}><ClipboardCheck size={14} /> {action.label}</button>)}{canDiscard && <button className="danger-button" disabled={saving} onClick={() => transition('discarded')}><X size={14} /> Descartar</button>}</div>}{canMeasure && <form className="workflow-block" onSubmit={saveMeasurement}><h3>Registrar medición</h3><NumberField label="Impresiones" value={measurement.impressions} onChange={(value) => setMeasurement({ ...measurement, impressions: value })} /><NumberField label="Clics" value={measurement.clicks} onChange={(value) => setMeasurement({ ...measurement, clicks: value })} /><FormField label="Notas"><textarea rows={3} value={measurement.notes} onChange={(event) => setMeasurement({ ...measurement, notes: event.target.value })} /></FormField><button className="primary-button" disabled={saving}><TrendingUp size={14} /> Guardar resultado</button></form>}{item.wordPressEditUrl && <div className="simulated-link"><small>BORRADOR REAL · WORDPRESS #{item.wordPressPostId}</small><a href={item.wordPressEditUrl} target="_blank" rel="noreferrer">Abrir para revisar y publicar manualmente</a></div>}<div className="history-block"><h3><History size={15} /> Historial</h3>{item.history.map((entry) => <div key={entry.id}><span><CircleDot size={11} /></span><p><strong>{statusLabels[entry.toStatus] ?? entry.toStatus}</strong><small>{entry.note || 'Sin nota'} · {new Date(entry.changedAt).toLocaleString('es-GT')}</small></p></div>)}</div></aside>
}

function toContentInput(item: ContentPiece): ContentPieceInput { return { seoOpportunityId: item.seoOpportunityId, title: item.title, contentType: item.contentType, primaryKeyword: item.primaryKeyword, searchIntent: item.searchIntent, hypothesis: item.hypothesis, baselineSummary: item.baselineSummary, objective: item.objective, owner: item.owner, brief: item.brief, draftMarkdown: item.draftMarkdown, metaTitle: item.metaTitle, metaDescription: item.metaDescription, scheduledFor: item.scheduledFor?.slice(0, 16) ?? null } }
function EditorDrawer({ title, onClose, children, wide = false }: { title: string; onClose: () => void; children: React.ReactNode; wide?: boolean }) { return <div className="drawer-overlay"><section className={`editor-drawer ${wide ? 'editor-drawer--wide' : ''}`} role="dialog" aria-modal="true" aria-label={title}><header><div><small>SEO Y CONTENIDO</small><h2>{title}</h2></div><button className="icon-button" onClick={onClose} aria-label="Cerrar editor"><X size={18} /></button></header>{children}</section></div> }
function FormField({ label, children }: { label: string; children: React.ReactNode }) { return <label className="form-field"><span>{label}</span>{children}</label> }
function NumberField({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) { return <FormField label={label}><input type="number" min="0" value={value} onChange={(event) => onChange(Number(event.target.value))} /></FormField> }
function Metric({ label, value, detail }: { label: string; value: string | number; detail: string }) { return <article><span>{label}</span><strong>{value}</strong><small>{detail}</small></article> }
function EmptyState({ text }: { text: string }) { return <div className="marketing-empty"><FileText size={20} /><p>{text}</p></div> }
