# Mejoras futuras

## Incorporadas después de cerrar la demo

- **Menú lateral plegable:** permite conservar solo los iconos y ampliar el espacio de trabajo; la preferencia queda guardada en el navegador.
- **Marketing por enlace:** PostHog ya muestra atribución por red social, campaña y contenido UTM, con eventos recientes bajo demanda y vínculo a propuestas de X cuando existe.
- **Lectura eficiente de X:** la generación reutiliza primero la cola local, deduplica IDs y descarta oportunidades de API con más de siete días. La sincronización pagada se puede forzar por separado y solo consulta posts, sin añadir lecturas de perfiles de autor al costo estimado.
- **Oportunidades SEO con pocos datos:** una oportunidad manual en estado `selected` puede alimentar el flujo de Gemini aunque Search Console aún no tenga métricas; mantiene su origen manual y pasa por deduplicación.

## Pendientes

- **Blog en FyrStudios:** crear una sección para organizar y mostrar los artículos SEO después de su revisión y publicación manual.
- **Instagram profesional:** validar el tipo de cuenta y sus permisos antes de conectar publicaciones y medición por UTM.
- **n8n cuando aporte valor:** usarlo para integraciones que necesiten orquestación visual; mantener reglas, presupuestos e historial en el dashboard y el worker de .NET.
- **Base de datos de producción:** migrar de SQLite a PostgreSQL con copias de seguridad y restauración probada antes de desplegar en VPS.
- **Operación segura en VPS:** reforzar acceso, secretos y supervisión de tareas para mantener las automatizaciones disponibles y detectar fallos.
  - Existe una guía de preparación y endpoints de salud; el despliegue, PostgreSQL, HTTPS, backups y proveedor siguen pendientes de decisión.
