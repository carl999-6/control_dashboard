# n8n local

n8n será el ejecutor de integraciones de la fase 7. El dashboard conserva la configuración, los límites, las aprobaciones y el historial; n8n recibe tareas autorizadas y conecta los proveedores.

## Preparación

1. Copia `env.example` como `.env` dentro de esta carpeta.
2. Sustituye `N8N_ENCRYPTION_KEY` por una clave larga y aleatoria. No la subas a Git.
3. Inicia el servicio:

```powershell
docker compose --env-file infrastructure/n8n/.env -f infrastructure/n8n/compose.yaml up -d
```

n8n quedará disponible únicamente desde la computadora local en `http://127.0.0.1:5678`. Sus credenciales se guardan cifradas en el volumen `n8n_data`. Los flujos de Search Console, Gemini y WordPress se añadirán de forma incremental; no se importan todavía credenciales ni se publica contenido.

## Detener

```powershell
docker compose --env-file infrastructure/n8n/.env -f infrastructure/n8n/compose.yaml stop
```

No uses `down -v` salvo que quieras eliminar permanentemente los datos locales de n8n.
