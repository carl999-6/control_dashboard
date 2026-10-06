# Centro de control multi-proyecto

Demo local de un dashboard para centralizar decisiones y actividad de desarrollo, operaciones, marketing, SEO, automatizaciones y consumo de APIs. FyrStudios es el primer caso de ejemplo, pero la arquitectura se diseña para varios proyectos.

> Estado actual: demo local cerrada por Carlos el 5 de octubre de 2026. PostHog ya mide visitas y conversiones procedentes de enlaces UTM. Las validaciones externas pendientes y las ideas posteriores se distinguen en [`fases.md`](fases.md) y [`docs/mejoras futuras.md`](docs/mejoras%20futuras.md).

## Requisitos

- Node.js 22 o posterior.
- npm 10 o posterior.
- .NET SDK 10.
- Docker Desktop para ejecutar n8n localmente.

## Iniciar la aplicación

En una terminal:

```powershell
cd dashboard-web
npm install
npm run dev
```

En otra terminal:

```powershell
dotnet tool restore --configfile NuGet.Config
dotnet run --project Dashboard.Api
```

La interfaz se abre en `http://localhost:5173` y la API escucha en `http://localhost:5168`.

### Acceso local

En desarrollo, si no se configura una contraseña, la demo utiliza `control-local-2026`. Para usar otra contraseña durante la sesión de PowerShell:

```powershell
$env:DASHBOARD_ADMIN_PASSWORD = "una-contraseña-local-segura"
dotnet run --project Dashboard.Api
```

No guardes la contraseña elegida en Git. La sesión usa una cookie `HttpOnly` y las claves locales se guardan fuera del control de versiones en `Dashboard.Api/Data/keys/`. En producción se deberá configurar un almacén de claves protegido y persistente.

### Secretos locales

Copia `.env.example` como `.env` en la raíz del proyecto y completa únicamente los valores que vayas habilitando. La API carga ese archivo al arrancar y las variables ya definidas en el sistema tienen prioridad.

```powershell
Copy-Item .env.example .env
```

Para Telegram completa `TELEGRAM_BOT_TOKEN` y `TELEGRAM_CHAT_ID` y reinicia la API. `TELEGRAM_TIMEOUT_SECONDS` controla cuánto espera cada entrega: usa 30 segundos por defecto y admite entre 5 y 120. El dashboard mostrará **Telegram conectado** sin revelar los secretos. El archivo `.env` está excluido de Git.

Para conectar Google Search Console:

1. Habilita **Google Search Console API** en tu proyecto de Google Cloud.
2. Crea un cliente OAuth 2.0 de tipo **Aplicación web**.
3. Registra exactamente `http://localhost:5168/api/integrations/search-console/callback` como URI de redirección autorizada.
4. Completa `GOOGLE_SEARCH_CONSOLE_CLIENT_ID` y `GOOGLE_SEARCH_CONSOLE_CLIENT_SECRET` en `.env` y reinicia la API.
5. Abre **Automatizaciones → Google Search Console → Conectar con Google**.

El conector solicita exclusivamente `https://www.googleapis.com/auth/webmasters.readonly`. El token de actualización se cifra antes de guardarse en SQLite mediante las claves locales de ASP.NET Data Protection; no se expone al frontend ni aparece en el historial. La cuenta autorizada debe tener acceso verificado a `fyrstudios.com` o a la propiedad correspondiente al dominio configurado en el proyecto.

Para habilitar Gemini, crea una API key en un proyecto **Free Tier sin facturación**, guárdala como `GEMINI_API_KEY` en `.env` y reinicia la API. El dashboard no puede comprobar ni desactivar la facturación de Google: esa garantía depende de que la clave pertenezca a un proyecto sin cuenta de facturación vinculada. El modelo, umbrales SEO, límite diario, extensión y tokens máximos se configuran desde **Automatizaciones**.

Para conectar WordPress completa `WORDPRESS_BASE_URL`, `WORDPRESS_USERNAME` y `WORDPRESS_APPLICATION_PASSWORD` en `.env`, reinicia la API y usa **Automatizaciones → WordPress → Probar conexión**. La contraseña de aplicación nunca se guarda en SQLite ni se devuelve al navegador. La URL, el usuario y la pausa sí se configuran por proyecto desde el dashboard.

Para el asistente de X no necesitas credenciales en el modo manual gratuito. Abre **Marketing → Asistente X**, pega la URL y el texto de un post, agrégalo a la cola y genera la siguiente propuesta. Gemini devuelve tres alternativas con un formato uniforme y un enlace UTM. **Responder en X** abre el compositor web con el post y texto preparados, pero X exige la confirmación humana final. Después copia la URL de tu respuesta ya publicada y pégala en el dashboard para registrarla en el historial.

