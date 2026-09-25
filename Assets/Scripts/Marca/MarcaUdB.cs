using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Una gran parte del recorrido (un edificio o los exteriores) con su color.
/// «Contiene» son palabras separadas por | que se buscan en el nombre del edificio.
/// </summary>
[System.Serializable]
public class ZonaRecorrido
{
    public string nombre;
    public string contiene;
    public Color color;

    public ZonaRecorrido() { }

    public ZonaRecorrido(string nombre, string contiene, string hex)
    {
        this.nombre = nombre;
        this.contiene = contiene;
        this.color = MarcaUdB.Hex(hex);
    }
}

/// <summary>
/// Valores del Sistema UdB Digital (Manual de Marca UdB 2026 + decisiones digitales) para Unity.
/// Colores del tema claro, escala tipográfica, espaciado, radios y sombras, más las fuentes
/// y los sprites de la carpeta Assets/Marca/Resources/MarcaUdB.
/// Si un valor cambia en el sistema de diseño, se cambia aquí y el recorrido entero se mueve con él.
/// </summary>
public static class MarcaUdB
{
    // ------------------------------------------------------------------ color (tema claro)

    // Primarios del manual
    public static readonly Color Rojo = Hex("#d02e26");          // udb-rojo: acento, una sola cosa por pantalla
    public static readonly Color RojoFuerte = Hex("#a8241e");    // hover / pressed y rojo como tinta sobre surface-200
    public static readonly Color RojoTenue = Hex("#fbe9e8");
    public static readonly Color Negro = Hex("#1d1d1b");         // udb-negro, Process Black

    // Superficie y tinta (sesgo cálido)
    public static readonly Color Surface100 = Hex("#ffffff");
    public static readonly Color Surface200 = Hex("#f6f4f0");
    public static readonly Color Surface300 = Hex("#eceae4");
    public static readonly Color Ink = Hex("#1d1d1b");
    public static readonly Color InkMuted = Hex("#5f5d57");
    public static readonly Color InkInverso = Hex("#ffffff");
    public static readonly Color Borde = Hex("#dedbd3");
    public static readonly Color BordeControl = Hex("#8a877f");
    public static readonly Color Foco = Hex("#0a58d0");

    // Sobre la fotografía 360°
    public static readonly Color VeloEscena = Hex("#1d1d1bb3");   // velo detrás de un panel o menú
    public static readonly Color VidrioPanel = Hex("#ffffffeb");  // paneles flotantes, opacidad mínima 0.92

    // ------------------------------------------------------------------ tipografía (px)

    public const float DisplayXL = 48f;   // Raleway 800
    public const float DisplayL = 32f;    // Raleway 700
    public const float DisplayM = 22f;    // Raleway 700
    public const float CuerpoL = 17f;     // Lato 400
    public const float Cuerpo = 15f;      // Lato 400
    public const float CuerpoS = 13f;     // Lato 400: pie, créditos, Vigilada Mineducación
    public const float UIControl = 15f;   // Lato 700
    public const float UIControlS = 13f;  // Lato 700
    public const float Etiqueta = 11f;    // Lato 700, mayúsculas por estilo, espaciado 0.09em
    public const float EtiquetaEspaciado = 9f; // TMP mide el espaciado en centésimas de em

    // ------------------------------------------------------------------ espaciado, radio, opacidad

    public const float Space1 = 4f, Space2 = 8f, Space3 = 12f, Space4 = 16f,
                       Space5 = 24f, Space6 = 32f, Space8 = 48f, Space10 = 64f;
    public const float RadiusSm = 4f, RadiusMd = 8f, RadiusLg = 16f;
    public const float AreaTactil = 44f;
    public const float OpacidadInactivo = 0.45f;

    /// <summary>
    /// Resolución de referencia de los Canvas de interfaz. Los tamaños del sistema están en px CSS;
    /// con 1536×864 un px del sistema se ve igual que en un navegador a escala 125 % sobre 1080p,
    /// que es lo habitual en un portátil con Windows.
    /// </summary>
    public static readonly Vector2 ResolucionReferencia = new Vector2(1536f, 864f);

