import { useState, type FormEvent } from 'react'
import { Check, Flag, FolderKanban, Pencil, Plus, Save, Trash2, X } from 'lucide-react'
import { api, type Goal, type GoalInput, type Project, type ProjectInput } from './api'

const emptyProject: ProjectInput = {
  name: '', domain: '', type: 'WordPress', status: 'active', timeZone: 'America/Guatemala',
  environment: 'local', description: '',
}

const emptyGoal: GoalInput = {
  title: '', description: '', metric: '', targetValue: null, status: 'active', dueDate: null,
}

export function ProjectsPage({ projects, onChanged }: { projects: Project[]; onChanged: () => Promise<void> }) {
  const [editing, setEditing] = useState<Project | 'new' | null>(null)
  const [form, setForm] = useState<ProjectInput>(emptyProject)
  const [goalForm, setGoalForm] = useState<GoalInput>(emptyGoal)
  const [editingGoalId, setEditingGoalId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  function openNew() {
    setEditing('new')
    setForm(emptyProject)
    setGoalForm(emptyGoal)
    setEditingGoalId(null)
    setError('')
  }

  function openEdit(project: Project) {
    setEditing(project)
    setForm({
      name: project.name, domain: project.domain, type: project.type, status: project.status,
      timeZone: project.timeZone, environment: project.environment, description: project.description,
    })
    setGoalForm(emptyGoal)
    setEditingGoalId(null)
    setError('')
  }

  async function saveProject(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      if (editing === 'new') await api.createProject(form)
      else if (editing) await api.updateProject(editing.id, form)
      await onChanged()
      setEditing(null)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No fue posible guardar el proyecto.')
    } finally {
      setSaving(false)
    }
  }

  async function removeProject() {
    if (!editing || editing === 'new') return
    if (!window.confirm(`¿Eliminar ${editing.name} y sus objetivos? Esta acción no se puede deshacer.`)) return
    setSaving(true)
    try {
      await api.deleteProject(editing.id)
      await onChanged()
      setEditing(null)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No fue posible eliminar el proyecto.')
    } finally {
      setSaving(false)
    }
  }

  async function addGoal(event: FormEvent) {
    event.preventDefault()
    if (!editing || editing === 'new') return
    setSaving(true)
    try {
      if (editingGoalId) await api.updateGoal(editing.id, editingGoalId, goalForm)
      else await api.createGoal(editing.id, goalForm)
      await onChanged()
      setGoalForm(emptyGoal)
      setEditingGoalId(null)
      const refreshed = (await api.getProjects()).find((project) => project.id === editing.id)
      if (refreshed) setEditing(refreshed)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No fue posible guardar el objetivo.')
    } finally {
      setSaving(false)
    }
  }

  async function removeGoal(goalId: string) {
    if (!editing || editing === 'new') return
    await api.deleteGoal(editing.id, goalId)
    await onChanged()
    setEditing({ ...editing, goals: editing.goals.filter((goal) => goal.id !== goalId) })
  }

  function editGoal(goal: Goal) {
    setEditingGoalId(goal.id)
    setGoalForm({
      title: goal.title,
      description: goal.description,
      metric: goal.metric,
      targetValue: goal.targetValue,
      status: goal.status,
      dueDate: goal.dueDate,
    })
  }

  return (
    <div className="page projects-page">
      <section className="page-heading">
        <div><div className="eyebrow"><span /> Fase 2 · datos persistentes</div><h1>Proyectos</h1><p>Entornos, objetivos y aislamiento desde una sola vista.</p></div>
        <button className="primary-button" onClick={openNew}><Plus size={16} /> Nuevo proyecto</button>
      </section>

      <section className="project-summary-grid">
        <div><strong>{projects.length}</strong><span>proyectos registrados</span></div>
        <div><strong>{projects.filter((project) => project.status === 'active').length}</strong><span>activos</span></div>
        <div><strong>{projects.reduce((total, project) => total + project.goals.length, 0)}</strong><span>objetivos definidos</span></div>
      </section>

      <section className="managed-projects">
        {projects.map((project) => (
          <article className="managed-project" key={project.id}>
            <div className="managed-project__avatar">{project.name.slice(0, 2).toUpperCase()}</div>
            <div className="managed-project__main">
              <div><h2>{project.name}</h2>{project.isDemoData && <span className="seed-badge">DATOS SEMILLA</span>}</div>
              <p>{project.description || 'Sin descripción'}</p>
              <div className="project-meta"><span>{project.domain}</span><span>{project.type}</span><span>{project.environment}</span></div>
            </div>
            <div className="managed-project__status">
              <span className={`status-badge status-badge--${project.status}`}>{project.status}</span>
              <small><Flag size={12} /> {project.goals.length} objetivos</small>
            </div>
            <button className="secondary-button" onClick={() => openEdit(project)}><Pencil size={14} /> Editar</button>
          </article>
        ))}
      </section>

      {editing && (
        <div className="drawer-overlay" role="presentation">
          <section className="editor-drawer" role="dialog" aria-modal="true" aria-label={editing === 'new' ? 'Nuevo proyecto' : `Editar ${editing.name}`}>
            <header><div><small>{editing === 'new' ? 'CREAR' : 'CONFIGURAR'}</small><h2>{editing === 'new' ? 'Nuevo proyecto' : editing.name}</h2></div><button className="icon-button" onClick={() => setEditing(null)} aria-label="Cerrar editor"><X size={18} /></button></header>
            <form className="project-form" onSubmit={saveProject}>
              <FormField label="Nombre"><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required maxLength={120} /></FormField>
              <FormField label="Dominio o identificador"><input value={form.domain} onChange={(event) => setForm({ ...form, domain: event.target.value })} required maxLength={200} /></FormField>
              <div className="form-row">
                <FormField label="Tipo"><select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value })}><option>WordPress</option><option>SaaS</option><option>API</option><option>Otro</option></select></FormField>
                <FormField label="Estado"><select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option value="active">Activo</option><option value="planning">Planificación</option><option value="paused">Pausado</option></select></FormField>
              </div>
              <div className="form-row">
                <FormField label="Zona horaria"><input value={form.timeZone} onChange={(event) => setForm({ ...form, timeZone: event.target.value })} required /></FormField>
                <FormField label="Entorno"><select value={form.environment} onChange={(event) => setForm({ ...form, environment: event.target.value })}><option value="local">Local</option><option value="demo">Demo</option><option value="staging">Staging</option><option value="production">Producción</option><option value="planning">Planificación</option></select></FormField>
              </div>
              <FormField label="Descripción"><textarea value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={600} rows={3} /></FormField>
              {error && <p className="form-error" role="alert">{error}</p>}
              <div className="form-actions">
                {editing !== 'new' && <button className="danger-button" type="button" onClick={removeProject}><Trash2 size={14} /> Eliminar</button>}
                <button className="primary-button" type="submit" disabled={saving}><Save size={15} /> {saving ? 'Guardando…' : 'Guardar proyecto'}</button>
              </div>
            </form>

            {editing !== 'new' && (
              <section className="goals-editor">
                <div className="drawer-section-title"><div><small>OBJETIVOS</small><h3>Aislados en este proyecto</h3></div><span>{editing.goals.length}</span></div>
                <div className="goal-list">
                  {editing.goals.map((goal) => <div className="goal-item" key={goal.id}><span><Check size={14} /></span><div><strong>{goal.title}</strong><small>{goal.metric || 'Sin métrica'}{goal.targetValue !== null ? ` · meta ${goal.targetValue}` : ''}</small></div><div className="goal-item__actions"><button onClick={() => editGoal(goal)} aria-label={`Editar objetivo ${goal.title}`}><Pencil size={14} /></button><button onClick={() => removeGoal(goal.id)} aria-label={`Eliminar objetivo ${goal.title}`}><Trash2 size={14} /></button></div></div>)}
                  {editing.goals.length === 0 && <p className="empty-copy">Este proyecto todavía no tiene objetivos.</p>}
                </div>
                <form className="goal-form" onSubmit={addGoal}>
                  <FormField label="Nuevo objetivo"><input value={goalForm.title} onChange={(event) => setGoalForm({ ...goalForm, title: event.target.value })} required placeholder="Ej. Aumentar contactos calificados" /></FormField>
                  <div className="form-row"><FormField label="Métrica"><input value={goalForm.metric} onChange={(event) => setGoalForm({ ...goalForm, metric: event.target.value })} placeholder="contactos_mensuales" /></FormField><FormField label="Meta"><input type="number" min="0" value={goalForm.targetValue ?? ''} onChange={(event) => setGoalForm({ ...goalForm, targetValue: event.target.value ? Number(event.target.value) : null })} /></FormField></div>
                  <div className="goal-form__actions">
                    {editingGoalId && <button className="secondary-button" type="button" onClick={() => { setEditingGoalId(null); setGoalForm(emptyGoal) }}>Cancelar edición</button>}
                    <button className="secondary-button" type="submit" disabled={saving}>{editingGoalId ? <Save size={14} /> : <Plus size={14} />} {editingGoalId ? 'Actualizar objetivo' : 'Añadir objetivo'}</button>
                  </div>
                </form>
              </section>
            )}
          </section>
        </div>
      )}
    </div>
  )
}

function FormField({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="form-field"><span>{label}</span>{children}</label>
}
