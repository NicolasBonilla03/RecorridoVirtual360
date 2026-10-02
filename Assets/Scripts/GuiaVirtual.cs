using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Guía virtual interactivo: un asistente con preguntas predefinidas sobre las dependencias y
/// servicios del campus. Un botón «Guía» abajo a la derecha abre el panel; al tocar una pregunta
/// se despliega la respuesta, con accesos a la ficha del lugar y a «Llévame» (salta a ese lugar).
///
/// Las preguntas están en Assets/Resources/PuntosInfo/catalogo.json («preguntas») y se editan
/// sin abrir Unity. Mismo estilo del menú lateral. MenuDesplegable lo añade solo.
/// </summary>
public class GuiaVirtual : MonoBehaviour
{
    static GuiaVirtual instancia;

    public static bool Abierta { get { return instancia != null && instancia.abierto; } }

    public static void CerrarSiAbierta()
    {
        if (instancia != null) instancia.Cerrar();
    }

    [Tooltip("Texto del botón flotante.")]
    public string textoBoton = "Guía";

    public string titulo = "¿En qué te ayudo?";
    public string subtitulo = "Toca una pregunta para ver la respuesta.";

    const int OrdenCanvas = 55;                      // sobre el menú (50), bajo la ficha (60)
    const float AnchoPanel = 400f;
    const float MargenPanel = MarcaUdB.Space4;
    const float MargenAncho = MarcaUdB.Space8;
    const float AltoControl = 48f;
    const float AnchoBoton = 112f;
    const float AnchoBarra = 8f;

    class Fila
    {
        public PreguntaGuia datos;
        public Color zona;
        public Button boton;
        public RectTransform flecha;
        public GameObject respuesta;
        public Button botonLlevame;
        public bool abierta;
    }

    RectTransform canvasRT;
    RectTransform zonaSegura;
    RectTransform panelRaiz;
    GameObject velo;
    GameObject botonGuia;
    GameObject sombraBoton;
    RectTransform botonRT;
    Vector2 desfaseSombra;
    ScrollRect scroll;
    RectTransform contenidoLista;
    float anchoLista;
    readonly List<Fila> filas = new List<Fila>();