    // ------------------------------------------------------------------ fuentes

    const string RutaFuentes = "MarcaUdB/Fuentes/";
    const string RutaSprites = "MarcaUdB/Sprites/";

    static TMP_FontAsset texto, textoBold, display, displayExtra;
    static bool fuentesCargadas;

    public static TMP_FontAsset Texto { get { CargarFuentes(); return texto; } }            // Lato Regular
    public static TMP_FontAsset TextoBold { get { CargarFuentes(); return textoBold; } }    // Lato Bold
    public static TMP_FontAsset Display { get { CargarFuentes(); return display; } }        // Raleway Bold
    public static TMP_FontAsset DisplayExtra { get { CargarFuentes(); return displayExtra; } } // Raleway ExtraBold

    static void CargarFuentes()
    {
        if (fuentesCargadas) return;
        fuentesCargadas = true;
        texto = CrearFuente("Lato-Regular");
        textoBold = CrearFuente("Lato-Bold");
        display = CrearFuente("Raleway-Bold");
        displayExtra = CrearFuente("Raleway-ExtraBold");
    }

    static TMP_FontAsset CrearFuente(string archivo)
    {
        Font fuente = Resources.Load<Font>(RutaFuentes + archivo);
        if (fuente == null)
        {
            Debug.LogWarning("[MarcaUdB] No se encontró la fuente " + archivo + " en Resources/" + RutaFuentes);
            return null;
        }

        TMP_FontAsset asset = null;
        try { asset = TMP_FontAsset.CreateFontAsset(fuente); }
        catch (System.Exception e) { Debug.LogWarning("[MarcaUdB] No se pudo crear la fuente " + archivo + ": " + e.Message); }
        if (asset == null) return null;

        asset.name = archivo + " (UdB)";

        // Respaldo real: si falta un glifo se usa la fuente por defecto de TextMeshPro
        if (TMP_Settings.defaultFontAsset != null)
        {
            if (asset.fallbackFontAssetTable == null) asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            asset.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
        }
        return asset;
    }

    /// <summary>true si la fuente ya es una de las de marca.</summary>
    public static bool EsFuenteDeMarca(TMP_FontAsset f)
    {
        CargarFuentes();
        return f != null && (f == texto || f == textoBold || f == display || f == displayExtra);
    }

    /// <summary>Aplica familia, tamaño y color. Si la fuente no está disponible, deja la que tenía.</summary>
    public static void Estilo(TMP_Text t, TMP_FontAsset fuente, float tamano, Color color)
    {
        if (t == null) return;
        if (fuente != null) t.font = fuente;
        t.fontSize = tamano;
        t.color = color;
    }

    /// <summary>Estilo «etiqueta»: 11 px, Lato 700, mayúsculas por estilo (no en el contenido), espaciado 0.09em.</summary>
    public static void EstiloEtiqueta(TMP_Text t, Color color)
    {
        Estilo(t, TextoBold, Etiqueta, color);
        t.fontStyle = FontStyles.UpperCase;
        t.characterSpacing = EtiquetaEspaciado;
    }

    // ------------------------------------------------------------------ sprites

    static Sprite redondeado, hotspot, sombra, logotipoHorizontal;

    /// <summary>Identificador oficial horizontal a color (micrositio de marca UdB), sin modificar.</summary>
    public static Sprite LogotipoHorizontal { get { if (logotipoHorizontal == null) logotipoHorizontal = Resources.Load<Sprite>("MarcaUdB/Logotipos/Logo-UdB-Horizontal-a-Color"); return logotipoHorizontal; } }

    public static Sprite Redondeado { get { if (redondeado == null) redondeado = Resources.Load<Sprite>(RutaSprites + "redondeado"); return redondeado; } }
    public static Sprite Hotspot { get { if (hotspot == null) hotspot = Resources.Load<Sprite>(RutaSprites + "hotspot"); return hotspot; } }
    public static Sprite Sombra { get { if (sombra == null) sombra = Resources.Load<Sprite>(RutaSprites + "sombra"); return sombra; } }