Telegram recibe el enlace del post original, las tres alternativas en bloques uniformes y un botón nativo **Copiar** para cada respuesta. Por seguridad y brevedad, esos botones copian solo el texto: desde Telegram se abre el post, se pega y se confirma manualmente. El dashboard conserva **Responder en X**, que abre el compositor e incluye también el UTM. Una vez configurada la 7F, PostHog capturará la llegada al sitio y la API importará sus conteos agregados; construir el enlace por sí solo no registra una visita.

La búsqueda reciente automática es opcional y de solo lectura. Requiere `X_BEARER_TOKEN` en `.env`, créditos disponibles en X y activar explícitamente **Lectura pagada**. **Generar siguiente propuesta** consume primero la cola local y solo consulta X si está vacía; **Sincronizar X ahora** permite forzar una lectura separada. La consulta solicita posts y métricas públicas, pero no perfiles de autor, para no añadir ese recurso al costo estimado. Antes de cada lectura se comprueba el costo máximo contra el presupuesto del proyecto. La tarifa por post es configurable porque X puede cambiar sus precios. El backend no incluye ninguna operación para publicar, responder, dar me gusta ni seguir cuentas.

### Datos locales y migraciones

La base SQLite se crea automáticamente en `Dashboard.Api/Data/dashboard.db`. Al arrancar, la API aplica las migraciones pendientes y agrega cada conjunto de datos semilla únicamente cuando todavía no existe.

### Módulo de marketing

Selecciona un proyecto y abre **Marketing** para:

- crear y editar campañas y publicaciones;
- generar enlaces con `utm_source`, `utm_medium`, `utm_campaign` y `utm_content`;
- revisar el embudo agregado por etapa y fuente;
- validar e importar CSV o JSON con conteos agregados.

El importador acepta las etapas `visit`, `interest`, `contact` y `quote`. Sus campos principales son `stage`, `source`, `medium`, `count`, `landingPath` y `occurredAt`; opcionalmente admite `campaignId` y `socialPostId` del mismo proyecto. No se deben importar nombres, correos, IP ni identificadores personales.

En **Marketing → PostHog** se configuran región, ID de proyecto, token público, ventana de importación y límite de filas. La clave personal de lectura se guarda únicamente en `.env` como `POSTHOG_PERSONAL_API_KEY`. El dashboard separa los conteos reales de PostHog de los registros manuales y simulados. La sincronización se puede ejecutar en la pantalla o programar en **Automatizaciones**. Repetirla reemplaza los agregados del mismo periodo, sin sumar de nuevo las visitas. Los detalles de instalación en WordPress, incluyendo el formulario WPForms ID 815, están en [`infrastructure/posthog/README.md`](infrastructure/posthog/README.md).

**Sesiones diarias** cuenta sesiones distintas dentro de cada día; una misma sesión que cruce la medianoche puede aparecer en dos días. **Visitas** de un enlace UTM cuenta sesiones distintas de ese enlace, mientras **páginas vistas** cuenta cada carga de página. En **Ver eventos** se consultan bajo demanda hasta 30 eventos recientes del periodo, con navegador, ciudad/país aproximados, dispositivo, sistema operativo y origen UTM o dominio de referencia. Estos detalles no se guardan en la base local ni incluyen IP, identificador de persona o sesión. Si antes aparecían cero sesiones junto con páginas vistas, reinicia la API y vuelve a sincronizar para reemplazar los agregados anteriores.

### SEO y contenido

Selecciona un proyecto y abre **SEO y contenido** para:

- registrar oportunidades con evidencia, hipótesis y línea base;
- convertirlas en briefs y borradores en Markdown;
- revisar el flujo `brief → borrador → revisión → aprobación → programación`;
- consultar fechas en el calendario editorial;
- crear o reintentar un borrador real de WordPress sin publicarlo;
- registrar mediciones agregadas y consultar todo el historial de estados.

Las transiciones se validan en la API. El conector de WordPress fija `status: draft` dentro del backend, comprueba que WordPress confirme ese mismo estado y evita duplicar un post al reintentar. El enlace guardado abre el editor real para que la revisión y publicación sean manuales.

Si Search Console todavía no tiene volumen suficiente, crea una oportunidad manual, añade evidencia e hipótesis y déjala en estado **Seleccionada**. El flujo de Gemini prioriza esa selección, conserva el origen `manual`, comprueba que no exista contenido similar y continúa con validación y WordPress `draft`; no la presenta como un hallazgo de Google.

### Ejecuciones y costos

