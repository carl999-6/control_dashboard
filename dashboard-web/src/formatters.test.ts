import { describe, expect, it } from 'vitest'
import { formatQuetzales, formatRelativeTime } from './formatters'

describe('formatters', () => {
  it('presenta costos en quetzales', () => {
    expect(formatQuetzales(18.42)).toContain('18.42')
    expect(formatQuetzales(18.42)).toContain('Q')
  })

  it('presenta tiempos relativos legibles', () => {
    expect(formatRelativeTime(12)).toBe('hace 12 min')
    expect(formatRelativeTime(130)).toBe('hace 2 h')
    expect(formatRelativeTime(3000)).toBe('hace 2 d')
  })
})

