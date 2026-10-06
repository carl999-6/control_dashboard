/* FyrStudios custom events for PostHog */
(function () {
  if (window.__fyrPostHogEventsInstalled) return;
  window.__fyrPostHogEventsInstalled = true;

  var key = 'fyrstudios_utm_session';
  var allowed = /^[a-zA-Z0-9_-]{1,160}$/;
  var names = [
    'utm_source',
    'utm_medium',
    'utm_campaign',
    'utm_content'
  ];

  function safePath() {
    return /^\/[a-zA-Z0-9_./-]{0,299}$/.test(window.location.pathname)
      ? window.location.pathname
      : '/';
  }

  function attribution() {
    var saved = {};

    try {
      saved = JSON.parse(
        window.sessionStorage.getItem(key) || '{}'
      );
    } catch (_) {
      saved = {};
    }

    if (!saved || typeof saved !== 'object') {
      saved = {};
    }

    var query = new URLSearchParams(window.location.search);
    var updated = false;

    names.forEach(function (name) {
      var value = query.get(name);

      if (value && allowed.test(value)) {
        saved[name] = value.toLowerCase();
        updated = true;
      }
    });

    if (updated) {
      try {
        window.sessionStorage.setItem(
          key,
          JSON.stringify(saved)
        );
      } catch (_) {}
    }

    var result = {
      '$pathname': safePath()
    };

    names.forEach(function (name) {
      if (
        typeof saved[name] === 'string' &&
        allowed.test(saved[name])
      ) {
        result[name] = saved[name];
      }
    });

    return result;
  }

  function capture(name) {
    if (
      window.posthog &&
      typeof window.posthog.capture === 'function'
    ) {
      window.posthog.capture(
        name,
        attribution()
      );
    }
  }

  attribution();

  // WPForms - Formulario de cotización
  if (window.jQuery) {
    window.jQuery(document).on(
      'wpformsAjaxSubmitSuccess',
      function (_, response) {
        if (
          Number(
            response &&
            response.data &&
            response.data.form_id
          ) === 815
        ) {
          capture('fyr_quote_request');
        }
      }
    );
  }

  // Joinchat - clic que abre WhatsApp
  document.addEventListener(
    'joinchat:open',
    function (event) {
      if (
        event.detail &&
        event.detail.chat_channel === 'whatsapp' &&
        window.posthog &&
        typeof window.posthog.capture === 'function'
      ) {
        window.posthog.capture(
          'fyr_whatsapp_click',
          attribution(),
          {
            send_instantly: true,
            transport: 'sendBeacon'
          }
        );
      }
    }
  );
}());