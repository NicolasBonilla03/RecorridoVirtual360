using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ficha de información de un PuntoInfo, con el mismo estilo del menú lateral:
/// panel de vidrio, encabezado negro con filete rojo, fila de la zona con su color,
/// barra de desplazamiento del color de la zona y la acción principal en rojo.
/// Entra deslizándose por la derecha (el menú entra por la izquierda).
/// Se cierra con la X, tocando fuera o con Esc. Mientras está abierta es lo único que responde.
/// Se crea sola la primera vez que se toca un punto.
/// </summary>
public class PanelInfo : MonoBehaviour
{
    static PanelInfo instancia;

    public static bool Abierto { get { return instancia != null && instancia.abierto; } }

    public static void Mostrar(PuntoInfo punto)
    {
        if (punto == null) return;
        Asegurar().Abrir(punto);
    }

    public static void CerrarSiAbierto()
    {
        if (instancia != null) instancia.Cerrar();
    }

    static PanelInfo Asegurar()
    {
        if (instancia == null)
        {
            GameObject go = new GameObject("PanelInfo_UdB");
            instancia = go.AddComponent<PanelInfo>();
        }
        return instancia;
    }

    // Mismas medidas que el menú lateral (MenuDesplegable)
    const int OrdenCanvas = 60;                      // encima del menú (50), debajo del fundido (1000)
    const float AnchoPanel = 400f;
    const float MargenPanel = MarcaUdB.Space4;       // 16 px
    const float AltoControl = 48f;
    const float AltoZona = 44f;
    const float AnchoBarra = 8f;

    RectTransform canvasRT;
    GameObject lienzo;
    GameObject velo;
    RectTransform zonaSegura;
    RectTransform panelRaiz;

    TextMeshProUGUI rotulo;
    TextMeshProUGUI titulo;
    GameObject filaZona;
    Image rellenoZona;
    Image bandaZona;
    TextMeshProUGUI textoZona;
    RectTransform contenido;
    ScrollRect scroll;
    Scrollbar barra;
    GameObject pie;
    Button botonEnlace;
    TextMeshProUGUI textoEnlace;

    bool abierto;
    Coroutine animacion;
    Vector2 ultimoTamano;
    Rect ultimaAreaSegura;

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        Construir();
        panelRaiz.anchoredPosition = new Vector2(PosicionOculta(), 0f);
        lienzo.SetActive(false);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Update()
    {
        if (!abierto) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cerrar();
            return;
        }