Selecciona un proyecto y abre **Operaciones** o **Costos de API** para:

- planificar intentos con clave de idempotencia, proveedor, modelo, flujo y unidades estimadas;
- exigir aprobación humana, cancelar, simular éxito o fallo y crear reintentos trazables;
- consultar la auditoría completa de cada ejecución;
- versionar tarifas de entrada/salida por millón de unidades;
- configurar límites diarios y mensuales, tipo de cambio GTQ/USD y pausa global;
- revisar estimaciones del día y del mes calculadas en la zona horaria del proyecto.

Los importes son reservas internas estimadas con la tarifa vigente al crear el intento. No son facturas ni garantizan un límite duro frente a consumos realizados fuera de este sistema. En esta fase todas las ejecuciones son locales y simuladas.

### Automatizaciones y alertas

Selecciona un proyecto y abre **Automatizaciones** para:

- activar o pausar cada flujo y configurar frecuencia, hora local, día e intentos máximos diarios;
- ejecutar manualmente un flujo y revisar su resultado;
- configurar el umbral de severidad, la ventana de agrupación y el horario silencioso local;
- enviar una prueba real si Telegram está configurado o simularla si no lo está;
- revisar qué evento fue entregado, agrupado, encolado, omitido o falló;
- recuperar manualmente un fallo y procesar la cola local.

En la misma pantalla, el bloque de **Google Search Console** permite autorizar una propiedad, configurar de 7 a 90 días de historial y hasta 25,000 filas, sincronizar manualmente y consultar clics, impresiones, CTR, posición media y consultas principales. La programación diaria utiliza ese mismo flujo. Para evitar datos parciales, la ventana termina dos días antes de la fecha actual.

Los bloques **Gemini para SEO y X** y **WordPress** ejecutan el flujo `oportunidad de Search Console o selección manual → deduplicación → Gemini → validación → draft local → WordPress draft → historial → Telegram`. SEO y X tienen topes diarios propios y un límite compartido configurable. Si no hay una oportunidad elegible, se alcanzó el límite diario o falta la API key, el flujo se bloquea antes de consumir cuota. Un `429` detiene la ejecución y notifica que la cuota gratuita se agotó; nunca activa otro proveedor. Si WordPress falla, el borrador local se conserva para reintento manual y no se crea una publicación en vivo.

La API lee `TELEGRAM_BOT_TOKEN` y `TELEGRAM_CHAT_ID` desde el `.env` local. Nunca guarda ni devuelve esos valores. Sin ambos secretos cambia automáticamente al modo `simulated`; en pruebas automatizadas siempre se fuerza ese modo para impedir llamadas externas accidentales.

Los resúmenes de Telegram incluyen siempre el proyecto y, cuando corresponde, flujo, proveedor, modelo, tokens y costo estimado. Las programaciones se guardan por proyecto. El worker local las revisa mientras la API está encendida; al apagar la computadora dejan de ejecutarse, pero no se pierde su configuración. PostHog Cloud puede seguir capturando tráfico del WordPress público mientras la computadora está apagada; el worker recupera los días configurados al volver a encenderse.

### n8n local

La configuración está en `infrastructure/n8n/`. Copia `infrastructure/n8n/env.example` como `infrastructure/n8n/.env`, cambia `N8N_ENCRYPTION_KEY` y ejecuta:

```powershell
docker compose --env-file infrastructure/n8n/.env -f infrastructure/n8n/compose.yaml up -d
```

n8n queda limitado a `127.0.0.1:5678`. No almacenes su clave de cifrado ni credenciales de proveedores en Git.

Para crear una migración después de modificar el modelo:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project Dashboard.Api --output-dir Data\Migrations
```

## Verificar

```powershell
cd dashboard-web
npm test
npm run build
cd ..
dotnet build Dashboard.Api
dotnet test Dashboard.Api.Tests
```

Con la API iniciada, `http://localhost:5168/api/health` debe responder con estado `ok`.

## Detener

Presiona `Ctrl+C` en cada terminal donde esté ejecutándose el frontend o la API.

## Documentación

- `docs/contexto.md`: visión, alcance y restricciones.
- `docs/metodologia de trabajo.md`: reglas de colaboración y desarrollo.
- `docs/observaciones.txt`: observaciones funcionales adicionales.
- [`docs/operacion-produccion.md`](docs/operacion-produccion.md): preparación y lista de comprobación para una VPS, sin desplegar todavía.
- `fases.md`: plan ordenado, fase activa y criterios de aceptación.

No guardes tokens, contraseñas, bases SQLite, claves de sesión ni archivos `.env` reales en Git.

