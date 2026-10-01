# Centro de control multi-proyecto

Demo local de un dashboard para centralizar decisiones y actividad de desarrollo, operaciones, marketing, SEO, automatizaciones y consumo de APIs. FyrStudios es el primer caso de ejemplo, pero la arquitectura se diseña para varios proyectos.

> Estado actual: Fase 6 lista para revisión. El sistema ya controla ejecuciones simuladas, presupuestos y alertas locales configurables con agrupación y recuperación de fallos. Los datos semilla siguen claramente identificados como simulados y no se realizan llamadas pagadas ni acciones externas.

## Requisitos

- Node.js 22 o posterior.
- npm 10 o posterior.
- .NET SDK 10.

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

### Datos locales y migraciones

La base SQLite se crea automáticamente en `Dashboard.Api/Data/dashboard.db`. Al arrancar, la API aplica las migraciones pendientes y agrega cada conjunto de datos semilla únicamente cuando todavía no existe.

### Módulo de marketing

Selecciona un proyecto y abre **Marketing** para:

- crear y editar campañas y publicaciones;
- generar enlaces con `utm_source`, `utm_medium`, `utm_campaign` y `utm_content`;
- revisar el embudo agregado por etapa y fuente;
- validar e importar CSV o JSON con conteos agregados.

El importador acepta las etapas `visit`, `interest`, `contact` y `quote`. Sus campos principales son `stage`, `source`, `medium`, `count`, `landingPath` y `occurredAt`; opcionalmente admite `campaignId` y `socialPostId` del mismo proyecto. No se deben importar nombres, correos, IP ni identificadores personales.

### SEO y contenido

Selecciona un proyecto y abre **SEO y contenido** para:

- registrar oportunidades con evidencia, hipótesis y línea base;
- convertirlas en briefs y borradores en Markdown;
- revisar el flujo `brief → borrador → revisión → aprobación → programación`;
- consultar fechas en el calendario editorial;
- simular el envío como borrador de WordPress sin realizar conexiones externas;
- registrar mediciones agregadas y consultar todo el historial de estados.

Las transiciones se validan en la API para impedir saltos de aprobación. La URL de WordPress generada por la simulación utiliza `wordpress.local` y no representa una publicación real.

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

- configurar el umbral de severidad, la ventana de agrupación y el horario silencioso local;
- simular una alerta o un fallo de entrega sin conectar Telegram;
- revisar qué evento fue entregado, agrupado, encolado, omitido o falló;
- recuperar manualmente un fallo y procesar la cola local.

El canal actual es un adaptador `simulated`: no acepta, muestra ni guarda token de bot, chat ID ni otra credencial. “Entregada” significa que la prueba local terminó correctamente; no confirma ningún envío a Telegram. Un bot real requiere autorización explícita y se incorporará mediante configuración segura en una fase posterior.

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

- `contexto.md`: visión, alcance y restricciones.
- `metodologia de trabajo.md`: reglas de colaboración y desarrollo.
- `fases.md`: plan ordenado, fase activa y criterios de aceptación.

No guardes tokens, contraseñas, bases SQLite, claves de sesión ni archivos `.env` reales en Git.

