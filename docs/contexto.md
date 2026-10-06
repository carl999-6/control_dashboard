# Contexto del proyecto: centro de control multi-proyecto

Actualizado: 1 de octubre de 2026. Este documento sirve para entregar el contexto a otra IA. **Es una especificación de intención y una hoja de ruta, no una afirmación de que todas las integraciones ya estén construidas.** La prioridad inmediata es una demo local funcional; la producción en VPS vendrá después.

## 1. Visión y propósito

Carlos quiere construir un sistema web propio, reutilizable para todos sus proyectos, que reúna en un solo dashboard desarrollo, operaciones, marketing, SEO, analítica de usuarios, automatizaciones, alertas y gastos de API. El primer proyecto conectado sería **FyrStudios** (`fyrstudios.com`, hecho en WordPress); más adelante se agregarían sus propios SaaS y, potencialmente, proyectos de clientes. No se quiere depender de visitar cinco paneles todos los días, aunque sí habrá cuentas externas que deban conectarse y mantenerse.

El objetivo no es reproducir completamente PostHog, GitHub, Google Search Console o WordPress, sino ofrecer una **consola de decisiones y ejecución** que reúna sus datos importantes, identifique oportunidades, proponga acciones, registre lo que se hizo y mida el resultado. Telegram es un canal de notificación, no el dashboard. El PDF de referencia compartido por Carlos, `intrucciones.pdf`, muestra una estética de consola oscura, vistas generales y por publicación/visita, un ciclo de respuestas en X y otro de blog/SEO. Las capturas son referencias conceptuales y visuales, no datos reales ni requisitos exactos de pantalla.

La metodología debe ser progresiva, de costo extra mínimo al inicio y escalable: empezar con entradas manuales y servicios gratuitos; posteriormente habilitar APIs pagadas y automatización más frecuente mediante configuración, sin rehacer el núcleo.

## 2. Alcance multi-proyecto

- Una cuenta administradora puede crear proyectos independientes: nombre, dominio, tipo (WordPress, SaaS propio, etc.), objetivos, zona horaria, entorno y estado.
- Todos los registros deben asociarse a `project_id`: integraciones, campañas/UTM, publicaciones, artículos, métricas, eventos, alertas, tareas, ejecuciones, consumos y presupuestos.
- El mismo sistema debe poder manejar FyrStudios ahora y un SaaS futuro sin duplicar código; cada proyecto podrá activar módulos y conectores distintos.
- Un dashboard global compara proyectos y un dashboard de proyecto da el detalle. Se requieren filtros de rango temporal y fuente de datos; mostrar hora y moneda de forma consistente (quetzales como moneda de presentación, reteniendo USD y tipo de cambio aplicado cuando la factura sea en USD).
- Las credenciales son por integración/proyecto, cifradas o protegidas como secretos. No almacenar contraseñas o tokens en Git, informes, Telegram ni logs visibles.

## 3. Módulos deseados

### A. Desarrollo y operaciones

- Repositorios GitHub: commits, ramas, pull requests, fases, resultados de pruebas, releases, despliegues y cambios pendientes de revisión. El desarrollo asistido por IA avanza por fases pequeñas: implementar, probar, revisar y aceptar antes de continuar.
- Estado de sitios y APIs; recursos de VPS (CPU, RAM, disco), contenedores Docker, incidencias y recuperaciones.
- Backups: fecha, éxito/fallo, tamaño, destino, último backup válido y **prueba de restauración** cuando exista. GitHub no sustituye el respaldo de bases de datos, archivos subidos y configuración.
- Avisos de dependencias y sistema operativo; en producción, actualizaciones y despliegues de riesgo requieren intervención o políticas explícitas.

### B. Marketing y comportamiento

- Publicaciones de X/Twitter e Instagram: calendario, URL/ID, tema, formato, campaña, interacción y métricas disponibles. Diferenciar datos capturados por API de datos introducidos manualmente.
- Generador de enlaces con parámetros UTM para atribuir visitas a publicaciones o campañas. Ejemplo: `utm_source=x&utm_medium=organic&utm_campaign=servicios&utm_content=post_001`.
- Tráfico y embudos: publicación -> clic -> landing -> contacto/registro -> primer uso -> pago o retorno, según el proyecto. Para FyrStudios la conversión principal puede ser formulario, WhatsApp o solicitud de cotización; para un SaaS, activación y pago.
- Recorridos, eventos, abandono, errores, sesiones y mapas de calor según capacidades de PostHog/Clarity/GA4. Interpretar atribución con cautela: UTM identifica la campaña del enlace, no prueba identidad ni causalidad perfecta entre impresiones en X y visitas web.
- Vistas globales, por publicación y por visita/sesión cuando el proveedor lo permita, respetando privacidad y enmascarando datos sensibles.

