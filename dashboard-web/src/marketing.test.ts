import { describe, expect, it } from 'vitest'
import { buildUtmUrl, parseMarketingImport } from './marketing'

describe('buildUtmUrl', () => {
  it('conserva parámetros y fragmento, y agrega UTM codificados', () => {
    expect(buildUtmUrl('https://fyrstudios.com/servicios?lang=es#contacto', 'Instagram', 'Social', 'Servicios Q4', 'Carrusel 1'))
      .toBe('https://fyrstudios.com/servicios?lang=es&utm_source=instagram&utm_medium=social&utm_campaign=servicios+q4&utm_content=carrusel+1#contacto')
  })
})

describe('parseMarketingImport', () => {
  it('convierte CSV a eventos agregados y etiqueta su origen', () => {
    const events = parseMarketingImport('stage,source,medium,count,landingPath\nvisit,instagram,social,20,/servicios\ncontact,instagram,social,2,/contacto', 'csv')
    expect(events).toHaveLength(2)
    expect(events[0]).toMatchObject({ stage: 'visit', source: 'instagram', count: 20, dataSource: 'import_csv' })
  })

  it('rechaza etapas que no pertenecen al embudo', () => {
    expect(() => parseMarketingImport('[{"stage":"purchase","source":"x","count":1}]', 'json')).toThrow('Etapa no válida')
  })
})
