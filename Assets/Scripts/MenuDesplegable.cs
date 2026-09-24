using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menú lateral desplegable para navegar el recorrido.
/// Construye su propia interfaz al iniciar (no necesita prefabs) y toma los
/// edificios y puntos de la lista "Edificios" del componente MenuNavegacion.
/// Abrir / cerrar: botón "Menú" arriba a la izquierda, tecla M, Esc o clic fuera del panel.
/// </summary>
public class MenuDesplegable : MonoBehaviour
{
    [Header("Datos")]
    [Tooltip("De aquí salen los edificios y sus puntos. Si está vacío se busca en la escena.")]
    public MenuNavegacion origenDatos;

    [Tooltip("Objetos del menú anterior que se ocultan al iniciar.")]
    public List<GameObject> ocultarAlIniciar = new List<GameObject>();

    [Header("Comportamiento")]
    [Tooltip("Al abrir un edificio se cierran los demás.")]
    public bool soloUnEdificioAbierto = true;

    [Tooltip("Al abrir el menú se despliega el edificio donde está el usuario.")]
    public bool abrirEdificioActual = true;

    [Tooltip("Los POI solo se muestran en el skybox de inicio (vista general).")]
    public bool poisSoloEnInicio = true;

    public KeyCode teclaMenu = KeyCode.M;

    [Header("Textos")]
    public string tituloMenu = "Recorrido virtual 360°";
    public string textoInicio = "Inicio · Vista general";

    [Header("Apariencia")]
    public float anchoPanel = 480f;
    public float altoEdificio = 64f;
    public float altoPunto = 56f;
    public float tamTextoTitulo = 30f;
    public float tamTextoEdificio = 26f;
    public float tamTextoPunto = 24f;
    public int ordenCanvas = 50;
    public Color colorFondo = new Color(0.07f, 0.08f, 0.10f, 0.94f);
    public Color colorEdificio = new Color(0.15f, 0.17f, 0.21f, 1f);
    public Color colorPunto = new Color(0.10f, 0.11f, 0.14f, 1f);
    public Color colorResaltado = new Color(0.98f, 0.74f, 0.20f, 1f);
    public Color colorTexto = new Color(0.95f, 0.96f, 0.98f, 1f);
    public Color colorTextoSecundario = new Color(0.64f, 0.69f, 0.77f, 1f);

    class FilaPunto
    {
        public PuntoRecorrido punto;
        public Image acento;
        public TextMeshProUGUI texto;
    }

    class Seccion
    {
        public EdificioRecorrido edificio;
        public TextMeshProUGUI textoEncabezado;
        public RectTransform chevron;
        public GameObject contenedor;
        public bool abierta;
        public List<FilaPunto> filas = new List<FilaPunto>();
    }

    readonly List<Seccion> secciones = new List<Seccion>();
    RectTransform canvasRT;
    RectTransform panel;
    GameObject fondoOscuro;
    GameObject botonMenu;
    GameObject chipUbicacion;
    TextMeshProUGUI textoChip;
    TextMeshProUGUI textoUbicacionPanel;
    TextMeshProUGUI textoFilaInicio;
    Image acentoInicio;
    GameObject[] pois = new GameObject[0];
    Material skyboxInicio;
    Material ultimoSkybox;
    bool abierto;
    Coroutine animacion;

    // ------------------------------------------------------------------ ciclo de vida

