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
- Estado: aprobada.

### Fase 6 — Telegram de prueba y recuperación de fallos

- Objetivo: validar notificaciones configurables sin exponer secretos o datos personales.
- Alcance incluido: adaptador de Telegram, modo simulado, agrupación, severidad, horarios y reintentos.
- Fuera de alcance por ahora: operación 24/7.
- Entregables: configuración, historial y prueba opcional autorizada.
- Criterios de aceptación: las alertas respetan preferencias y los fallos quedan registrados sin duplicarse.
- Cómo se probará: bot simulado y, solo con autorización, bot real de prueba.
- Estado: aprobada.

### Fase 7 — Conectores reales seguros, uno por vez

- Objetivo: sustituir simulaciones por lecturas y escrituras autorizadas de bajo riesgo.
- Alcance incluido:
  - Automatizaciones persistentes y configurables por proyecto, con ejecución programada y manual mientras la aplicación esté encendida.
  - Telegram real mediante secretos de entorno, conservando modo simulado para desarrollo y pruebas.
  - n8n Community local en Docker disponible para flujos que justifiquen orquestación visual; el worker interno de .NET ejecuta los flujos actuales y la API mantiene políticas, límites, aprobaciones, auditoría e historial.
  - Search Console en modo lectura como primer conector de datos.
  - Flujo `Search Console → deduplicación → selección → Gemini gratuito → validación → WordPress draft → historial → Telegram`.
  - Resúmenes diarios y semanales configurables desde el dashboard.
  - Reparto configurable de la cuota gratuita de Gemini entre borradores SEO y propuestas para X, sin prioridad fija ni proveedor pagado de respaldo.
- Fuera de alcance por ahora: publicación automática amplia, scraping y producción VPS.
- Entregables: adaptadores, configuración segura de secretos, programaciones y ejecución manual, worker local reutilizable en VPS, auditoría y documentación.
- Criterios de aceptación: cada conector funciona con permisos mínimos, límites, salida manual, ejecución programada, trazabilidad y pruebas de fallo; ninguna cuota agotada activa un proveedor pagado y WordPress nunca recibe `publish` en esta fase.
- Cómo se probará: entorno de prueba y checklist de seguridad por conector.
- Entregas internas:
  - 7A — Base de automatización, Telegram y n8n: lista para revisión. Incluye programaciones configurables, límites diarios, próxima/última ejecución, ejecución manual, historial, worker local, Telegram real por `.env`, metadatos de IA y configuración local de n8n. Search Console y el flujo SEO aparecen pausados hasta incorporar sus conectores.
  - 7B — Search Console OAuth de solo lectura y sincronización diaria: lista para revisión. Incluye OAuth con estado de un solo uso, token de actualización cifrado localmente, selección de la propiedad verificada del proyecto, sincronización manual o diaria de consultas y páginas, métricas persistentes, límites configurables, historial operativo y errores notificables por Telegram. La validación contra la cuenta real queda pendiente de completar el cliente OAuth local.
  - 7C — Gemini gratuito, deduplicación y validación de borradores: lista para revisión técnica. Incluye configuración por proyecto, selección de oportunidades con umbrales de Search Console, comprobación de contenido similar antes de consumir cuota, llamada real mediante clave de entorno, JSON validado, borrador local pendiente de revisión, tokens y costo cero en historial, límite diario y detención con aviso ante cuota agotada. La validación contra una clave real queda pendiente de disponer de datos procesados en Search Console y configurar `GEMINI_API_KEY`.
  - 7D — WordPress exclusivamente `draft` y notificación completa: aprobada. Incluye configuración por proyecto sin persistir la contraseña de aplicación, prueba de credenciales de solo lectura, creación real mediante REST con estado fijado en el backend, verificación de la respuesta `draft`, idempotencia por pieza, enlace al editor, historial/costo cero, conservación local ante fallos y Telegram con metadatos de Gemini cuando corresponde.
  - 7E — Asistente de X con revisión humana: aprobada por Carlos el 4 de octubre de 2026 tras probar el flujo. Incluye entrada manual sin costo y búsqueda reciente opcional de solo lectura, fuentes configurables, límites y costo estimado antes de consultar, selección/deduplicación de oportunidades, tres propuestas mediante Gemini gratuito, cola de revisión, registro manual de la respuesta publicada, UTM, historial, auditoría y Telegram. El sistema no contiene operaciones de escritura hacia X.
  - 7F — Analítica web real con PostHog: aprobada. Objetivo: medir visitas y conversiones de FyrStudios atribuidas a UTM y mostrarlas por proyecto, campaña y respuesta de X. Alcance: configuración PostHog Cloud EE. UU. por proyecto, captura en WordPress de páginas vistas y eventos de conversión sin datos personales, sincronización automática configurable y ejecución manual mediante el worker .NET, importación agregada e idempotente, vistas de marketing, historial y avisos Telegram. La conversión principal es un envío confirmado del formulario de solicitud de cotización; ambos nombres representan el mismo evento. El clic en WhatsApp es una señal secundaria. Fuera de alcance: grabación de sesiones, perfiles individuales, publicación automática, VPS y sustituir Search Console. Entregables: conector de consulta de solo lectura, instrumentación WordPress, configuración segura, dashboard y documentación. Criterios de aceptación: un enlace UTM de prueba produce una visita y una conversión distinguibles en PostHog y el dashboard; la sincronización manual y programada no duplica datos; los resultados se aíslan por proyecto y no contienen información personal; los fallos quedan en historial y notifican según la política. Pruebas: cliente simulado y pruebas de API para validación, atribución e idempotencia; compilación frontend/backend; prueba real guiada con FyrStudios una vez instaladas las credenciales y la instrumentación.
    - Implementación inicial verificada: 13 pruebas de API y compilación del frontend correctas. El propietario configuró PostHog y WordPress después; ya hay páginas vistas y conversiones reales importadas.
    - Corrección posterior: las consultas de sesiones ya no ocultan la columna `event`; contrastadas con la API real de PostHog. Vista de detalles técnicos por evento consultada bajo demanda, con origen UTM o referencia y sin persistencia de identificadores. 15 pruebas de API y compilación del frontend correctas. Carlos confirmó las cifras y detalles en pantalla tras reiniciar y sincronizar; la ejecución programada consta como exitosa en el historial local.
    - Mejoras posteriores a la demo: menú lateral plegable; atribución detallada de PostHog por UTM; oportunidades SEO manuales seleccionables cuando Search Console no tiene volumen; lectura de X separada de la generación, con reutilización de cola y sin solicitar perfiles de autor. Verificación automatizada: 17 pruebas de API y 5 de frontend, más compilación de producción.
- Estado: demo local cerrada por decisión de Carlos el 5 de octubre de 2026; entregas 7D–7F aprobadas. Las validaciones externas pendientes de 7A–7C se conservan arriba y las mejoras posteriores se registran en [`docs/mejoras futuras.md`](docs/mejoras%20futuras.md). No se define ninguna fase nueva sin acuerdo explícito.

