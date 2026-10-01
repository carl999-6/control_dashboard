import { useEffect, useState, type FormEvent } from 'react'
import { Check, Save, Settings, ShieldCheck } from 'lucide-react'
import { api, type Preference } from './api'

export function SettingsPage() {
  const [settings, setSettings] = useState<Preference | null>(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.getSettings().then(setSettings).catch((reason) => setError(reason instanceof Error ? reason.message : 'No fue posible cargar los ajustes.'))
  }, [])

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!settings) return
    setSaving(true)
    setMessage('')
    setError('')
    try {
      const updated = await api.updateSettings({
        timeZone: settings.timeZone,
        currency: settings.currency,
        defaultDateRangeDays: settings.defaultDateRangeDays,
        compactNotifications: settings.compactNotifications,
      })
      setSettings(updated)
      setMessage('Preferencias guardadas localmente.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No fue posible guardar los ajustes.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page settings-page">
      <section className="page-heading"><div><div className="eyebrow"><span /> Configuración local</div><h1>Ajustes</h1><p>Preferencias generales para presentar fechas, costos y notificaciones.</p></div></section>
      <div className="settings-layout">
        <nav className="settings-nav"><button className="active"><Settings size={16} /> General</button><button disabled><ShieldCheck size={16} /> Seguridad <small>Próximamente</small></button></nav>
        <section className="settings-panel">
          {!settings ? <p className="loading-copy">Cargando preferencias…</p> : (
            <form onSubmit={submit}>
              <div className="settings-heading"><span><Settings size={20} /></span><div><h2>Preferencias generales</h2><p>Se aplicarán a todos los proyectos salvo que exista una configuración específica.</p></div></div>
              <label className="setting-row"><div><strong>Zona horaria</strong><small>Base para calendarios, tareas y reportes.</small></div><input value={settings.timeZone} onChange={(event) => setSettings({ ...settings, timeZone: event.target.value })} required /></label>
              <label className="setting-row"><div><strong>Moneda de presentación</strong><small>Los importes originales conservarán su moneda y tipo de cambio.</small></div><select value={settings.currency} onChange={(event) => setSettings({ ...settings, currency: event.target.value })}><option value="GTQ">GTQ · Quetzal</option><option value="USD">USD · Dólar</option></select></label>
              <label className="setting-row"><div><strong>Rango predeterminado</strong><small>Periodo inicial de las vistas analíticas.</small></div><select value={settings.defaultDateRangeDays} onChange={(event) => setSettings({ ...settings, defaultDateRangeDays: Number(event.target.value) })}><option value={7}>7 días</option><option value={30}>30 días</option><option value={45}>45 días</option><option value={90}>90 días</option></select></label>
              <label className="setting-row setting-row--toggle"><div><strong>Agrupar notificaciones</strong><small>Evita mensajes individuales para actividad de baja prioridad.</small></div><input type="checkbox" checked={settings.compactNotifications} onChange={(event) => setSettings({ ...settings, compactNotifications: event.target.checked })} /></label>
              {message && <p className="form-success"><Check size={14} /> {message}</p>}
              {error && <p className="form-error">{error}</p>}
              <div className="settings-actions"><button className="primary-button" type="submit" disabled={saving}><Save size={15} /> {saving ? 'Guardando…' : 'Guardar cambios'}</button></div>
            </form>
          )}
        </section>
      </div>
    </div>
  )
}
