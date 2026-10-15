# Recorrido virtual 360° — Universidad de Boyacá

Este archivo es el punto de partida para seguir el proyecto con Claude Code. Léelo completo antes de tocar código.

## Qué es

Trabajo de grado de Ingeniería en Multimedia (Universidad de Boyacá, Tunja, 2026). Autores: Juan Felipe Martínez Villate y Iozef Nicolás Bonilla Valbuena. Director: Mg. José Manuel Rentería Andrade.

Es un recorrido virtual interactivo en 360° del campus, hecho en Unity y publicado como WebGL. Funciona en celular y computador.

- Versión publicada (la mantiene Nicolás en otro repositorio): https://nicolasbonilla03.github.io/RecorridovirtualWeb/
- Propósito: *mostrar* el campus a estudiantes de primer semestre. No es un sistema para llevar de un punto a otro: no hay minimapa, buscador de rutas ni ruta paso a paso.
- Cobertura: 52 de 60 espacios identificados. Se excluyeron los pisos 7 y 10 del Edificio 12 a solicitud institucional, y el Edificio 4 porque sus pasillos estrechos no permitieron fotos de calidad. No nombres en código ni textos los espacios excluidos por solicitud institucional.

## Entorno

- **Unity 2022.3.62f3**, URP, TextMeshPro. No abras el proyecto con Unity 6: el equipo también lo tiene instalado.
- Ruta local (equipo `redessociales2`): `C:\Users\Admin\Documents\GitHub\RecorridoVirtual360`
- Escena principal: `Assets/Scenes/RecorridoMain`
- Las panorámicas viven en `Assets/StreamingAssets/Panoramas` y se cargan por demanda en web (ver `CargadorPanoramas.cs`).

## Reglas de trabajo (importantes)

1. **No edites la escena ni los prefabs a mano desde código.** Toda la interfaz se construye en tiempo de ejecución desde los scripts (`MenuDesplegable`, `GuiaVirtual`, `PanelInfo`, `AplicarMarcaUdB`, `FadeController`). Si necesitas algo en la escena, dilo y que lo haga una persona en el editor.
2. **Idioma:** código, comentarios y textos de la interfaz en español neutro, tuteando y sin voseo («toca», «desliza», nunca «tocá»).
3. **Saltos de línea:** casi todos los `.cs` usan CRLF y `MarcaUdB.cs` usa LF. Respeta el que ya tenga cada archivo. Este equipo tiene `core.autocrlf = true`: al sacar archivos de Git todos quedan en CRLF, incluso `MarcaUdB.cs`. Dentro del repositorio se guardan en LF y Unity compila igual.
4. **No inventes datos** (puntajes, cifras, resultados de la validación). El trabajo de grado ya se entregó con datos reales.
5. **No presentes como probado lo que no se probó.** Antes de dar algo por terminado: compila en Unity, revisa que la consola no tenga errores y prueba en las tres vistas de abajo.
6. **Unity solo recompila cuando la ventana del editor tiene el foco.** Después de guardar, enfoca Unity o usa *Assets → Refresh* (Ctrl+R), y no pruebes hasta que termine «Compiling Scripts».
7. **En la vista Game, el primer clic después de usar otra ventana de Unity solo enfoca la vista.** Si un botón «no responde», haz un segundo clic antes de buscar un error. En el navegador no pasa.
8. **Antes de cambiar de rama en GitHub Desktop, haz commit.** Si se cambia de rama con cambios sin guardar, GitHub Desktop los mueve en un stash y, al aplicarlos sobre otra versión, pueden chocar y quedar marcas de conflicto (`<<<<<<<`, `=======`, `>>>>>>>`) dentro del código. Antes de cada commit revisa que no haya: `git grep -nE "^(<<<<<<<|=======|>>>>>>>)" -- "*.cs"`.

## Cómo probar

En la vista Game hay tres resoluciones guardadas, y cada una activa un modo distinto de la interfaz:

| Resolución | Modo | Cómo se ve |
| --- | --- | --- |
| Celular vertical (1080×2340) | móvil vertical, estilo Street View | Foto a pantalla completa. Arriba flotan el botón redondo de menú (vidrio) y el logo con «Vigilada Mineducación» debajo. Abajo, la hoja del lugar con tres alturas: compacta (edificio y lugar, con un botón redondo «i» a la derecha), normal (además el resumen) y completa (la ficha). Botón redondo «?» del guía encima de la hoja |
| Celular horizontal (2340×1080) | compacto | Botón negro «Menú», chip debajo, botón «Guía» |
| Full HD (1920×1080) | escritorio | «Menú» y chip en fila, logo a la derecha, botón «Pregúntale al guía» |