### C. Asistente de X

El concepto del PDF sigue un ciclo **escucha -> evalúa -> redacta -> Carlos publica -> mide -> aprende**:

1. Buscar contenido relevante de cuentas/temas seleccionados y detectar oportunidades, como publicaciones con alto alcance relativo y poca conversación, solo si las métricas están disponibles legal y técnicamente. No suponer que las impresiones de publicaciones ajenas estarán accesibles.
2. Combinar reglas (relevancia, frescura, métricas, presupuesto y frecuencia) con valoración de IA. Priorizar las oportunidades y evitar ruido.
3. Generar alternativas de respuesta en un estilo definido por Carlos, útiles para la conversación, sin promociones forzadas. Mostrar contexto y por qué se sugieren.
4. Notificar o dejar la propuesta en el dashboard. **Carlos revisa, copia y publica manualmente**. Registrar enlace al post resultante y UTM si corresponde.
5. Medir interacción y tráfico/conversiones propios; resumir aprendizajes y ajustar futuras propuestas. La IA puede sugerir cambios de estilo, pero no actualizar silenciosamente reglas críticas.

Sin X API: Carlos registra URLs y algunas métricas manualmente. Con X API: conector opcional, activable por proyecto, sujeto a disponibilidad, permisos, términos, costos y límites; no scraping como dependencia del producto. Publicar manualmente no elimina la necesidad de cumplir reglas contra spam. No prometer crecimiento automático ni publicar respuestas masivas.

### D. SEO y blog

El concepto del PDF sigue **escucha -> decide -> escribe -> publica -> comprueba -> repite**:

1. Consultar Search Console, rendimiento de páginas, consultas y contenido ya existente; detectar temas sin cobertura o páginas susceptibles de mejora. Los datos requieren propiedad verificada y volumen suficiente.
2. Decidir si crear, actualizar, fusionar o no publicar; guardar hipótesis, línea base y objetivo. Evitar artículos repetidos por calendario.
3. Generar brief y borrador con IA (Gemini API gratuita si aplica, proveedor pago opcional, o redacción manual en ChatGPT/Codex y pegado en el dashboard). Añadir revisión factual, utilidad original, enlaces internos, título y metadatos.
4. Enviar a WordPress por REST API preferentemente con estado `draft`/`pending`; **no publicar automáticamente en la fase inicial**. Carlos revisa y aprueba. En una fase posterior, permitir publicación programada o automática solo bajo políticas expresas y registro de auditoría.
5. Medir impresiones, clics, CTR, consultas, conversiones y cambios a 21/45/90 días, contemplando estacionalidad y muestras pequeñas. Presentar conclusiones como hipótesis, no como certeza causal.

Google no prohíbe el uso de IA por sí mismo, pero el contenido masivo sin valor para el usuario puede infringir sus políticas de spam. No consultar resultados de Google mediante scraping automatizado; preferir Search Console y fuentes autorizadas.

### E. Calendario, decisiones y experimentos

- Calendario mensual de tareas, objetivos, fechas de revisión editorial, campañas, mantenimiento y experimentos.
- Cola de propuestas con estados (`detectada`, `borrador`, `pendiente_de_revision`, `aprobada`, `programada`, `publicada`, `medida`, `descartada`, `fallida`), responsable, timestamps y trazabilidad.
- Para cambios de sitio, comparar antes/después con línea base, muestra y periodo. La IA puede proponer mejoras de landing, CTA y navegación; cambios en producción deben pasar por revisión, pruebas y mecanismo de reversión.

### F. Consumo de APIs y controles