    bool abierto;
    Coroutine animacion;
    Vector2 ultimoTamano;
    Rect ultimaAreaSegura;

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(this); return; }
        instancia = this;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Start()
    {
        if (CatalogoInfo.Preguntas.Length == 0) { enabled = false; return; }
        Construir();
        Acomodar();
    }

    void Update()
    {
        if (canvasRT == null) return;

        if (abierto && Input.GetKeyDown(KeyCode.Escape))
        {
            Cerrar();
            return;
        }

        if (canvasRT.rect.size != ultimoTamano || Screen.safeArea != ultimaAreaSegura)
            Acomodar();

        // El botón se esconde mientras otro menú está abierto o hay un cambio de lugar
        bool ocupado = abierto || MenuDesplegable.EstaAbierto || PanelInfo.Abierto || POIController.HayMenuAbierto
                       || (FadeController.Instance != null && FadeController.Instance.EnTransicion);
        if (botonGuia.activeSelf == ocupado)
        {
            botonGuia.SetActive(!ocupado);
            if (sombraBoton != null) sombraBoton.SetActive(!ocupado);
        }
    }

    // ------------------------------------------------------------------ abrir y cerrar

    public void Abrir()
    {
        if (abierto) return;
        POIController.CerrarAbierto();
        MenuDesplegable.CerrarSiAbierto();
        PanelInfo.CerrarSiAbierto();

        abierto = true;
        velo.SetActive(true);
        Acomodar();
        LlenarLista();
        ActualizarLlevame();
        Animar(-MargenPanel);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;
        velo.SetActive(false);
        Animar(panelRaiz.sizeDelta.x + 80f);
    }

    void Animar(float destinoX)
    {
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(Deslizar(destinoX));
    }

    IEnumerator Deslizar(float destinoX)
    {
        panelRaiz.gameObject.SetActive(true);
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

        if (!abierto) panelRaiz.gameObject.SetActive(false);
        animacion = null;
    }

    void Acomodar()
    {
        ultimoTamano = canvasRT.rect.size;
        ultimaAreaSegura = Screen.safeArea;
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        bool compacto = MarcaUdB.EsCompacto(canvasRT);
        float margen = compacto ? MarcaUdB.Space4 : MargenAncho;

        // En vertical, el botón sube para no tapar la nota legal ni la pista de uso
        botonRT.anchoredPosition = new Vector2(-margen, compacto ? margen + 72f : margen);
        if (sombraBoton != null)
            ((RectTransform)sombraBoton.transform).anchoredPosition = botonRT.anchoredPosition + desfaseSombra;

        float disponible = zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
        float ancho = Mathf.Min(AnchoPanel, disponible - MargenPanel * 2f);
        panelRaiz.sizeDelta = new Vector2(ancho, -MargenPanel * 2f);
        if (animacion == null)
            panelRaiz.anchoredPosition = new Vector2(abierto ? -MargenPanel : ancho + 80f, 0f);

        // Si cambió el ancho con la guía abierta, las filas se vuelven a medir
        if (abierto && contenidoLista != null) { LlenarLista(); ActualizarLlevame(); }
    }

    // ------------------------------------------------------------------ preguntas

    void Alternar(Fila f)
    {
        bool abrir = !f.abierta;
        foreach (Fila otra in filas) Poner(otra, false);
        Poner(f, abrir);
        if (abrir) RegistroUso.Anotar("guia", f.datos.pregunta);
    }

    void Poner(Fila f, bool abierta)
    {
        f.abierta = abierta;
        f.respuesta.SetActive(abierta);
        f.flecha.localEulerAngles = new Vector3(0f, 0f, abierta ? -90f : 0f);
    }

    // «Llévame» no aparece si ya estás en ese lugar
    void ActualizarLlevame()
    {
        Material actual = RenderSettings.skybox;
        foreach (Fila f in filas)
        {
            if (f.botonLlevame == null) continue;
            Material destino = BuscarSkybox(f.datos.skybox);
            f.botonLlevame.gameObject.SetActive(destino != null && destino != actual);
        }
    }

    static Material BuscarSkybox(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        GestorTeleports g = GestorTeleports.Instance;
        if (g == null) return null;
        if (g.skyboxInicial != null && g.skyboxInicial.name == nombre) return g.skyboxInicial;
        if (g.configuraciones == null) return null;
        foreach (ConfiguracionSkybox c in g.configuraciones)
            if (c != null && c.skybox != null && c.skybox.name == nombre) return c.skybox;
        return null;
    }

    void Llevar(PreguntaGuia p)
    {
        Material destino = BuscarSkybox(p.skybox);
        if (destino == null || FadeController.Instance == null) return;
        RegistroUso.Anotar("guia-llevame", p.pregunta);
        Cerrar();
        FadeController.Instance.CambiarSkybox(destino);
    }

    void VerFicha(PreguntaGuia p)
    {
        FichaInfo f = CatalogoInfo.PorId(p.ficha);
        if (f != null) PanelInfo.Mostrar(f); // la ficha cierra la guía
    }

    // ------------------------------------------------------------------ construcción

    void Construir()
    {
        if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject lienzo = new GameObject("GuiaVirtual_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        Image imgVelo = CrearImagen("VeloEscena", canvasRT, MarcaUdB.VeloEscena);
        Estirar((RectTransform)imgVelo.transform, 0f, 0f);
        Button botonVelo = imgVelo.gameObject.AddComponent<Button>();
        botonVelo.transition = Selectable.Transition.None;
        SinNavegacion(botonVelo);
        botonVelo.onClick.AddListener(Cerrar);
        velo = imgVelo.gameObject;
        velo.SetActive(false);

        GameObject goSegura = new GameObject("AreaSegura", typeof(RectTransform));
        zonaSegura = (RectTransform)goSegura.transform;
        zonaSegura.SetParent(canvasRT, false);
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        ConstruirBoton();
        ConstruirPanel();
    }

    // Botón flotante en negro institucional, como el de «Menú»
    void ConstruirBoton()
    {
        Image img;
        Button boton = CrearBoton("BotonGuia", zonaSegura, out img);
        botonRT = (RectTransform)boton.transform;
        botonRT.anchorMin = botonRT.anchorMax = botonRT.pivot = new Vector2(1f, 0f);
        botonRT.sizeDelta = new Vector2(AnchoBoton, AltoControl);
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(boton, MarcaUdB.Negro, MarcaUdB.Hex("#3a3a36"), MarcaUdB.Hex("#4a4a45"));

        // Círculo blanco con «?»
        Image circulo = CrearImagen("Icono", botonRT, MarcaUdB.InkInverso);
        circulo.raycastTarget = false;
        RectTransform rc = (RectTransform)circulo.transform;
        rc.anchorMin = rc.anchorMax = new Vector2(0f, 0.5f);
        rc.pivot = new Vector2(0f, 0.5f);
        rc.sizeDelta = new Vector2(22f, 22f);
        rc.anchoredPosition = new Vector2(MarcaUdB.Space4, 0f);
        MarcaUdB.Redondear(circulo, 11f);
        TextMeshProUGUI signo = CrearTexto("Signo", rc, "?", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.Negro);
        signo.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)signo.transform, 0f, 0f);

        TextMeshProUGUI texto = CrearTexto("Texto", botonRT, textoBoton, MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        Estirar((RectTransform)texto.transform, MarcaUdB.Space4 + 22f + MarcaUdB.Space2, MarcaUdB.Space2);

        boton.onClick.AddListener(Abrir);
        botonGuia = boton.gameObject;

        Image sombra = MarcaUdB.SombraFlotante(botonRT);
        if (sombra != null)
        {
            sombraBoton = sombra.gameObject;
            desfaseSombra = ((RectTransform)sombra.transform).anchoredPosition - botonRT.anchoredPosition;
        }
    }

    void ConstruirPanel()
    {
        GameObject goRaiz = new GameObject("PanelRaiz", typeof(RectTransform));
        panelRaiz = (RectTransform)goRaiz.transform;
        panelRaiz.SetParent(zonaSegura, false);
        panelRaiz.anchorMin = new Vector2(1f, 0f);
        panelRaiz.anchorMax = new Vector2(1f, 1f);
        panelRaiz.pivot = new Vector2(1f, 0.5f);
        panelRaiz.sizeDelta = new Vector2(AnchoPanel, -MargenPanel * 2f);
        panelRaiz.anchoredPosition = new Vector2(AnchoPanel + 80f, 0f);

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
        ConstruirLista(panel);

        panelRaiz.gameObject.SetActive(false);
    }

    // Encabezado negro con filete rojo, igual al del menú
    void ConstruirEncabezado(RectTransform panel)
    {
        Image fondo = CrearImagen("Encabezado", panel, MarcaUdB.Negro);
        fondo.raycastTarget = false;
        MarcaUdB.Redondear(fondo, MarcaUdB.RadiusMd);
        LayoutElement le = fondo.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 112f;
        RectTransform rtE = (RectTransform)fondo.transform;

        float p = MarcaUdB.Space4;

        Image filete = CrearImagen("FileteRojo", rtE, MarcaUdB.Rojo);
        filete.raycastTarget = false;
        RectTransform rf = (RectTransform)filete.transform;
        rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0f, 1f);
        rf.sizeDelta = new Vector2(32f, 4f);
        rf.anchoredPosition = new Vector2(p, -p);
        MarcaUdB.Redondear(filete, 2f);

        TextMeshProUGUI rotulo = CrearTexto("Rotulo", rtE, "Guía virtual", null, 0f, Color.white);
        MarcaUdB.EstiloEtiqueta(rotulo, new Color(1f, 1f, 1f, 0.72f));
        Colocar((RectTransform)rotulo.transform, p + 10f, 16f, p, 56f);

        TextMeshProUGUI tit = CrearTexto("Titulo", rtE, titulo, MarcaUdB.Display, MarcaUdB.DisplayM, MarcaUdB.InkInverso);
        Colocar((RectTransform)tit.transform, p + 30f, 28f, p, 56f);

        TextMeshProUGUI sub = CrearTexto("Subtitulo", rtE, subtitulo, MarcaUdB.Texto, MarcaUdB.CuerpoS, new Color(1f, 1f, 1f, 0.80f));
        Colocar((RectTransform)sub.transform, p + 62f, 20f, p, p);

        Image imgCerrar;
        Button cerrar = CrearBoton("Cerrar", rtE, out imgCerrar);
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

    void ConstruirLista(RectTransform panel)
    {
        Image imgLista = CrearImagen("Lista", panel, new Color(0f, 0f, 0f, 0f));
        LayoutElement leL = imgLista.gameObject.AddComponent<LayoutElement>();
        leL.flexibleHeight = 1f;
        imgLista.gameObject.AddComponent<RectMask2D>();
        scroll = imgLista.gameObject.AddComponent<ScrollRect>();

        GameObject goCont = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contenido = (RectTransform)goCont.transform;
        contenido.SetParent(imgLista.transform, false);
        contenido.anchorMin = new Vector2(0f, 1f);
        contenido.anchorMax = new Vector2(1f, 1f);
        contenido.pivot = new Vector2(0.5f, 1f);
        contenido.sizeDelta = new Vector2(-MarcaUdB.Space3, 0f);
        contenido.anchoredPosition = new Vector2(-MarcaUdB.Space3 / 2f, 0f);
        VerticalLayoutGroup vl = goCont.GetComponent<VerticalLayoutGroup>();
        vl.spacing = MarcaUdB.Space2;
        vl.padding = new RectOffset(0, 0, 0, (int)MarcaUdB.Space2);
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
        scroll.scrollSensitivity = AltoControl + 4f;

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
        Scrollbar barra = imgBarra.gameObject.AddComponent<Scrollbar>();
        barra.handleRect = (RectTransform)manija.transform;
        barra.targetGraphic = manija;
        barra.direction = Scrollbar.Direction.BottomToTop;
        SinNavegacion(barra);
        MarcaUdB.EstilizarBarra(barra, MarcaUdB.Negro, Color.white, AnchoBarra);
        scroll.verticalScrollbar = barra;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        contenidoLista = contenido;
    }

    float AnchoLista()
    {
        // ancho del panel − relleno del panel a cada lado − el carril de la barra de desplazamiento
        return Mathf.Max(120f, panelRaiz.sizeDelta.x - MarcaUdB.Space4 * 2f - MarcaUdB.Space3);
    }

    // Las filas se arman con el ancho real del panel: cada una mide lo que mide su texto
    void LlenarLista()
    {
        float ancho = AnchoLista();
        if (filas.Count > 0 && Mathf.Abs(ancho - anchoLista) < 0.5f) return;
        anchoLista = ancho;

        for (int i = contenidoLista.childCount - 1; i >= 0; i--)
        {
            GameObject viejo = contenidoLista.GetChild(i).gameObject;
            viejo.SetActive(false);
            Destroy(viejo);
        }
        filas.Clear();

        string categoria = null;
        foreach (PreguntaGuia p in CatalogoInfo.Preguntas)
        {
            if (p == null || string.IsNullOrEmpty(p.pregunta)) continue;
            if (p.categoria != categoria && !string.IsNullOrEmpty(p.categoria))
            {
                categoria = p.categoria;
                TextMeshProUGUI rot = CrearTexto("Categoria", contenidoLista, categoria, null, 0f, Color.white);
                MarcaUdB.EstiloEtiqueta(rot, MarcaUdB.InkMuted);
                rot.margin = new Vector4(MarcaUdB.Space1, 0f, 0f, 0f);
                rot.alignment = TextAlignmentOptions.BottomLeft;
                LayoutElement lr = rot.gameObject.AddComponent<LayoutElement>();
                lr.minHeight = lr.preferredHeight = filas.Count == 0 ? 20f : 30f;
            }
            CrearFila(contenidoLista, p, ancho);
        }
    }

    void CrearFila(RectTransform padre, PreguntaGuia p, float ancho)
    {
        Fila f = new Fila();
        f.datos = p;
        FichaInfo ficha = CatalogoInfo.PorId(p.ficha);
        f.zona = ficha != null && !string.IsNullOrEmpty(ficha.zona) ? ficha.ColorZona : MarcaUdB.Negro;
        Color tinta = MarcaUdB.TintaLegible(f.zona, MarcaUdB.Tenue(f.zona, 0.32f));

        // Pregunta: relleno suave de la zona + banda, como el encabezado de un edificio en el menú
        Image img;
        f.boton = CrearBoton("Pregunta", padre, out img);
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(f.boton, MarcaUdB.Tenue(f.zona, 0.14f), MarcaUdB.Tenue(f.zona, 0.24f), MarcaUdB.Tenue(f.zona, 0.32f));

        Image banda = CrearImagen("Banda", f.boton.transform, f.zona);
        banda.raycastTarget = false;
        RectTransform rb = (RectTransform)banda.transform;
        rb.anchorMin = new Vector2(0f, 0f);
        rb.anchorMax = new Vector2(0f, 1f);
        rb.pivot = new Vector2(0f, 0.5f);
        rb.sizeDelta = new Vector2(6f, 0f);
        rb.anchoredPosition = Vector2.zero;
        MarcaUdB.Redondear(banda, 3f);

        TextMeshProUGUI texto = CrearTexto("Texto", f.boton.transform, p.pregunta, MarcaUdB.TextoBold, MarcaUdB.UIControl, tinta);
        texto.enableWordWrapping = true;
        texto.overflowMode = TextOverflowModes.Overflow;
        texto.alignment = TextAlignmentOptions.TopLeft;
        float anchoPregunta = ancho - MarcaUdB.Space5 - MarcaUdB.Space8;
        float altoPregunta = Medir(texto, anchoPregunta);
        float altoFila = Mathf.Max(AltoControl, altoPregunta + MarcaUdB.Space3 * 2f);
        Ubicar((RectTransform)texto.transform, MarcaUdB.Space5, (altoFila - altoPregunta) * 0.5f, anchoPregunta, altoPregunta);
        LayoutElement le = f.boton.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = altoFila;

        TextMeshProUGUI flecha = CrearTexto("Flecha", f.boton.transform, ">", MarcaUdB.TextoBold, MarcaUdB.UIControl, tinta);
        flecha.alignment = TextAlignmentOptions.Center;
        RectTransform rtF = (RectTransform)flecha.transform;
        rtF.anchorMin = rtF.anchorMax = new Vector2(1f, 0.5f);
        rtF.pivot = new Vector2(0.5f, 0.5f);
        rtF.sizeDelta = new Vector2(24f, 24f);
        rtF.anchoredPosition = new Vector2(-MarcaUdB.Space5, 0f);
        f.flecha = rtF;

        // Respuesta: texto y acciones, con el alto medido
        f.respuesta = new GameObject("Respuesta", typeof(RectTransform));
        RectTransform rr = (RectTransform)f.respuesta.transform;
        rr.SetParent(padre, false);

        float x = MarcaUdB.Space3;
        float w = ancho - x * 2f;
        float y = MarcaUdB.Space1;

        TextMeshProUGUI resp = CrearTexto("Texto", rr, p.respuesta, MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.Ink);
        resp.enableWordWrapping = true;
        resp.overflowMode = TextOverflowModes.Overflow;
        resp.alignment = TextAlignmentOptions.TopLeft;
        resp.lineSpacing = 10f;
        float altoResp = Medir(resp, w);
        Ubicar((RectTransform)resp.transform, x, y, w, altoResp);
        y += altoResp;

        // Acciones: «Llévame» (principal, rojo) y «Ver ficha»
        bool conFicha = ficha != null;
        bool conLugar = !string.IsNullOrEmpty(p.skybox);
        if (conFicha || conLugar)
        {
            y += MarcaUdB.Space3;
            float separacion = MarcaUdB.Space2;
            float anchoBoton = conFicha && conLugar ? (w - separacion) * 0.5f : w;
            float xb = x;

            if (conLugar)
            {
                f.botonLlevame = BotonAccion(rr, "Llévame", true);
                f.botonLlevame.onClick.AddListener(() => Llevar(p));
                Ubicar((RectTransform)f.botonLlevame.transform, xb, y, anchoBoton, MarcaUdB.AreaTactil);
                xb += anchoBoton + separacion;
            }
            if (conFicha)
            {
                Button ver = BotonAccion(rr, "Ver ficha", false);
                ver.onClick.AddListener(() => VerFicha(p));
                Ubicar((RectTransform)ver.transform, xb, y, anchoBoton, MarcaUdB.AreaTactil);
            }
            y += MarcaUdB.AreaTactil;
        }
        y += MarcaUdB.Space3;

        LayoutElement lr = f.respuesta.AddComponent<LayoutElement>();
        lr.minHeight = lr.preferredHeight = Mathf.Ceil(y);

        f.boton.onClick.AddListener(() => Alternar(f));
        Poner(f, false);
        filas.Add(f);
    }

    static float Medir(TextMeshProUGUI t, float ancho)
    {
        return Mathf.Ceil(t.GetPreferredValues(t.text, ancho, 0f).y);
    }

    // Posición y tamaño fijos, medidos desde la esquina superior izquierda del padre
    static void Ubicar(RectTransform rt, float x, float y, float ancho, float alto)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(ancho, alto);
    }

    Button BotonAccion(Transform padre, string texto, bool principal)
    {
        Image img;
        Button b = CrearBoton(texto, padre, out img);
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        if (principal) MarcaUdB.ColoresBoton(b, MarcaUdB.Rojo, MarcaUdB.RojoFuerte, MarcaUdB.RojoFuerte);
        else MarcaUdB.ColoresBoton(b, MarcaUdB.Negro, MarcaUdB.Hex("#3a3a36"), MarcaUdB.Hex("#4a4a45"));
        TextMeshProUGUI t = CrearTexto("Texto", b.transform, texto, MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        t.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)t.transform, MarcaUdB.Space2, MarcaUdB.Space2);
        return b;
    }

    // ------------------------------------------------------------------ utilidades de UI (las mismas del menú)

    static Button CrearBoton(string nombre, Transform padre, out Image img)
    {
        img = CrearImagen(nombre, padre, Color.white);
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        SinNavegacion(btn);
        return btn;
    }

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

    static void Colocar(RectTransform rt, float desdeArriba, float alto, float izquierda, float derecha)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(izquierda, -desdeArriba - alto);
        rt.offsetMax = new Vector2(-derecha, -desdeArriba);
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
