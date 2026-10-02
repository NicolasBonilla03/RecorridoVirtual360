/**
 * Registro de uso del Recorrido UdB 360° en Google Sheets.
 *
 * Recibe lo que envía el recorrido (RegistroUso.cs) y lo escribe en tres pestañas:
 *   Sesiones: una fila por persona (completitud, tiempo total, promedio por espacio).
 *   Visitas:  una fila por cada espacio visitado (segundos entre la entrada y la salida).
 *   Eventos:  fichas abiertas y preguntas hechas al guía virtual.
 *
 * No guarda nombres ni correos. La columna «Código» es la que el participante
 * escribe en la encuesta, para cruzar sus respuestas con su recorrido.
 */

var HOJA_SESIONES = 'Sesiones';
var HOJA_VISITAS = 'Visitas';
var HOJA_EVENTOS = 'Eventos';

var COLUMNAS_SESIONES = ['Sesión', 'Código', 'Inicio', 'Última actividad', 'Dispositivo', 'Pantalla',
  'Espacios visitados', 'Total de espacios', 'Completitud (%)', 'Tiempo total (s)',
  'Tiempo promedio por espacio (s)', 'Fichas abiertas', 'Preguntas al guía'];
var COLUMNAS_VISITAS = ['Clave', 'Sesión', 'Código', 'Orden', 'Espacio', 'Foto 360', 'Entrada', 'Segundos'];
var COLUMNAS_EVENTOS = ['Hora', 'Sesión', 'Código', 'Tipo', 'Detalle', 'Espacio'];

/** Ejecuta esta función una sola vez: crea las tres pestañas con sus encabezados. */
function prepararHoja() {
  hoja_(HOJA_SESIONES, COLUMNAS_SESIONES);
  hoja_(HOJA_VISITAS, COLUMNAS_VISITAS);
  hoja_(HOJA_EVENTOS, COLUMNAS_EVENTOS);
}

function doPost(e) {
  var candado = LockService.getScriptLock();
  candado.waitLock(20000);
  try {
    var d = JSON.parse(e.parameter.datos);
    var ahora = new Date();

    // --- Sesiones: una fila por sesión, que se va actualizando
    var sesiones = hoja_(HOJA_SESIONES, COLUMNAS_SESIONES);
    var total = Number(d.totalEspacios) || 0;
    var visitados = Number(d.espaciosVisitados) || 0;
    var segundos = Number(d.segundosTotales) || 0;
    guardar_(sesiones, d.sesion, [
      d.sesion, d.codigo, fecha_(d.inicio), ahora, d.dispositivo, d.pantalla,
      visitados, total,
      total > 0 ? Math.round(1000 * visitados / total) / 10 : '',
      segundos,
      visitados > 0 ? Math.round(10 * segundos / visitados) / 10 : '',
      Number(d.fichasAbiertas) || 0, Number(d.preguntasAlGuia) || 0
    ]);

    // --- Visitas: una fila por espacio visitado; la visita en curso se actualiza
    var visitas = hoja_(HOJA_VISITAS, COLUMNAS_VISITAS);
    (d.visitas || []).forEach(function (v) {
      var clave = d.sesion + '|' + v.orden;
      guardar_(visitas, clave, [clave, d.sesion, d.codigo, v.orden, v.espacio, v.skybox, fecha_(v.entrada), Number(v.segundos) || 0]);
    });

    // --- Eventos: se agregan al final
    var eventos = hoja_(HOJA_EVENTOS, COLUMNAS_EVENTOS);
    (d.eventos || []).forEach(function (ev) {
      eventos.appendRow([fecha_(ev.hora), d.sesion, d.codigo, ev.tipo, ev.detalle, ev.espacio]);
    });

    return ContentService.createTextOutput('ok');
  } catch (err) {
    return ContentService.createTextOutput('error: ' + err);
  } finally {
    candado.releaseLock();
  }
}

/** Para comprobar en el navegador que la dirección funciona. */
function doGet() {
  return ContentService.createTextOutput('Registro del Recorrido UdB 360° activo.');
}

function hoja_(nombre, columnas) {
  var libro = SpreadsheetApp.getActiveSpreadsheet();
  var hoja = libro.getSheetByName(nombre);
  if (!hoja) {
    hoja = libro.insertSheet(nombre);
    hoja.appendRow(columnas);
    hoja.getRange(1, 1, 1, columnas.length).setFontWeight('bold');
    hoja.setFrozenRows(1);
  }
  return hoja;
}

// Busca la clave en la columna A: si existe, reemplaza la fila; si no, la agrega
function guardar_(hoja, clave, fila) {
  var encontrada = hoja.getRange('A:A').createTextFinder(String(clave)).matchEntireCell(true).findNext();
  if (encontrada) hoja.getRange(encontrada.getRow(), 1, 1, fila.length).setValues([fila]);
  else hoja.appendRow(fila);
}

function fecha_(texto) {
  var f = new Date(texto);
  return isNaN(f.getTime()) ? texto : f;
}