- Registro por llamada o lote: proyecto, proveedor, modelo/endpoint, flujo, fecha, cantidad (tokens de entrada/salida, objetos leídos, solicitudes), costo estimado en moneda original y Q, estado, identificador de ejecución y error si hubo.
- Panel de costos: total por proyecto, proveedor y flujo; tendencias; consultas/acciones más costosas; presupuesto diario/mensual; previsión simple y alertas de umbral.
- Controles de ejecución: límites de volumen, frecuencia, topes internos, modo pausa, botón de emergencia, aprobación humana y presupuesto por integración. Si se llega a cuota gratuita o límite propio, bloquear nuevas solicitudes de ese flujo y notificar.
- El gasto interno es una **estimación** calculada con tarifas versionadas; contrastarlo con la facturación real del proveedor. Un control interno por sí solo no garantiza un límite duro frente a llamadas ajenas al sistema, precios cambiantes, cargos diferidos o reintentos concurrentes. Usar créditos prepagados/topes del proveedor cuando existan; distinguir alertas de presupuesto de límites efectivos de facturación.

## 4. Telegram

Telegram comunica incidentes, tareas pendientes, acciones completadas y resúmenes; el dashboard conserva el historial y permite configuración de categorías, proyecto, severidad, horario y frecuencia. Evitar un mensaje por cada commit o API call salvo que Carlos lo active; agrupar el resto.

Ejemplo de actividad solicitada:

> ✅ Automatización completada  
> Proyecto: FyrStudios  
> Servicio: Gemini  
> Flujo: Generar artículo SEO  
> Modelo: Gemini Flash  
> Fecha: 30/09/2026  
> Tokens entrada: 8,400  
> Tokens salida: 2,100  
> Costo estimado: Q0.12  
> Estado: borrador creado en WordPress; pendiente de aprobación.

Otros mensajes: caídas y recuperación, pruebas fallidas, despliegue, backup fallido/completado, poco espacio, límite de API alcanzado, tarea manual requerida, resumen diario/semanal. No incluir secretos ni datos personales de visitantes.

## 5. Demo LOCAL: alcance y límites

**Decisión actual: construir primero la demo local, no desplegar producción ni conectar operaciones de escritura en FyrStudios sin aprobación explícita.** La demo debe ser multi-proyecto desde su modelo de datos, aunque se muestre FyrStudios como ejemplo.

Funcionalidad viable localmente:

- Login del administrador y dashboard global/por proyecto; datos semilla realistas claramente etiquetados como simulados.
- CRUD de proyectos, objetivos, campañas, publicaciones, artículos, calendario, integraciones (configuración sin secretos reales al principio), reglas, presupuestos y notificaciones.
- Vistas de analítica, embudos, visitas, actividad técnica y costos. Simular resultados de X, Gemini, Search Console, WordPress y GitHub para demostración sin facturación.
- Flujo manual completo: detectar/seleccionar oportunidad -> generar o pegar borrador -> revisar -> simular envío a WordPress -> registrar resultado -> simular Telegram. Como pruebas opcionales y separadas, conectar APIs reales con consentimiento y modo seguro (`draft` para WordPress).
- Botones para importar CSV/JSON o sincronizar bajo demanda datos reales autorizados. Si se usa una herramienta de analítica alojada, ella puede captar datos de FyrStudios mientras la app local está apagada; el dashboard los consulta al encenderse.
- Scheduler local opcional que procesa tareas vencidas al arrancar, con idempotencia, sin prometer avisos puntuales mientras el equipo esté apagado. Tests de cuotas agotadas, fallos/reintentos, duplicados, aprobación y aislamiento de proyectos.

La app local **no necesita estar encendida 24/7** para panel e informes bajo demanda. Solo ejecutará generación, sincronización, publicación y alertas mientras la laptop esté encendida y el proceso corra. No puede recibir webhooks públicos sin infraestructura adicional. La demo puede correr con frontend, API y PostgreSQL/SQLite; n8n es opcional, no un requisito para mostrar flujos. Evitar instalar PostHog completo, Grafana y un LLM local juntos sin necesidad. Las cifras previas de RAM fueron orientativas, no mediciones: probar en la laptop real (Lenovo LOQ, 12 GB RAM, RTX 4050 6 GB); el consumo depende mucho de Docker Desktop/WSL, navegador, número de servicios y si corre Ollama.

### Forma de trabajo para implementar la demo