    void Start()
    {
        if (origenDatos == null)
            origenDatos = FindObjectOfType<MenuNavegacion>();

        foreach (GameObject go in ocultarAlIniciar)
            if (go != null) go.SetActive(false);

        skyboxInicio = (GestorTeleports.Instance != null && GestorTeleports.Instance.skyboxInicial != null)
            ? GestorTeleports.Instance.skyboxInicial
            : RenderSettings.skybox;

        try { pois = GameObject.FindGameObjectsWithTag("POI"); }
        catch (UnityException) { pois = new GameObject[0]; }

        AsegurarEventSystem();
        ConstruirInterfaz();
        ultimoSkybox = null; // fuerza la actualización en el primer Update
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaMenu))
            Alternar();
        else if (abierto && Input.GetKeyDown(KeyCode.Escape))
            Cerrar();

        // Se detecta el cambio de skybox venga de donde venga (menú, botón TP o POI)
        if (RenderSettings.skybox != ultimoSkybox)
        {
            ultimoSkybox = RenderSettings.skybox;
            ActualizarUbicacion();

            if (poisSoloEnInicio)
            {
                bool enInicio = ultimoSkybox != null && ultimoSkybox == skyboxInicio;
                foreach (GameObject poi in pois)
                    if (poi != null) poi.SetActive(enInicio);
            }
        }
    }

    // ------------------------------------------------------------------ API pública

    public void Abrir()
    {
        if (abierto || panel == null) return;
        abierto = true;

        float ancho = Mathf.Min(anchoPanel, canvasRT.rect.width - 32f);
        panel.sizeDelta = new Vector2(ancho, 0f);

        if (abrirEdificioActual)
        {
            foreach (Seccion s in secciones)
            {
                if (ContieneSkybox(s, RenderSettings.skybox)) PonerSeccion(s, true);
                else if (soloUnEdificioAbierto) PonerSeccion(s, false);
            }
        }

        fondoOscuro.SetActive(true);
        botonMenu.SetActive(false);
        chipUbicacion.SetActive(false);
        Animar(0f);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;

        fondoOscuro.SetActive(false);
        botonMenu.SetActive(true);
        chipUbicacion.SetActive(true);
        Animar(-panel.sizeDelta.x - 40f);
    }

    public void Alternar()
    {
        if (abierto) Cerrar();
        else Abrir();
    }

    public void IrAlInicio()
    {
        IrA(skyboxInicio);
    }

    // ------------------------------------------------------------------ navegación

    void IrA(Material sky)
    {
        if (sky == null)
        {
            Debug.LogWarning("[MenuDesplegable] Este punto no tiene skybox asignado.");
            return;
        }

        Cerrar();
        if (sky == RenderSettings.skybox) return;

        if (FadeController.Instance != null)
        {
            FadeController.Instance.CambiarSkybox(sky);
        }
        else
        {
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
            if (GestorTeleports.Instance != null)
                GestorTeleports.Instance.ActivarTeleports(sky);
        }
    }

    void AlternarSeccion(Seccion s)
    {
        bool abrir = !s.abierta;
        if (abrir && soloUnEdificioAbierto)
            foreach (Seccion otra in secciones)
                if (otra != s) PonerSeccion(otra, false);
        PonerSeccion(s, abrir);
    }

    void PonerSeccion(Seccion s, bool abrir)
    {
        s.abierta = abrir;
        s.contenedor.SetActive(abrir);
        s.chevron.localEulerAngles = new Vector3(0f, 0f, abrir ? -90f : 0f);
    }

    static bool ContieneSkybox(Seccion s, Material sky)
    {
        if (sky == null) return false;
        foreach (FilaPunto f in s.filas)
            if (f.punto.skybox == sky) return true;
        return false;
    }

    void ActualizarUbicacion()
    {
        Material sky = RenderSettings.skybox;
        bool enInicio = sky != null && sky == skyboxInicio;
        string lugar = null;

        foreach (Seccion s in secciones)
        {
            bool contiene = false;
            foreach (FilaPunto f in s.filas)
            {
                bool actual = sky != null && f.punto.skybox == sky;
                f.acento.gameObject.SetActive(actual);
                f.texto.color = actual ? colorResaltado : colorTexto;
                f.texto.fontStyle = actual ? FontStyles.Bold : FontStyles.Normal;
                if (actual)
                {
                    contiene = true;
                    if (lugar == null) lugar = s.edificio.nombreEdificio + " · " + f.punto.nombre;
                }
            }
            s.textoEncabezado.color = contiene ? colorResaltado : colorTexto;
        }

        acentoInicio.gameObject.SetActive(enInicio);
        textoFilaInicio.color = enInicio ? colorResaltado : colorTexto;

        if (enInicio) lugar = "Inicio";
        if (lugar == null) lugar = NombreDesdeGestor(sky);

        textoChip.text = "Estás en: <b>" + lugar + "</b>";
        textoUbicacionPanel.text = "Estás en: " + lugar;
    }

    static string NombreDesdeGestor(Material sky)
    {
        if (sky == null) return "—";
        GestorTeleports g = GestorTeleports.Instance;
        if (g != null && g.configuraciones != null)
            foreach (ConfiguracionSkybox c in g.configuraciones)
                if (c != null && c.skybox == sky && !string.IsNullOrEmpty(c.nombreSkybox))
                    return c.nombreSkybox.Trim();
        return sky.name.Replace("Skybox_", "");
    }

    // ------------------------------------------------------------------ construcción de la interfaz

    void ConstruirInterfaz()
    {
        // Canvas propio, siempre por encima del resto de la interfaz
        GameObject goCanvas = new GameObject("MenuDesplegable_UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        goCanvas.transform.SetParent(transform, false);
        Canvas canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvas;
        CanvasScaler escalador = goCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        canvasRT = (RectTransform)goCanvas.transform;

        ConstruirBotonMenu();
        ConstruirChipUbicacion();

        // Fondo oscuro: bloquea la cámara mientras el menú está abierto y lo cierra al tocarlo
        Image fondo = CrearImagen("FondoOscuro", canvasRT, new Color(0f, 0f, 0f, 0.45f));
        Estirar((RectTransform)fondo.transform, 0f, 0f);
        Button botonFondo = fondo.gameObject.AddComponent<Button>();
        botonFondo.transition = Selectable.Transition.None;
        SinNavegacion(botonFondo);
        botonFondo.onClick.AddListener(Cerrar);
        fondoOscuro = fondo.gameObject;
        fondoOscuro.SetActive(false);

        ConstruirPanel();
    }

    void ConstruirBotonMenu()
    {
        Image img;
        Button boton = CrearBoton("BotonMenu", canvasRT, colorFondo, out img);
        RectTransform rt = (RectTransform)boton.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -24f);
        rt.sizeDelta = new Vector2(180f, 64f);

        // Ícono de hamburguesa (tres barras)
        for (int i = 0; i < 3; i++)
        {
            Image barra = CrearImagen("Barra" + i, rt, colorTexto);
            barra.raycastTarget = false;
            RectTransform rb = (RectTransform)barra.transform;
            rb.anchorMin = rb.anchorMax = new Vector2(0f, 0.5f);
            rb.pivot = new Vector2(0f, 0.5f);
            rb.sizeDelta = new Vector2(28f, 4f);
            rb.anchoredPosition = new Vector2(22f, 10f - i * 10f);
        }

        TextMeshProUGUI texto = CrearTexto("Texto", rt, "Menú", tamTextoEdificio, colorTexto, FontStyles.Bold);
        Estirar((RectTransform)texto.transform, 64f, 12f);

        boton.onClick.AddListener(Alternar);
        botonMenu = boton.gameObject;
    }

    void ConstruirChipUbicacion()
    {
        Color c = colorFondo;
        c.a = 0.8f;
        Image chip = CrearImagen("ChipUbicacion", canvasRT, c);
        chip.raycastTarget = false;
        RectTransform rt = (RectTransform)chip.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(216f, -24f);
        rt.sizeDelta = new Vector2(0f, 64f);

        HorizontalLayoutGroup hl = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(20, 20, 0, 0);
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
        ContentSizeFitter ajuste = chip.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        textoChip = CrearTexto("Texto", rt, "", tamTextoPunto, colorTexto, FontStyles.Normal);
        chipUbicacion = chip.gameObject;
    }

    void ConstruirPanel()
    {
        Image imgPanel = CrearImagen("Panel", canvasRT, colorFondo);
        panel = (RectTransform)imgPanel.transform;
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.sizeDelta = new Vector2(anchoPanel, 0f);
        panel.anchoredPosition = new Vector2(-anchoPanel - 40f, 0f);

        VerticalLayoutGroup vl = imgPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(24, 24, 28, 24);
        vl.spacing = 14f;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // --- Encabezado: título, ubicación actual y botón cerrar
        GameObject encabezado = new GameObject("Encabezado", typeof(RectTransform));
        encabezado.transform.SetParent(panel, false);
        LayoutElement leE = encabezado.AddComponent<LayoutElement>();
        leE.minHeight = 96f;
        leE.preferredHeight = 96f;
        RectTransform rtE = (RectTransform)encabezado.transform;

        TextMeshProUGUI titulo = CrearTexto("Titulo", rtE, tituloMenu, tamTextoTitulo, colorTexto, FontStyles.Bold);
        RectTransform rtT = (RectTransform)titulo.transform;
        rtT.anchorMin = new Vector2(0f, 1f);
        rtT.anchorMax = new Vector2(1f, 1f);
        rtT.pivot = new Vector2(0.5f, 1f);
        rtT.sizeDelta = new Vector2(-64f, 46f);
        rtT.anchoredPosition = new Vector2(-32f, 0f);

        textoUbicacionPanel = CrearTexto("Ubicacion", rtE, "", tamTextoPunto - 2f, colorTextoSecundario, FontStyles.Normal);
        RectTransform rtU = (RectTransform)textoUbicacionPanel.transform;
        rtU.anchorMin = new Vector2(0f, 0f);
        rtU.anchorMax = new Vector2(1f, 0f);
        rtU.pivot = new Vector2(0.5f, 0f);
        rtU.sizeDelta = new Vector2(-64f, 40f);
        rtU.anchoredPosition = new Vector2(-32f, 6f);

        Image imgCerrar;
        Button cerrar = CrearBoton("Cerrar", rtE, colorEdificio, out imgCerrar);
        RectTransform rtX = (RectTransform)cerrar.transform;
        rtX.anchorMin = rtX.anchorMax = rtX.pivot = new Vector2(1f, 1f);
        rtX.sizeDelta = new Vector2(56f, 56f);
        rtX.anchoredPosition = Vector2.zero;
        TextMeshProUGUI x = CrearTexto("X", rtX, "×", 40f, colorTexto, FontStyles.Normal);
        x.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)x.transform, 0f, 0f);
        cerrar.onClick.AddListener(Cerrar);

        // --- Botón de inicio
        Button inicio = CrearFila(panel, textoInicio, altoEdificio, colorEdificio, tamTextoEdificio,
            FontStyles.Bold, 24f, out textoFilaInicio, out acentoInicio);
        inicio.onClick.AddListener(IrAlInicio);

        // --- Lista con scroll
        Image imgLista = CrearImagen("Lista", panel, new Color(0f, 0f, 0f, 0f));
        LayoutElement leL = imgLista.gameObject.AddComponent<LayoutElement>();
        leL.flexibleHeight = 1f;
        imgLista.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = imgLista.gameObject.AddComponent<ScrollRect>();

        GameObject contenido = new GameObject("Contenido", typeof(RectTransform));
        RectTransform rtC = (RectTransform)contenido.transform;
        rtC.SetParent(imgLista.transform, false);
        rtC.anchorMin = new Vector2(0f, 1f);
        rtC.anchorMax = new Vector2(1f, 1f);
        rtC.pivot = new Vector2(0.5f, 1f);
        rtC.sizeDelta = new Vector2(-16f, 0f);
        rtC.anchoredPosition = new Vector2(-8f, 0f);
        VerticalLayoutGroup vlc = contenido.AddComponent<VerticalLayoutGroup>();
        vlc.spacing = 6f;
        vlc.childControlWidth = true;
        vlc.childControlHeight = true;
        vlc.childForceExpandWidth = true;
        vlc.childForceExpandHeight = false;
        ContentSizeFitter ajuste = contenido.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = rtC;
        scroll.viewport = (RectTransform)imgLista.transform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        // Barra de desplazamiento delgada
        Image barra = CrearImagen("Barra", imgLista.transform, new Color(1f, 1f, 1f, 0.06f));
        RectTransform rtB = (RectTransform)barra.transform;
        rtB.anchorMin = new Vector2(1f, 0f);
        rtB.anchorMax = new Vector2(1f, 1f);
        rtB.pivot = new Vector2(1f, 0.5f);
        rtB.sizeDelta = new Vector2(8f, 0f);
        rtB.anchoredPosition = Vector2.zero;
        GameObject area = new GameObject("Area", typeof(RectTransform));
        area.transform.SetParent(rtB, false);
        Estirar((RectTransform)area.transform, 0f, 0f);
        Image manija = CrearImagen("Manija", area.transform, new Color(1f, 1f, 1f, 0.35f));
        Estirar((RectTransform)manija.transform, 0f, 0f);
        Scrollbar sb = barra.gameObject.AddComponent<Scrollbar>();
        sb.handleRect = (RectTransform)manija.transform;
        sb.targetGraphic = manija;
        sb.direction = Scrollbar.Direction.BottomToTop;
        SinNavegacion(sb);
        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // --- Edificios
        if (origenDatos != null && origenDatos.edificios != null)
        {
            foreach (EdificioRecorrido ed in origenDatos.edificios)
                if (ed != null) CrearSeccion(rtC, ed);
        }
        else
        {
            Debug.LogWarning("[MenuDesplegable] No se encontró MenuNavegacion: el menú solo tendrá el botón de inicio.");
        }

        panel.gameObject.SetActive(false);
    }

    void CrearSeccion(RectTransform padre, EdificioRecorrido ed)
    {
        Seccion s = new Seccion();
        s.edificio = ed;

        int n = ed.puntos != null ? ed.puntos.Count : 0;
        string conteo = "  <size=75%><color=#" + ColorUtility.ToHtmlStringRGB(colorTextoSecundario) + ">"
                        + n + (n == 1 ? " lugar" : " lugares") + "</color></size>";

        TextMeshProUGUI textoEnc;
        Image acentoEnc;
        Button encabezado = CrearFila(padre, ed.nombreEdificio + conteo, altoEdificio, colorEdificio,
            tamTextoEdificio, FontStyles.Bold, 20f, out textoEnc, out acentoEnc);
        s.textoEncabezado = textoEnc;

        TextMeshProUGUI chevron = CrearTexto("Flecha", encabezado.transform, ">", tamTextoEdificio, colorTextoSecundario, FontStyles.Bold);
        chevron.alignment = TextAlignmentOptions.Center;
        RectTransform rtF = (RectTransform)chevron.transform;
        rtF.anchorMin = rtF.anchorMax = new Vector2(1f, 0.5f);
        rtF.pivot = new Vector2(0.5f, 0.5f);
        rtF.sizeDelta = new Vector2(40f, 40f);
        rtF.anchoredPosition = new Vector2(-28f, 0f);
        s.chevron = rtF;

        s.contenedor = new GameObject("Puntos_" + ed.nombreEdificio, typeof(RectTransform));
        s.contenedor.transform.SetParent(padre, false);
        VerticalLayoutGroup vl = s.contenedor.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4f;
        vl.padding = new RectOffset(0, 0, 2, 10);
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        if (ed.puntos != null)
        {
            foreach (PuntoRecorrido p in ed.puntos)
            {
                if (p == null) continue;
                FilaPunto fila = new FilaPunto();
                fila.punto = p;
                Button b = CrearFila(s.contenedor.transform, p.nombre, altoPunto, colorPunto, tamTextoPunto,
                    FontStyles.Normal, 44f, out fila.texto, out fila.acento);
                Material destino = p.skybox;
                b.onClick.AddListener(() => IrA(destino));
                s.filas.Add(fila);
            }
        }

        encabezado.onClick.AddListener(() => AlternarSeccion(s));
        PonerSeccion(s, false);
        secciones.Add(s);
    }

    // ------------------------------------------------------------------ utilidades de UI

    Button CrearFila(Transform padre, string texto, float alto, Color fondo, float tam, FontStyles estilo,
                     float sangria, out TextMeshProUGUI tmp, out Image acento)
    {
        Image img;
        Button btn = CrearBoton("Fila", padre, fondo, out img);
        LayoutElement le = btn.gameObject.AddComponent<LayoutElement>();
        le.minHeight = alto;
        le.preferredHeight = alto;

        acento = CrearImagen("Acento", btn.transform, colorResaltado);
        acento.raycastTarget = false;
        RectTransform ra = (RectTransform)acento.transform;
        ra.anchorMin = new Vector2(0f, 0f);
        ra.anchorMax = new Vector2(0f, 1f);
        ra.pivot = new Vector2(0f, 0.5f);
        ra.sizeDelta = new Vector2(6f, 0f);
        ra.anchoredPosition = Vector2.zero;
        acento.gameObject.SetActive(false);

        tmp = CrearTexto("Texto", btn.transform, texto, tam, colorTexto, estilo);
        Estirar((RectTransform)tmp.transform, sangria, 56f);
        return btn;
    }

    Button CrearBoton(string nombre, Transform padre, Color fondo, out Image img)
    {
        img = CrearImagen(nombre, padre, Color.white);
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = fondo;
        cb.highlightedColor = Aclarar(fondo, 0.10f);
        cb.pressedColor = Aclarar(fondo, 0.20f);
        cb.selectedColor = fondo;
        cb.disabledColor = fondo * 0.6f;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
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

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, float tam, Color color, FontStyles estilo)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.fontStyle = estilo;
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

    static Color Aclarar(Color c, float t)
    {
        Color r = Color.Lerp(c, Color.white, t);
        r.a = c.a;
        return r;
    }

    static void AsegurarEventSystem()
    {
        if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // ------------------------------------------------------------------ animación

    void Animar(float destinoX)
    {
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(Deslizar(destinoX));
    }

    IEnumerator Deslizar(float destinoX)
    {
        panel.gameObject.SetActive(true);
        float inicioX = panel.anchoredPosition.x;
        float t = 0f;
        const float duracion = 0.2f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            panel.anchoredPosition = new Vector2(Mathf.Lerp(inicioX, destinoX, e), 0f);
            yield return null;
        }

        if (!abierto) panel.gameObject.SetActive(false);
        animacion = null;
    }
}