Cambia de resolución con el juego corriendo: la interfaz se reacomoda sola.

Para publicar la web: *Recorrido → Web y móvil → 1. Preparar ajustes para web* y luego *2. Construir para web*. La salida queda en `Builds/WebGL`. Si un build se interrumpe, usa *Restaurar fotos 360*.

## Arquitectura de scripts (`Assets/Scripts`)

| Script | Qué hace |
| --- | --- |
| `CameraController` | Mirar alrededor (arrastrar o rueda en escritorio; dedo o pellizco en móvil). En vertical amplía el campo de visión. |
| `GestorTeleports`, `TeleportPoint` | Saltos entre panorámicas (skyboxes) |
| `CargadorPanoramas` | En web descarga cada foto 360 al entrar y precarga las vecinas |
| `FadeController` | Pantalla de carga y fundido entre escenas (su lienzo va por encima de todo) |
| `MenuDesplegable` | Menú lateral por edificio, botón de menú y chip «Estás en» (horizontal y escritorio). En celular vertical arma la hoja inferior del lugar y publica su alto en `MarcaUdB.AltoHojaMovil`. Añade `GuiaVirtual` solo. |
| `GuiaVirtual` | Guía con preguntas predefinidas, «Ver ficha» y «Llévame». Botón flotante, halo y mensajes que salen del botón: el primero es la invitación general y los siguientes son preguntas del catálogo, de preferencia del lugar actual. Se repiten durante todo el recorrido: uno cada 25 s, o al llegar a un lugar nuevo si ya pasaron 10 s desde el anterior. Cuando el usuario ya abrió el guía, cada 60 s, o 30 s al cambiar de lugar. La invitación vuelve cada cinco mensajes, y las preguntas no se repiten hasta haber salido todas. Tocar el mensaje abre el guía con esa respuesta desplegada. |
| `PanelInfo`, `PuntoInfo`, `CatalogoInfo` | Fichas de información. En celular vertical la ficha sube desde abajo y se cierra con la X, tocando fuera o arrastrando el encabezado hacia abajo. El catálogo está en `Assets/Resources/PuntosInfo/catalogo.json` (fichas, con su `resumen`, y preguntas del guía) y se edita sin abrir Unity. |
| `POIController`, `MenusDeZona` | Puntos de edificio en el espacio, con su lista de lugares |
| `RegistroUso` | Registro anónimo de uso (completitud, tiempo por espacio, uso de fichas y guía). Ver `Docs/registro-de-uso.md` (Google Apps Script). |
| `Editor/HerramientasRecorrido` | Menú «Recorrido»: crear y mover botones de teletransporte (Ctrl+Alt+T), puntos de información (Ctrl+Alt+I), revisiones y nombres oficiales |
| `Editor/ConstruccionWeb` | Build WebGL con fotos por demanda |

### Sistema de marca (`Assets/Scripts/Marca`)

- `MarcaUdB.cs`: **tokens del sistema de diseño**. Úsalos siempre en vez de valores sueltos.
  - Colores: rojo `#d02e26` (una sola cosa roja por pantalla), negro `#1d1d1b`, superficies, tintas y foco `#0a58d0`. Hay un color por zona o edificio.
  - Tipografía: Raleway para títulos, Lato para texto e interfaz.
  - Espaciado: escala de 4 px (`Space1`–`Space10`). Radios: 4, 8 y 16, más píldora. Área táctil mínima de 44 px.
  - Funciones: `EsCompacto` (ancho menor a 900) y `EsMovilVertical` (compacto y alto mayor a 1.2 veces el ancho).
  - Medidas compartidas: `AltoControlFlotante` (48) y `DiametroGuiaMovil` (64). Hay dos que publican otros scripts: `AltoFilaSuperior`, que publica `AplicarMarcaUdB` para alinear el menú con el logo, y `AltoHojaMovil`, que publica `MenuDesplegable` para apoyar encima de la hoja el botón del guía y la pista.
  - Áreas seguras: `InsetSuperior` (muesca) e `InsetInferior` (barra de gestos).
  - **Texto sobre color de zona:** siempre en blanco. Usa `FondoParaBlanco(color)`, que oscurece el relleno lo justo para llegar a 4.5:1, o 3:1 con `ContrasteIcono` para glifos sueltos como «i» o «+». No uses `TintaSobre` en botones de zona: elige negro sobre los colores claros y se ve sucio.
  - Ícono de ubicación dibujado en código: `IconoUbicacion`.