Trabajar por fases pequeñas con IA: una fase implementada -> pruebas automáticas y manuales -> revisión de Carlos -> commit/rama -> recién entonces fase siguiente. No pedir a la IA que genere todo el sistema de golpe. `main` estable y ramas de tarea; `develop` solo si aporta valor, no es obligatorio. Commits frecuentes y claros no afectan el rendimiento de la app; eliminar ramas ya integradas y evitar credenciales o archivos grandes en Git.

Fases sugeridas (propuesta, ajustable tras especificar pantallas y stack):

1. Base de proyecto, Docker opcional, navegación y diseño del dashboard.
2. Modelo multi-proyecto, autenticación local, ajustes y datos simulados.
3. Marketing: publicaciones, campañas/UTM, embudo y visitas simuladas/importables.
4. SEO: oportunidades, briefs, borradores, calendario y máquina de estados.
5. Flujos de ejecución, aprobación, historial, presupuestos y consumo simulado.
6. Notificaciones Telegram de prueba y manejo de fallos (conector desacoplado).
7. Conectores de lectura y escritura seguros, uno por vez (por ejemplo, Search Console y WordPress solo como borrador), si Carlos los autoriza.

Stack propuesto, **no decidido irrevocablemente**: React + Vite o Next.js para frontend, .NET Web API para backend, PostgreSQL para datos, Docker Compose para reproducibilidad. Para una demo muy ligera SQLite puede sustituir PostgreSQL, siempre que se planifique migración. n8n Community puede orquestar algunos flujos; alternativa: jobs/worker propios y cron. Diseñar interfaces/adaptadores para proveedores en lugar de acoplar lógica de negocio a n8n. La versión exacta de framework y estructura de repositorio se define antes de programar.

## 6. Producción futura: VPS y automatización 24/7

Para monitorizar caídas mientras la PC está apagada, recibir webhooks, enviar alertas oportunas y ejecutar tareas por horario hace falta un entorno siempre activo (VPS o servicio equivalente). Una VPS propia es la ruta considerada; no es obligatorio que todos los servicios compartan una sola máquina. Migrar la app local a VPS manteniendo frontend/API/BD y habilitar workers/scheduler persistentes.

Componentes posibles:

- HTTPS y dominio/subdominio privado para dashboard, acceso autenticado y 2FA si procede; secretos fuera del repo y permisos mínimos.
- Frontend/API + PostgreSQL con migraciones; job queue/worker y scheduler con reintentos acotados, idempotencia, rate limiting y registro de auditoría.
- Conectores por proyecto: WordPress REST, Search Console, analítica/PostHog, GitHub, Telegram, X API opcional, Instagram/Meta opcional, proveedor de IA configurable.
- Uptime Kuma/monitor externo para disponibilidad, monitoreo de VPS y Docker, alertas, copias fuera de la VPS y restauraciones ensayadas. Si el monitor vive **solo en la misma VPS**, no podrá avisar cuando la VPS completa caiga; para esa situación usar monitor externo.
- GitHub Actions para pruebas/despliegues controlados; WordPress no se reemplaza por el dashboard. No publicar, actualizar sistema operativo ni desplegar cambios críticos sin políticas aprobadas.
- Privacidad: política y consentimiento donde correspondan, retención de datos, minimización, enmascaramiento de sesiones, exclusión de formularios, contraseñas, tarjetas y documentos; segregación por proyecto/cliente.

Modos por proyecto/flujo: `manual`, `asistido`, `automático limitado`; un modo automático más amplio solo tras pruebas. Activar una API pagada debe implicar habilitación explícita, tarifas configuradas, cuota, presupuesto y comportamiento al fallar. Así FyrStudios puede empezar barato y crecer; los SaaS futuros pueden usar las mismas piezas.

## 7. Integración con WordPress de FyrStudios

WordPress permanece como sitio público y gestor de contenidos. El dashboard es un sistema separado. La REST API nativa de WordPress permite crear/editar posts con estados `draft`, `pending`, `future` y `publish`; se puede autenticar vía HTTPS con una **contraseña de aplicación revocable** de una cuenta dedicada y permisos mínimos. Verificar que la instalación/hosting, plugin de seguridad y rol permitan esas operaciones. La contraseña de aplicación hereda permisos de la cuenta; no asumir que el rol Editor impide publicar. Si se requiere *solo borradores*, el backend debe bloquear `publish` y/o usar una cuenta/capacidades restringidas.

