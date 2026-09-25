# Marca UdB en el recorrido

Recursos del Manual de Marca UdB 2026, aplicados a través del Sistema UdB Digital.

- `Resources/MarcaUdB/Fuentes/`: Lato (Regular y Bold) para texto e interfaz, y Raleway (Bold y ExtraBold) para títulos. Las dos están aprobadas por el manual y tienen licencia SIL Open Font License (ver los `OFL-*.txt`). Raleway se instanció en pesos fijos a partir de la fuente variable de Google Fonts.
- `Resources/MarcaUdB/Sprites/`: formas de interfaz (esquinas redondeadas, hotspot con halo, sombra suave). No son elementos de marca.
- Los valores (colores, tamaños, espaciados) están en `Assets/Scripts/Marca/MarcaUdB.cs`.
- `AplicarMarcaUdB` aplica todo en tiempo de ejecución. Si no está en la escena, `MenuDesplegable` lo agrega solo.

## Logotipo

`Resources/MarcaUdB/Logotipos/Logo-UdB-Horizontal-a-Color.jpg` es el identificador oficial horizontal a color, descargado del micrositio de marca (uniboyaca.edu.co/es/node/7934) sin ninguna modificación. Ya incluye la marca registrada, «Vigilada Mineducación» y las acreditaciones.

Se muestra arriba a la derecha sobre una placa blanca sólida («sobre blanco: logotipo a color»), a 260 px de ancho (mínimo en pantalla: 152 px) y con el área de protección de 1.5x por los cuatro lados. No se recolorea, no se recorta y no lleva sombra ni efectos. Para usar otro archivo oficial, añade `AplicarMarcaUdB` al objeto `MenuManager` y asígnalo en **Logotipo Horizontal**.

## Colores por zona

Cada gran parte del recorrido tiene su color, tomado de la paleta del manual: Central azul `#5367aa`, Múltiple naranja `#df7b30`, Edificio 12 cian `#02a6b9`, Edificio 3 verde `#94bc44` y exteriores morado `#91277d`. El rojo institucional queda para la acción principal (Inicio) y los hotspots, y el negro institucional para el botón de menú y el encabezado del panel.

La lista está en el componente `AplicarMarcaUdB` (campo **Zonas**): «Contiene» son palabras que se buscan en el nombre del edificio, separadas por `|`. Cuando un color se usa para escribir, el código lo oscurece lo justo para llegar a 4.5:1 (la «tinta paralela» del sistema de diseño).

## Web y móvil

- `Assets/WebGLTemplates/RecorridoUdB/`: plantilla web con la marca (pantalla de carga con el logotipo, barra roja, «Armando el campus…», Vigilada Mineducación) y ajustada a móvil (pantalla completa, sin zoom del navegador, densidad de píxeles limitada).
- Menú **Recorrido → Web y móvil**: prepara los ajustes del reproductor, baja las panorámicas a 1024 px por cara solo para WebGL y construye en `Builds/WebGL`.
