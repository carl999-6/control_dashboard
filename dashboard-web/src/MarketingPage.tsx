import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  BarChart3, Bot, Check, Clipboard, ExternalLink, FileUp, Link2, Megaphone, Pencil, Plus, RefreshCw, Save,
  Send, ShieldCheck, Sparkles, Trash2, TrendingUp, X,
} from 'lucide-react'
import {
  api, type Campaign, type CampaignInput, type MarketingEventInput, type MarketingSummary,
  type Project, type SocialPost, type SocialPostInput, type XAssistantSettings,
  type XAssistantSettingsInput, type XReplyProposal, type XSourcePost, type XSourcePostInput,
} from './api'
import { buildUtmUrl, parseMarketingImport } from './marketing'
import { PostHogPanel } from './PostHogPanel'

type MarketingTab = 'summary' | 'campaigns' | 'posts' | 'import' | 'x-assistant' | 'posthog'
type Editor = { kind: 'campaign'; item: Campaign | null } | { kind: 'post'; item: SocialPost | null } | null

const emptyCampaign: CampaignInput = {
  name: '', objective: '', status: 'draft', utmCampaign: '', startDate: null, endDate: null,
}
const emptyPost: SocialPostInput = {
  campaignId: null, platform: 'instagram', topic: '', format: 'post', status: 'draft', externalUrl: '',
  publishedAt: null, impressions: 0, engagements: 0, clicks: 0,
}
const emptyXSource: XSourcePostInput = {
  url: '', authorUsername: '', text: '', language: 'es', likeCount: 0, replyCount: 0,
  repostCount: 0, quoteCount: 0, impressionCount: 0, postedAt: null,
}

const stageLabels: Record<string, string> = { visit: 'Visitas', interest: 'Interés', contact: 'Contactos', quote: 'Cotizaciones' }
const sourceLabels: Record<string, string> = { instagram: 'Instagram', x: 'X', organic: 'Orgánico', direct: 'Directo' }
const dataLabels: Record<string, string> = { simulated: 'Simulado', manual: 'Manual', import_csv: 'CSV', import_json: 'JSON' }