Flujo asistido: Search Console/analítica -> oportunidad SEO -> Gemini genera borrador **o** Carlos pega texto generado manualmente -> dashboard valida/guarda -> WordPress recibe `draft` -> Telegram avisa -> Carlos revisa y aprueba -> se publica/programa según política. El paso WordPress REST es independiente de la IA: aunque Gemini se quede sin cuota, Carlos puede pegar el texto en el dashboard y reanudar la automatización desde ahí. No automatizar extracción del contenido de ChatGPT web mediante credenciales o scraping.

Para eventos web se podrá instalar un SDK/píxel de analítica y eventos relevantes en WordPress (plugin/Tag Manager/código del tema, conforme a privacidad); la app recopilará resultados agregados mediante integraciones disponibles. Metadatos de plugins SEO pueden requerir compatibilidad o integración particular: probar antes de prometer edición universal por REST.

## 8. Servicios, cuotas y costos: hipótesis, no garantías

- Search Console API: gratuita con cuotas; Google debe tener FyrStudios verificado. Telegram Bot API: sin tarifa por los mensajes usuales, sujeto a límites. GitHub y herramientas autoalojadas pueden iniciar con planes/licencias gratuitas, pero servidor, mantenimiento y backups tienen costo real.
- PostHog alojado tiene un nivel gratuito sujeto a límites y precios cambiantes. Sirve como motor de eventos/embudos/sesiones; el dashboard propio muestra resúmenes y decisiones. GA4/Clarity son alternativas/complementos, evitando duplicación innecesaria y cumpliendo privacidad.
- Gemini API dispone de nivel gratuito **para modelos elegibles** y límites variables por modelo, proyecto, nivel y tiempo; Google muestra límites vigentes en AI Studio. Se evalúan RPM, tokens/minuto y solicitudes/día, entre otros. **No hay un número universal de artículos gratis.** Un artículo puede requerir varias llamadas, reintentos y miles de tokens; el techo real se obtiene midiendo prompts y respuesta, comprobando el límite en la cuenta. El nivel gratuito puede tener condiciones de uso de datos distintas al pago; no enviar PII. Si se agota la cuota, pausar flujo, avisar y pasar a generación manual o reintentar después del reinicio; no migrar automáticamente a proveedor de pago sin autorización.
- X API: modelo y tarifas por uso pueden cambiar; leer posts, perfiles, métricas y escribir pueden facturarse de maneras distintas. Antes de habilitarla validar si los endpoints y métricas deseados están disponibles para la cuenta y revisar el tarifario oficial. Sin API se siguen midiendo **clics que llegan a tu sitio** con UTM, pero impresiones/interacciones de X se ingresan manualmente.
- ChatGPT/Codex usado para programar o redactar a mano no equivale a una API incluida para ejecución desatendida. Cualquier modelo por API pagada debe tener presupuesto y autorización separada.
- Cifras discutidas anteriormente (por ejemplo, Q75 para IA y Q100 para X como topes internos, o rangos más amplios de VPS/producción) fueron **presupuestos de ejemplo**, no facturas, cotizaciones ni límites garantizados. Una estimación real necesita frecuencia, tamaño de artículos, número de posts leídos, tarifa del modelo/endpoints, infraestructura contratada e impuestos/cambio. No prometer costo Q0 si aún hay hosting, dominio, electricidad o servicios existentes.