    // Borde del sprite redondeado (px) y del sprite de sombra (px), ver los .meta
    const float BordeRedondeado = 64f;
    const float NucleoSombra = 32f;

    /// <summary>Redondea las esquinas de una Image con el radio indicado (en unidades de la interfaz).</summary>
    public static void Redondear(Image img, float radio)
    {
        if (img == null || Redondeado == null || radio <= 0f) return;
        img.sprite = Redondeado;
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = BordeRedondeado / radio;
    }

    /// <summary>Radio de píldora: la mitad de la altura del elemento.</summary>
    public static void Pildora(Image img, float alto)
    {
        Redondear(img, alto * 0.5f);
    }

    /// <summary>
    /// Sombra suave detrás de un elemento (sombra-flotante o sombra-panel del sistema).
    /// La sombra solo separa la interfaz de la fotografía 360°.
    /// </summary>
    public static Image AgregarSombra(RectTransform objetivo, float desenfoque, float desplazamientoY, float opacidad)
    {
        if (objetivo == null || Sombra == null || objetivo.parent == null) return null;

        GameObject go = new GameObject(objetivo.name + "_Sombra", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(objetivo.parent, false);
        rt.SetSiblingIndex(objetivo.GetSiblingIndex());

        rt.anchorMin = objetivo.anchorMin;
        rt.anchorMax = objetivo.anchorMax;
        rt.pivot = objetivo.pivot;
        // Crece «desenfoque» por cada lado sin importar dónde esté el pivote del objetivo
        rt.anchoredPosition = objetivo.anchoredPosition + new Vector2(
            desenfoque * (2f * objetivo.pivot.x - 1f),
            desenfoque * (2f * objetivo.pivot.y - 1f) - desplazamientoY);
        rt.sizeDelta = objetivo.sizeDelta + new Vector2(desenfoque * 2f, desenfoque * 2f);

        Image img = go.GetComponent<Image>();
        img.sprite = Sombra;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = NucleoSombra / Mathf.Max(1f, desenfoque);
        img.color = new Color(Negro.r, Negro.g, Negro.b, opacidad);
        img.raycastTarget = false;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        return img;
    }

    /// <summary>sombra-flotante: 0 2px 8px, 14 %.</summary>
    public static Image SombraFlotante(RectTransform objetivo) { return AgregarSombra(objetivo, 8f, 2f, 0.14f); }

    /// <summary>sombra-panel: 0 12px 32px, 20 %.</summary>
    public static Image SombraPanel(RectTransform objetivo) { return AgregarSombra(objetivo, 32f, 12f, 0.20f); }

    // ------------------------------------------------------------------ botones

    /// <summary>
    /// Colores de un botón (o cualquier Selectable) con transición ColorTint. La Image queda en blanco y los colores van en el bloque.
    /// </summary>
    public static void ColoresBoton(Selectable b, Color normal, Color hover, Color presionado)
    {
        if (b == null) return;
        if (b.targetGraphic != null) b.targetGraphic.color = Color.white;
        ColorBlock cb = b.colors;
        cb.normalColor = normal;
        cb.highlightedColor = hover;
        cb.pressedColor = presionado;
        cb.selectedColor = normal;
        cb.disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * OpacidadInactivo);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.12f; // micro-interacciones: 120 ms
        b.colors = cb;
    }

    // ------------------------------------------------------------------ barras de desplazamiento

    /// <summary>
    /// Color para un control que no es texto (una manija, un indicador): el mismo tono
    /// oscurecido lo justo para llegar a 3:1 sobre el fondo (WCAG 1.4.11).
    /// </summary>
    public static Color ColorControl(Color c, Color fondo)
    {
        Color r = c;
        r.a = 1f;
        for (int i = 0; i < 20 && Contraste(r, fondo) < 3f; i++)
            r = Color.Lerp(c, Negro, (i + 1) * 0.05f);
        r.a = 1f;
        return r;
    }

