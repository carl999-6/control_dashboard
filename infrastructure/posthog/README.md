# Instrumentación PostHog de FyrStudios (fase 7F)

Esta carpeta contiene únicamente el código del evento de conversión. No contiene credenciales ni instala nada en el WordPress público por sí sola.

1. Crea un proyecto en **PostHog Cloud US** (`https://us.posthog.com`). En *Project settings* copia el **Project ID** numérico y el **Project token** público `phc_...`. Crea además una **personal API key** restringida al proyecto y al permiso `query:read`. Guarda esa clave únicamente en `.env` local como `POSTHOG_PERSONAL_API_KEY=...`; nunca la pegues en WordPress.
2. En WordPress instala el **snippet oficial para JavaScript** que PostHog muestra para ese proyecto y región (por ejemplo, mediante tu gestor de snippets o el encabezado del tema hijo). En la configuración de `posthog.init(...)` usa `api_host: 'https://us.i.posthog.com'`, `autocapture: false`, `capture_pageview: true`, `disable_session_recording: true`, `person_profiles: 'never'` y `persistence: 'sessionStorage'`. Desactiva también las grabaciones en los ajustes del proyecto PostHog. No copies el *personal API key* en el snippet: el navegador solo usa el token público. Para no enviar la cadena de consulta de la URL ni el referente completo, añade también esta función a la configuración:

   ```javascript
   before_send: function (event) {
     if (event && event.properties) {
       if (event.properties.$current_url) {
         try { var url = new URL(event.properties.$current_url); event.properties.$current_url = url.origin + url.pathname; } catch (_) { delete event.properties.$current_url; }
       }
       delete event.properties.$referrer;
     }
     return event;
   }
   ```
3. Añade el contenido de [`fyrstudios-events.js`](fyrstudios-events.js) como JavaScript de pie de página **después** del snippet oficial. El formulario público actual `/contacto/` es WPForms AJAX, ID `815`; el código escucha únicamente el evento de éxito de ese formulario. Si cambias el formulario, actualiza ese ID y vuelve a probar. Un clic en WhatsApp se captura aparte.
4. En el dashboard: **Marketing → PostHog**. Indica región US, ID numérico, token público, días y límite de filas; activa la integración y guarda. Ajusta su horario en **Automatizaciones** si lo deseas. Reinicia la API después de cambiar `.env`.
5. Prueba con un enlace como `https://fyrstudios.com/contacto/?utm_source=x&utm_medium=organic&utm_campaign=prueba-7f&utm_content=reply-prueba`. Abre el enlace en una ventana privada, comprueba `$pageview` en PostHog *Activity*, envía el formulario con datos de prueba y comprueba `fyr_quote_request`. Usa **Sincronizar ahora** en el dashboard. Evita crear cotizaciones de prueba innecesarias en el sitio real.

El código de eventos no lee campos de formulario, nombres, correo, teléfono, URL de WhatsApp ni identificadores personales. Conserva únicamente etiquetas UTM válidas durante la pestaña actual, en `sessionStorage`, para atribuir la cotización aunque el visitante navegue antes de enviar el formulario. PostHog puede procesar datos técnicos de conexión conforme a su propia configuración y política; revisa el aviso de privacidad de FyrStudios antes de habilitarlo públicamente.

El dashboard consulta solo conteos agregados mediante la [Query API de PostHog](https://posthog.com/docs/api/query). Sus métricas son datos reales de PostHog; los datos manuales o simulados del resumen antiguo se muestran por separado. Si la consulta supera el máximo de filas, no se guarda un resultado incompleto: reduce el rango o aumenta el límite.
