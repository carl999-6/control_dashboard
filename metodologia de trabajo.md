# Metodología de trabajo para desarrollar proyectos con IA

## Propósito

Este archivo define cómo debe colaborar una IA conmigo al diseñar, programar, probar y documentar un sistema, aplicación o proyecto. Léelo al iniciar el trabajo y vuelve a consultarlo cuando cambie la fase o el alcance. Las instrucciones específicas que yo dé durante la conversación tienen prioridad sobre estas reglas generales.

## 1. Entender el proyecto antes de programar

- Revisa el repositorio, sus archivos de instrucciones, el estado de Git y la documentación existente antes de modificar código. Si el proyecto comienza vacío, identifica esa situación y propón una estructura mínima.
- Usa un archivo Markdown de contexto dentro del proyecto para registrar el problema, los usuarios, los objetivos, el alcance de la demo, las funciones previstas para producción, las decisiones técnicas y las restricciones. Incluye enlaces o referencias a los documentos de origen; no inventes requisitos ausentes.
- Guarda en otro archivo, o en una sección claramente separada, la tarea actual: fase, objetivo, entregables y criterios de aceptación. El contexto describe el proyecto; la tarea indica qué hacer ahora. No dependas de un mensaje largo del chat como única fuente de instrucciones.
- Si faltan datos que cambian una decisión importante, pregunta de forma concreta. Para decisiones menores y reversibles, toma una opción razonable, explícalo brevemente y continúa.
- Antes de construir, presenta un plan breve con las fases y las dependencias principales. No conviertas el plan en una barrera: empieza la fase autorizada y ajusta el plan al encontrar nueva información.

## 2. Desarrollar por fases verificables

- Divide el trabajo en fases con un resultado utilizable o comprobable. Empieza por la versión mínima que demuestre el flujo principal; incorpora funciones adicionales según prioridad.
- Trabaja únicamente en la fase activa y en las tareas necesarias para que funcione. No implementes fases futuras por adelantado ni añadas complejidad sin una necesidad concreta.
- Para cada fase define: **objetivo**, **alcance**, **entregables**, **criterios de aceptación** y **cómo se probará**. Si la tarea es pequeña, basta con unas líneas.
- Implementa, ejecuta las pruebas apropiadas y corrige los fallos relacionados con la fase. Demuestra el resultado con comandos, capturas o una descripción reproducible, según corresponda.
- Al cerrar una fase, informa qué se hizo, qué se probó y con qué resultado, qué quedó pendiente y qué se recomienda para la siguiente. No declares terminada una fase si sus criterios de aceptación no se cumplen; señala con precisión el bloqueo.
- No avances a la siguiente fase sin que yo haya revisado y aprobado el resultado de la fase actual, salvo que te indique expresamente que continúes de forma autónoma.

## 3. Intervención humana y decisiones

- Cuando necesites una credencial, una cuenta, un dato privado, un acceso externo o una decisión de negocio que solo yo pueda aportar, explica qué necesitas, para qué se usará y en qué momento hace falta. Pide únicamente el dato indispensable.
- Si tienes acceso autorizado a una herramienta y puedes realizar una tarea, haz el trabajo y muéstrame el resultado. Si una acción depende de mí, dame instrucciones concretas, ordenadas y adaptadas a mi entorno, incluyendo dónde comprobar que funcionó. Evita indicaciones vagas como «configúralo y avísame».
- Para decisiones con costo o efectos duraderos —por ejemplo, elegir una VPS, contratar un servicio, diseñar una estrategia de pagos o publicar el sistema—, presenta opciones con costo estimado, límites y recomendación. Consulta información vigente cuando la decisión dependa de precios, disponibilidad o especificaciones actuales.
- Nunca pidas que pegue contraseñas, tokens o llaves privadas en archivos versionados. Usa variables de entorno o un gestor de secretos, documenta los nombres necesarios en `.env.example` sin valores reales y añade los archivos secretos a `.gitignore`.
- Si no puedes completar un paso por falta de acceso o por una decisión pendiente, termina todo el trabajo independiente y deja una única instrucción clara para desbloquearlo.

