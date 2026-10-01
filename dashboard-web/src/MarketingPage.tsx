import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  BarChart3, Check, Clipboard, FileUp, Link2, Megaphone, Pencil, Plus, Save,
  Send, Sparkles, Trash2, TrendingUp, X,
} from 'lucide-react'
import {
  api, type Campaign, type CampaignInput, type MarketingEventInput, type MarketingSummary,
  type Project, type SocialPost, type SocialPostInput,
} from './api'
import { buildUtmUrl, parseMarketingImport } from './marketing'

type MarketingTab = 'summary' | 'campaigns' | 'posts' | 'import'
type Editor = { kind: 'campaign'; item: Campaign | null } | { kind: 'post'; item: SocialPost | null } | null

const emptyCampaign: CampaignInput = {
  name: '', objective: '', status: 'draft', utmCampaign: '', startDate: null, endDate: null,
}
const emptyPost: SocialPostInput = {
  campaignId: null, platform: 'instagram', topic: '', format: 'post', status: 'draft', externalUrl: '',
  publishedAt: null, impressions: 0, engagements: 0, clicks: 0,
}

const stageLabels: Record<string, string> = { visit: 'Visitas', interest: 'Interés', contact: 'Contactos', quote: 'Cotizaciones' }
const sourceLabels: Record<string, string> = { instagram: 'Instagram', x: 'X', organic: 'Orgánico', direct: 'Directo' }
const dataLabels: Record<string, string> = { simulated: 'Simulado', manual: 'Manual', import_csv: 'CSV', import_json: 'JSON' }

