import { useState, type FormEvent } from 'react'
import { ArrowRight, Eye, EyeOff, LockKeyhole, ShieldCheck } from 'lucide-react'
import { api } from './api'

export function LoginPage({ onAuthenticated }: { onAuthenticated: () => void }) {
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await api.login(password)
      onAuthenticated()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No fue posible iniciar sesión.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <div className="login-glow" />
      <section className="login-card">
        <div className="login-brand">
          <div className="brand__mark" aria-hidden="true"><span /><span /><span /></div>
          <div><strong>CONTROL</strong><small>Centro de proyectos</small></div>
        </div>
        <div className="login-copy">
          <span className="login-lock"><LockKeyhole size={22} /></span>
          <small>ACCESO LOCAL</small>
          <h1>Bienvenido de nuevo.</h1>
          <p>Ingresa la contraseña del administrador para acceder al centro de control.</p>
        </div>
        <form onSubmit={submit}>
          <label htmlFor="admin-password">Contraseña</label>
          <div className="password-field">
            <input
              id="admin-password"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
              autoFocus
            />
            <button type="button" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
              {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
            </button>
          </div>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="login-submit" type="submit" disabled={submitting || !password}>
            {submitting ? 'Verificando…' : 'Entrar al dashboard'} <ArrowRight size={16} />
          </button>
        </form>
        <div className="login-footnote"><ShieldCheck size={15} /><span>Sesión local protegida · sin servicios externos</span></div>
      </section>
    </main>
  )
}
