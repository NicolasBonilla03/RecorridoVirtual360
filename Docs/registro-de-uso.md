# Registro de uso del recorrido

El recorrido registra, de forma anónima, los indicadores que pide la metodología:

- **Tasa de completitud:** espacios visitados sobre el total, por usuario.
- **Tiempo de navegación por espacio:** segundos entre la entrada y la salida de cada foto 360.
- **Uso de las fichas** de información y de las **preguntas al guía virtual**.

No se guardan nombres, correos ni datos personales. Cada sesión recibe un identificador al azar y un **código de seis caracteres**. El participante ve ese código al final del menú lateral y lo escribe en la encuesta; así se cruzan sus respuestas con su recorrido.

## Dónde quedan los datos

En una hoja de Google Sheets tuya, con tres pestañas:

| Pestaña | Qué tiene | Para qué sirve |
|---|---|---|
| Sesiones | Una fila por persona | Completitud (%), tiempo total y tiempo promedio por espacio |
| Visitas | Una fila por espacio visitado | Tiempo por espacio; cuáles se visitan más |
| Eventos | Fichas abiertas y preguntas al guía | Uso de los puntos informativos y del guía |

## Cómo conectarlo (una sola vez)

1. Crea una hoja de cálculo nueva en Google Sheets.
2. En el menú: **Extensiones → Apps Script**.
3. Borra lo que haya y pega todo el contenido de `Docs/registro-de-uso.gs`. Guarda.
4. Arriba, elige la función **prepararHoja** y dale **Ejecutar**. Acepta los permisos. Esto crea las tres pestañas.
5. **Implementar → Nueva implementación → Aplicación web**:
   - Ejecutar como: **Yo**.
   - Quién tiene acceso: **Cualquier persona**.
6. Copia la **URL de la aplicación web** (termina en `/exec`).
7. Abre `Assets/Resources/RegistroUso.json` y pega la URL entre las comillas de `"url"`.

Para comprobarlo, abre la URL en el navegador: debe decir «Registro del Recorrido UdB 360° activo.»

## Ajustes de `RegistroUso.json`

- `url`: dirección de la aplicación web. Vacía = no se envía nada y el menú no muestra el código.
- `enviarDesdeEditor`: `false` para que tus pruebas en Unity no ensucien los datos. Ponlo en `true` solo para probar la conexión.
- `segundosEntreEnvios`: cada cuánto se envía la visita en curso (20 por defecto). Al salir de un espacio se envía de una vez.

## Para tener en cuenta

- El formato de consentimiento informado debe mencionar este registro (Ley 1581 de 2012).
- En la web no hay aviso cuando alguien cierra la pestaña: de la última visita se pueden perder hasta 20 segundos.
- Si cambias el script, crea una **nueva versión** de la implementación (Implementar → Gestionar implementaciones → Editar → Versión nueva). La URL no cambia.
