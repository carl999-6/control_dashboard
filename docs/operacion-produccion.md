# Preparación para producción

Este documento prepara el paso de la demo local a una VPS. No despliega el sistema ni autoriza cambios sobre el dominio actual.

## Lo que ya protege la aplicación

- Fuera de `Development` no puede arrancar sin `DASHBOARD_ADMIN_PASSWORD` y rechaza la contraseña de demostración.
- Las respuestas de `/api` no se almacenan en caché y llevan cabeceras contra interpretación de contenido y embebido en marcos.
- Los errores no controlados usan el manejador estándar de producción y se activa HSTS.
- `GET /api/health` confirma que la API responde y `GET /api/health/ready` confirma además conexión a base de datos.
- Las claves, tokens y contraseñas continúan fuera del repositorio; `.env` es solo para local.

## Decisiones necesarias antes de desplegar

1. Elegir proveedor, región, tamaño de VPS y método de acceso administrativo.
2. Elegir dominio o subdominio para el dashboard y configurar HTTPS mediante un proxy inverso.
3. Migrar SQLite a PostgreSQL y decidir dónde se conservarán copias de seguridad cifradas.
4. Definir cómo se administrarán los secretos en la VPS; no copiar el `.env` local a un repositorio ni a una imagen pública.
5. Configurar el proxy para transmitir correctamente el protocolo HTTPS a la aplicación antes de usar cookies seguras.
6. Decidir una política de actualizaciones, restauración y supervisión.

## Lista de comprobación de despliegue

- Usar una contraseña de administrador larga y exclusiva en `DASHBOARD_ADMIN_PASSWORD`.
- Definir `ASPNETCORE_ENVIRONMENT=Production`.
- Restringir firewall a SSH y HTTPS; la API no debe exponerse directamente si existe un proxy inverso.
- Usar PostgreSQL con usuario de privilegios mínimos y una cadena de conexión almacenada como secreto.
- Persistir las claves de Data Protection en un volumen protegido y respaldado; perderlas invalida tokens OAuth cifrados y sesiones existentes.
- Ejecutar migraciones de base de datos de manera controlada y probar restauración antes de depender de tareas automáticas.
- Configurar comprobaciones del proceso contra `/api/health/ready`.
- Probar manualmente inicio de sesión, Search Console, borrador de WordPress, PostHog, Telegram y las programaciones del worker.
- Verificar que la aplicación use HTTPS y que ninguna respuesta de API se almacene en cachés intermedias.

## Copias y recuperación

- Base de datos: copia diaria, retención definida y restauración de prueba periódica.
- Claves de Data Protection: copia junto con la base; son necesarias para recuperar integraciones cifradas.
- Secretos: conservarlos en un gestor de secretos o ubicación administrativa separada, nunca dentro de la copia pública del código.
- Antes de actualizar: tomar una copia recuperable y ejecutar pruebas automatizadas en una copia del entorno.

El worker interno de .NET conserva el mismo comportamiento en una VPS: ejecutará las automatizaciones mientras el proceso esté vivo. n8n sigue siendo opcional y no sustituye límites, permisos, historial ni revisión humana del dashboard.
