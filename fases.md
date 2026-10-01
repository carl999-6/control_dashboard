# Plan de desarrollo

Este archivo define el trabajo activo. El alcance general y las restricciones están en `contexto.md`; la forma de trabajo está en `metodologia de trabajo.md`.

## Decisiones técnicas iniciales

- Frontend: React, TypeScript y Vite.
- Backend: ASP.NET Core Web API.
- Datos de la demo: SQLite a partir de la Fase 2, conservando una capa de persistencia que permita migrar a PostgreSQL.
- Estructura: frontend y API separados, con conectores externos detrás de adaptadores.
- Seguridad inicial: sin credenciales reales ni escrituras externas; todos los datos visibles en la Fase 1 son simulados.

Estas decisiones se pueden revisar al cerrar la Fase 1 si la validación de Carlos requiere cambios.

### Fase 1 — Base, navegación y diseño del dashboard

- Objetivo: disponer de una aplicación local navegable que materialice la estructura del producto y su lenguaje visual.
- Alcance incluido:
  - Estructura inicial del frontend y de la API.
  - Dashboard global de demostración con datos simulados claramente identificados.
  - Selector visual de proyecto, navegación principal y páginas de módulo vacías pero navegables.
  - Diseño adaptable a escritorio y móvil.
  - Endpoint de salud de la API.
  - Documentación de instalación, ejecución, pruebas y detención.
- Fuera de alcance por ahora:
  - Base de datos, autenticación real, CRUD e integraciones externas.
  - Envíos a WordPress, Telegram, X o proveedores de IA.
  - Automatizaciones y despliegue.
- Entregables:
  - Aplicación web en `dashboard-web/`.
  - API en `Dashboard.Api/`.
  - Documentación y archivos de configuración seguros.
- Criterios de aceptación:
  - La interfaz inicia localmente y presenta una vista global coherente.
  - La navegación cambia entre todos los módulos previstos sin errores.
  - Los datos de demostración se distinguen de datos reales.
  - La interfaz es utilizable en anchos de escritorio y móvil.
  - El frontend compila, sus pruebas pasan y la API compila y responde en `/api/health`.
- Cómo se probará:
  - `npm test` y `npm run build` en el frontend.
  - `dotnet build` y solicitud HTTP al endpoint de salud.
  - Revisión visual en navegador en escritorio y móvil.
- Estado: aprobada.

### Fase 2 — Modelo multi-proyecto, acceso local y configuración

- Objetivo: persistir proyectos y preferencias con aislamiento por `project_id`.
- Alcance incluido: SQLite, migraciones, entidades base, datos semilla, acceso de administrador local, CRUD de proyectos y objetivos, y ajustes generales.
- Fuera de alcance por ahora: integraciones reales y acceso de clientes.
- Entregables: esquema de datos, API de proyectos, formularios y pruebas de aislamiento.
- Criterios de aceptación: los proyectos se crean, editan y consultan sin mezclar sus datos; el acceso local protege la aplicación.
- Cómo se probará: pruebas de API, migraciones desde cero y flujo manual en navegador.
- Estado: aprobada.

### Fase 3 — Marketing, campañas y analítica simulada

- Objetivo: demostrar el flujo de campañas, publicaciones, UTM, visitas y conversión.
- Alcance incluido: CRUD de campañas/publicaciones, generador UTM, embudo y datos importables/simulados.
- Fuera de alcance por ahora: X/Instagram y analítica conectados en vivo.
- Entregables: pantallas, endpoints y pruebas del flujo de atribución.
- Criterios de aceptación: una publicación puede asociarse a campaña y métricas sin afirmar causalidad o identidad no disponible.
- Cómo se probará: pruebas de validación, importación y recorrido completo.
- Estado: aprobada.

### Fase 4 — SEO, contenido y calendario editorial

- Objetivo: administrar oportunidades SEO desde la detección hasta la medición.
- Alcance incluido: oportunidades, briefs, borradores, calendario y estados de revisión.
- Fuera de alcance por ahora: generación pagada y publicación real.
- Entregables: flujo editorial manual completo y simulación de envío como borrador.
- Criterios de aceptación: cada pieza conserva hipótesis, línea base, responsable, estado e historial.
- Cómo se probará: pruebas de transiciones y recorrido editorial.
- Estado: aprobada.

### Fase 5 — Ejecuciones, aprobaciones, consumo y presupuestos

- Objetivo: registrar y controlar acciones automatizadas y su costo estimado.
- Alcance incluido: ejecuciones, auditoría, aprobación humana, tarifas versionadas, presupuestos, pausa y límites.
- Fuera de alcance por ahora: facturación garantizada por proveedores.
- Entregables: panel de costos, reglas de bloqueo y pruebas de concurrencia/idempotencia.
- Criterios de aceptación: un flujo sobre presupuesto se bloquea y toda ejecución es trazable.
- Cómo se probará: escenarios de éxito, fallo, reintento, duplicado y cuota agotada.
- Estado: lista para revisión.

### Fase 6 — Telegram de prueba y recuperación de fallos

- Objetivo: validar notificaciones configurables sin exponer secretos o datos personales.
- Alcance incluido: adaptador de Telegram, modo simulado, agrupación, severidad, horarios y reintentos.
- Fuera de alcance por ahora: operación 24/7.
- Entregables: configuración, historial y prueba opcional autorizada.
- Criterios de aceptación: las alertas respetan preferencias y los fallos quedan registrados sin duplicarse.
- Cómo se probará: bot simulado y, solo con autorización, bot real de prueba.
- Estado: pendiente.

### Fase 7 — Conectores reales seguros, uno por vez

- Objetivo: sustituir simulaciones por lecturas y escrituras autorizadas de bajo riesgo.
- Alcance incluido: conectores priorizados tras revisión; WordPress solo `draft` inicialmente.
- Fuera de alcance por ahora: publicación automática amplia, scraping y producción VPS.
- Entregables: adaptadores, configuración de secretos, sincronización manual, auditoría y documentación.
- Criterios de aceptación: cada conector funciona con permisos mínimos, límites, salida manual y pruebas de fallo.
- Cómo se probará: entorno de prueba y checklist de seguridad por conector.
- Estado: pendiente.

