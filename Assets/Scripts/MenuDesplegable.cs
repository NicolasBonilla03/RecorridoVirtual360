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
/// Abrir / cerrar: botón "Menú" arriba a la izquierda, tecla M, Esc o tocando fuera del panel.
/// Cada edificio lleva su color de zona (ver MarcaUdB y AplicarMarcaUdB).
/// Se adapta a móvil: área segura, versión compacta en pantallas angostas.
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

    // Medidas del Sistema UdB Digital
    const float AnchoPanel = 400f;
    const float AltoControl = 48f;       // botón de menú, chip, fila de inicio (≥ 44 px de área táctil)
    const float AnchoBotonMenu = 124f;
    const float AltoEdificio = 52f;
    const float AltoPunto = 44f;
    const float MargenAncho = MarcaUdB.Space8;     // 48 px al borde del visor
    const float MargenCompacto = MarcaUdB.Space4;  // 16 px en móvil vertical
    const float MargenPanel = MarcaUdB.Space4;
    const float AltoEncabezado = 112f;
    const float AnchoBarra = 8f;          // barra de la lista: toma el color del edificio que se está viendo
    const int OrdenCanvas = 50;

    class FilaPunto
    {
        public PuntoRecorrido punto;
        public Image acento;
        public Button boton;
        public TextMeshProUGUI texto;
    }

    class Seccion
    {
        public EdificioRecorrido edificio;
        public Color zona;
        public RectTransform chevron;
        public RectTransform encabezado;
        public GameObject contenedor;
        public bool abierta;
        public List<FilaPunto> filas = new List<FilaPunto>();
    }

    readonly List<Seccion> secciones = new List<Seccion>();
    Scrollbar barraLista;
    ScrollRect scrollLista;
    Color colorBarra = MarcaUdB.Negro;
    bool colorBarraIniciado;
    RectTransform canvasRT;
    RectTransform zonaSegura;
    RectTransform panelRaiz;
    RectTransform botonMenuRT;
    RectTransform chipRT;
    GameObject fondoOscuro;
    GameObject botonMenu;
    GameObject botonMenuSombra;
    Vector2 desfaseSombra;
    GameObject chipUbicacion;
    Image puntoZonaChip;
    TextMeshProUGUI etiquetaChip;
    TextMeshProUGUI textoChip;
    TextMeshProUGUI textoUbicacionPanel;
    TextMeshProUGUI textoFilaInicio;
    Image acentoInicio;
    Button botonInicio;
    GameObject[] pois = new GameObject[0];
    Material skyboxInicio;
    Material ultimoSkybox;
    bool abierto;
    bool compacto;
    Vector2 ultimoTamano;
    Rect ultimaAreaSegura;
    Coroutine animacion;

    // ------------------------------------------------------------------ ciclo de vida

    static MenuDesplegable instancia;

    /// <summary>Cierra el menú lateral si está abierto (lo usan los puntos de información al abrirse).</summary>
    public static void CerrarSiAbierto()
    {
        if (instancia != null) instancia.Cerrar();
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Awake()
    {
        instancia = this;

        // La marca institucional se aplica siempre, aunque nadie haya añadido el componente a mano
        if (FindObjectOfType<AplicarMarcaUdB>() == null)
            gameObject.AddComponent<AplicarMarcaUdB>();
    }

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
        if (abierto) ActualizarColorBarra();

        if (Input.GetKeyDown(teclaMenu))
            Alternar();
        else if (abierto && Input.GetKeyDown(KeyCode.Escape))
            Cerrar();

        // Rotación del teléfono, cambio de tamaño de la ventana o de área segura
        if (canvasRT != null && (canvasRT.rect.size != ultimoTamano || Screen.safeArea != ultimaAreaSegura))
        {
            ultimoTamano = canvasRT.rect.size;
            ultimaAreaSegura = Screen.safeArea;
            AplicarLayout();
        }

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
        if (abierto || panelRaiz == null) return;
        POIController.CerrarAbierto(); // un solo menú a la vez
        abierto = true;

        AjustarAnchoPanel();

        if (abrirEdificioActual)
        {
            foreach (Seccion s in secciones)
            {
                if (ContieneSkybox(s, RenderSettings.skybox)) PonerSeccion(s, true);
                else if (soloUnEdificioAbierto) PonerSeccion(s, false);
            }
        }

        fondoOscuro.SetActive(true);
        MostrarControles(false);
        Animar(MargenPanel);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;

        fondoOscuro.SetActive(false);
        MostrarControles(true);
        Animar(-panelRaiz.sizeDelta.x - 80f);
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

    void MostrarControles(bool visibles)
    {
        botonMenu.SetActive(visibles);
        if (botonMenuSombra != null) botonMenuSombra.SetActive(visibles);
        chipUbicacion.SetActive(visibles);
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
        bool tieneZona = false;
        Color zonaActual = MarcaUdB.InkMuted;

        foreach (Seccion s in secciones)
        {
            foreach (FilaPunto f in s.filas)
            {
                bool actual = sky != null && f.punto.skybox == sky;
                MarcarPunto(f, s.zona, actual);
                if (actual && lugar == null)
                {
                    lugar = s.edificio.nombreEdificio + " · " + f.punto.nombre;
                    zonaActual = s.zona;
                    tieneZona = true;
                }
            }
        }

        // Inicio: botón primario en rojo; cuando es el lugar actual lleva la barra blanca
        acentoInicio.gameObject.SetActive(enInicio);

        if (enInicio)
        {
            lugar = "Inicio";
            zonaActual = MarcaUdB.Rojo;
            tieneZona = true;
        }
        if (lugar == null) lugar = NombreDesdeGestor(sky);

        puntoZonaChip.color = tieneZona ? zonaActual : MarcaUdB.InkMuted;
        textoChip.color = tieneZona && !enInicio ? MarcaUdB.TintaLegible(zonaActual) : MarcaUdB.Ink;
        textoChip.text = lugar;
        textoUbicacionPanel.text = "Estás en: " + lugar;
        AjustarChip();
    }

    // El lugar actual se marca con tres señales, no solo color: barra de acento, relleno y peso
    void MarcarPunto(FilaPunto f, Color zona, bool actual)
    {
        f.acento.gameObject.SetActive(actual);
        f.acento.color = zona;
        if (MarcaUdB.TextoBold != null && MarcaUdB.Texto != null)
            f.texto.font = actual ? MarcaUdB.TextoBold : MarcaUdB.Texto;
        Color normal = actual ? MarcaUdB.Tenue(zona, 0.18f) : Color.clear;
        MarcaUdB.ColoresBoton(f.boton, normal, MarcaUdB.Tenue(zona, actual ? 0.24f : 0.10f), MarcaUdB.Tenue(zona, 0.30f));
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

    // ------------------------------------------------------------------ disposición (escritorio / móvil)

    float Margen { get { return compacto ? MargenCompacto : MargenAncho; } }

    void AplicarLayout()
    {
        MarcaUdB.AjustarAreaSegura(zonaSegura);
        compacto = MarcaUdB.EsCompacto(canvasRT);

        botonMenuRT.anchoredPosition = new Vector2(Margen, -Margen);
        if (botonMenuSombra != null)
            ((RectTransform)botonMenuSombra.transform).anchoredPosition = botonMenuRT.anchoredPosition + desfaseSombra;

        // En móvil vertical el chip baja debajo del botón para no chocar con el logotipo
        chipRT.anchoredPosition = compacto
            ? new Vector2(Margen, -Margen - AltoControl - MarcaUdB.Space2)
            : new Vector2(Margen + AnchoBotonMenu + MarcaUdB.Space2, -Margen);

        AjustarChip();
        AjustarAnchoPanel();
        if (!abierto) panelRaiz.anchoredPosition = new Vector2(-panelRaiz.sizeDelta.x - 80f, 0f);
    }

    void AjustarAnchoPanel()
    {
        if (panelRaiz == null || zonaSegura == null) return;
        float disponible = zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
        float ancho = Mathf.Min(AnchoPanel, disponible - MargenPanel * 2f);
        panelRaiz.sizeDelta = new Vector2(ancho, -MargenPanel * 2f);
    }

    void AjustarChip()
    {
        if (chipRT == null || textoChip == null) return;
        float pad = MarcaUdB.Space4;
        float ancho = pad * 2f + 10f + MarcaUdB.Space2
                      + etiquetaChip.GetPreferredValues(etiquetaChip.text).x + MarcaUdB.Space2
                      + textoChip.GetPreferredValues(textoChip.text).x + 4f;

        float disponible = zonaSegura != null && zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
        float maximo = compacto
            ? disponible - Margen * 2f
            : disponible - (Margen + AnchoBotonMenu + MarcaUdB.Space2) - 360f; // deja sitio al logotipo
        maximo = Mathf.Max(160f, maximo);
        chipRT.sizeDelta = new Vector2(Mathf.Min(Mathf.Ceil(ancho), maximo), AltoControl);
    }

    // ------------------------------------------------------------------ construcción de la interfaz

    void ConstruirInterfaz()
    {
        GameObject goCanvas = new GameObject("MenuDesplegable_UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        goCanvas.transform.SetParent(transform, false);
        Canvas canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OrdenCanvas;
        CanvasScaler escalador = goCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = MarcaUdB.ResolucionReferencia;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        canvasRT = (RectTransform)goCanvas.transform;

        // velo-escena a pantalla completa: aísla la lectura del panel, bloquea la cámara y cierra al tocarlo
        Image fondo = CrearImagen("VeloEscena", canvasRT, MarcaUdB.VeloEscena);
        Estirar((RectTransform)fondo.transform, 0f, 0f);
        Button botonFondo = fondo.gameObject.AddComponent<Button>();
        botonFondo.transition = Selectable.Transition.None;
        SinNavegacion(botonFondo);
        botonFondo.onClick.AddListener(Cerrar);
        fondoOscuro = fondo.gameObject;
        fondoOscuro.SetActive(false);

        // Todo lo demás vive dentro del área segura (muescas, barra de gestos)
        GameObject goSegura = new GameObject("AreaSegura", typeof(RectTransform));
        zonaSegura = (RectTransform)goSegura.transform;
        zonaSegura.SetParent(canvasRT, false);
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        ConstruirBotonMenu();
        ConstruirChipUbicacion();
        ConstruirPanel();

        // El velo va debajo del panel pero encima del botón y el chip
        fondo.transform.SetParent(zonaSegura, false);
        fondo.transform.SetSiblingIndex(panelRaiz.GetSiblingIndex());
        Estirar((RectTransform)fondo.transform, 0f, 0f);
        ((RectTransform)fondo.transform).offsetMin = new Vector2(-4000f, -4000f);
        ((RectTransform)fondo.transform).offsetMax = new Vector2(4000f, 4000f);
    }

    void ConstruirBotonMenu()
    {
        // Negro institucional: el rojo queda para los hotspots y la acción principal
        Image img;
        Button boton = CrearBoton("BotonMenu", zonaSegura, out img);
        RectTransform rt = (RectTransform)boton.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(MargenAncho, -MargenAncho);
        rt.sizeDelta = new Vector2(AnchoBotonMenu, AltoControl);
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(boton, MarcaUdB.Negro, MarcaUdB.Hex("#3a3a36"), MarcaUdB.Hex("#4a4a45"));

        // Ícono de menú (tres trazos de 2 px, como Lucide «menu»)
        for (int i = 0; i < 3; i++)
        {
            Image barra = CrearImagen("Trazo" + i, rt, MarcaUdB.InkInverso);
            barra.raycastTarget = false;
            RectTransform rb = (RectTransform)barra.transform;
            rb.anchorMin = rb.anchorMax = new Vector2(0f, 0.5f);
            rb.pivot = new Vector2(0f, 0.5f);
            rb.sizeDelta = new Vector2(18f, 2f);
            rb.anchoredPosition = new Vector2(MarcaUdB.Space4, 6f - i * 6f);
            MarcaUdB.Redondear(barra, 1f);
        }

        TextMeshProUGUI texto = CrearTexto("Texto", rt, "Menú", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        Estirar((RectTransform)texto.transform, MarcaUdB.Space4 + 18f + MarcaUdB.Space2, MarcaUdB.Space2);

        boton.onClick.AddListener(Alternar);
        botonMenu = boton.gameObject;
        botonMenuRT = rt;

        Image sombra = MarcaUdB.SombraFlotante(rt);
        if (sombra != null)
        {
            botonMenuSombra = sombra.gameObject;
            desfaseSombra = ((RectTransform)sombra.transform).anchoredPosition - rt.anchoredPosition;
        }
    }

    void ConstruirChipUbicacion()
    {
        Image chip = CrearImagen("ChipUbicacion", zonaSegura, MarcaUdB.VidrioPanel);
        chip.raycastTarget = false;
        MarcaUdB.Redondear(chip, MarcaUdB.RadiusMd);
        chipRT = (RectTransform)chip.transform;
        chipRT.anchorMin = chipRT.anchorMax = chipRT.pivot = new Vector2(0f, 1f);
        chipRT.sizeDelta = new Vector2(240f, AltoControl);

        HorizontalLayoutGroup hl = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset((int)MarcaUdB.Space4, (int)MarcaUdB.Space4, 0, 0);
        hl.spacing = MarcaUdB.Space2;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Punto con el color de la zona actual
        puntoZonaChip = CrearImagen("Zona", chipRT, MarcaUdB.InkMuted);
        puntoZonaChip.raycastTarget = false;
        MarcaUdB.Redondear(puntoZonaChip, 5f);
        LayoutElement lp = puntoZonaChip.gameObject.AddComponent<LayoutElement>();
        lp.minWidth = lp.preferredWidth = 10f;
        lp.minHeight = lp.preferredHeight = 10f;
        lp.flexibleWidth = 0f;

        etiquetaChip = CrearTexto("Etiqueta", chipRT, "Estás en", MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.InkMuted);
        LayoutElement le = etiquetaChip.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 0f;

        textoChip = CrearTexto("Lugar", chipRT, "", MarcaUdB.TextoBold, MarcaUdB.Cuerpo, MarcaUdB.Ink);
        LayoutElement ll = textoChip.gameObject.AddComponent<LayoutElement>();
        ll.minWidth = 40f;
        ll.flexibleWidth = 1f;

        chipUbicacion = chip.gameObject;
    }

    void ConstruirPanel()
    {
        // Raíz que se desliza: lleva la sombra y el panel juntos
        GameObject goRaiz = new GameObject("PanelRaiz", typeof(RectTransform));
        panelRaiz = (RectTransform)goRaiz.transform;
        panelRaiz.SetParent(zonaSegura, false);
        panelRaiz.anchorMin = new Vector2(0f, 0f);
        panelRaiz.anchorMax = new Vector2(0f, 1f);
        panelRaiz.pivot = new Vector2(0f, 0.5f);
        panelRaiz.sizeDelta = new Vector2(AnchoPanel, -MargenPanel * 2f);
        panelRaiz.anchoredPosition = new Vector2(-AnchoPanel - 80f, 0f);

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

        // --- Inicio: acción primaria en rojo institucional
        Image imgInicio;
        botonInicio = CrearBoton("Inicio", panel, out imgInicio);
        MarcaUdB.Redondear(imgInicio, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(botonInicio, MarcaUdB.Rojo, MarcaUdB.RojoFuerte, MarcaUdB.RojoFuerte);
        LayoutElement leI = botonInicio.gameObject.AddComponent<LayoutElement>();
        leI.minHeight = leI.preferredHeight = AltoControl;
        acentoInicio = CrearAcento(botonInicio.transform, MarcaUdB.InkInverso, AltoControl);
        textoFilaInicio = CrearTexto("Texto", botonInicio.transform, textoInicio, MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        Estirar((RectTransform)textoFilaInicio.transform, MarcaUdB.Space5, MarcaUdB.Space4);
        botonInicio.onClick.AddListener(IrAlInicio);

        // --- Rótulo de la lista
        TextMeshProUGUI rotuloLista = CrearTexto("RotuloEdificios", panel, "Edificios", null, 0f, Color.white);
        MarcaUdB.EstiloEtiqueta(rotuloLista, MarcaUdB.InkMuted);
        LayoutElement leR = rotuloLista.gameObject.AddComponent<LayoutElement>();
        leR.minHeight = leR.preferredHeight = 20f;

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
        rtC.sizeDelta = new Vector2(-MarcaUdB.Space3, 0f);
        rtC.anchoredPosition = new Vector2(-MarcaUdB.Space3 / 2f, 0f);
        VerticalLayoutGroup vlc = contenido.AddComponent<VerticalLayoutGroup>();
        vlc.spacing = MarcaUdB.Space2;
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
        scroll.scrollSensitivity = AltoEdificio + 4f; // una fila por paso de rueda

        // Barra de desplazamiento delgada
        Image barra = CrearImagen("Barra", imgLista.transform, MarcaUdB.Surface300);
        RectTransform rtB = (RectTransform)barra.transform;
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
        Scrollbar sb = barra.gameObject.AddComponent<Scrollbar>();
        sb.handleRect = (RectTransform)manija.transform;
        sb.targetGraphic = manija;
        sb.direction = Scrollbar.Direction.BottomToTop;
        SinNavegacion(sb);
        MarcaUdB.EstilizarBarra(sb, MarcaUdB.Negro, Color.white, AnchoBarra);
        scroll.verticalScrollbar = sb;
        barraLista = sb;
        scrollLista = scroll;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // --- Edificios, cada uno con su color de zona
        if (origenDatos != null && origenDatos.edificios != null)
        {
            foreach (EdificioRecorrido ed in origenDatos.edificios)
                if (ed != null) CrearSeccion(rtC, ed);
        }
        else
        {
            Debug.LogWarning("[MenuDesplegable] No se encontró MenuNavegacion: el menú solo tendrá el botón de inicio.");
        }

        panelRaiz.gameObject.SetActive(false);
    }

    // Encabezado en negro institucional con el filete rojo de marca
    void ConstruirEncabezado(RectTransform panel)
    {
        Image fondo = CrearImagen("Encabezado", panel, MarcaUdB.Negro);
        fondo.raycastTarget = false;
        MarcaUdB.Redondear(fondo, MarcaUdB.RadiusMd);
        LayoutElement le = fondo.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = AltoEncabezado;
        RectTransform rtE = (RectTransform)fondo.transform;

        float pad = MarcaUdB.Space4;

        Image filete = CrearImagen("FileteRojo", rtE, MarcaUdB.Rojo);
        filete.raycastTarget = false;
        RectTransform rf = (RectTransform)filete.transform;
        rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0f, 1f);
        rf.sizeDelta = new Vector2(32f, 4f);
        rf.anchoredPosition = new Vector2(pad, -pad);
        MarcaUdB.Redondear(filete, 2f);

        TextMeshProUGUI rotulo = CrearTexto("Rotulo", rtE, "Universidad de Boyacá", null, 0f, Color.white);
        MarcaUdB.EstiloEtiqueta(rotulo, new Color(1f, 1f, 1f, 0.72f));
        Colocar((RectTransform)rotulo.transform, pad + 10f, 16f, pad, 56f);

        TextMeshProUGUI titulo = CrearTexto("Titulo", rtE, tituloMenu, MarcaUdB.Display, MarcaUdB.DisplayM, MarcaUdB.InkInverso);
        Colocar((RectTransform)titulo.transform, pad + 30f, 28f, pad, 56f);

        textoUbicacionPanel = CrearTexto("Ubicacion", rtE, "", MarcaUdB.Texto, MarcaUdB.CuerpoS, new Color(1f, 1f, 1f, 0.80f));
        Colocar((RectTransform)textoUbicacionPanel.transform, pad + 62f, 20f, pad, pad);

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

    void CrearSeccion(RectTransform padre, EdificioRecorrido ed)
    {
        Seccion s = new Seccion();
        s.edificio = ed;
        s.zona = MarcaUdB.ColorZona(ed.nombreEdificio);
        // Tinta legible sobre el relleno más oscuro que toma el encabezado (presionado)
        Color tinta = MarcaUdB.TintaLegible(s.zona, MarcaUdB.Tenue(s.zona, 0.32f));

        int n = ed.puntos != null ? ed.puntos.Count : 0;
        string conteo = "  <size=" + MarcaUdB.CuerpoS + "><color=" + MarcaUdB.HexRGB(MarcaUdB.InkMuted) + ">"
                        + n + (n == 1 ? " lugar" : " lugares") + "</color></size>";

        // Encabezado del edificio: relleno suave de la zona + banda sólida a la izquierda
        Image img;
        Button encabezado = CrearBoton("Edificio", padre, out img);
        s.encabezado = (RectTransform)encabezado.transform;
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(encabezado, MarcaUdB.Tenue(s.zona, 0.14f), MarcaUdB.Tenue(s.zona, 0.24f), MarcaUdB.Tenue(s.zona, 0.32f));
        LayoutElement le = encabezado.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = AltoEdificio;

        Image banda = CrearImagen("Banda", encabezado.transform, s.zona);
        banda.raycastTarget = false;
        RectTransform rb = (RectTransform)banda.transform;
        rb.anchorMin = new Vector2(0f, 0f);
        rb.anchorMax = new Vector2(0f, 1f);
        rb.pivot = new Vector2(0f, 0.5f);
        rb.sizeDelta = new Vector2(6f, -MarcaUdB.Space3);
        rb.anchoredPosition = new Vector2(MarcaUdB.Space2, 0f);
        MarcaUdB.Redondear(banda, 3f);

        TextMeshProUGUI textoEnc = CrearTexto("Texto", encabezado.transform, ed.nombreEdificio + conteo,
            MarcaUdB.TextoBold, MarcaUdB.UIControl, tinta);
        Estirar((RectTransform)textoEnc.transform, MarcaUdB.Space5, MarcaUdB.Space8);

        TextMeshProUGUI chevron = CrearTexto("Flecha", encabezado.transform, ">", MarcaUdB.TextoBold, MarcaUdB.UIControl, tinta);
        chevron.alignment = TextAlignmentOptions.Center;
        RectTransform rtF = (RectTransform)chevron.transform;
        rtF.anchorMin = rtF.anchorMax = new Vector2(1f, 0.5f);
        rtF.pivot = new Vector2(0.5f, 0.5f);
        rtF.sizeDelta = new Vector2(24f, 24f);
        rtF.anchoredPosition = new Vector2(-MarcaUdB.Space5, 0f);
        s.chevron = rtF;

        s.contenedor = new GameObject("Puntos_" + ed.nombreEdificio, typeof(RectTransform));
        s.contenedor.transform.SetParent(padre, false);
        VerticalLayoutGroup vl = s.contenedor.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 2f;
        vl.padding = new RectOffset(0, 0, 0, (int)MarcaUdB.Space2);
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

                Image ip;
                fila.boton = CrearBoton("Punto", s.contenedor.transform, out ip);
                MarcaUdB.Redondear(ip, MarcaUdB.RadiusMd);
                LayoutElement lp = fila.boton.gameObject.AddComponent<LayoutElement>();
                lp.minHeight = lp.preferredHeight = AltoPunto;
                fila.acento = CrearAcento(fila.boton.transform, s.zona, AltoPunto);
                fila.texto = CrearTexto("Texto", fila.boton.transform, p.nombre, MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.Ink);
                Estirar((RectTransform)fila.texto.transform, MarcaUdB.Space6, MarcaUdB.Space4);
                MarcarPunto(fila, s.zona, false);

                Material destino = p.skybox;
                fila.boton.onClick.AddListener(() => IrA(destino));
                s.filas.Add(fila);
            }
        }

        encabezado.onClick.AddListener(() => AlternarSeccion(s));
        PonerSeccion(s, false);
        secciones.Add(s);
    }

    // ------------------------------------------------------------------ barra de la lista

    // La manija se tiñe del color del edificio que ocupa el centro de la lista, con una transición suave
    void ActualizarColorBarra()
    {
        if (barraLista == null || scrollLista == null || !barraLista.gameObject.activeInHierarchy) return;

        Color objetivo = ZonaVisible();
        if (!colorBarraIniciado)
        {
            colorBarra = objetivo;
            colorBarraIniciado = true;
        }
        else
        {
            if (colorBarra == objetivo) return;
            colorBarra = Color.Lerp(colorBarra, objetivo, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            if (Mathf.Abs(colorBarra.r - objetivo.r) + Mathf.Abs(colorBarra.g - objetivo.g) + Mathf.Abs(colorBarra.b - objetivo.b) < 0.004f)
                colorBarra = objetivo;
        }
        MarcaUdB.ColorBarra(barraLista, colorBarra, Color.white);
    }

    Color ZonaVisible()
    {
        if (secciones.Count == 0) return MarcaUdB.Negro;

        RectTransform vista = scrollLista.viewport != null ? scrollLista.viewport : (RectTransform)scrollLista.transform;
        float centro = vista.rect.center.y;
        Color zona = secciones[0].zona;
        foreach (Seccion s in secciones)
        {
            if (s.encabezado == null) continue;
            if (vista.InverseTransformPoint(s.encabezado.position).y >= centro) zona = s.zona;
            else break;
        }
        return zona;
    }

    // ------------------------------------------------------------------ utilidades de UI

    static Image CrearAcento(Transform padre, Color color, float alto)
    {
        Image acento = CrearImagen("Acento", padre, color);
        acento.raycastTarget = false;
        RectTransform ra = (RectTransform)acento.transform;
        ra.anchorMin = new Vector2(0f, 0.5f);
        ra.anchorMax = new Vector2(0f, 0.5f);
        ra.pivot = new Vector2(0f, 0.5f);
        ra.sizeDelta = new Vector2(4f, alto - MarcaUdB.Space4);
        ra.anchoredPosition = new Vector2(MarcaUdB.Space2, 0f);
        MarcaUdB.Redondear(acento, 2f);
        acento.gameObject.SetActive(false);
        return acento;
    }

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

    // Ubica un texto dentro de un bloque: desde arriba, con alto fijo y márgenes laterales
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
}