        // Rotación del teléfono, cambio de tamaño de la ventana o de área segura
        if (canvasRT.rect.size != ultimoTamano || Screen.safeArea != ultimaAreaSegura)
            AjustarAncho();
    }

    // ------------------------------------------------------------------ abrir y cerrar

    void Abrir(PuntoInfo punto)
    {
        // Un solo menú a la vez
        POIController.CerrarAbierto();
        MenuDesplegable.CerrarSiAbierto();

        bool yaAbierto = abierto;
        abierto = true;
        lienzo.SetActive(true);
        velo.SetActive(true);
        AjustarAncho();
        Rellenar(punto);
        scroll.verticalNormalizedPosition = 1f;

        if (!yaAbierto) Animar(-MargenPanel);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;
        velo.SetActive(false);
        if (isActiveAndEnabled) Animar(PosicionOculta());
        else lienzo.SetActive(false);
    }

    float PosicionOculta()
    {
        return panelRaiz.sizeDelta.x + 80f;
    }

    void Animar(float destinoX)
    {
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(Deslizar(destinoX));
    }

    // Igual que el menú: 200 ms con salida suave
    IEnumerator Deslizar(float destinoX)
    {
        float inicioX = panelRaiz.anchoredPosition.x;
        float t = 0f;
        const float duracion = 0.2f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            panelRaiz.anchoredPosition = new Vector2(Mathf.Lerp(inicioX, destinoX, e), 0f);
            yield return null;
        }

        if (!abierto) lienzo.SetActive(false);
        animacion = null;
    }

    void AjustarAncho()
    {
        ultimoTamano = canvasRT.rect.size;
        ultimaAreaSegura = Screen.safeArea;
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        float disponible = zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
        float ancho = Mathf.Min(AnchoPanel, disponible - MargenPanel * 2f);
        panelRaiz.sizeDelta = new Vector2(ancho, -MargenPanel * 2f);
        if (abierto && animacion == null)
            panelRaiz.anchoredPosition = new Vector2(-MargenPanel, 0f);
    }

    // ------------------------------------------------------------------ contenido

    void Rellenar(PuntoInfo p)
    {
        Color zona = p.ColorZona;

        rotulo.text = p.NombreTipo;
        titulo.text = p.titulo;

        // Fila de la zona: relleno suave, banda sólida y nombre en la tinta legible de la zona
        bool conZona = !string.IsNullOrEmpty(p.zona);
        filaZona.SetActive(conZona);
        if (conZona)
        {
            Color fondoZona = MarcaUdB.Tenue(zona, 0.14f);
            rellenoZona.color = fondoZona;
            bandaZona.color = zona;
            textoZona.text = p.zona;
            textoZona.color = MarcaUdB.TintaLegible(zona, fondoZona);
        }

        MarcaUdB.ColorBarra(barra, zona, Color.white);

        // Cuerpo: se vacía y se arma de nuevo
        for (int i = contenido.childCount - 1; i >= 0; i--)
            Destroy(contenido.GetChild(i).gameObject);

        if (p.foto != null)
        {
            Image foto = CrearImagen("Foto", contenido, Color.white);
            foto.sprite = p.foto;
            foto.preserveAspect = true;
            foto.raycastTarget = false;
            float proporcion = p.foto.rect.width > 0f ? p.foto.rect.height / p.foto.rect.width : 0.56f;
            LayoutElement le = foto.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = Mathf.Clamp((AnchoPanel - MarcaUdB.Space4 * 2f) * proporcion, 120f, 240f);
        }

        if (!string.IsNullOrEmpty(p.resumen))
            Parrafo(contenido, p.resumen.Trim(), MarcaUdB.CuerpoL);

        if (!string.IsNullOrEmpty(p.descripcion))
            Parrafo(contenido, p.descripcion.Trim(), MarcaUdB.Cuerpo);

        // «Adentro encuentras»: filas como las de los lugares del menú, con acento del color de la zona
        if (p.queEncuentras != null)
        {
            RectTransform grupo = null;
            foreach (string s in p.queEncuentras)
            {
                if (string.IsNullOrEmpty(s) || s.Trim().Length == 0) continue;
                if (grupo == null)
                {
                    grupo = Grupo(MarcaUdB.Space1);
                    Rotulo(grupo, p.tipo == TipoPuntoInfo.Edificio ? "Adentro encuentras" : "Aquí puedes");
                }
                FilaLista(grupo, s.Trim(), zona);
            }
        }

        Dato("Horario", p.horario);
        Dato("Ubicación", p.ubicacion);
        Dato("Contacto", p.contacto);

        // Pie: enlace opcional (acción principal en rojo, como «Inicio» en el menú)
        bool conEnlace = !string.IsNullOrEmpty(p.enlace) && p.enlace.Trim().Length > 0;
        pie.SetActive(conEnlace);
        if (conEnlace)
        {
            textoEnlace.text = string.IsNullOrEmpty(p.textoEnlace) ? "Más información" : p.textoEnlace;
            botonEnlace.onClick.RemoveAllListeners();
            string url = p.enlace.Trim();
            botonEnlace.onClick.AddListener(() => Application.OpenURL(url));
        }
    }

    TextMeshProUGUI Parrafo(RectTransform padre, string texto, float tamano)
    {
        TextMeshProUGUI t = CrearTexto("Parrafo", padre, texto, MarcaUdB.Texto, tamano, MarcaUdB.Ink);
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.lineSpacing = 12f;       // interlineado holgado para leer en pantalla
        t.paragraphSpacing = 12f;  // una línea en blanco separa párrafos
        t.margin = new Vector4(MarcaUdB.Space1, 0f, MarcaUdB.Space1, 0f);
        return t;
    }

    void Rotulo(RectTransform padre, string texto)
    {
        TextMeshProUGUI t = CrearTexto("Rotulo", padre, texto, null, 0f, Color.white);
        MarcaUdB.EstiloEtiqueta(t, MarcaUdB.InkMuted);
        t.margin = new Vector4(MarcaUdB.Space1, MarcaUdB.Space2, 0f, 0f);
        LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 28f;
    }

    // Fila con acento: mismo tratamiento que el lugar actual del menú, sin ser botón
    void FilaLista(RectTransform padre, string texto, Color zona)
    {
        Image fila = CrearImagen("Fila", padre, MarcaUdB.Tenue(zona, 0.08f));
        fila.raycastTarget = false;
        MarcaUdB.Redondear(fila, MarcaUdB.RadiusMd);
        HorizontalLayoutGroup hl = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset((int)MarcaUdB.Space6, (int)MarcaUdB.Space4, (int)MarcaUdB.Space3, (int)MarcaUdB.Space3);
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = true;
        hl.childForceExpandHeight = false;
        LayoutElement le = fila.gameObject.AddComponent<LayoutElement>();
        le.minHeight = MarcaUdB.AreaTactil;

        Image acento = CrearImagen("Acento", fila.transform, zona);
        acento.raycastTarget = false;
        LayoutElement la = acento.gameObject.AddComponent<LayoutElement>();
        la.ignoreLayout = true;
        RectTransform ra = (RectTransform)acento.transform;
        ra.anchorMin = new Vector2(0f, 0f);
        ra.anchorMax = new Vector2(0f, 1f);
        ra.pivot = new Vector2(0f, 0.5f);
        ra.sizeDelta = new Vector2(4f, -MarcaUdB.Space4);
        ra.anchoredPosition = new Vector2(MarcaUdB.Space2, 0f);
        MarcaUdB.Redondear(acento, 2f);

        TextMeshProUGUI t = CrearTexto("Texto", fila.transform, texto, MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.Ink);
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.alignment = TextAlignmentOptions.MidlineLeft;
    }

    RectTransform Grupo(float espaciado)
    {
        GameObject go = new GameObject("Grupo", typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(contenido, false);
        VerticalLayoutGroup vl = go.GetComponent<VerticalLayoutGroup>();
        vl.spacing = espaciado;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        return (RectTransform)go.transform;
    }

    void Dato(string nombre, string valor)
    {
        if (string.IsNullOrEmpty(valor) || valor.Trim().Length == 0) return;
        RectTransform g = Grupo(0f);
        Rotulo(g, nombre);
        Parrafo(g, valor.Trim(), MarcaUdB.Cuerpo);
    }

    // ------------------------------------------------------------------ construcción

    void Construir()
    {
        if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        lienzo = new GameObject("PanelInfo_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);
        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OrdenCanvas;
        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = MarcaUdB.ResolucionReferencia;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        canvasRT = (RectTransform)lienzo.transform;

        // velo-escena a pantalla completa: aísla la lectura, bloquea la cámara y cierra al tocarlo
        Image imgVelo = CrearImagen("VeloEscena", canvasRT, MarcaUdB.VeloEscena);
        Estirar((RectTransform)imgVelo.transform, 0f, 0f);
        Button botonVelo = imgVelo.gameObject.AddComponent<Button>();
        botonVelo.transition = Selectable.Transition.None;
        SinNavegacion(botonVelo);
        botonVelo.onClick.AddListener(Cerrar);
        velo = imgVelo.gameObject;

        GameObject goSegura = new GameObject("AreaSegura", typeof(RectTransform));
        zonaSegura = (RectTransform)goSegura.transform;
        zonaSegura.SetParent(canvasRT, false);
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        // Raíz que se desliza: lleva la sombra y el panel juntos (anclada a la derecha)
        GameObject goRaiz = new GameObject("PanelRaiz", typeof(RectTransform));
        panelRaiz = (RectTransform)goRaiz.transform;
        panelRaiz.SetParent(zonaSegura, false);
        panelRaiz.anchorMin = new Vector2(1f, 0f);
        panelRaiz.anchorMax = new Vector2(1f, 1f);
        panelRaiz.pivot = new Vector2(1f, 0.5f);
        panelRaiz.sizeDelta = new Vector2(AnchoPanel, -MargenPanel * 2f);

        Image imgPanel = CrearImagen("Panel", panelRaiz, MarcaUdB.VidrioPanel);
        RectTransform panel = (RectTransform)imgPanel.transform;
        Estirar(panel, 0f, 0f);
        MarcaUdB.Redondear(imgPanel, MarcaUdB.RadiusLg);
        MarcaUdB.SombraPanel(panel);

        VerticalLayoutGroup vl = imgPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        int pad = (int)MarcaUdB.Space4;
        vl.padding = new RectOffset(pad, pad, pad, pad);
        vl.spacing = MarcaUdB.Space3;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        ConstruirEncabezado(panel);
        ConstruirFilaZona(panel);
        ConstruirCuerpo(panel);
        ConstruirPie(panel);
    }

    // Encabezado negro con filete rojo, como el del menú
    void ConstruirEncabezado(RectTransform panel)
    {
        Image fondo = CrearImagen("Encabezado", panel, MarcaUdB.Negro);
        fondo.raycastTarget = false;
        MarcaUdB.Redondear(fondo, MarcaUdB.RadiusMd);
        RectTransform rtE = (RectTransform)fondo.transform;

        float p = MarcaUdB.Space4;
        VerticalLayoutGroup vl = fondo.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset((int)p, (int)(p + MarcaUdB.AreaTactil), (int)(p + 4f + 10f), (int)(p + 2f));
        vl.spacing = MarcaUdB.Space1;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        LayoutElement le = fondo.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 112f;

        Image filete = CrearImagen("FileteRojo", rtE, MarcaUdB.Rojo);
        filete.raycastTarget = false;
        filete.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        RectTransform rf = (RectTransform)filete.transform;
        rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0f, 1f);
        rf.sizeDelta = new Vector2(32f, 4f);
        rf.anchoredPosition = new Vector2(p, -p);
        MarcaUdB.Redondear(filete, 2f);

        rotulo = CrearTexto("Rotulo", rtE, "", null, 0f, Color.white);
        MarcaUdB.EstiloEtiqueta(rotulo, new Color(1f, 1f, 1f, 0.72f));

        titulo = CrearTexto("Titulo", rtE, "", MarcaUdB.Display, MarcaUdB.DisplayM, MarcaUdB.InkInverso);
        titulo.enableWordWrapping = true;
        titulo.overflowMode = TextOverflowModes.Overflow;
        titulo.alignment = TextAlignmentOptions.TopLeft;

        Image imgCerrar = CrearImagen("Cerrar", rtE, Color.white);
        imgCerrar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Button cerrar = imgCerrar.gameObject.AddComponent<Button>();
        cerrar.targetGraphic = imgCerrar;
        SinNavegacion(cerrar);
        RectTransform rtX = (RectTransform)cerrar.transform;
        rtX.anchorMin = rtX.anchorMax = rtX.pivot = new Vector2(1f, 1f);
        rtX.sizeDelta = new Vector2(MarcaUdB.AreaTactil, MarcaUdB.AreaTactil);
        rtX.anchoredPosition = new Vector2(-MarcaUdB.Space2, -MarcaUdB.Space2);
        MarcaUdB.Redondear(imgCerrar, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(cerrar, new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.14f), new Color(1f, 1f, 1f, 0.22f));
        TextMeshProUGUI x = CrearTexto("X", rtX, "×", MarcaUdB.Texto, 28f, MarcaUdB.InkInverso);
        x.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)x.transform, 0f, 0f);
        cerrar.onClick.AddListener(Cerrar);
    }

    // Fila de la zona: igual al encabezado de un edificio en el menú (relleno suave + banda)
    void ConstruirFilaZona(RectTransform panel)
    {
        rellenoZona = CrearImagen("Zona", panel, MarcaUdB.Surface200);
        rellenoZona.raycastTarget = false;
        MarcaUdB.Redondear(rellenoZona, MarcaUdB.RadiusMd);
        LayoutElement le = rellenoZona.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = AltoZona;
        filaZona = rellenoZona.gameObject;
        RectTransform rt = (RectTransform)rellenoZona.transform;

        bandaZona = CrearImagen("Banda", rt, MarcaUdB.Negro);
        bandaZona.raycastTarget = false;
        RectTransform rb = (RectTransform)bandaZona.transform;
        rb.anchorMin = new Vector2(0f, 0f);
        rb.anchorMax = new Vector2(0f, 1f);
        rb.pivot = new Vector2(0f, 0.5f);
        rb.sizeDelta = new Vector2(6f, 0f);
        rb.anchoredPosition = Vector2.zero;
        MarcaUdB.Redondear(bandaZona, 3f);

        textoZona = CrearTexto("Texto", rt, "", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.Ink);
        textoZona.alignment = TextAlignmentOptions.MidlineLeft;
        textoZona.overflowMode = TextOverflowModes.Ellipsis;
        Estirar((RectTransform)textoZona.transform, MarcaUdB.Space5, MarcaUdB.Space4);
    }

    void ConstruirCuerpo(RectTransform panel)
    {
        Image imgLista = CrearImagen("Lista", panel, new Color(0f, 0f, 0f, 0f));
        LayoutElement le = imgLista.gameObject.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        imgLista.gameObject.AddComponent<RectMask2D>();
        scroll = imgLista.gameObject.AddComponent<ScrollRect>();

        GameObject goCont = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contenido = (RectTransform)goCont.transform;
        contenido.SetParent(imgLista.transform, false);
        contenido.anchorMin = new Vector2(0f, 1f);
        contenido.anchorMax = new Vector2(1f, 1f);
        contenido.pivot = new Vector2(0.5f, 1f);
        contenido.sizeDelta = new Vector2(-MarcaUdB.Space3, 0f);
        contenido.anchoredPosition = new Vector2(-MarcaUdB.Space3 / 2f, 0f);
        VerticalLayoutGroup vl = goCont.GetComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(0, 0, (int)MarcaUdB.Space1, (int)MarcaUdB.Space2);
        vl.spacing = MarcaUdB.Space4;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        goCont.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contenido;
        scroll.viewport = (RectTransform)imgLista.transform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = AltoControl + 4f; // un renglón largo por paso de rueda

        // Barra de desplazamiento como la del menú: toma el color de la zona
        Image imgBarra = CrearImagen("Barra", imgLista.transform, MarcaUdB.Surface300);
        RectTransform rtB = (RectTransform)imgBarra.transform;
        rtB.anchorMin = new Vector2(1f, 0f);
        rtB.anchorMax = new Vector2(1f, 1f);
        rtB.pivot = new Vector2(1f, 0.5f);
        rtB.sizeDelta = new Vector2(AnchoBarra, 0f);
        rtB.anchoredPosition = Vector2.zero;
        GameObject area = new GameObject("Area", typeof(RectTransform));
        area.transform.SetParent(rtB, false);
        Estirar((RectTransform)area.transform, 0f, 0f);
        Image manija = CrearImagen("Manija", area.transform, Color.white);
        Estirar((RectTransform)manija.transform, 0f, 0f);
        barra = imgBarra.gameObject.AddComponent<Scrollbar>();
        barra.handleRect = (RectTransform)manija.transform;
        barra.targetGraphic = manija;
        barra.direction = Scrollbar.Direction.BottomToTop;
        SinNavegacion(barra);
        MarcaUdB.EstilizarBarra(barra, MarcaUdB.Negro, Color.white, AnchoBarra);
        scroll.verticalScrollbar = barra;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    // Acción principal en rojo institucional, igual a «Inicio» en el menú
    void ConstruirPie(RectTransform panel)
    {
        Image img = CrearImagen("Enlace", panel, Color.white);
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        botonEnlace = img.gameObject.AddComponent<Button>();
        botonEnlace.targetGraphic = img;
        SinNavegacion(botonEnlace);
        MarcaUdB.ColoresBoton(botonEnlace, MarcaUdB.Rojo, MarcaUdB.RojoFuerte, MarcaUdB.RojoFuerte);
        LayoutElement le = img.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = AltoControl;

        textoEnlace = CrearTexto("Texto", img.transform, "Más información", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        Estirar((RectTransform)textoEnlace.transform, MarcaUdB.Space5, MarcaUdB.Space4);
        pie = img.gameObject;
        pie.SetActive(false);
    }

    // ------------------------------------------------------------------ utilidades de UI (las mismas del menú)

    static Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, TMP_FontAsset fuente, float tam, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        if (fuente != null) t.font = fuente;
        if (tam > 0f) t.fontSize = tam;
        t.color = color;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    static void Estirar(RectTransform rt, float izquierda, float derecha)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(izquierda, 0f);
        rt.offsetMax = new Vector2(-derecha, 0f);
    }

    static void SinNavegacion(Selectable s)
    {
        Navigation nav = s.navigation;
        nav.mode = Navigation.Mode.None;
        s.navigation = nav;
    }
}