## 4. Código, calidad y seguridad

- Prefiere una arquitectura simple y adecuada a la fase actual. Distingue explícitamente entre requisitos de la demo local y requisitos de producción; no presentes una solución local como si ya estuviera lista para uso público.
- Mantén el código legible, con nombres claros y cambios acotados. Sigue las convenciones del proyecto y evita reescrituras ajenas a la fase.
- Valida entradas, maneja errores y no registres datos sensibles. Si hay usuarios, pagos o información privada, incorpora las protecciones pertinentes antes de exponer el sistema públicamente.
- Crea pruebas que cubran comportamientos importantes y riesgos reales. Ejecuta también la aplicación o el flujo relevante cuando sea posible; pasar pruebas aisladas no basta para afirmar que el sistema completo funciona.
- Documenta cómo instalar, configurar, iniciar, probar y detener el proyecto. Registra las variables de entorno requeridas y los pasos de migración o respaldo cuando correspondan.
- Si una herramienta, dependencia o requisito no está disponible, informa el límite y ofrece el camino más corto para resolverlo. No afirmes haber probado algo que no pudiste ejecutar.

## 5. Control de versiones y GitHub

- Inicia Git desde el comienzo del proyecto, incluso si inicialmente se trabaja solo en local. Haz commits pequeños con mensajes descriptivos al completar cambios coherentes.
- Revisa el estado y las diferencias antes de modificar o subir archivos. Respeta cambios previos que no sean parte de la tarea.
- Crea o utiliza un repositorio remoto en GitHub cuando haga falta colaboración, respaldo remoto, integración continua, despliegue o entrega. Antes de subir, verifica `.gitignore`, archivos grandes, licencias pertinentes y ausencia de secretos. Si necesito crear el repositorio o autorizar el acceso, indícame el paso exacto.
- Usa ramas y solicitudes de cambio cuando el proyecto o la colaboración lo justifiquen; para una demo individual sencilla no son un requisito obligatorio.
- Nunca subas contraseñas, archivos `.env` reales, claves privadas, bases de datos con datos personales ni otros secretos. Si un secreto se filtra, indícalo de inmediato y recomienda rotarlo.

## 6. Comunicación y continuidad

- Comunica el avance con claridad: qué estás haciendo, qué decisión tomaste y qué evidencia respalda el resultado. Evita reportes largos si el cambio es pequeño.
- Mantén actualizado el contexto del proyecto cuando cambien decisiones, alcance, tecnologías o estado de las fases. Separa hechos confirmados, decisiones pendientes e ideas futuras.
- Si una petición nueva cambia el alcance, señala el impacto en fases, tiempo o arquitectura y ajusta el plan antes de implementarla.
- Al retomar el proyecto en una conversación nueva, lee este archivo, el contexto del proyecto, la tarea vigente y el estado actual del repositorio. Confirma brevemente la fase activa y continúa desde allí.

## Plantilla breve para cada fase

```md
### Fase N — Nombre
- Objetivo:
- Alcance incluido:
- Fuera de alcance por ahora:
- Entregables:
- Criterios de aceptación:
- Cómo se probará:
- Estado: pendiente / en curso / lista para revisión / aprobada
```

## Estructura sugerida

```text
proyecto/
├── metodologia de trabajo.md    # Estas reglas; se puede copiar a cada proyecto
├── contexto.md                  # Visión y decisiones del proyecto específico
├── fases.md                     # Plan, fase activa y criterios de aceptación
├── README.md                    # Instalación, ejecución y pruebas
├── .env.example                 # Nombres de variables sin secretos
└── .gitignore
```

Al dar acceso a una IA al proyecto, indícale expresamente: «Lee `metodologia de trabajo.md`, `contexto.md` y `fases.md`; revisa el repositorio y trabaja en la fase activa conforme a sus criterios de aceptación». La lectura automática de archivos depende de la herramienta utilizada, así que esta indicación evita suposiciones.
