# Centro de control multi-proyecto

Demo local de un dashboard para centralizar decisiones y actividad de desarrollo, operaciones, marketing, SEO, automatizaciones y consumo de APIs. FyrStudios es el primer caso de ejemplo, pero la arquitectura se diseña para varios proyectos.

> Estado actual: Fase 1 lista para revisión. La información presentada es simulada y ninguna integración externa realiza acciones reales.

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
dotnet run --project Dashboard.Api
```

La interfaz se abre en `http://localhost:5173` y la API escucha en `http://localhost:5168`.

## Verificar

```powershell
cd dashboard-web
npm test
npm run build
cd ..
dotnet build Dashboard.Api
```

Con la API iniciada, `http://localhost:5168/api/health` debe responder con estado `ok`.

## Detener

Presiona `Ctrl+C` en cada terminal donde esté ejecutándose el frontend o la API.

## Documentación

- `contexto.md`: visión, alcance y restricciones.
- `metodologia de trabajo.md`: reglas de colaboración y desarrollo.
- `fases.md`: plan ordenado, fase activa y criterios de aceptación.

No guardes tokens, contraseñas ni archivos `.env` reales en Git.

