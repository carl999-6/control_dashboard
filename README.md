# Centro de control multi-proyecto

Demo local de un dashboard para centralizar decisiones y actividad de desarrollo, operaciones, marketing, SEO, automatizaciones y consumo de APIs. FyrStudios es el primer caso de ejemplo, pero la arquitectura se diseña para varios proyectos.

> Estado actual: Fase 2 lista para revisión. Las métricas del dashboard siguen siendo simuladas; proyectos, objetivos y ajustes ya se guardan localmente en SQLite.

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

La base SQLite se crea automáticamente en `Dashboard.Api/Data/dashboard.db`. Al arrancar, la API aplica las migraciones pendientes y agrega los datos semilla únicamente cuando no existen proyectos.

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