Fuentes oficiales útiles para revisar antes de implementar: [WordPress REST Posts](https://developer.wordpress.org/rest-api/reference/posts/), [WordPress Application Passwords](https://developer.wordpress.org/rest-api/using-the-rest-api/authentication/), [Search Console API](https://developers.google.com/webmaster-tools/), [Gemini límites](https://ai.google.dev/gemini-api/docs/rate-limits), [Gemini precios](https://ai.google.dev/gemini-api/docs/pricing), [X Developer Platform](https://developer.x.com/), [PostHog precios](https://posthog.com/pricing), [políticas de spam de Google](https://developers.google.com/search/docs/essentials/spam-policies) y [autenticidad de X](https://help.x.com/en/rules-and-policies/authenticity).

## 9. Decisiones tomadas y preguntas pendientes

**Tomadas:** empezar con demo local; arquitectura multi-proyecto; FyrStudios como caso inicial; dashboard propio como centro y Telegram como avisos; WordPress como CMS existente; humanos revisan respuestas en X y artículos al inicio; priorizar herramientas gratuitas y topes de gasto; la VPS llegará cuando se desee autonomía 24/7. Para la demo se adoptó React + TypeScript + Vite en el frontend, ASP.NET Core en la API y SQLite con migraciones desde la Fase 2. El acceso inicial es de un único administrador local mediante cookie; las cuentas de clientes quedan fuera de la demo actual. En la Fase 3 se decidió almacenar analítica de marketing como conteos agregados sin identidad personal, etiquetar su procedencia (`manual`, `simulated`, `import_csv` o `import_json`) y validar que campañas, publicaciones y eventos asociados pertenezcan siempre al mismo proyecto. Los UTM se presentan como señal de atribución del enlace, no como prueba de identidad o causalidad. En la Fase 4 el flujo editorial usa transiciones validadas y un historial inmutable de decisiones; cada pieza conserva hipótesis, línea base, objetivo y responsable. El supuesto envío a WordPress solo genera una referencia local simulada y no realiza ninguna llamada externa. En la Fase 5 cada ejecución conserva una clave de idempotencia única por proyecto, la versión exacta de la tarifa usada, costo estimado en USD y GTQ, estado y auditoría. Las ejecuciones pendientes, aprobadas, completadas o fallidas reservan presupuesto; las canceladas y bloqueadas no. Los límites diarios y mensuales se calculan con la zona horaria configurada en el proyecto. La exclusión concurrente actual es local al proceso y la base añade una restricción única; una producción con varias instancias necesitará coordinación distribuida. Ningún registro de esta fase llama a un proveedor ni representa facturación real. En la Fase 6 cada proyecto dispone de una política de notificaciones con severidad mínima, agrupación y horario silencioso calculado en su zona horaria. El adaptador actual de Telegram es exclusivamente simulado: no recibe credenciales ni realiza tráfico externo. Los registros persistentes conservan estado, ocurrencias agrupadas, intentos y errores; un fallo simulado se recupera por un reintento manual. La agrupación concurrente está protegida dentro del proceso local; una futura ejecución multiinstancia requerirá coordinación distribuida.

**Decisiones de la Fase 7:** las automatizaciones deben funcionar realmente en local mientras la API y la computadora estén encendidas, con la misma arquitectura migrable a VPS. El worker interno de .NET ejecuta los flujos actuales; n8n Community en Docker queda disponible para futuras integraciones que justifiquen orquestación visual. La API mantiene políticas, límites, aprobaciones, programación e historial. Search Console será el primer conector y usará acceso de solo lectura. Telegram enviará a un chat privado desde un bot general para varios proyectos; token y chat ID solo existen en `.env`. Los avisos iniciales cubren errores, fallos externos, sincronizaciones, borradores de WordPress, cuota Gemini agotada, flujos completados y resúmenes diarios/semanales. LinkedIn queda fuera por ahora; X conserva propuesta automática y publicación humana; Instagram queda pendiente de confirmar si la cuenta profesional es Business o Creator. Gemini utilizará solo cuota gratuita y, al agotarse, el flujo se detendrá sin proveedor pagado alternativo. Su cuota se repartirá de forma configurable entre borradores SEO y propuestas de respuesta para X, sin prioridad fija de un flujo sobre el otro. WordPress usará `https://fyrstudios.com/`, el usuario dedicado `dashboard-bot` y únicamente estado `draft`.

**Implementación de la Fase 7B:** Search Console se conecta mediante OAuth 2.0 con el alcance `webmasters.readonly`; el estado OAuth es aleatorio, caduca y solo puede utilizarse una vez. El token de actualización se cifra localmente y nunca se entrega al frontend. La API selecciona únicamente una propiedad verificada compatible con el dominio del proyecto y sincroniza consultas y páginas consolidadas hasta dos días antes de la fecha actual. El rango, límite de filas, frecuencia y ejecución manual se controlan desde el dashboard. Los resultados y errores se guardan por proyecto y pasan por el mismo historial de automatizaciones y notificaciones.

**Implementación de la Fase 7C:** la clave de Gemini solo se carga desde `.env` y el proyecto debe permanecer en Free Tier sin facturación para garantizar que Google no cobre. Antes de cada llamada, el sistema exige datos de Search Console, aplica umbrales configurables de impresiones, CTR y posición, descarta consultas con contenido similar y respeta un máximo diario. Gemini devuelve un documento estructurado; título, extensión, campos editoriales y metadatos se validan antes de guardar. El resultado queda como `draft` local pendiente de revisión humana. Tokens de entrada/salida, proveedor, modelo, estado y costo estimado cero quedan en el historial. La cuota agotada bloquea el flujo y genera un aviso, sin proveedor pagado alternativo.

**Implementación de la Fase 7D:** WordPress utiliza autenticación Basic sobre HTTPS con el usuario dedicado y una contraseña de aplicación que solo se lee desde `.env`. URL, usuario y activación se configuran por proyecto. La única operación de escritura disponible crea posts con `status: draft` fijado en el adaptador; la respuesta también debe confirmar `draft`. El ID y enlace de edición se guardan para que un reintento sea idempotente y nunca duplique la pieza. Si la llamada falla, el borrador local permanece disponible. Cada intento queda en ejecuciones con costo cero y el éxito o fallo genera una notificación de Telegram con metadatos de IA cuando el contenido proviene de Gemini. Revisar y publicar continúa siendo manual dentro de WordPress.

**Implementación de la Fase 7E:** el asistente de X admite una ruta manual sin costo de X y una búsqueda reciente opcional mediante API de solo lectura. La lectura automática está desactivada por defecto, exige token de entorno, habilitación explícita, tarifa editable y presupuesto disponible. Las fuentes se deduplican por ID del post y se priorizan con señales agregadas. Gemini devuelve una respuesta recomendada y dos alternativas de hasta 235 caracteres para reservar espacio a un enlace medible; quedan en una cola donde Carlos puede elegir, editar o descartar. Cada opción usa el formato `respuesta + enlace UTM`. El botón del dashboard y los enlaces de Telegram abren el compositor de respuesta de X con el texto precargado, pero no publican: Carlos confirma en X y luego registra la URL de su respuesta. El sistema guarda tokens, costos USD/GTQ, estados y auditoría. El UTM solo se convierte en datos de visitas cuando una herramienta de analítica instalada en FyrStudios lo captura y esos datos se importan o sincronizan; construir el enlace no registra visitas por sí mismo. No existe ninguna operación de escritura hacia X.

**Decisión de la Fase 7F:** PostHog Cloud en región estadounidense será la fuente de analítica web de FyrStudios y convivirá con Search Console. La conversión principal es el envío confirmado del formulario de solicitud de cotización; “formulario enviado” y “solicitud de cotización” son el mismo evento y se cuentan una sola vez. Los clics en WhatsApp son una señal secundaria. El sitio público usa WPForms AJAX, formulario ID 815 en `/contacto/`, verificado el 4 de octubre de 2026. La API importa únicamente agregados por fecha, evento y UTM; la clave personal de consulta se lee de `.env` y debe tener permiso `query:read`. La instalación del rastreador en WordPress y la prueba con datos reales requieren acceso del propietario. El worker .NET programa y ejecuta la sincronización; n8n no es necesario para este flujo.

**Por decidir antes de implementar:** nombre del producto; pantallas exactas del MVP; política de privacidad y retención de analítica; periodicidad editorial; definición de “publicación muy recurrente”; presupuestos reales y modo de IA; esquema de acceso a clientes; y si habrá monetización del dashboard como producto o como servicio de FyrStudios.

## 10. Instrucciones para cualquier IA que reciba este documento

1. Distingue **demo local** de **producción VPS** y datos simulados de datos reales. No presupongas que el proyecto ya existe ni que hay credenciales configuradas.
2. No construyas todo de golpe: propón una fase acotada, entrega criterios de aceptación, implementa y prueba; espera validación antes de avanzar.
3. Antes de recomendar una integración, verifica documentación, tarifas, acceso y políticas actuales. Nunca prometas un número fijo de artículos gratuitos ni una métrica de terceros que la API no exponga.
4. Ninguna llamada pagada, publicación en vivo, despliegue o cambio a WordPress/FyrStudios se hace sin la autorización correspondiente. Mantén modos simulados y `draft` como valores iniciales.
5. Preserva independencia entre proyectos, trazabilidad, privacidad, reversibilidad, presupuestos y una salida manual si falla una API.