export function MarketingPage({ project }: { project: Project | null }) {
  const [tab, setTab] = useState<MarketingTab>('summary')
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [posts, setPosts] = useState<SocialPost[]>([])
  const [summary, setSummary] = useState<MarketingSummary | null>(null)
  const [xSettings, setXSettings] = useState<XAssistantSettings | null>(null)
  const [xSources, setXSources] = useState<XSourcePost[]>([])
  const [xProposals, setXProposals] = useState<XReplyProposal[]>([])
  const [editor, setEditor] = useState<Editor>(null)
  const [campaignForm, setCampaignForm] = useState<CampaignInput>(emptyCampaign)
  const [postForm, setPostForm] = useState<SocialPostInput>(emptyPost)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const loadMarketing = useCallback(async () => {
    if (!project) return
    setLoading(true)
    setError('')
    try {
      const [campaignItems, postItems, summaryData, settings, sources, proposals] = await Promise.all([
        api.getCampaigns(project.id), api.getSocialPosts(project.id), api.getMarketingSummary(project.id),
        api.getXAssistantSettings(project.id), api.getXSources(project.id), api.getXProposals(project.id),
      ])
      setCampaigns(campaignItems)
      setPosts(postItems)
      setSummary(summaryData)
      setXSettings(settings)
      setXSources(sources)
      setXProposals(proposals)
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
        {([['summary', 'Resumen'], ['campaigns', 'Campañas'], ['posts', 'Publicaciones'], ['import', 'Importar datos'], ['x-assistant', 'Asistente X'], ['posthog', 'PostHog']] as const).map(([value, label]) =>
          <button key={value} className={tab === value ? 'active' : ''} onClick={() => setTab(value)}>{label}</button>)}
      </nav>
      {error && <p className="global-error marketing-error" role="alert">{error}</p>}
      {notice && <p className="form-success marketing-notice"><Check size={14} /> {notice}</p>}
      {loading && <p className="loading-copy">Cargando datos del proyecto…</p>}
      {!loading && tab === 'summary' && <MarketingSummaryView summary={summary} campaigns={campaigns} project={project} />}
      {!loading && tab === 'campaigns' && <CampaignsView items={campaigns} onAdd={() => openCampaign(null)} onEdit={openCampaign} />}
      {!loading && tab === 'posts' && <PostsView items={posts} campaigns={campaigns} onAdd={() => openPost(null)} onEdit={openPost} />}
      {!loading && tab === 'import' && <ImportView projectId={project.id} onImported={loadMarketing} />}
      {!loading && tab === 'x-assistant' && xSettings && <XAssistantView projectId={project.id} settings={xSettings} sources={xSources} proposals={xProposals} onReload={loadMarketing} onError={setError} onNotice={setNotice} />}
      {!loading && tab === 'posthog' && <PostHogPanel projectId={project.id} campaigns={campaigns} proposals={xProposals} />}

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

function XAssistantView({ projectId, settings, sources, proposals, onReload, onError, onNotice }: {
  projectId: string; settings: XAssistantSettings; sources: XSourcePost[]; proposals: XReplyProposal[];
  onReload: () => Promise<void>; onError: (value: string) => void; onNotice: (value: string) => void
}) {
  const [form, setForm] = useState<XAssistantSettingsInput>({
    isEnabled: settings.isEnabled, apiReadEnabled: settings.apiReadEnabled, searchQuery: settings.searchQuery,
    language: settings.language, maximumPostsPerSync: settings.maximumPostsPerSync,
    readCostUsdPerPost: settings.readCostUsdPerPost, toneInstructions: settings.toneInstructions,
    landingPath: settings.landingPath, utmCampaign: settings.utmCampaign,
  })
  const [source, setSource] = useState<XSourcePostInput>(emptyXSource)
  const [busy, setBusy] = useState(false)
  useEffect(() => setForm({
    isEnabled: settings.isEnabled, apiReadEnabled: settings.apiReadEnabled, searchQuery: settings.searchQuery,
    language: settings.language, maximumPostsPerSync: settings.maximumPostsPerSync,
    readCostUsdPerPost: settings.readCostUsdPerPost, toneInstructions: settings.toneInstructions,
    landingPath: settings.landingPath, utmCampaign: settings.utmCampaign,
  }), [settings])

  async function act(action: () => Promise<unknown>, message: string) {
    setBusy(true); onError(''); onNotice('')
    try { await action(); onNotice(message); await onReload() }
    catch (reason) { onError(reason instanceof Error ? reason.message : 'No se pudo completar la acción.') }
    finally { setBusy(false) }
  }
  async function run() {
    await act(async () => {
      const result = await api.runAutomation(projectId, 'x_response_pipeline')
      if (result.status !== 'succeeded') throw new Error(result.errorMessage || `El flujo terminó con estado ${result.status}.`)
    }, 'Gemini generó tres respuestas pendientes de revisión. No se publicó nada en X.')
  }
  async function importSource(event: FormEvent) {
    event.preventDefault()
    await act(() => api.importXSource(projectId, source), 'Publicación de X agregada a la cola de oportunidades.')
    setSource(emptyXSource)
  }

  return <section className="x-assistant-layout">
    <article className="panel x-control-panel">
      <header className="x-phase-heading"><span><Bot size={20} /></span><div><small>FASE 7E · LECTURA Y REVISIÓN HUMANA</small><h2>Asistente de conversación para X</h2><p>Detecta o recibe publicaciones, propone tres respuestas y espera tu revisión. La publicación siempre se hace manualmente en X.</p></div><b className={`integration-state ${settings.isEnabled ? 'integration-state--connected' : ''}`}>{settings.isEnabled ? 'Activo' : 'Pausado'}</b></header>
      <div className="x-safety"><ShieldCheck size={18} /><div><strong>Sin permisos de escritura</strong><span>Este sistema no contiene ningún endpoint para publicar, responder, dar me gusta ni seguir cuentas en X.</span></div></div>
      <form className="x-settings-grid" onSubmit={event => { event.preventDefault(); void act(() => api.updateXAssistantSettings(projectId, form), 'Configuración del asistente de X actualizada.') }}>
        <label className="pause-control"><input type="checkbox" checked={form.isEnabled} onChange={event => setForm({ ...form, isEnabled: event.target.checked })} /><span><Bot size={18} /><b>{form.isEnabled ? 'Generación habilitada' : 'Generación pausada'}</b><small>La cola existente se conserva al pausar.</small></span></label>
        <label className={`pause-control x-paid-toggle ${form.apiReadEnabled ? 'x-paid-toggle--active' : ''}`}><input type="checkbox" checked={form.apiReadEnabled} onChange={event => setForm({ ...form, apiReadEnabled: event.target.checked })} /><span><RefreshCw size={18} /><b>{form.apiReadEnabled ? 'Lectura pagada habilitada' : 'Solo entrada manual, sin costo de X'}</b><small>Al activarla, cada ejecución puede consumir créditos de X.</small></span></label>
        {form.apiReadEnabled && <div className="x-cost-warning"><strong>Costo máximo configurado por sincronización: USD {(form.maximumPostsPerSync * form.readCostUsdPerPost).toFixed(4)}</strong><span>La tarifa es editable porque X puede cambiarla. El presupuesto del proyecto puede bloquear la consulta.</span></div>}
        <FormField label="Búsqueda reciente"><input maxLength={400} required={form.apiReadEnabled} value={form.searchQuery} onChange={event => setForm({ ...form, searchQuery: event.target.value })} placeholder="diseño web OR branding" /></FormField>
        <div className="form-row form-row--three"><FormField label="Idioma"><input value={form.language} maxLength={3} onChange={event => setForm({ ...form, language: event.target.value })} /></FormField><NumberField label="Posts máximos" value={form.maximumPostsPerSync} onChange={value => setForm({ ...form, maximumPostsPerSync: value })} /><FormField label="Costo USD por post"><input type="number" min="0" max="1" step="0.000001" value={form.readCostUsdPerPost} onChange={event => setForm({ ...form, readCostUsdPerPost: Number(event.target.value) })} /></FormField></div>
        <FormField label="Tono e instrucciones"><textarea rows={3} maxLength={2000} value={form.toneInstructions} onChange={event => setForm({ ...form, toneInstructions: event.target.value })} /></FormField>
        <div className="form-row"><FormField label="Ruta del sitio para UTM"><input value={form.landingPath} onChange={event => setForm({ ...form, landingPath: event.target.value })} placeholder="/servicios" /></FormField><FormField label="utm_campaign"><input value={form.utmCampaign} onChange={event => setForm({ ...form, utmCampaign: event.target.value })} /></FormField></div>
        <div className="x-connection-state"><span className={settings.bearerTokenConfigured ? 'ok' : ''}>{settings.bearerTokenConfigured ? '✓ Bearer token detectado' : '— Sin X_BEARER_TOKEN; la entrada manual funciona'}</span>{settings.lastSyncAt && <span>Última lectura: {new Date(settings.lastSyncAt).toLocaleString('es-GT')}</span>}</div>
        {settings.lastError && <p className="form-error">{settings.lastError}</p>}
        <div className="form-actions"><button className="secondary-button" disabled={busy}><Save size={14} /> Guardar configuración</button><button className="primary-button" type="button" disabled={busy || !form.isEnabled} onClick={() => void run()}><Sparkles size={14} /> {busy ? 'Procesando…' : 'Generar siguiente propuesta'}</button></div>
      </form>
    </article>

    <div className="x-work-grid">
      <article className="panel x-source-panel"><PanelTitle icon={Plus} title="Agregar oportunidad manual" detail="Ruta gratuita: copia un post público que quieras responder" />
        <form className="project-form" onSubmit={importSource}><FormField label="URL del post"><input type="url" required value={source.url} onChange={event => setSource({ ...source, url: event.target.value })} placeholder="https://x.com/usuario/status/123" /></FormField><div className="form-row"><FormField label="Usuario"><input required value={source.authorUsername} onChange={event => setSource({ ...source, authorUsername: event.target.value })} placeholder="@usuario" /></FormField><FormField label="Idioma"><input required maxLength={3} value={source.language} onChange={event => setSource({ ...source, language: event.target.value })} /></FormField></div><FormField label="Texto del post"><textarea required rows={5} maxLength={10000} value={source.text} onChange={event => setSource({ ...source, text: event.target.value })} /></FormField><div className="form-row form-row--three"><NumberField label="Me gusta" value={source.likeCount} onChange={value => setSource({ ...source, likeCount: value })} /><NumberField label="Respuestas" value={source.replyCount} onChange={value => setSource({ ...source, replyCount: value })} /><NumberField label="Republicaciones" value={source.repostCount} onChange={value => setSource({ ...source, repostCount: value })} /></div><div className="form-row form-row--three"><NumberField label="Citas" value={source.quoteCount} onChange={value => setSource({ ...source, quoteCount: value })} /><NumberField label="Impresiones" value={source.impressionCount} onChange={value => setSource({ ...source, impressionCount: value })} /><FormField label="Fecha del post"><input type="datetime-local" value={source.postedAt ?? ''} onChange={event => setSource({ ...source, postedAt: event.target.value || null })} /></FormField></div><div className="form-actions"><button className="secondary-button" disabled={busy}><Plus size={14} /> Agregar a la cola</button></div></form>
      </article>
      <article className="panel x-queue-panel"><PanelTitle icon={TrendingUp} title="Cola de oportunidades" detail="Se priorizan señales de interés con poca conversación" /><div className="x-source-list">{sources.filter(item => item.status === 'detected').map(item => <div key={item.id}><div><a href={item.url} target="_blank" rel="noreferrer">@{item.authorUsername} <ExternalLink size={12} /></a><p>{item.text}</p><small>{item.likeCount} me gusta · {item.replyCount} respuestas · {item.dataSource === 'manual' ? 'manual' : 'API X'}</small></div><button className="icon-button" disabled={busy} onClick={() => void act(() => api.dismissXSource(projectId, item.id), 'Oportunidad descartada.')} aria-label="Descartar oportunidad"><Trash2 size={14} /></button></div>)}{sources.every(item => item.status !== 'detected') && <p className="empty-copy">No hay oportunidades pendientes.</p>}</div></article>
    </div>

    <section className="x-proposals"><div className="section-toolbar"><div><h2>Revisión de propuestas</h2><p>Elige o edita una respuesta y abre el compositor de X con el texto y enlace medible preparados.</p></div></div>{proposals.map(item => <XProposalCard key={item.id} projectId={projectId} proposal={item} busy={busy} act={act} />)}{proposals.length === 0 && <EmptyState text="Aún no hay propuestas. Agrega una oportunidad y ejecuta el flujo." />}</section>
  </section>
}

function XProposalCard({ projectId, proposal, busy, act }: { projectId: string; proposal: XReplyProposal; busy: boolean; act: (action: () => Promise<unknown>, message: string) => Promise<void> }) {
  const [selected, setSelected] = useState(proposal.selectedReply)
  const [publishedUrl, setPublishedUrl] = useState(proposal.publishedReplyUrl)
  const terminal = proposal.status === 'published' || proposal.status === 'discarded'
  const formattedReply = formatXReply(selected, proposal.trackingUrl)
  async function openReply() {
    window.open(buildXReplyIntent(proposal.sourceUrl, selected, proposal.trackingUrl), '_blank', 'noopener,noreferrer')
    if (proposal.status === 'pending_review') {
      await act(() => api.transitionXProposal(projectId, proposal.id, 'approved', selected), 'Se abrió el compositor de X con la respuesta preparada. X espera tu confirmación final.')
    }
  }
  return <article className="panel x-proposal-card"><header><div><span className={`status-badge status-badge--${proposal.status}`}>{proposal.status.replace('_', ' ')}</span><a href={proposal.sourceUrl} target="_blank" rel="noreferrer">Post de @{proposal.sourceAuthor} <ExternalLink size={12} /></a></div><time>{new Date(proposal.createdAt).toLocaleString('es-GT')}</time></header><blockquote>{proposal.sourceText}</blockquote><div className="x-reply-options">{[['Recomendada', proposal.recommendedReply], ['Alternativa 1', proposal.alternativeOne], ['Alternativa 2', proposal.alternativeTwo]].map(([label, value]) => <label key={label}><input type="radio" name={`reply-${proposal.id}`} checked={selected === value} disabled={terminal} onChange={() => setSelected(value)} /><span><b>{label}</b><span className="x-formatted-option">{formatXReply(value, proposal.trackingUrl)}</span></span></label>)}</div><FormField label={`Respuesta editable · ${Array.from(selected).length}/235`}><textarea rows={3} maxLength={235} disabled={terminal} value={selected} onChange={event => setSelected(event.target.value)} /></FormField><div className="x-reply-preview"><strong>Formato que se abrirá en X</strong><pre>{formattedReply}</pre></div><div className="x-proposal-notes"><p><strong>Por qué:</strong> {proposal.rationale}</p>{proposal.riskNotes && <p><strong>Riesgos:</strong> {proposal.riskNotes}</p>}</div>{proposal.status === 'published' ? <p className="form-success"><Check size={14} /> Registrada como publicada: <a href={proposal.publishedReplyUrl} target="_blank" rel="noreferrer">abrir respuesta</a></p> : proposal.status === 'discarded' ? <p className="empty-copy">Propuesta descartada.</p> : <><div className="form-actions"><button className="primary-button" disabled={busy || !selected.trim()} onClick={() => void openReply()}><Send size={14} /> Responder en X</button><button className="danger-button" disabled={busy} onClick={() => void act(() => api.transitionXProposal(projectId, proposal.id, 'discarded', selected), 'Propuesta descartada.')}><Trash2 size={14} /> Descartar</button></div><div className="x-publish-proof"><div><FormField label="URL de tu respuesta ya publicada"><input type="url" value={publishedUrl} onChange={event => setPublishedUrl(event.target.value)} placeholder="https://x.com/tu_usuario/status/123" /></FormField><small>Después de confirmar la respuesta en X, abre tu respuesta, copia su enlace y pégalo aquí. Esto registra el resultado; no realiza la publicación.</small></div><button className="secondary-button" disabled={busy || !publishedUrl.trim()} onClick={() => void act(() => api.transitionXProposal(projectId, proposal.id, 'published', selected, publishedUrl), 'Publicación manual registrada en el historial.')}><Link2 size={14} /> Registrar como publicada</button></div></>}</article>
}

function formatXReply(reply: string, trackingUrl: string) {
  return `${reply.trim()}\n\nMás información: ${trackingUrl}`
}

function buildXReplyIntent(sourceUrl: string, reply: string, trackingUrl: string) {
  const match = sourceUrl.match(/\/(?:status)\/(\d+)/i)
  const parameters = new URLSearchParams({ text: formatXReply(reply, trackingUrl) })
  if (match?.[1]) parameters.set('in_reply_to', match[1])
  return `https://twitter.com/intent/tweet?${parameters.toString()}`
}

function EditorDrawer({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) { return <div className="drawer-overlay"><section className="editor-drawer" role="dialog" aria-modal="true" aria-label={title}><header><div><small>MARKETING</small><h2>{title}</h2></div><button className="icon-button" onClick={onClose} aria-label="Cerrar editor"><X size={18} /></button></header>{children}</section></div> }
function FormField({ label, children }: { label: string; children: React.ReactNode }) { return <label className="form-field"><span>{label}</span>{children}</label> }
function NumberField({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) { return <FormField label={label}><input type="number" min="0" value={value} onChange={(event) => onChange(Number(event.target.value))} /></FormField> }
function Metric({ label, value, detail }: { label: string; value: string | number; detail: string }) { return <article><span>{label}</span><strong>{value}</strong><small>{detail}</small></article> }
function PanelTitle({ icon: Icon, title, detail }: { icon: typeof BarChart3; title: string; detail: string }) { return <header className="marketing-panel__header"><span><Icon size={17} /></span><div><h2>{title}</h2><p>{detail}</p></div></header> }
function EmptyState({ text }: { text: string }) { return <div className="marketing-empty"><Sparkles size={20} /><p>{text}</p></div> }
