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
        public Coroutine fundido;
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
    TextMeshProUGUI textoPie;
    Button botonInfoChip;
    FichaInfo fichaActual;
    const float LadoInfoChip = 32f;
    const float AnchoIconoChip = 15f;
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
    bool vertical;                 // celular en vertical: foto a pantalla completa y hoja del lugar abajo
    bool controlesVisibles = true; // false mientras el menú está abierto
    Button botonMenuBoton;
    TextMeshProUGUI textoBotonMenu;
    readonly List<Image> trazosMenu = new List<Image>();
    HorizontalLayoutGroup disposicionChip;

    // Hoja inferior del celular vertical (como la tarjeta de Street View): edificio, lugar, resumen
    // de la ficha y «Ver más información». Reemplaza al chip «Estás en» en ese modo.
    RectTransform hojaRT;
    HojaDeslizable hojaGesto;
    CanvasGroup grupoResumenHoja;
    float altoHojaNormal;          // alto visible de la hoja en su altura normal
    float bajadaHojaCompacta;      // cuánto baja en la compacta (lo que mide la descripción)
    Image tiradorHoja;
    Image pinHoja;
    TextMeshProUGUI etiquetaHoja;
    TextMeshProUGUI tituloHoja;
    TextMeshProUGUI resumenHoja;
    Button botonHojaInfo;
    Image circuloHojaInfo;
    TextMeshProUGUI letraHojaInfo;
    TextMeshProUGUI textoHojaInfo;
    CanvasGroup grupoBotonHoja;
    Button botonHojaRedondo;       // «i» redondo junto al título en la altura compacta
    CanvasGroup grupoBotonRedondo;
    TextMeshProUGUI letraHojaRedondo;
    const float LadoBotonRedondo = MarcaUdB.AreaTactil;
    const float AltoBotonHoja = 48f;
    const float AnchoTirador = 36f;
    Vector2 ultimoTamano;
    Rect ultimaAreaSegura;
    float ultimaFilaSuperior;
    Coroutine animacion;

    // ------------------------------------------------------------------ ciclo de vida

    static MenuDesplegable instancia;

    /// <summary>Cierra el menú lateral si está abierto (lo usan los puntos de información al abrirse).</summary>
    public static void CerrarSiAbierto()
    {
        if (instancia != null) instancia.Cerrar();
    }

    public static bool EstaAbierto { get { return instancia != null && instancia.abierto; } }

    void OnEnable() { FadeController.AlFallarTransicion += AlFallarTransicion; }
    void OnDisable() { FadeController.AlFallarTransicion -= AlFallarTransicion; }

    // Si un cambio de lugar no se completó, se vuelve a pintar el lugar actual (puntos, chip)
    void AlFallarTransicion() { ultimoSkybox = null; }

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

        // El guía virtual (preguntas predefinidas) también se añade solo
        if (FindObjectOfType<GuiaVirtual>() == null)
            gameObject.AddComponent<GuiaVirtual>();
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
        if (canvasRT != null && (canvasRT.rect.size != ultimoTamano || Screen.safeArea != ultimaAreaSegura
                                 || !Mathf.Approximately(MarcaUdB.AltoFilaSuperior, ultimaFilaSuperior)))
        {
            ultimoTamano = canvasRT.rect.size;
            ultimaAreaSegura = Screen.safeArea;
            ultimaFilaSuperior = MarcaUdB.AltoFilaSuperior;
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
        PanelInfo.CerrarSiAbierto();
        GuiaVirtual.CerrarSiAbierta();
        abierto = true;
        ActualizarPie();

        AjustarAnchoPanel();

        if (abrirEdificioActual)
        {
            foreach (Seccion s in secciones)
            {
                if (ContieneSkybox(s, RenderSettings.skybox)) PonerSeccion(s, true);
                else if (soloUnEdificioAbierto) PonerSeccion(s, false);
            }
        }

        VeloSuave.Mostrar(fondoOscuro);
        MostrarControles(false);
        Animar(MargenPanel);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;

        VeloSuave.Ocultar(fondoOscuro);
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

    /// <summary>Vuelve a la vista de inicio (lo usa el logotipo de la cabecera).</summary>
    public static void IrInicio()
    {
        if (instancia != null) instancia.IrAlInicio();
    }

    /// <summary>true si el usuario ya está en la vista de inicio.</summary>
    public static bool EnInicio
    {
        get { return instancia != null && instancia.skyboxInicio != null && RenderSettings.skybox == instancia.skyboxInicio; }
    }

    void MostrarControles(bool visibles)
    {
        controlesVisibles = visibles;
        // Se funden al ritmo del velo (160 ms al volver, 140 ms al irse) en lugar de desaparecer de golpe
        Fundir(botonMenu, visibles);
        Fundir(botonMenuSombra, visibles);
        // En el celular vertical el lugar va en la hoja de abajo; en el resto, en el chip «Estás en»
        Fundir(chipUbicacion, visibles && !vertical);
        if (hojaRT != null) Fundir(hojaRT.gameObject, visibles && vertical);
    }

    static void Fundir(GameObject objeto, bool mostrar)
    {
        if (objeto == null) return;
        if (mostrar) VeloSuave.Mostrar(objeto);
        else VeloSuave.Ocultar(objeto);
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

        // Solo al tocar el edificio (no al abrir el menú, que ya entra deslizándose): los lugares
        // aparecen con un fundido corto en vez de saltar, como las respuestas del guía
        if (s.fundido != null) StopCoroutine(s.fundido);
        s.fundido = abrir && AnimacionesUI.Activas && isActiveAndEnabled ? StartCoroutine(Aparecer(s)) : null;
        if (!abrir || s.fundido == null) PonerAlfa(s.contenedor, 1f);
    }

    // 150 ms con salida fuerte. Solo cambia la opacidad de un CanvasGroup: no recalcula la lista
    IEnumerator Aparecer(Seccion s)
    {
        float t = 0f;
        while (t < 1f && s.contenedor != null)
        {
            t += Time.unscaledDeltaTime / 0.15f;
            PonerAlfa(s.contenedor, AnimacionesUI.SalidaFuerte(Mathf.Clamp01(t)));
            yield return null;
        }
        s.fundido = null;
    }

    static void PonerAlfa(GameObject objeto, float alfa)
    {
        if (objeto == null) return;
        CanvasGroup grupo = objeto.GetComponent<CanvasGroup>();
        if (grupo == null)
        {
            if (alfa >= 1f) return;
            grupo = objeto.AddComponent<CanvasGroup>();
        }
        grupo.alpha = alfa;
    }

    void PonerSeccion(Seccion s, bool abrir)
    {
        s.abierta = abrir;
        s.contenedor.SetActive(abrir);
        GiroSuave.Poner(s.chevron, abrir ? -90f : 0f);
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
        string edificioHoja = null, lugarHoja = null; // la hoja del celular los muestra en dos renglones
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
                    edificioHoja = s.edificio.nombreEdificio;
                    lugarHoja = f.punto.nombre;
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

        // Ficha del lugar: el botón «i» del chip solo aparece si el catálogo tiene información
        fichaActual = CatalogoInfo.PorSkybox(sky);
        if (botonInfoChip != null)
        {
            botonInfoChip.gameObject.SetActive(fichaActual != null);
            if (fichaActual != null)
            {
                // Blanco sobre el color de la zona; el «i» es un glifo suelto, así que basta un contraste de 3:1
                Color ci = MarcaUdB.FondoParaBlanco(!string.IsNullOrEmpty(fichaActual.zona) ? fichaActual.ColorZona : MarcaUdB.Negro, MarcaUdB.ContrasteIcono);
                MarcaUdB.ColoresBoton(botonInfoChip, ci, Color.Lerp(ci, MarcaUdB.Negro, 0.18f), Color.Lerp(ci, MarcaUdB.Negro, 0.32f));
                TMP_Text ti = botonInfoChip.GetComponentInChildren<TMP_Text>(true);
                if (ti != null) ti.color = MarcaUdB.InkInverso;
            }
        }

        puntoZonaChip.color = tieneZona ? zonaActual : MarcaUdB.InkMuted;
        textoChip.color = tieneZona && !enInicio ? MarcaUdB.TintaLegible(zonaActual) : MarcaUdB.Ink;
        textoChip.text = lugar;
        textoUbicacionPanel.text = "Estás en: " + lugar;
        AjustarChip();

        if (enInicio || lugarHoja == null) { edificioHoja = "Estás en"; lugarHoja = lugar; }
        ActualizarHoja(edificioHoja, lugarHoja, tieneZona && !enInicio ? zonaActual : MarcaUdB.Negro);
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
        vertical = MarcaUdB.EsMovilVertical(canvasRT);
        EstiloBotonMenu(vertical);

        // Fila superior: el botón de menú se centra en el alto de la placa del logotipo, así los dos
        // quedan sobre la misma línea (en móvil miden lo mismo: 48 px). En el celular vertical el botón
        // es un círculo de vidrio que flota sobre la foto, como los de Street View.
        float fila = Mathf.Max(AltoControl, MarcaUdB.AltoFilaSuperior);
        float yFila = -Margen - (fila - AltoControl) * 0.5f;
        botonMenuRT.anchoredPosition = new Vector2(Margen, yFila);
        if (botonMenuSombra != null)
            ((RectTransform)botonMenuSombra.transform).anchoredPosition = botonMenuRT.anchoredPosition + desfaseSombra;

        // En móvil horizontal el chip baja debajo de la fila superior, alineado con el botón de menú
        chipRT.anchoredPosition = compacto
            ? new Vector2(Margen, -Margen - fila - MarcaUdB.Space2)
            : new Vector2(Margen + AnchoBotonMenu + MarcaUdB.Space2, yFila);

        MostrarControles(controlesVisibles);
        AjustarChip();
        AcomodarHoja();
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

    // Botón de menú: negro con «Menú» en pantalla ancha; en el celular vertical, círculo de vidrio solo con el ícono
    void EstiloBotonMenu(bool redondo)
    {
        if (botonMenuBoton == null) return;
        Image fondo = botonMenuBoton.targetGraphic as Image;
        if (redondo)
        {
            botonMenuRT.sizeDelta = new Vector2(AltoControl, AltoControl);
            if (fondo != null) MarcaUdB.Redondear(fondo, AltoControl * 0.5f);
            MarcaUdB.ColoresBoton(botonMenuBoton, MarcaUdB.VidrioPanel, MarcaUdB.Surface200, MarcaUdB.Surface300);
        }
        else
        {
            botonMenuRT.sizeDelta = new Vector2(AnchoBotonMenu, AltoControl);
            if (fondo != null) MarcaUdB.Redondear(fondo, MarcaUdB.RadiusMd);
            MarcaUdB.ColoresBoton(botonMenuBoton, MarcaUdB.Negro, MarcaUdB.Hex("#3a3a36"), MarcaUdB.Hex("#4a4a45"));
        }
        if (textoBotonMenu != null) textoBotonMenu.gameObject.SetActive(!redondo);
        // Sobre la foto el botón siempre lleva su sombra (sombra-flotante: 8 px por lado)
        if (botonMenuSombra != null)
            ((RectTransform)botonMenuSombra.transform).sizeDelta = botonMenuRT.sizeDelta + new Vector2(16f, 16f);

        for (int i = 0; i < trazosMenu.Count; i++)
        {
            RectTransform rb = (RectTransform)trazosMenu[i].transform;
            trazosMenu[i].color = redondo ? MarcaUdB.Ink : MarcaUdB.InkInverso;
            if (redondo)
            {
                rb.anchorMin = rb.anchorMax = rb.pivot = new Vector2(0.5f, 0.5f);
                rb.sizeDelta = new Vector2(20f, 2f);
                rb.anchoredPosition = new Vector2(0f, 6f - i * 6f);
            }
            else
            {
                rb.anchorMin = rb.anchorMax = new Vector2(0f, 0.5f);
                rb.pivot = new Vector2(0f, 0.5f);
                rb.sizeDelta = new Vector2(18f, 2f);
                rb.anchoredPosition = new Vector2(MarcaUdB.Space4, 6f - i * 6f);
            }
        }
    }

    void AjustarChip()
    {
        if (chipRT == null || textoChip == null) return;
        float pad = MarcaUdB.Space4;
        float ancho = (disposicionChip != null ? disposicionChip.padding.left + disposicionChip.padding.right : pad * 2f)
                      + AnchoIconoChip + MarcaUdB.Space2
                      + etiquetaChip.GetPreferredValues(etiquetaChip.text).x + MarcaUdB.Space2
                      + textoChip.GetPreferredValues(textoChip.text).x + 4f;
        if (botonInfoChip != null && botonInfoChip.gameObject.activeSelf)
            ancho += MarcaUdB.Space2 + LadoInfoChip;

        float disponible = zonaSegura != null && zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
        float maximo = compacto
            ? disponible - Margen * 2f
            : disponible - (Margen + AnchoBotonMenu + MarcaUdB.Space2) - 360f; // deja sitio al logotipo
        maximo = Mathf.Max(160f, maximo);
        float anchoChip = Mathf.Min(Mathf.Ceil(ancho), maximo);

        // «Estás en» nunca se recorta; si el nombre del lugar no cabe, pasa a dos renglones en vez de cortarse
        LayoutElement le = etiquetaChip.GetComponent<LayoutElement>();
        if (le != null) le.minWidth = Mathf.Ceil(etiquetaChip.GetPreferredValues(etiquetaChip.text).x);
        bool dosRenglones = Mathf.Ceil(ancho) > maximo;
        textoChip.enableWordWrapping = dosRenglones;
        float alto = AltoControl;
        if (dosRenglones)
        {
            float resto = Mathf.Ceil(ancho) - textoChip.GetPreferredValues(textoChip.text).x;
            float libre = Mathf.Max(40f, anchoChip - resto);
            float altoTexto = textoChip.GetPreferredValues(textoChip.text, libre, 0f).y;
            alto = Mathf.Max(AltoControl, Mathf.Ceil(altoTexto) + MarcaUdB.Space3 * 2f);
        }
        chipRT.sizeDelta = new Vector2(anchoChip, alto);
    }

    // ------------------------------------------------------------------ hoja inferior del celular vertical

    // Rellena la hoja con el lugar actual. La ficha (si la hay) aporta el resumen y el botón.
    void ActualizarHoja(string edificio, string lugar, Color zona)
    {
        if (hojaRT == null) return;
        bool conFicha = fichaActual != null;

        pinHoja.color = zona;
        etiquetaHoja.text = edificio;
        etiquetaHoja.color = MarcaUdB.TintaLegible(zona, MarcaUdB.Surface100);
        tituloHoja.text = lugar;

        string resumen = conFicha && !string.IsNullOrEmpty(fichaActual.resumen) ? fichaActual.resumen.Trim() : "";
        resumenHoja.text = resumen;
        resumenHoja.gameObject.SetActive(resumen.Length > 0);

        // El botón toma el color de la zona de la ficha, como el «i» del chip (el rojo queda para el guía),
        // oscurecido lo justo para que el texto en blanco se lea bien
        botonHojaInfo.gameObject.SetActive(conFicha);
        botonHojaRedondo.gameObject.SetActive(conFicha);
        if (conFicha)
        {
            Color c = MarcaUdB.FondoParaBlanco(!string.IsNullOrEmpty(fichaActual.zona) ? fichaActual.ColorZona : MarcaUdB.Negro);
            MarcaUdB.ColoresBoton(botonHojaInfo, c, Color.Lerp(c, MarcaUdB.Negro, 0.18f), Color.Lerp(c, MarcaUdB.Negro, 0.32f));
            Color tinta = MarcaUdB.InkInverso;
            textoHojaInfo.color = tinta;
            circuloHojaInfo.color = tinta;
            letraHojaInfo.color = c;
            // El redondo es el mismo botón en pequeño: mismo color y la misma «i» en blanco
            MarcaUdB.ColoresBoton(botonHojaRedondo, c, Color.Lerp(c, MarcaUdB.Negro, 0.18f), Color.Lerp(c, MarcaUdB.Negro, 0.32f));
            letraHojaRedondo.color = tinta;
        }

        // Sin ficha no hay nada que abrir: sin tirador y sin gesto (la hoja solo informa)
        tiradorHoja.gameObject.SetActive(conFicha);
        hojaGesto.alAbrir = conFicha ? (System.Action)AbrirFichaActual : null;

        AcomodarHoja();
    }

    void AbrirFichaActual()
    {
        if (fichaActual == null) return;
        if (FadeController.Instance != null && FadeController.Instance.EnTransicion) return;
        PanelInfo.Mostrar(fichaActual);
    }

    // Apila las piezas de arriba abajo con sus altos reales y publica el alto visible de la hoja
    void AcomodarHoja()
    {
        if (hojaRT == null) return;
        if (!vertical)
        {
            MarcaUdB.AltoHojaMovil = 0f;
            return;
        }

        float lado = Margen;
        float ancho = (canvasRT.rect.width > 0f ? canvasRT.rect.width : MarcaUdB.AnchoMovil) - lado * 2f;
        float y = MarcaUdB.Space5; // deja sitio al tirador

        // Con ficha, el título deja libre a su derecha el sitio del botón redondo de la altura compacta. Se
        // reserva también en la altura normal: así el título no cambia de renglones al pasar de una a otra.
        bool conBoton = botonHojaInfo.gameObject.activeSelf;
        float anchoTitulo = conBoton ? ancho - LadoBotonRedondo - MarcaUdB.Space3 : ancho;
        float inicioTitulo = y;

        RectTransform fila = (RectTransform)etiquetaHoja.transform.parent;
        ColocarEnHoja(fila, lado, y, anchoTitulo, 18f);
        y += 18f + MarcaUdB.Space1;

        float altoTitulo = AltoTexto(tituloHoja, anchoTitulo, 2);
        ColocarEnHoja((RectTransform)tituloHoja.transform, lado, y, anchoTitulo, altoTitulo);
        y += altoTitulo;

        // Botón redondo de la altura compacta: a la derecha, centrado en el bloque del edificio y el título
        if (conBoton)
        {
            float centro = (inicioTitulo + y) * 0.5f;
            ColocarEnHoja((RectTransform)botonHojaRedondo.transform, lado + ancho - LadoBotonRedondo,
                          centro - LadoBotonRedondo * 0.5f, LadoBotonRedondo, LadoBotonRedondo);
        }

        // Lo que sigue (botón ancho y descripción) es lo que la altura compacta esconde: la hoja baja justo eso
        // y queda bajo el borde de la pantalla. Lo de arriba no cambia de lugar dentro de la hoja.
        float alTitulo = y;
        if (conBoton)
        {
            y += MarcaUdB.Space4;
            ColocarEnHoja((RectTransform)botonHojaInfo.transform, lado, y, ancho, AltoBotonHoja);
            y += AltoBotonHoja;
        }

        if (resumenHoja.gameObject.activeSelf)
        {
            float altoResumen = AltoTexto(resumenHoja, ancho, 2);
            y += MarcaUdB.Space3;
            ColocarEnHoja((RectTransform)resumenHoja.transform, lado, y, ancho, altoResumen);
            y += altoResumen;
        }
        float bajadaCompacta = y - alTitulo; // con el margen de abajo, bajo el título queda el mismo espacio
        y += MarcaUdB.Space4;

        // La hoja baja hasta el borde de la pantalla (cubre la barra de gestos) y un poco más, para que solo
        // se vean redondeadas las esquinas de arriba
        float inset = MarcaUdB.InsetInferior(canvasRT);
        hojaRT.sizeDelta = new Vector2(0f, y + inset + MarcaUdB.RadiusLg);
        altoHojaNormal = Mathf.Ceil(y);
        bajadaHojaCompacta = bajadaCompacta;
        hojaGesto.FijarBase(-MarcaUdB.RadiusLg, bajadaCompacta); // publica el alto visible con MoverHoja
    }

    // Cada vez que la hoja se mueve (gesto o animación): el botón del guía y la pista se apoyan en lo que
    // queda a la vista. El cambio de altura va atado a la misma posición, así se puede interrumpir y sigue
    // al dedo: primero se va la descripción, luego el botón ancho, y el redondo aparece junto al título.
    void MoverHoja(float bajada)
    {
        if (!vertical) return;
        MarcaUdB.AltoHojaMovil = Mathf.Ceil(altoHojaNormal - bajada);
        float p = bajadaHojaCompacta > 0f ? Mathf.Clamp01(bajada / bajadaHojaCompacta) : 0f;

        if (grupoResumenHoja != null) grupoResumenHoja.alpha = 1f - Mathf.Clamp01(p / 0.4f);
        if (grupoBotonHoja != null) grupoBotonHoja.alpha = 1f - Mathf.Clamp01((p - 0.2f) / 0.4f);

        if (grupoBotonRedondo != null)
        {
            // Aparece con fundido en la segunda mitad del recorrido, mientras la hoja baja. Su escala no se
            // toca: la maneja ToqueSuave para el encogimiento al presionar
            float r = AnimacionesUI.SalidaFuerte(Mathf.Clamp01((p - 0.45f) / 0.55f));
            grupoBotonRedondo.alpha = r;
            // Solo se bloquean los toques: con «interactable» el botón tomaría su color de deshabilitado
            bool activo = r > 0.5f;
            grupoBotonRedondo.blocksRaycasts = activo;
        }
        if (grupoBotonHoja != null)
        {
            bool activo = grupoBotonHoja.alpha > 0.5f;
            grupoBotonHoja.blocksRaycasts = activo;
        }
    }

    // Alto de un texto con un tope de renglones (lo que no cabe termina en «…»)
    static float AltoTexto(TextMeshProUGUI t, float ancho, int maximoRenglones)
    {
        t.maxVisibleLines = maximoRenglones;
        float alto = t.GetPreferredValues(t.text, ancho, 0f).y;
        string renglones = "A";
        for (int i = 1; i < maximoRenglones; i++) renglones += "\nA";
        float tope = t.GetPreferredValues(renglones, ancho, 0f).y;
        return Mathf.Ceil(Mathf.Min(alto, tope));
    }

    static void ColocarEnHoja(RectTransform rt, float x, float y, float ancho, float alto)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(ancho, alto);
    }

    void ConstruirHoja()
    {
        // Va directo en el lienzo (no en el área segura): ocupa todo el ancho y baja hasta el borde.
        // Queda debajo de todo lo demás, así el velo del menú y de la ficha la cubren.
        GameObject goHoja = new GameObject("HojaLugar", typeof(RectTransform));
        hojaRT = (RectTransform)goHoja.transform;
        hojaRT.SetParent(canvasRT, false);
        hojaRT.SetSiblingIndex(0);
        hojaRT.anchorMin = new Vector2(0f, 0f);
        hojaRT.anchorMax = new Vector2(1f, 0f);
        hojaRT.pivot = new Vector2(0.5f, 0f);
        // El gesto va en la raíz: los toques del fondo y del texto suben hasta ella
        hojaGesto = goHoja.AddComponent<HojaDeslizable>();
        hojaGesto.alMover = MoverHoja;

        // Fondo y sombra estirados dentro de la raíz: los dos siguen el alto de la hoja y se esconden con ella
        Image fondo = CrearImagen("Fondo", hojaRT, MarcaUdB.Surface100);
        fondo.raycastTarget = true; // el arrastre sobre la hoja no gira la cámara
        RectTransform cuerpo = (RectTransform)fondo.transform;
        Estirar(cuerpo, 0f, 0f);
        MarcaUdB.Redondear(fondo, MarcaUdB.RadiusLg);
        MarcaUdB.SombraFlotante(cuerpo);

        // Tirador: avisa que la hoja se puede subir para ver más
        tiradorHoja = CrearImagen("Tirador", cuerpo, MarcaUdB.Borde);
        tiradorHoja.raycastTarget = false;
        RectTransform rt = (RectTransform)tiradorHoja.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(AnchoTirador, 4f);
        rt.anchoredPosition = new Vector2(0f, -MarcaUdB.Space2);
        MarcaUdB.Redondear(tiradorHoja, 2f);

        // Edificio: pin y nombre en etiqueta, con la tinta legible de la zona
        GameObject goFila = new GameObject("Edificio", typeof(RectTransform));
        RectTransform fila = (RectTransform)goFila.transform;
        fila.SetParent(cuerpo, false);
        pinHoja = CrearImagen("Pin", fila, MarcaUdB.Negro);
        pinHoja.raycastTarget = false;
        Sprite pin = MarcaUdB.IconoUbicacion;
        if (pin != null) { pinHoja.sprite = pin; pinHoja.preserveAspect = true; }
        else MarcaUdB.Redondear(pinHoja, 5f);
        RectTransform rp = (RectTransform)pinHoja.transform;
        rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0f, 0.5f);
        rp.sizeDelta = new Vector2(12f, 16f);
        etiquetaHoja = CrearTexto("Etiqueta", fila, "", null, 0f, MarcaUdB.InkMuted);
        MarcaUdB.EstiloEtiqueta(etiquetaHoja, MarcaUdB.InkMuted);
        etiquetaHoja.overflowMode = TextOverflowModes.Ellipsis;
        Estirar((RectTransform)etiquetaHoja.transform, 12f + MarcaUdB.Space2, 0f);

        // Lugar: el dato principal de la hoja, en Raleway como los títulos de las fichas
        tituloHoja = CrearTexto("Lugar", cuerpo, "", MarcaUdB.Display, MarcaUdB.DisplayM, MarcaUdB.Ink);
        tituloHoja.enableWordWrapping = true;
        tituloHoja.alignment = TextAlignmentOptions.TopLeft;

        // Resumen de la ficha: dos renglones como máximo, el resto está en «Ver más información». Va al final
        // de la hoja y se desvanece cuando la hoja baja a su altura compacta
        resumenHoja = CrearTexto("Resumen", cuerpo, "", MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.InkMuted);
        resumenHoja.enableWordWrapping = true;
        resumenHoja.alignment = TextAlignmentOptions.TopLeft;
        grupoResumenHoja = resumenHoja.gameObject.AddComponent<CanvasGroup>();
        grupoResumenHoja.blocksRaycasts = false;

        // «Ver más información»: a todo el ancho y con relleno de color para que se vea a primera vista
        Image imgBoton;
        botonHojaInfo = CrearBoton("VerMas", cuerpo, out imgBoton);
        MarcaUdB.Pildora(imgBoton, AltoBotonHoja);
        HorizontalLayoutGroup hl = imgBoton.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = MarcaUdB.Space2;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        circuloHojaInfo = CrearImagen("Icono", botonHojaInfo.transform, MarcaUdB.InkInverso);
        circuloHojaInfo.raycastTarget = false;
        MarcaUdB.Redondear(circuloHojaInfo, 11f);
        LayoutElement lc = circuloHojaInfo.gameObject.AddComponent<LayoutElement>();
        lc.minWidth = lc.preferredWidth = 22f;
        lc.minHeight = lc.preferredHeight = 22f;
        letraHojaInfo = CrearTexto("i", circuloHojaInfo.transform, "i", MarcaUdB.TextoBold, MarcaUdB.Cuerpo, MarcaUdB.Negro);
        letraHojaInfo.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)letraHojaInfo.transform, 0f, 0f);

        textoHojaInfo = CrearTexto("Texto", botonHojaInfo.transform, "Ver más información", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        LayoutElement lt = textoHojaInfo.gameObject.AddComponent<LayoutElement>();
        lt.flexibleWidth = 0f;
        botonHojaInfo.onClick.AddListener(AbrirFichaActual);
        grupoBotonHoja = botonHojaInfo.gameObject.AddComponent<CanvasGroup>();

        // El mismo botón en pequeño para la altura compacta: círculo del color de la zona con «i» en blanco,
        // a la derecha del título. Empieza invisible; MoverHoja lo muestra cuando la hoja baja.
        Image imgRedondo;
        botonHojaRedondo = CrearBoton("VerMasRedondo", cuerpo, out imgRedondo);
        MarcaUdB.Redondear(imgRedondo, LadoBotonRedondo * 0.5f);
        letraHojaRedondo = CrearTexto("i", botonHojaRedondo.transform, "i", MarcaUdB.TextoBold, MarcaUdB.CuerpoL + 3f, MarcaUdB.InkInverso);
        letraHojaRedondo.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)letraHojaRedondo.transform, 0f, 0f);
        botonHojaRedondo.onClick.AddListener(AbrirFichaActual);
        grupoBotonRedondo = botonHojaRedondo.gameObject.AddComponent<CanvasGroup>();
        grupoBotonRedondo.alpha = 0f;
        grupoBotonRedondo.blocksRaycasts = false;

        hojaRT.gameObject.SetActive(false);
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
        MarcaUdB.EscalarSegunPantalla(escalador);
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
        ConstruirHoja();
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
            trazosMenu.Add(barra);
        }

        TextMeshProUGUI texto = CrearTexto("Texto", rt, "Menú", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        Estirar((RectTransform)texto.transform, MarcaUdB.Space4 + 18f + MarcaUdB.Space2, MarcaUdB.Space2);
        textoBotonMenu = texto;
        botonMenuBoton = boton;

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
        hl.padding = new RectOffset((int)MarcaUdB.Space4, (int)MarcaUdB.Space2 + 4, 0, 0);
        disposicionChip = hl;
        hl.spacing = MarcaUdB.Space2;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Ícono de ubicación con el color de la zona actual (rojo institucional en el inicio)
        puntoZonaChip = CrearImagen("Zona", chipRT, MarcaUdB.InkMuted);
        puntoZonaChip.raycastTarget = false;
        Sprite pin = MarcaUdB.IconoUbicacion;
        if (pin != null) { puntoZonaChip.sprite = pin; puntoZonaChip.preserveAspect = true; }
        else MarcaUdB.Redondear(puntoZonaChip, 5f);
        LayoutElement lp = puntoZonaChip.gameObject.AddComponent<LayoutElement>();
        lp.minWidth = lp.preferredWidth = AnchoIconoChip;
        lp.minHeight = lp.preferredHeight = pin != null ? 20f : 10f;
        lp.flexibleWidth = 0f;

        etiquetaChip = CrearTexto("Etiqueta", chipRT, "Estás en", MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.InkMuted);
        LayoutElement le = etiquetaChip.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 0f;

        textoChip = CrearTexto("Lugar", chipRT, "", MarcaUdB.TextoBold, MarcaUdB.Cuerpo, MarcaUdB.Ink);
        LayoutElement ll = textoChip.gameObject.AddComponent<LayoutElement>();
        ll.minWidth = 40f;
        ll.flexibleWidth = 1f;

        // Botón «i»: abre la ficha del lugar actual (catálogo de información), si la hay
        Image imgInfo;
        botonInfoChip = CrearBoton("Info", chipRT, out imgInfo);
        MarcaUdB.Redondear(imgInfo, LadoInfoChip * 0.5f);
        LayoutElement li = botonInfoChip.gameObject.AddComponent<LayoutElement>();
        li.minWidth = li.preferredWidth = LadoInfoChip;
        li.minHeight = li.preferredHeight = LadoInfoChip;
        li.flexibleWidth = 0f;
        TextMeshProUGUI textoInfo = CrearTexto("i", botonInfoChip.transform, "i", MarcaUdB.TextoBold, MarcaUdB.CuerpoL, MarcaUdB.InkInverso);
        textoInfo.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)textoInfo.transform, 0f, 0f);
        botonInfoChip.onClick.AddListener(() => { if (fichaActual != null) PanelInfo.Mostrar(fichaActual); });
        botonInfoChip.gameObject.SetActive(false);

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

        // --- Pie: aviso del registro anónimo y código de la sesión (para la encuesta)
        textoPie = CrearTexto("Pie", panel, "", MarcaUdB.Texto, MarcaUdB.CuerpoS, MarcaUdB.InkMuted);
        textoPie.enableWordWrapping = true;
        textoPie.overflowMode = TextOverflowModes.Overflow;
        textoPie.alignment = TextAlignmentOptions.TopLeft;
        textoPie.margin = new Vector4(MarcaUdB.Space1, 0f, MarcaUdB.Space1, 0f);
        textoPie.gameObject.SetActive(false);

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
        MarcaUdB.AjustarACaja(textoUbicacionPanel, false, 0.8f);
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
        MarcaUdB.AjustarACaja(textoEnc, false, 0.8f);

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
                // Nombres largos: hasta dos renglones dentro de la fila antes de recortar
                MarcaUdB.AjustarACaja(fila.texto, true, 0.78f);
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

    // ------------------------------------------------------------------ pie del panel

    void ActualizarPie()
    {
        if (textoPie == null) return;
        bool mostrar = RegistroUso.Activo && !string.IsNullOrEmpty(RegistroUso.Codigo);
        textoPie.gameObject.SetActive(mostrar);
        if (!mostrar) return;
        textoPie.text = "Tu código para la encuesta: <b><color=" + MarcaUdB.HexRGB(MarcaUdB.Ink) + ">" + RegistroUso.Codigo + "</color></b>\n"
                        + "Este recorrido registra de forma anónima los lugares que visitas y el tiempo en cada uno, con fines académicos.";
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
        // Cajón: entra en 240 ms y sale en 180 ms. Parte de donde esté, así se puede interrumpir.
        float duracion = abierto ? AnimacionesUI.DuracionPanelEntrada : AnimacionesUI.DuracionPanelSalida;

        while (t < 1f)
        {
            t = AnimacionesUI.Activas ? t + Time.unscaledDeltaTime / duracion : 1f;
            float e = AnimacionesUI.Cajon(Mathf.Clamp01(t));
            panelRaiz.anchoredPosition = new Vector2(Mathf.LerpUnclamped(inicioX, destinoX, e), 0f);
            yield return null;
        }

        if (!abierto) panelRaiz.gameObject.SetActive(false);
        animacion = null;
    }
}