- `EscalaAdaptable.cs`: CanvasScaler adaptable. En escritorio usa la referencia 1536×864. En celular vertical el ancho equivale a 400 px y en horizontal el alto equivale a 400 px.
- `AplicarMarcaUdB.cs`: logo oficial (mínimo 152 px de ancho, con área de protección de 1.5x), nota «Vigilada Mineducación» (obligatoria, no se esconde; en celular vertical va bajo el logo y en el resto abajo a la izquierda). La placa del logo se dibuja encima de la nota, para que su pista «Ir al inicio» no quede tapada, pista de uso, hotspots y fuentes.
- **Gestos:**
  - `HojaDeslizable.cs`: la hoja del celular vertical tiene tres alturas.
    - Arrastrar hacia abajo la lleva a compacta.
    - Hacia arriba, pasa de compacta a normal y de normal abre la ficha.
    - Tocarla en compacta la sube a normal; en normal abre la ficha.
    - En compacta el botón ancho «Ver más información» se convierte en un botón redondo «i» (44 px, color de la zona) a la derecha del título. El cambio va atado a la posición de la hoja: primero se desvanece el resumen, luego el botón ancho, y el redondo aparece en la segunda mitad del recorrido. El título deja reservado el sitio del botón redondo en las dos alturas, para no cambiar de renglones. La escala del botón redondo no se anima, porque la maneja `ToqueSuave`.
    - El orden de la hoja es título → botón → resumen. La hoja nunca cambia de tamaño: en compacta baja lo que miden el botón ancho y el resumen, que quedan bajo el borde de la pantalla. Recuerda la altura al cambiar de lugar.
  - `DeslizarParaCerrar.cs`: arrastrar hacia abajo el encabezado de la ficha la cierra.
  - Los dos cambian de estado con un gesto rápido (más de 0.11 px/ms) aunque sea corto.
  - **Ojo:** cuando un mismo objeto maneja toque y arrastre, Unity manda también el toque al soltar un arrastre, y antes que `OnEndDrag`. En `OnPointerClick` hay que ignorarlo con `if (datos.dragging) return;`.
- **Animación:** `AnimacionesUI.cs` (curvas y duraciones), `ToqueSuave.cs` (respuesta al toque), `VeloSuave.cs` (fundido del velo de los paneles), `GiroSuave.cs` (flechas de las listas) y `PistaAlPasar.cs`.

## Reglas de animación (filosofía de Emil Kowalski)

Las skills de Emil están instaladas en este equipo, en `~/.agents/skills/`: `emil-design-eng`, `improve-animations` y `review-animations`. Úsalas para cualquier cambio de movimiento. Lo ya aplicado:

- Curvas en `AnimacionesUI`:
  - `SalidaFuerte` = cubic-bezier(0.23, 1, 0.32, 1), para todo lo que entra o sale.
  - `Cajon` = cubic-bezier(0.32, 0.72, 0, 1), para los paneles laterales.
  - Nunca uses curvas que arranquen lento (*ease-in*) en la interfaz.
- Duraciones:
  - Paneles: 240 ms al entrar y 180 ms al salir (`DuracionPanelEntrada` / `DuracionPanelSalida`).
  - Velo: 160 ms al entrar y 140 ms al salir. Los controles flotantes (botón de menú, chip, hoja y botón del guía) se funden con ese mismo ritmo, mediante `VeloSuave`, cuando se abre o cierra un panel. Nunca se apagan de golpe.
  - Saltos entre lugares: ningún control se esconde por su cuenta. La pantalla del fundido los tapa a todos a la vez, así aparecen y desaparecen junto con la foto.
  - Mensaje del guía: se despliega en 280 ms y se recoge en 200 ms. Se queda 8 s. El «?» gira una vuelta a la izquierda mientras el mensaje sale y regresa a la derecha mientras se recoge; el giro va atado al mismo progreso. Además da un salto de 300 ms y el halo late dos veces.
  - Flechas: 150 ms. Panel del guía en móvil: 220 ms al abrir y 150 ms al cerrar.
  - Lugares de un edificio en el menú: fundido de 150 ms al abrirlo.
  - Salto entre lugares: si la foto ya está en memoria, 250 ms para tapar y 350 ms para destapar. Si hay que descargarla, se usa el fundido lineal de `velocidadFade`.
  - Ninguna animación de interfaz pasa de 300 ms.
- **Nada aparece desde escala 0:** arranca desde 0.95 con fundido.
- **El origen es el disparador:**
  - El panel del guía en móvil crece desde el botón «?».
  - El mensaje del guía sale de detrás del botón, de derecha a izquierda, con un recorte (`RectMask2D.padding`) que se abre, y se recoge de izquierda a derecha hasta meterse en él.
  - La ficha en celular vertical sube desde la hoja donde está su botón.