export function MarketingPage({ project }: { project: Project | null }) {
  const [tab, setTab] = useState<MarketingTab>('summary')
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [posts, setPosts] = useState<SocialPost[]>([])
  const [summary, setSummary] = useState<MarketingSummary | null>(null)
  const [editor, setEditor] = useState<Editor>(null)
  const [campaignForm, setCampaignForm] = useState<CampaignInput>(emptyCampaign)
  const [postForm, setPostForm] = useState<SocialPostInput>(emptyPost)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadMarketing = useCallback(async () => {
    if (!project) return
    setLoading(true)
    setError('')
    try {
      const [campaignItems, postItems, summaryData] = await Promise.all([
        api.getCampaigns(project.id), api.getSocialPosts(project.id), api.getMarketingSummary(project.id),
      ])
      setCampaigns(campaignItems)
      setPosts(postItems)
      setSummary(summaryData)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo cargar el módulo de marketing.')
    } finally {
      setLoading(false)
    }
  }, [project])

  useEffect(() => { void loadMarketing() }, [loadMarketing])

  if (!project) {
    return (
      <div className="page module-page">
        <section className="page-heading"><div><div className="eyebrow"><span /> Fase 3</div><h1>Marketing</h1><p>Campañas, publicaciones y atribución responsable.</p></div></section>
        <div className="project-required"><TrendingUp size={30} /><h2>Selecciona un proyecto</h2><p>El marketing se consulta por proyecto para que campañas y métricas nunca se mezclen.</p></div>
      </div>
    )
  }
  const activeProject = project

  function openCampaign(item: Campaign | null) {
    setCampaignForm(item ? {
      name: item.name, objective: item.objective, status: item.status, utmCampaign: item.utmCampaign,
      startDate: item.startDate, endDate: item.endDate,
    } : emptyCampaign)
    setEditor({ kind: 'campaign', item })
    setError('')
  }

  function openPost(item: SocialPost | null) {
    setPostForm(item ? {
      campaignId: item.campaignId, platform: item.platform, topic: item.topic, format: item.format,
      status: item.status, externalUrl: item.externalUrl,
      publishedAt: item.publishedAt ? item.publishedAt.slice(0, 16) : null,
      impressions: item.impressions, engagements: item.engagements, clicks: item.clicks,
    } : emptyPost)
    setEditor({ kind: 'post', item })
    setError('')
  }

  async function saveCampaign(event: FormEvent) {
    event.preventDefault(); if (!editor || editor.kind !== 'campaign') return
    setSaving(true); setError('')
    try {
      if (editor.item) await api.updateCampaign(activeProject.id, editor.item.id, campaignForm)
      else await api.createCampaign(activeProject.id, campaignForm)
      await loadMarketing(); setEditor(null)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo guardar la campaña.') }
    finally { setSaving(false) }
  }

  async function savePost(event: FormEvent) {
    event.preventDefault(); if (!editor || editor.kind !== 'post') return
    setSaving(true); setError('')
    try {
      const payload = { ...postForm, publishedAt: postForm.publishedAt || null }
      if (editor.item) await api.updateSocialPost(activeProject.id, editor.item.id, payload)
      else await api.createSocialPost(activeProject.id, payload)
      await loadMarketing(); setEditor(null)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo guardar la publicación.') }
    finally { setSaving(false) }
  }

  async function removeEditorItem() {
    if (!editor?.item) return
    const label = editor.kind === 'campaign' ? 'la campaña' : 'la publicación'
    if (!window.confirm(`¿Eliminar ${label}? Las métricas agregadas se conservarán sin esa asociación.`)) return
    setSaving(true)
    try {
      if (editor.kind === 'campaign') await api.deleteCampaign(activeProject.id, editor.item.id)
      else await api.deleteSocialPost(activeProject.id, editor.item.id)
      await loadMarketing(); setEditor(null)
    } catch (reason) { setError(reason instanceof Error ? reason.message : `No se pudo eliminar ${label}.`) }
    finally { setSaving(false) }
  }

  return (
    <div className="page marketing-page">
      <section className="page-heading">
        <div><div className="eyebrow"><span /> Fase 3 · {project.name}</div><h1>Marketing</h1><p>Campañas, contenido y señales agregadas del embudo.</p></div>
        <span className="demo-pill"><Sparkles size={14} /> Datos trazables por origen</span>
      </section>

      <div className="marketing-disclaimer"><Check size={16} /><span>Los UTM atribuyen el enlace usado; no identifican a una persona ni prueban que una campaña causó la conversión.</span></div>
      <nav className="module-tabs" aria-label="Secciones de marketing">
        {([['summary', 'Resumen'], ['campaigns', 'Campañas'], ['posts', 'Publicaciones'], ['import', 'Importar datos']] as const).map(([value, label]) =>
          <button key={value} className={tab === value ? 'active' : ''} onClick={() => setTab(value)}>{label}</button>)}
      </nav>
      {error && <p className="global-error marketing-error" role="alert">{error}</p>}
      {loading && <p className="loading-copy">Cargando datos del proyecto…</p>}
      {!loading && tab === 'summary' && <MarketingSummaryView summary={summary} campaigns={campaigns} project={project} />}
      {!loading && tab === 'campaigns' && <CampaignsView items={campaigns} onAdd={() => openCampaign(null)} onEdit={openCampaign} />}
      {!loading && tab === 'posts' && <PostsView items={posts} campaigns={campaigns} onAdd={() => openPost(null)} onEdit={openPost} />}
      {!loading && tab === 'import' && <ImportView projectId={project.id} onImported={loadMarketing} />}

      {editor?.kind === 'campaign' && (
        <EditorDrawer title={editor.item ? 'Editar campaña' : 'Nueva campaña'} onClose={() => setEditor(null)}>
          <form className="project-form" onSubmit={saveCampaign}>
            <FormField label="Nombre"><input required maxLength={140} value={campaignForm.name} onChange={(event) => setCampaignForm({ ...campaignForm, name: event.target.value })} /></FormField>
            <FormField label="Objetivo"><textarea required maxLength={500} rows={3} value={campaignForm.objective} onChange={(event) => setCampaignForm({ ...campaignForm, objective: event.target.value })} /></FormField>
            <div className="form-row"><FormField label="Estado"><select value={campaignForm.status} onChange={(event) => setCampaignForm({ ...campaignForm, status: event.target.value })}><option value="draft">Borrador</option><option value="active">Activa</option><option value="paused">Pausada</option><option value="completed">Completada</option></select></FormField><FormField label="utm_campaign"><input required maxLength={120} value={campaignForm.utmCampaign} onChange={(event) => setCampaignForm({ ...campaignForm, utmCampaign: event.target.value })} placeholder="servicios-creativos" /></FormField></div>
            <div className="form-row"><FormField label="Inicio"><input type="date" value={campaignForm.startDate ?? ''} onChange={(event) => setCampaignForm({ ...campaignForm, startDate: event.target.value || null })} /></FormField><FormField label="Fin"><input type="date" value={campaignForm.endDate ?? ''} onChange={(event) => setCampaignForm({ ...campaignForm, endDate: event.target.value || null })} /></FormField></div>
            {error && <p className="form-error">{error}</p>}
            <div className="form-actions">{editor.item && <button type="button" className="danger-button" onClick={removeEditorItem}><Trash2 size={14} /> Eliminar</button>}<button className="primary-button" disabled={saving}><Save size={15} /> {saving ? 'Guardando…' : 'Guardar campaña'}</button></div>
          </form>
        </EditorDrawer>
      )}

      {editor?.kind === 'post' && (
        <EditorDrawer title={editor.item ? 'Editar publicación' : 'Nueva publicación'} onClose={() => setEditor(null)}>
          <form className="project-form" onSubmit={savePost}>
            <div className="form-row"><FormField label="Plataforma"><select value={postForm.platform} onChange={(event) => setPostForm({ ...postForm, platform: event.target.value })}><option value="instagram">Instagram</option><option value="x">X</option><option value="linkedin">LinkedIn</option><option value="other">Otra</option></select></FormField><FormField label="Estado"><select value={postForm.status} onChange={(event) => setPostForm({ ...postForm, status: event.target.value })}><option value="draft">Borrador</option><option value="scheduled">Programada</option><option value="published">Publicada</option><option value="archived">Archivada</option></select></FormField></div>
            <FormField label="Tema"><input required maxLength={220} value={postForm.topic} onChange={(event) => setPostForm({ ...postForm, topic: event.target.value })} /></FormField>
            <div className="form-row"><FormField label="Campaña"><select value={postForm.campaignId ?? ''} onChange={(event) => setPostForm({ ...postForm, campaignId: event.target.value || null })}><option value="">Sin campaña</option>{campaigns.map((item) => <option value={item.id} key={item.id}>{item.name}</option>)}</select></FormField><FormField label="Formato"><input required value={postForm.format} onChange={(event) => setPostForm({ ...postForm, format: event.target.value })} /></FormField></div>
            <FormField label="URL externa"><input type="url" maxLength={1000} value={postForm.externalUrl} onChange={(event) => setPostForm({ ...postForm, externalUrl: event.target.value })} placeholder="https://…" /></FormField>
            <FormField label="Fecha de publicación"><input type="datetime-local" value={postForm.publishedAt ?? ''} onChange={(event) => setPostForm({ ...postForm, publishedAt: event.target.value || null })} /></FormField>
            <div className="form-row form-row--three"><NumberField label="Impresiones" value={postForm.impressions} onChange={(value) => setPostForm({ ...postForm, impressions: value })} /><NumberField label="Interacciones" value={postForm.engagements} onChange={(value) => setPostForm({ ...postForm, engagements: value })} /><NumberField label="Clics" value={postForm.clicks} onChange={(value) => setPostForm({ ...postForm, clicks: value })} /></div>
            {error && <p className="form-error">{error}</p>}
            <div className="form-actions">{editor.item && <button type="button" className="danger-button" onClick={removeEditorItem}><Trash2 size={14} /> Eliminar</button>}<button className="primary-button" disabled={saving}><Save size={15} /> {saving ? 'Guardando…' : 'Guardar publicación'}</button></div>
          </form>
        </EditorDrawer>
      )}
    </div>
  )
}

function MarketingSummaryView({ summary, campaigns, project }: { summary: MarketingSummary | null; campaigns: Campaign[]; project: Project }) {
  if (!summary) return <p className="empty-copy">No hay resumen disponible.</p>
  const visits = summary.funnel.find((item) => item.stage === 'visit')?.count ?? 0
  const contacts = summary.funnel.find((item) => item.stage === 'contact')?.count ?? 0
  const maxFunnel = Math.max(1, ...summary.funnel.map((item) => item.count))
  return (
    <>
      <section className="marketing-kpis">
        <Metric label="Campañas" value={summary.campaigns} detail={`${summary.posts} publicaciones`} />
        <Metric label="Alcance registrado" value={summary.impressions.toLocaleString('es-GT')} detail={`${summary.engagements.toLocaleString('es-GT')} interacciones`} />
        <Metric label="Clics declarados" value={summary.clicks.toLocaleString('es-GT')} detail="manuales o simulados" />
        <Metric label="Contacto / visita" value={visits ? `${((contacts / visits) * 100).toFixed(1)}%` : '—'} detail={`${contacts} de ${visits}`} />
      </section>
      <section className="marketing-grid">
        <article className="panel marketing-panel"><PanelTitle icon={BarChart3} title="Embudo agregado" detail="Conteos sin identidad personal" />
          <div className="marketing-funnel">{summary.funnel.map((item) => <div key={item.stage}><span>{stageLabels[item.stage] ?? item.stage}</span><i><b style={{ width: `${Math.max(3, (item.count / maxFunnel) * 100)}%` }} /></i><strong>{item.count.toLocaleString('es-GT')}</strong></div>)}</div>
        </article>
        <article className="panel marketing-panel"><PanelTitle icon={TrendingUp} title="Fuentes" detail="Señales registradas, no causalidad" />
          <div className="source-list">{summary.sources.map((item) => <div key={item.source}><span>{sourceLabels[item.source] ?? item.source}</span><strong>{item.visits.toLocaleString('es-GT')} visitas</strong><small>{item.contacts} contactos</small></div>)}{summary.sources.length === 0 && <p className="empty-copy">Sin datos importados.</p>}</div>
        </article>
      </section>
      <UtmBuilder project={project} campaigns={campaigns} />
      {summary.containsDemoData && <p className="simulation-note">Parte de este resumen usa datos simulados identificados como tales.</p>}
    </>
  )
}

function UtmBuilder({ project, campaigns }: { project: Project; campaigns: Campaign[] }) {
  const [baseUrl, setBaseUrl] = useState(project.domain.includes('.') ? `https://${project.domain}/` : '')
  const [source, setSource] = useState('instagram')
  const [medium, setMedium] = useState('social')
  const [campaign, setCampaign] = useState(campaigns[0]?.utmCampaign ?? '')
  const [content, setContent] = useState('')
  const [copied, setCopied] = useState(false)
  const generated = useMemo(() => { try { return baseUrl && source && medium && campaign ? buildUtmUrl(baseUrl, source, medium, campaign, content) : '' } catch { return '' } }, [baseUrl, source, medium, campaign, content])
  async function copy() { if (!generated) return; await navigator.clipboard.writeText(generated); setCopied(true); window.setTimeout(() => setCopied(false), 1400) }
  return (
    <section className="panel utm-builder"><PanelTitle icon={Link2} title="Generador de enlaces UTM" detail="Construye enlaces; no realiza seguimiento por sí solo" />
      <div className="utm-fields"><FormField label="URL destino"><input type="url" value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} placeholder="https://ejemplo.com/landing" /></FormField><FormField label="Fuente"><input value={source} onChange={(event) => setSource(event.target.value)} /></FormField><FormField label="Medio"><input value={medium} onChange={(event) => setMedium(event.target.value)} /></FormField><FormField label="Campaña"><select value={campaign} onChange={(event) => setCampaign(event.target.value)}><option value="">Seleccionar</option>{campaigns.map((item) => <option key={item.id} value={item.utmCampaign}>{item.name}</option>)}</select></FormField><FormField label="Contenido (opcional)"><input value={content} onChange={(event) => setContent(event.target.value)} placeholder="carrusel-01" /></FormField></div>
      <div className="utm-output"><code>{generated || 'Completa una URL y una campaña válidas.'}</code><button className="secondary-button" disabled={!generated} onClick={copy}>{copied ? <Check size={14} /> : <Clipboard size={14} />} {copied ? 'Copiado' : 'Copiar'}</button></div>
    </section>
  )
}

function CampaignsView({ items, onAdd, onEdit }: { items: Campaign[]; onAdd: () => void; onEdit: (item: Campaign) => void }) {
  return <section><div className="section-toolbar"><div><h2>Campañas</h2><p>Objetivo, periodo y etiqueta UTM.</p></div><button className="primary-button" onClick={onAdd}><Plus size={15} /> Nueva campaña</button></div><div className="marketing-cards">{items.map((item) => <article key={item.id} className="marketing-card"><div className="marketing-card__icon"><Megaphone size={18} /></div><div><span className={`status-badge status-badge--${item.status}`}>{item.status}</span>{item.isDemoData && <span className="seed-badge">SIMULADA</span>}<h3>{item.name}</h3><p>{item.objective}</p><small>utm_campaign={item.utmCampaign}</small></div><button className="secondary-button" onClick={() => onEdit(item)}><Pencil size={13} /> Editar</button></article>)}{items.length === 0 && <EmptyState text="Aún no hay campañas para este proyecto." />}</div></section>
}

function PostsView({ items, campaigns, onAdd, onEdit }: { items: SocialPost[]; campaigns: Campaign[]; onAdd: () => void; onEdit: (item: SocialPost) => void }) {
  return <section><div className="section-toolbar"><div><h2>Publicaciones</h2><p>Métricas declaradas y asociación con campaña.</p></div><button className="primary-button" onClick={onAdd}><Plus size={15} /> Nueva publicación</button></div><div className="post-table"><div className="post-row post-row--head"><span>Contenido</span><span>Campaña</span><span>Impresiones</span><span>Clics</span><span>Origen</span><span /></div>{items.map((item) => <div className="post-row" key={item.id}><div><strong>{item.topic}</strong><small>{item.platform} · {item.format} · {item.status}</small></div><span>{campaigns.find((campaign) => campaign.id === item.campaignId)?.name ?? 'Sin campaña'}</span><strong>{item.impressions.toLocaleString('es-GT')}</strong><strong>{item.clicks.toLocaleString('es-GT')}</strong><span className={`data-source data-source--${item.dataSource}`}>{dataLabels[item.dataSource] ?? item.dataSource}</span><button className="icon-button" onClick={() => onEdit(item)} aria-label={`Editar ${item.topic}`}><Pencil size={14} /></button></div>)}{items.length === 0 && <EmptyState text="Aún no hay publicaciones para este proyecto." />}</div></section>
}

function ImportView({ projectId, onImported }: { projectId: string; onImported: () => Promise<void> }) {
  const sample = 'stage,source,medium,count,landingPath,occurredAt\nvisit,instagram,social,120,/servicios,2026-09-30\ncontact,instagram,social,6,/contacto,2026-09-30'
  const [format, setFormat] = useState<'csv' | 'json'>('csv')
  const [text, setText] = useState(sample)
  const [preview, setPreview] = useState<MarketingEventInput[]>([])
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  function analyze() { try { setPreview(parseMarketingImport(text, format)); setError(''); setMessage('') } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo leer el archivo.'); setPreview([]) } }
  async function readFile(file?: File) { if (!file) return; const nextFormat = file.name.toLowerCase().endsWith('.json') ? 'json' : 'csv'; setFormat(nextFormat); setText(await file.text()); setPreview([]); setMessage(''); setError('') }
  async function importEvents() { try { const result = await api.importMarketingEvents(projectId, preview); setMessage(`${result.imported} filas importadas · ${result.totalCount.toLocaleString('es-GT')} eventos agregados.`); setPreview([]); setError(''); await onImported() } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo importar.') } }
  return <section className="import-layout"><article className="panel import-panel"><PanelTitle icon={FileUp} title="Importar embudo" detail="CSV o JSON con conteos agregados" /><div className="import-actions"><label className="file-picker"><FileUp size={15} /> Elegir archivo<input type="file" accept=".csv,.json,text/csv,application/json" onChange={(event) => void readFile(event.target.files?.[0])} /></label><select value={format} onChange={(event) => { setFormat(event.target.value as 'csv' | 'json'); setPreview([]) }}><option value="csv">CSV</option><option value="json">JSON</option></select></div><textarea className="import-text" rows={11} value={text} onChange={(event) => { setText(event.target.value); setPreview([]) }} aria-label="Datos para importar" /><div className="form-actions"><button className="secondary-button" onClick={analyze}><BarChart3 size={14} /> Validar y previsualizar</button>{preview.length > 0 && <button className="primary-button" onClick={importEvents}><Send size={14} /> Importar {preview.length} filas</button>}</div>{error && <p className="form-error">{error}</p>}{message && <p className="form-success"><Check size={14} /> {message}</p>}</article><aside className="panel import-help"><h3>Columnas aceptadas</h3><code>stage, source, medium, count, landingPath, occurredAt</code><p><strong>stage</strong> admite visit, interest, contact o quote. También puedes asociar campaignId y socialPostId del mismo proyecto.</p><p>No importes nombres, correos, IP ni identificadores de personas.</p>{preview.length > 0 && <div className="import-preview"><strong>Vista previa</strong>{preview.slice(0, 4).map((item, index) => <span key={index}>{stageLabels[item.stage]} · {item.source} · {item.count}</span>)}</div>}</aside></section>
}

function EditorDrawer({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) { return <div className="drawer-overlay"><section className="editor-drawer" role="dialog" aria-modal="true" aria-label={title}><header><div><small>MARKETING</small><h2>{title}</h2></div><button className="icon-button" onClick={onClose} aria-label="Cerrar editor"><X size={18} /></button></header>{children}</section></div> }
function FormField({ label, children }: { label: string; children: React.ReactNode }) { return <label className="form-field"><span>{label}</span>{children}</label> }
function NumberField({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) { return <FormField label={label}><input type="number" min="0" value={value} onChange={(event) => onChange(Number(event.target.value))} /></FormField> }
function Metric({ label, value, detail }: { label: string; value: string | number; detail: string }) { return <article><span>{label}</span><strong>{value}</strong><small>{detail}</small></article> }
function PanelTitle({ icon: Icon, title, detail }: { icon: typeof BarChart3; title: string; detail: string }) { return <header className="marketing-panel__header"><span><Icon size={17} /></span><div><h2>{title}</h2><p>{detail}</p></div></header> }
function EmptyState({ text }: { text: string }) { return <div className="marketing-empty"><Sparkles size={20} /><p>{text}</p></div> }