    /// <summary>
    /// Barra de desplazamiento de marca: riel en píldora con el relleno suave del color y
    /// manija en píldora con el color pleno. Los colores van en las Image y el bloque de
    /// colores solo oscurece al pasar el puntero y al arrastrar, así el color puede cambiar en vivo.
    /// </summary>
    public static void EstilizarBarra(Scrollbar sb, Color acento, Color fondo, float ancho)
    {
        if (sb == null) return;

        RectTransform rt = (RectTransform)sb.transform;
        rt.sizeDelta = new Vector2(ancho, rt.sizeDelta.y);

        Image riel = sb.GetComponent<Image>();
        if (riel != null)
        {
            Redondear(riel, ancho * 0.5f);
            riel.raycastTarget = true;
        }

        if (sb.handleRect != null)
        {
            // La manija ocupa todo el ancho del riel y llega a sus extremos
            RectTransform area = sb.handleRect.parent as RectTransform;
            if (area != null && area != rt)
            {
                area.anchorMin = Vector2.zero;
                area.anchorMax = Vector2.one;
                area.sizeDelta = Vector2.zero;
                area.anchoredPosition = Vector2.zero;
            }
            sb.handleRect.sizeDelta = Vector2.zero;
            sb.handleRect.anchoredPosition = Vector2.zero;

            Image manija = sb.handleRect.GetComponent<Image>();
            if (manija != null)
            {
                Redondear(manija, ancho * 0.5f);
                sb.targetGraphic = manija;
            }
        }

        ColorBlock cb = sb.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(1f, 1f, 1f, OpacidadInactivo);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.12f;
        sb.colors = cb;

        ColorBarra(sb, acento, fondo);
    }

    /// <summary>Cambia el color de una barra ya estilizada (riel suave, manija en el color pleno).</summary>
    public static void ColorBarra(Scrollbar sb, Color acento, Color fondo)
    {
        if (sb == null) return;
        Image riel = sb.GetComponent<Image>();
        if (riel != null) riel.color = Tenue(acento, 0.2f);
        if (sb.targetGraphic != null) sb.targetGraphic.color = ColorControl(acento, fondo);
    }

    // ------------------------------------------------------------------ zonas del recorrido

    /// <summary>
    /// Colores de las grandes partes del recorrido. Salen de la paleta del Manual de Marca
    /// (los colores de facultad), usados aquí como color por edificio. El rojo institucional
    /// no entra: queda reservado para la acción principal y los hotspots.
    /// </summary>
    public static List<ZonaRecorrido> ZonasPorDefecto()
    {
        return new List<ZonaRecorrido>
        {
            new ZonaRecorrido("Edificio Central", "central", "#5367aa"),
            new ZonaRecorrido("Edificio Múltiple", "múltiple|multiple", "#df7b30"),
            new ZonaRecorrido("Edificio 12", "12", "#02a6b9"),
            new ZonaRecorrido("Edificio 3", "edificio 3|ed3|ed. 3", "#94bc44"),
            new ZonaRecorrido("Exteriores", "exterior|campus|camino|cancha|coliseo|porter", "#91277d"),
        };
    }

    static List<ZonaRecorrido> zonas = ZonasPorDefecto();

    /// <summary>Reemplaza las zonas (lo hace AplicarMarcaUdB con la lista de su Inspector).</summary>
    public static void RegistrarZonas(List<ZonaRecorrido> lista)
    {
        if (lista != null && lista.Count > 0) zonas = lista;
    }

    /// <summary>Busca la zona cuyo texto aparece en el nombre dado (sin distinguir mayúsculas).</summary>
    public static bool BuscarZona(string nombre, out ZonaRecorrido zona)
    {
        zona = null;
        if (string.IsNullOrEmpty(nombre) || zonas == null) return false;
        string n = nombre.ToLowerInvariant();
        foreach (ZonaRecorrido z in zonas)
        {
            if (z == null || string.IsNullOrEmpty(z.contiene)) continue;
            foreach (string clave in z.contiene.Split('|'))
            {
                string c = clave.Trim().ToLowerInvariant();
                if (c.Length > 0 && n.Contains(c)) { zona = z; return true; }
            }
        }
        return false;
    }