- **Todo se puede interrumpir:** cada animación parte del valor actual.
- Al presionar, los botones se encogen a 0.97 (0.96 en el espacio). El crecimiento al pasar es solo con mouse (`AnimacionesUI.HoverConMouse`).
- **Rendimiento:** anima solo posición, escala, rotación, transparencia (CanvasGroup) y recortes (`RectMask2D.padding`), nunca tamaños. Respeta `AnimacionesUI.Activas`, que es el interruptor de movimiento reducido. Nada debe trabajar en cada cuadro cuando la pantalla está quieta: el recorrido va dentro de otra página web.
- **Lección aprendida:** no pongas un `Canvas` anidado en el halo del botón del guía. Al probarlo, el botón en forma de píldora dejó de recibir clics en escritorio y en horizontal. La causa exacta no se identificó.

## Estado actual (13 de octubre de 2026)

**Ya hecho:**

- Interfaz reorganizada para escritorio y móvil.
- Pasada de animaciones con los criterios de Emil.
- Celular vertical rediseñado al estilo de Street View. Reemplaza a la versión de la maqueta «Frame 10», que tenía barra blanca y chip colgando.
- Mensajes del guía que salen de su botón, con gestos para abrir la ficha (deslizar arriba) y cerrarla (deslizar abajo).
- 15 de octubre: hoja del celular con tres alturas (compacta con botón redondo «i», normal y ficha). Probada en el editor con el mouse: bajar a compacta, tocar el botón redondo, subir a normal, subir a la ficha y tocar en compacta.
- Optimización para web:
  - La búsqueda de botones nuevos ya no corre cada 0,75 s.
  - El halo solo late mientras hay un mensaje.
  - Salto rápido entre lugares cuando la foto ya está cargada.
  - En celular se piden primero las fotos ASTC.
  - HDR apagado y volumen «Via Scripting» en `URP-Balanced`.
  - En los ajustes WebGL: nombres de archivo con hash y preferencia de energía «Default».

Todo se probó en el editor en las tres resoluciones. Después del merge, `main` se volvió a compilar sin errores el 15 de octubre. Los gestos se probaron con el mouse en la vista Game, no con el dedo.

**Pendiente:**

- [ ] **Recorrer los 4 lugares nuevos de Nicolás** (Camino Edificio 12, Camino Portería, Frente Portería, Pasillo 502) en celular vertical. `main` ya compila sin errores en Unity 2022.3 (probado el 15 de octubre, después del arreglo de `GuiaVirtual.cs` en `26d30e0`), pero esos lugares no se han visitado con la interfaz nueva.
- [ ] **Nuevo build WebGL** y publicarlo en el repositorio de Nicolás. Este equipo no tiene instalado *WebGL Build Support* para la 2022.3. Con los nombres con hash, borra la carpeta `Build` vieja antes de copiar la nueva.
- [ ] **Probar en un celular real**, en vertical y en horizontal, incluido un equipo con muesca:
  - el área segura: la foto ahora llega hasta arriba y la hoja cubre la barra de gestos;
  - los gestos con el dedo;
  - un lugar con nombre largo, que use los dos renglones del título de la hoja.
- [ ] **Incrustar en la web de la universidad:** usar un `<iframe>` que apunte al `index.html` del build. Si se mete de otra forma, hay que cambiar `streamingAssetsUrl` en `Assets/WebGLTemplates/RecorridoUdB/index.html` para que las fotos 360 se sigan encontrando.
- [ ] **Teletransporte que falló:** en la validación del 7 de octubre un punto de teletransporte no funcionó y no se identificó cuál. Usa *Recorrido → Revisar botones sin destino*.
- [ ] **Tamaño del botón redondo del guía:** está en 64 px; en la maqueta medía cerca de 110 px. Se ajusta en `MarcaUdB.DiametroGuiaMovil`.
- [ ] **Opcional:** los puntos de avanzar del espacio (círculos de color con «+») se ven grandes en vertical cuando la cámara mira hacia abajo. Se podrían escalar solo en vertical.

## Documentos

- `Docs/`: documento del trabajo de grado (.docx), figura de arquitectura y registro de uso.
- Sistema de diseño «Sistema UdB Digital» (en claude.ai): tokens, guías de marca y componentes. La regla es que toda interfaz nueva siga el estilo del proyecto: panel de vidrio, encabezado negro con filete rojo, color por zona y acción principal en rojo.
