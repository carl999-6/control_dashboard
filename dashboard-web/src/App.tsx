import { lazy, Suspense, useCallback, useEffect, useMemo, useState } from 'react'
import { NavLink, Route, Routes, useLocation } from 'react-router-dom'
import {
  Activity,
  ArrowRight,
  Bell,
  Bot,
  CalendarDays,
  Check,
  ChevronDown,
  CircleDollarSign,
  Command,
  ExternalLink,
  FolderKanban,
  Gauge,
  GitBranch,
  Globe2,
  LayoutDashboard,
  LogOut,
  Menu,
  MessageSquareText,
  MoreHorizontal,
  PanelLeftClose,
  PanelLeftOpen,
  Search,
  Settings,
  ShieldCheck,
  Sparkles,
  Target,
  TrendingUp,
  X,
  Zap,
  type LucideIcon,
} from 'lucide-react'
import { api, type Project, type Session } from './api'
import { formatQuetzales, formatRelativeTime } from './formatters'
import { LoginPage } from './LoginPage'

const ProjectsPage = lazy(() => import('./ProjectsPage').then(({ ProjectsPage }) => ({ default: ProjectsPage })))
const SettingsPage = lazy(() => import('./SettingsPage').then(({ SettingsPage }) => ({ default: SettingsPage })))
const MarketingPage = lazy(() => import('./MarketingPage').then(({ MarketingPage }) => ({ default: MarketingPage })))
const SeoPage = lazy(() => import('./SeoPage').then(({ SeoPage }) => ({ default: SeoPage })))
const OperationsPage = lazy(() => import('./OperationsPage').then(({ OperationsPage }) => ({ default: OperationsPage })))
const NotificationsPage = lazy(() => import('./NotificationsPage').then(({ NotificationsPage }) => ({ default: NotificationsPage })))

type NavigationItem = {
  label: string
  path: string
  icon: LucideIcon
}

const navigation: NavigationItem[] = [
  { label: 'Vista general', path: '/', icon: LayoutDashboard },
  { label: 'Proyectos', path: '/proyectos', icon: FolderKanban },
  { label: 'Marketing', path: '/marketing', icon: TrendingUp },
  { label: 'SEO y contenido', path: '/seo', icon: Search },
  { label: 'Operaciones', path: '/operaciones', icon: Activity },
  { label: 'Automatizaciones', path: '/automatizaciones', icon: Zap },
  { label: 'Costos de API', path: '/costos', icon: CircleDollarSign },
  { label: 'Calendario', path: '/calendario', icon: CalendarDays },
]

const moduleDetails: Record<string, { title: string; description: string; icon: LucideIcon }> = {
  '/proyectos': {
    title: 'Proyectos',
    description: 'Administra entornos, objetivos, módulos activos e integraciones por proyecto.',
    icon: FolderKanban,
  },
  '/marketing': {
    title: 'Marketing',
    description: 'Campañas, publicaciones, enlaces UTM, visitas y conversiones en una misma vista.',
    icon: TrendingUp,
  },
  '/seo': {
    title: 'SEO y contenido',
    description: 'Oportunidades, briefs, borradores, revisión editorial y medición de resultados.',
    icon: Search,
  },
  '/operaciones': {
    title: 'Operaciones',
    description: 'Ejecuciones controladas, aprobaciones, reintentos y trazabilidad.',
    icon: Activity,
  },
  '/automatizaciones': {
    title: 'Automatizaciones',
    description: 'Programaciones locales, ejecución manual, alertas por Telegram e historial de fallos.',
    icon: Zap,
  },
  '/costos': {
    title: 'Costos de API',
    description: 'Consumo estimado, presupuestos, tendencias y alertas por proveedor.',
    icon: CircleDollarSign,
  },
  '/calendario': {
    title: 'Calendario',
    description: 'Campañas, revisiones editoriales, mantenimiento y experimentos.',
    icon: CalendarDays,
  },
  '/ajustes': {
    title: 'Ajustes',
    description: 'Preferencias locales, zona horaria, moneda y políticas del sistema.',
    icon: Settings,
  },
}