    /// <summary>Color de la zona a la que pertenece un nombre; negro institucional si no tiene zona.</summary>
    public static Color ColorZona(string nombre)
    {
        ZonaRecorrido z;
        return BuscarZona(nombre, out z) ? z.color : Negro;
    }

    /// <summary>Relleno suave de un color: mezcla con blanco (t = cuánto color).</summary>
    public static Color Tenue(Color c, float t)
    {
        Color r = Color.Lerp(Color.white, c, t);
        r.a = 1f;
        return r;
    }

    /// <summary>
    /// «Tinta paralela»: el mismo tono oscurecido hasta llegar a 4.5:1 sobre blanco,
    /// para escribir con el color sin cambiar el color de marca.
    /// </summary>
    public static Color TintaLegible(Color c)
    {
        return TintaLegible(c, Color.white);
    }

    /// <summary>Igual, pero sobre un fondo dado (por ejemplo el relleno suave de la zona).</summary>
    public static Color TintaLegible(Color c, Color fondo)
    {
        Color r = c;
        r.a = 1f;
        for (int i = 0; i < 20 && Contraste(r, fondo) < 4.5f; i++)
            r = Color.Lerp(c, Negro, (i + 1) * 0.05f);
        r.a = 1f;
        return r;
    }

    /// <summary>Tinta para escribir SOBRE un relleno: blanco o negro institucional, la que más contraste dé.</summary>
    public static Color TintaSobre(Color fondo)
    {
        return Contraste(InkInverso, fondo) >= Contraste(Ink, fondo) ? InkInverso : Ink;
    }

    /// <summary>Contraste WCAG entre dos colores opacos.</summary>
    public static float Contraste(Color a, Color b)
    {
        float la = Luminancia(a), lb = Luminancia(b);
        if (la < lb) { float t = la; la = lb; lb = t; }
        return (la + 0.05f) / (lb + 0.05f);
    }

    static float Luminancia(Color c)
    {
        return 0.2126f * Lineal(c.r) + 0.7152f * Lineal(c.g) + 0.0722f * Lineal(c.b);
    }

    static float Lineal(float v)
    {
        return v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    // ------------------------------------------------------------------ pantalla

    /// <summary>true en pantallas angostas (móvil vertical): la interfaz pasa a su versión compacta.</summary>
    public static bool EsCompacto(RectTransform canvas)
    {
        return canvas != null && canvas.rect.width > 0f && canvas.rect.width < 900f;
    }

    /// <summary>Ajusta un RectTransform al área segura de la pantalla (muescas y barras del sistema).</summary>
    public static void AjustarAreaSegura(RectTransform rt)
    {
        if (rt == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect seguro = Screen.safeArea;
        Vector2 min = new Vector2(seguro.xMin / Screen.width, seguro.yMin / Screen.height);
        Vector2 max = new Vector2(seguro.xMax / Screen.width, seguro.yMax / Screen.height);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------------ utilidades

    /// <summary>
    /// Convierte "#rrggbb" o "#rrggbbaa" en Color. Es C# puro (sin llamadas nativas de Unity)
    /// para poder usarse en inicializadores de campos serializados.
    /// </summary>
    public static Color Hex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new Color(1f, 0f, 1f, 1f);
        string h = hex.TrimStart('#');
        if (h.Length != 6 && h.Length != 8) return new Color(1f, 0f, 1f, 1f);
        float r = Byte(h, 0), g = Byte(h, 2), b = Byte(h, 4);
        float a = h.Length == 8 ? Byte(h, 6) : 1f;
        return new Color(r, g, b, a);
    }

    static float Byte(string h, int i)
    {
        int v = 0;
        for (int k = i; k < i + 2; k++)
        {
            char c = h[k];
            int d = c >= '0' && c <= '9' ? c - '0'
                  : c >= 'a' && c <= 'f' ? c - 'a' + 10
                  : c >= 'A' && c <= 'F' ? c - 'A' + 10 : 0;
            v = v * 16 + d;
        }
        return v / 255f;
    }

    public static string HexRGB(Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}