const activityItems = [
  {
    title: 'Backup verificado',
    detail: 'FyrStudios · copia diaria completada',
    minutes: 12,
    icon: ShieldCheck,
    tone: 'success',
  },
  {
    title: 'Borrador SEO listo',
    detail: 'Guía de identidad visual · pendiente de revisión',
    minutes: 48,
    icon: Sparkles,
    tone: 'violet',
  },
  {
    title: 'Oportunidad detectada',
    detail: 'Consulta con impresiones y CTR bajo',
    minutes: 134,
    icon: Target,
    tone: 'amber',
  },
  {
    title: 'Despliegue registrado',
    detail: 'FyrStudios · actualización de landing',
    minutes: 310,
    icon: GitBranch,
    tone: 'blue',
  },
]

function App() {
  const [session, setSession] = useState<Session | null>(null)
  const [connectionError, setConnectionError] = useState('')

  const loadSession = useCallback(async () => {
    setConnectionError('')
    try {
      setSession(await api.getSession())
    } catch {
      setConnectionError('No se pudo conectar con la API local. Comprueba que ASP.NET Core esté en ejecución.')
    }
  }, [])

  useEffect(() => { void loadSession() }, [loadSession])

  if (connectionError) {
    return <main className="boot-screen"><span className="boot-logo">!</span><h1>La API local no responde</h1><p>{connectionError}</p><button className="primary-button" onClick={loadSession}>Reintentar</button></main>
  }
  if (!session) return <main className="boot-screen"><span className="boot-loader" /><p>Preparando el centro de control…</p></main>
  if (!session.authenticated) return <LoginPage onAuthenticated={loadSession} />

  return <DashboardShell onLoggedOut={() => setSession({ authenticated: false, displayName: null })} />
}

function DashboardShell({ onLoggedOut }: { onLoggedOut: () => void }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(() => localStorage.getItem('dashboard.sidebarCollapsed') === 'true')
  const [selectedProjectId, setSelectedProjectId] = useState(() => {
    try { return localStorage.getItem('dashboard.selectedProjectId') || 'all' }
    catch { return 'all' }
  })
  const [projects, setProjects] = useState<Project[]>([])
  const [projectsError, setProjectsError] = useState('')
  const location = useLocation()

  const loadProjects = useCallback(async () => {
    try {
      const loaded = await api.getProjects()
      setProjects(loaded)
      setProjectsError('')
      setSelectedProjectId(current => current === 'all' || loaded.some(project => project.id === current) ? current : 'all')
    } catch (reason) {
      setProjectsError(reason instanceof Error ? reason.message : 'No fue posible cargar los proyectos.')
    }
  }, [])

  useEffect(() => {
    try { localStorage.setItem('dashboard.selectedProjectId', selectedProjectId) }
    catch { /* La selección sigue funcionando si el navegador bloquea el almacenamiento. */ }
  }, [selectedProjectId])

  useEffect(() => { void loadProjects() }, [loadProjects])

  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

  const selectedProject = projects.find((project) => project.id === selectedProjectId) ?? null

  async function logout() {
    await api.logout()
    onLoggedOut()
  }

  function toggleSidebar() {
    setSidebarCollapsed((collapsed) => {
      localStorage.setItem('dashboard.sidebarCollapsed', String(!collapsed))
      return !collapsed
    })
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Saltar al contenido principal</a>
      <aside className={`sidebar ${sidebarOpen ? 'sidebar--open' : ''} ${sidebarCollapsed ? 'sidebar--collapsed' : ''}`}>
        <div className="brand">
          <div className="brand__mark" aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
          <div className="brand__copy">
            <strong>CONTROL</strong>
            <small>Centro de proyectos</small>
          </div>
          <button className="icon-button sidebar__collapse" onClick={toggleSidebar} aria-label={sidebarCollapsed ? 'Expandir menú' : 'Contraer menú'} title={sidebarCollapsed ? 'Expandir menú' : 'Contraer menú'}>
            {sidebarCollapsed ? <PanelLeftOpen size={17} /> : <PanelLeftClose size={17} />}
          </button>
          <button className="icon-button sidebar__close" onClick={() => setSidebarOpen(false)} aria-label="Cerrar menú">
            <X size={18} />
          </button>
        </div>

        <div className="project-picker">
          <span className="project-picker__label">ESPACIO DE TRABAJO</span>
          <label>
            <span className="project-avatar">{selectedProject ? selectedProject.name.slice(0, 2).toUpperCase() : 'CP'}</span>
            <select value={selectedProjectId} onChange={(event) => setSelectedProjectId(event.target.value)} aria-label="Proyecto activo">
              <option value="all">Todos los proyectos</option>
              {projects.map((project) => <option value={project.id} key={project.id}>{project.name}</option>)}
            </select>
            <ChevronDown size={15} />
          </label>
        </div>

        <nav className="navigation" aria-label="Navegación principal">
          <span className="navigation__label">MENÚ</span>
          {navigation.map((item) => (
            <NavLink key={item.path} to={item.path} end={item.path === '/'} title={sidebarCollapsed ? item.label : undefined}>
              <item.icon size={18} strokeWidth={1.8} />
              <span>{item.label}</span>
              {item.path === '/automatizaciones' && <span className="nav-count">3</span>}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar__footer">
          <NavLink to="/ajustes" className="settings-link" title={sidebarCollapsed ? 'Ajustes' : undefined}>
            <Settings size={18} />
            <span>Ajustes</span>
          </NavLink>
          <div className="user-card">
            <span className="user-card__avatar">CA</span>
            <span>
              <strong>Administrador</strong>
              <small>Sesión local</small>
            </span>
            <button className="logout-button" onClick={logout} aria-label="Cerrar sesión"><LogOut size={16} /></button>
          </div>
        </div>
      </aside>

      {sidebarOpen && <button className="sidebar-overlay" onClick={() => setSidebarOpen(false)} aria-label="Cerrar menú" />}

      <div className={`workspace ${sidebarCollapsed ? 'workspace--expanded' : ''}`}>
        <header className="topbar">
          <div className="topbar__left">
            <button className="icon-button mobile-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú">
              <Menu size={20} />
            </button>
            <div className="crumbs">
              <span>Centro de control</span>
              <span>/</span>
              <strong>{location.pathname === '/' ? 'Vista general' : moduleDetails[location.pathname]?.title}</strong>
            </div>
          </div>
          <div className="topbar__actions">
            <button className="command-search">
              <Search size={16} />
              <span>Buscar...</span>
              <kbd><Command size={11} /> K</kbd>
            </button>
            <button className="icon-button notification-button" aria-label="Notificaciones">
              <Bell size={18} />
              <span />
            </button>
          </div>
        </header>

        <main id="main-content" tabIndex={-1}>
          {projectsError && <div className="global-error" role="alert">{projectsError}</div>}
          <Suspense fallback={<div className="route-loading" role="status">Cargando módulo…</div>}>
            <Routes>
            <Route path="/" element={<Overview project={selectedProject} projects={projects} />} />
            <Route path="/proyectos" element={<ProjectsPage projects={projects} onChanged={loadProjects} />} />
            <Route path="/marketing" element={<MarketingPage project={selectedProject} />} />
            <Route path="/seo" element={<SeoPage project={selectedProject} />} />
            <Route path="/operaciones" element={<OperationsPage project={selectedProject} initialTab="executions" />} />
            <Route path="/costos" element={<OperationsPage project={selectedProject} initialTab="costs" />} />
            <Route path="/automatizaciones" element={<NotificationsPage project={selectedProject} />} />
            <Route path="/ajustes" element={<SettingsPage />} />
            {Object.entries(moduleDetails).filter(([path]) => !['/proyectos', '/marketing', '/seo', '/operaciones', '/automatizaciones', '/costos', '/ajustes'].includes(path)).map(([path, details]) => (
              <Route key={path} path={path} element={<ModulePlaceholder {...details} />} />
            ))}
            </Routes>
          </Suspense>
        </main>
      </div>
    </div>
  )
}

function Overview({ project, projects }: { project: Project | null; projects: Project[] }) {
  const isGlobal = project === null
  const dateLabel = useMemo(
    () =>
      new Intl.DateTimeFormat('es-GT', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
      }).format(new Date()),
    [],
  )

  return (
    <div className="page overview-page">
      <section className="page-heading">
        <div>
          <div className="eyebrow"><span /> {dateLabel}</div>
          <h1>{isGlobal ? 'Todo bajo control.' : `${project.name}, en resumen.`}</h1>
          <p>{isGlobal ? 'Una vista clara de lo que necesita atención en tus proyectos.' : 'Rendimiento, actividad y prioridades del proyecto.'}</p>
        </div>
        <div className="heading-actions">
          <span className="demo-pill"><Sparkles size={14} /> Datos simulados</span>
          <button className="primary-button">Crear tarea <ArrowRight size={16} /></button>
        </div>
      </section>

      <section className="status-strip" aria-label="Estado del sistema">
        <div><span className="pulse-dot" /><strong>Sistemas operativos</strong><small>3 de 3 servicios</small></div>
        <div><Gauge size={18} /><strong>2 tareas pendientes</strong><small>requieren tu revisión</small></div>
        <div><Bot size={18} /><strong>Automatización en pausa</strong><small>modo seguro activo</small></div>
        <button>Ver actividad <ArrowRight size={15} /></button>
      </section>

      <section className="metric-grid">
        <MetricCard
          label="Visitas este mes"
          value={isGlobal ? '2,841' : '2,184'}
          change="+12.4%"
          footnote="frente al periodo anterior"
          bars={[35, 48, 42, 61, 54, 77, 70, 84, 73, 91, 82, 96]}
        />
        <MetricCard
          label="Conversiones"
          value={isGlobal ? '73' : '58'}
          change="+8.1%"
          footnote="formularios y contactos"
          bars={[22, 37, 31, 48, 40, 46, 58, 51, 68, 62, 74, 71]}
        />
        <MetricCard
          label="Oportunidades abiertas"
          value="14"
          change="4 nuevas"
          footnote="SEO, contenido y producto"
          accent="amber"
          bars={[42, 42, 50, 44, 57, 60, 55, 64, 72, 70, 83, 88]}
        />
        <MetricCard
          label="Consumo estimado"
          value={formatQuetzales(isGlobal ? 18.42 : 12.26)}
          change="23%"
          footnote="del presupuesto mensual"
          accent="violet"
          progress={23}
        />
      </section>

      <section className="dashboard-grid">
        <div className="panel performance-panel">
          <PanelHeader title="Rendimiento de proyectos" description="Señales clave de los últimos 30 días" action="Ver informe" />
          <div className="project-table" role="table" aria-label="Rendimiento de proyectos">
            <div className="project-row project-row--head" role="row">
              <span>Proyecto</span><span>Estado</span><span>Visitas</span><span>Conversión</span><span>Salud</span>
            </div>
            {projects.map((item, index) => (
              <ProjectRow
                key={item.id}
                initials={item.name.slice(0, 2).toUpperCase()}
                name={item.name}
                domain={item.domain}
                status={{ active: 'Activo', planning: 'Diseño', paused: 'Pausado' }[item.status] ?? item.status}
                visits={['2,184', '—', '657'][index] ?? '—'}
                conversion={['2.7%', '—', '1.9%'][index] ?? '—'}
                health={[92, 68, 74][index] ?? 70}
                muted={item.status !== 'active'}
              />
            ))}
          </div>
        </div>

        <div className="panel activity-panel">
          <PanelHeader title="Actividad reciente" description="Eventos relevantes, sin ruido" action="Ver todo" />
          <div className="activity-list">
            {activityItems.map((item) => (
              <div className="activity-item" key={item.title}>
                <span className={`activity-icon activity-icon--${item.tone}`}><item.icon size={17} /></span>
                <div><strong>{item.title}</strong><p>{item.detail}</p></div>
                <time>{formatRelativeTime(item.minutes)}</time>
              </div>
            ))}
          </div>
        </div>

        <div className="panel attention-panel">
          <PanelHeader title="Necesita tu atención" description="Decisiones y revisiones pendientes" />
          <div className="attention-list">
            <AttentionItem
              icon={MessageSquareText}
              eyebrow="RESPUESTA EN X"
              title="Revisar propuesta de respuesta"
              detail="Oportunidad con alta afinidad para FyrStudios"
              action="Revisar"
              tone="blue"
            />
            <AttentionItem
              icon={Search}
              eyebrow="BORRADOR SEO"
              title="Aprobar brief de contenido"
              detail="Potencial estimado: 90–140 visitas mensuales"
              action="Abrir brief"
              tone="violet"
            />
            <AttentionItem
              icon={CircleDollarSign}
              eyebrow="PRESUPUESTO"
              title="Confirmar límite de Gemini"
              detail="El flujo seguirá pausado hasta su aprobación"
              action="Configurar"
              tone="amber"
            />
          </div>
        </div>

        <div className="panel funnel-panel">
          <PanelHeader title="Embudo de FyrStudios" description="Origen y conversión del periodo" action="Analizar" />
          <div className="funnel">
            <FunnelStep label="Visitas" value="2,184" width={100} />
            <FunnelStep label="Interés" value="486" width={76} />
            <FunnelStep label="Contactos" value="58" width={51} />
            <FunnelStep label="Cotizaciones" value="17" width={31} />
          </div>
          <div className="funnel-note"><TrendingUp size={16} /><span><strong>+0.4 puntos</strong> en conversión frente al periodo anterior</span></div>
        </div>
      </section>

      <p className="simulation-note">Datos ilustrativos para validar la experiencia. No representan actividad real de FyrStudios.</p>
    </div>
  )
}

function MetricCard({
  label,
  value,
  change,
  footnote,
  bars,
  accent = 'green',
  progress,
}: {
  label: string
  value: string
  change: string
  footnote: string
  bars?: number[]
  accent?: 'green' | 'amber' | 'violet'
  progress?: number
}) {
  return (
    <article className={`metric-card metric-card--${accent}`}>
      <div className="metric-card__top"><span>{label}</span><MoreHorizontal size={17} /></div>
      <div className="metric-card__content">
        <strong>{value}</strong>
        {bars && <div className="mini-chart" aria-hidden="true">{bars.map((height, index) => <i key={index} style={{ height: `${height}%` }} />)}</div>}
      </div>
      {progress !== undefined && <div className="progress-track"><span style={{ width: `${progress}%` }} /></div>}
      <div className="metric-card__footer"><b>{change}</b><span>{footnote}</span></div>
    </article>
  )
}

function PanelHeader({ title, description, action }: { title: string; description: string; action?: string }) {
  return (
    <div className="panel-header">
      <div><h2>{title}</h2><p>{description}</p></div>
      {action && <button>{action} <ArrowRight size={14} /></button>}
    </div>
  )
}

function ProjectRow({ initials, name, domain, status, visits, conversion, health, muted = false }: {
  initials: string; name: string; domain: string; status: string; visits: string; conversion: string; health: number; muted?: boolean
}) {
  return (
    <div className={`project-row ${muted ? 'project-row--muted' : ''}`} role="row">
      <div className="project-name"><span>{initials}</span><div><strong>{name}</strong><small>{domain}</small></div></div>
      <span className={`status-badge status-badge--${status.toLowerCase()}`}>{status}</span>
      <strong>{visits}</strong>
      <strong>{conversion}</strong>
      <div className="health-score"><span><i style={{ width: `${health}%` }} /></span><b>{health}%</b></div>
    </div>
  )
}

function AttentionItem({ icon: Icon, eyebrow, title, detail, action, tone }: {
  icon: LucideIcon; eyebrow: string; title: string; detail: string; action: string; tone: string
}) {
  return (
    <div className="attention-item">
      <span className={`attention-item__icon attention-item__icon--${tone}`}><Icon size={19} /></span>
      <div className="attention-item__copy"><small>{eyebrow}</small><strong>{title}</strong><p>{detail}</p></div>
      <button>{action} <ArrowRight size={14} /></button>
    </div>
  )
}

function FunnelStep({ label, value, width }: { label: string; value: string; width: number }) {
  return (
    <div className="funnel-step">
      <span>{label}</span>
      <div><i style={{ width: `${width}%` }} /></div>
      <strong>{value}</strong>
    </div>
  )
}

function ModulePlaceholder({ title, description, icon: Icon }: { title: string; description: string; icon: LucideIcon }) {
  return (
    <div className="page module-page">
      <section className="page-heading">
        <div><div className="eyebrow"><span /> Fase futura</div><h1>{title}</h1><p>{description}</p></div>
        <span className="demo-pill"><Sparkles size={14} /> Estructura de navegación</span>
      </section>
      <div className="module-placeholder">
        <span className="module-placeholder__icon"><Icon size={30} /></span>
        <div>
          <small>MÓDULO PLANIFICADO</small>
          <h2>{title} se construirá en su fase correspondiente</h2>
          <p>La navegación ya forma parte de la base del producto. Sus datos, formularios y acciones se incorporarán de forma verificable sin adelantar alcance.</p>
        </div>
        <div className="module-placeholder__rule"><Check size={16} /><span>Sin acciones externas ni datos reales</span></div>
      </div>
    </div>
  )
}

export default App
