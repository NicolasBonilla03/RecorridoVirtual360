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

    [Tooltip("Mensaje del globo que invita a usar la guía.")]
    public string textoBurbuja = "¿Buscas una oficina o un servicio? Pregúntame.";

    public string titulo = "¿En qué te ayudo?";
    public string subtitulo = "Toca una pregunta para ver la respuesta.";

    const int OrdenCanvas = 55;                      // sobre el menú (50), bajo la ficha (60)
    const float AnchoPanel = 400f;
    const float MargenPanel = MarcaUdB.Space4;
    const float MargenAncho = MarcaUdB.Space8;
    const float AltoControl = 48f;
    const float AnchoBoton = 112f;
    const float AltoBotonGuia = MarcaUdB.AltoControlFlotante; // mismo alto que el botón de menú
    const float DiametroIcono = 28f;
    const string EtiquetaCompacta = "Guía";
>>>>>>> Juanfe
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

    // Una categoría plegable: su encabezado y las preguntas que contiene
    class Grupo
    {
        public RectTransform flecha;
        public bool abierto;
        public readonly List<Fila> filas = new List<Fila>();
    }

    RectTransform canvasRT;
    RectTransform zonaSegura;
    RectTransform panelRaiz;
    GameObject velo;
    GameObject botonGuia;
    GameObject sombraBoton;
    RectTransform botonRT;
    Vector2 desfaseSombra;
    Image halo;
    RectTransform haloRT;
    RectTransform burbujaRT;
    CanvasGroup burbuja;
    RectMask2D recorteBurbuja;
    Image fondoBurbuja;
    RectTransform textoBurbujaRT;
    float anchoBurbuja = 260f;
    float progresoBurbuja;
    PreguntaGuia preguntaMensaje;                // pregunta del mensaje actual (null = invitación general)
    readonly HashSet<PreguntaGuia> yaMostradas = new HashSet<PreguntaGuia>();
    float anchoBotonGuia = AnchoBoton;
    TextMeshProUGUI textoBotonGuia;
    string etiquetaAncha;
    TextMeshProUGUI textoBurbujaTMP;
    Image imagenBotonGuia;
    RectTransform iconoGuiaRT;
    Image iconoGuia;
    TextMeshProUGUI signoGuia;
    bool vertical;
    bool yaAbrioGuia;
>>>>>>> Juanfe
    ScrollRect scroll;
    RectTransform contenidoLista;
    float anchoLista;
    string lugarLista;
    readonly List<Fila> filas = new List<Fila>();
    readonly List<Grupo> grupos = new List<Grupo>();

    bool abierto;
    Coroutine animacion;
    Vector2 ultimoTamano;
    Rect ultimaAreaSegura;
    float ultimaHoja = -1f;
    bool botonOculto;                            // true mientras otro panel está abierto (el botón se funde)

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

        // También cuando la hoja del celular cambia de alto: el botón va apoyado encima de ella
        if (canvasRT.rect.size != ultimoTamano || Screen.safeArea != ultimaAreaSegura
            || !Mathf.Approximately(MarcaUdB.AltoHojaMovil, ultimaHoja))
            Acomodar();

        // Mientras otro panel está abierto el botón se funde (160 ms al volver, 140 ms al irse), al mismo ritmo
        // que el velo. En un cambio de lugar ya no se esconde: la pantalla del fundido lo tapa junto con todo lo
        // demás, así aparece y desaparece a la vez que la foto (antes se apagaba y prendía de golpe).
        bool ocupado = abierto || MenuDesplegable.EstaAbierto || PanelInfo.Abierto || POIController.HayMenuAbierto;
        if (ocupado != botonOculto)
        {
            botonOculto = ocupado;
            if (ocupado)
            {
                VeloSuave.Ocultar(botonGuia);
                if (sombraBoton != null) VeloSuave.Ocultar(sombraBoton);
            }
            else
            {
                VeloSuave.Mostrar(botonGuia);
                if (sombraBoton != null && !vertical) VeloSuave.Mostrar(sombraBoton);
            }
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
        yaAbrioGuia = true;
        velo.SetActive(true);
=======
        yaAbrioGuia = true;
        VeloSuave.Mostrar(velo);
        // En el celular vertical el panel nace del botón redondo: arranca un poco más pequeño y transparente
        if (MarcaUdB.EsMovilVertical(canvasRT) && !panelRaiz.gameObject.activeSelf && AnimacionesUI.Activas)
        {
            GrupoPanel().alpha = 0f;
            panelRaiz.localScale = new Vector3(EscalaInicial, EscalaInicial, 1f);
        }
>>>>>>> Stashed changes
        // El panel se activa antes de armar la lista: un texto inactivo no se puede medir bien
        panelRaiz.gameObject.SetActive(true);
        Acomodar();
        LlenarLista();
        ActualizarLlevame();
        Reacomodar();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        Animar(-MargenPanel);
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;
        VeloSuave.Ocultar(velo);
        Animar(panelRaiz.sizeDelta.x + 80f);
    }

    // Celular vertical: el panel crece desde la esquina del botón redondo (origen en el disparador).
    // Escritorio y horizontal: se desliza desde la derecha como un cajón.
    void Animar(float destinoX)
    {
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(vertical ? Crecer() : Deslizar(destinoX));
    }

    const float EscalaInicial = 0.95f;   // nunca desde cero: arranca casi de su tamaño
    const float EscalaSalida = 0.97f;

    CanvasGroup grupoPanel;
    CanvasGroup GrupoPanel()
    {
        if (grupoPanel == null) grupoPanel = panelRaiz.GetComponent<CanvasGroup>();
        if (grupoPanel == null) grupoPanel = panelRaiz.gameObject.AddComponent<CanvasGroup>();
        return grupoPanel;
    }

    // Pivote del panel: abajo a la derecha (junto al botón) en el celular vertical; centrado a la derecha en el resto
    void PonerPivotePanel(bool desdeBoton)
    {
        if (desdeBoton)
        {
            panelRaiz.pivot = new Vector2(1f, 0f);
            panelRaiz.anchoredPosition = new Vector2(-MargenPanel, MargenPanel);
        }
        else
        {
            panelRaiz.pivot = new Vector2(1f, 0.5f);
            panelRaiz.anchoredPosition = new Vector2(panelRaiz.anchoredPosition.x, 0f);
        }
    }

    IEnumerator Crecer()
    {
        panelRaiz.gameObject.SetActive(true);
        PonerPivotePanel(true);
        CanvasGroup g = GrupoPanel();
        bool abrir = abierto;
        float a0 = g.alpha, a1 = abrir ? 1f : 0f;
        float s0 = panelRaiz.localScale.x, s1 = abrir ? 1f : EscalaSalida;
        float duracion = abrir ? 0.22f : 0.15f;
        float t = 0f;

        while (t < 1f)
        {
            t = AnimacionesUI.Activas ? t + Time.unscaledDeltaTime / duracion : 1f;
            float e = AnimacionesUI.SalidaFuerte(Mathf.Clamp01(t));
            g.alpha = Mathf.Lerp(a0, a1, e);
            float s = Mathf.Lerp(s0, s1, e);
            panelRaiz.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        if (!abrir)
        {
            panelRaiz.gameObject.SetActive(false);
            panelRaiz.localScale = Vector3.one;
        }
        animacion = null;
    }

    IEnumerator Deslizar(float destinoX)
    {
        panelRaiz.gameObject.SetActive(true);
        PonerPivotePanel(false);
        GrupoPanel().alpha = 1f;
        panelRaiz.localScale = Vector3.one;
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

    void Acomodar()
    {
        ultimoTamano = canvasRT.rect.size;
        ultimaAreaSegura = Screen.safeArea;
        ultimaHoja = MarcaUdB.AltoHojaMovil;
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        bool compacto = MarcaUdB.EsCompacto(canvasRT);
        vertical = MarcaUdB.EsMovilVertical(canvasRT);
        float margen = compacto ? MarcaUdB.Space4 : MargenAncho;

        // El botón comparte la fila inferior con la nota legal (izquierda): mismo margen en los dos modos.
        // Celular vertical: botón redondo solo con el «?». Celular horizontal: «Guía». Escritorio: «Pregúntale al guía».
        AjustarBoton(compacto, vertical);
        // En el celular vertical el botón flota encima de la hoja del lugar, como el botón redondo de Street View
        botonRT.anchoredPosition = new Vector2(-margen, vertical ? MarcaUdB.AltoHojaMovil + MarcaUdB.Space3 : margen);
        if (sombraBoton != null)
            ((RectTransform)sombraBoton.transform).anchoredPosition = botonRT.anchoredPosition + desfaseSombra;
        if (haloRT != null)
        {
            haloRT.sizeDelta = botonRT.sizeDelta;
            haloRT.anchoredPosition = botonRT.anchoredPosition + new Vector2(-botonRT.sizeDelta.x * 0.5f, botonRT.sizeDelta.y * 0.5f);
        }
        // El mensaje sale del botón: a su izquierda, a su misma altura y con el borde escondido bajo él
        AcomodarBurbuja();
>>>>>>> Juanfe

        float disponible = zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
        float ancho = Mathf.Min(AnchoPanel, disponible - MargenPanel * 2f);
        panelRaiz.sizeDelta = new Vector2(ancho, -MargenPanel * 2f);
        if (animacion == null)
        {
            if (vertical) PonerPivotePanel(true);
            else
            {
                PonerPivotePanel(false);
                GrupoPanel().alpha = 1f;
                panelRaiz.localScale = Vector3.one;
                panelRaiz.anchoredPosition = new Vector2(abierto ? -MargenPanel : ancho + 80f, 0f);
            }
        }

        // Si cambió el ancho con la guía abierta, las filas se vuelven a medir
        if (abierto && contenidoLista != null) { LlenarLista(); ActualizarLlevame(); Reacomodar(); }
    }

    // ------------------------------------------------------------------ preguntas

    void Alternar(Fila f)
    {
        bool abrir = !f.abierta;
        foreach (Fila otra in filas) Poner(otra, false);
        Poner(f, abrir);
        Reacomodar();
        if (abrir) RegistroUso.Anotar("guia", f.datos.pregunta);
    }

    void AlternarGrupo(Grupo g)
    {
        PonerGrupo(g, !g.abierto);
        Reacomodar();
    }

    void PonerGrupo(Grupo g, bool abierto)
    {
        g.abierto = abierto;
        if (g.flecha != null) GiroSuave.Poner(g.flecha, abierto ? -90f : 0f);
        foreach (Fila f in g.filas)
        {
            f.boton.gameObject.SetActive(abierto);
            if (!abierto) Poner(f, false);
        }
    }

    // Recalcula de inmediato las posiciones de la lista, para que nada quede encima de otra cosa
    void Reacomodar()
    {
        if (contenidoLista == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contenidoLista);
    }

    void Poner(Fila f, bool abierta)
    {
        bool estabaAbierta = f.abierta;
        f.abierta = abierta;
        f.respuesta.SetActive(abierta);
        if (abierta && !estabaAbierta && AnimacionesUI.Activas && isActiveAndEnabled)
            StartCoroutine(Aparecer(f.respuesta));
        GiroSuave.Poner(f.flecha, abierta ? -90f : 0f);
    }

    // La respuesta entra con un fundido corto
    IEnumerator Aparecer(GameObject objeto)
    {
        CanvasGroup grupo = objeto.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = objeto.AddComponent<CanvasGroup>();
        float t = 0f;
        const float duracion = 0.18f;
        while (t < 1f && objeto != null && grupo != null)
        {
            t += Time.unscaledDeltaTime / duracion;
            grupo.alpha = AnimacionesUI.SalidaFuerte(Mathf.Clamp01(t));
            yield return null;
        }
        if (grupo != null) grupo.alpha = 1f;
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
        MarcaUdB.EscalarSegunPantalla(escalador);
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
        StartCoroutine(AnimarLlamado());
    }

    // Botón flotante en rojo institucional: es la entrada principal a la guía y debe verse a primera vista
    void ConstruirBoton()
    {
        string etiqueta = string.IsNullOrEmpty(textoBoton) || textoBoton == "Guía" ? "Pregúntale al guía" : textoBoton;
        etiquetaAncha = etiqueta;

        // Halo que late detrás del botón hasta que el usuario abre la guía por primera vez
        halo = CrearImagen("HaloGuia", zonaSegura, MarcaUdB.Rojo);
        halo.raycastTarget = false;
        haloRT = (RectTransform)halo.transform;
        // Anclado como el botón pero con el pivote al centro: el pulso crece parejo hacia todos los lados
        haloRT.anchorMin = haloRT.anchorMax = new Vector2(1f, 0f);
        haloRT.pivot = new Vector2(0.5f, 0.5f);
        MarcaUdB.Pildora(halo, AltoBotonGuia);
        // Sin lienzo propio: probado en Unity, un Canvas anidado en el halo dejaba sin clic al botón en forma
        // de píldora. El pulso es un solo cuadro de imagen y se detiene al abrir la guía, así que su costo es mínimo.
>>>>>>> Juanfe
        Image img;
        Button boton = CrearBoton("BotonGuia", zonaSegura, out img);
        botonRT = (RectTransform)boton.transform;
        botonRT.anchorMin = botonRT.anchorMax = botonRT.pivot = new Vector2(1f, 0f);
        MarcaUdB.Pildora(img, AltoBotonGuia);
        MarcaUdB.ColoresBoton(boton, MarcaUdB.Rojo, MarcaUdB.RojoFuerte, MarcaUdB.RojoFuerte);

        // Círculo blanco con «?» en rojo
        const float diametro = DiametroIcono;
>>>>>>> Juanfe
        Image circulo = CrearImagen("Icono", botonRT, MarcaUdB.InkInverso);
        circulo.raycastTarget = false;
        RectTransform rc = (RectTransform)circulo.transform;
        rc.anchorMin = rc.anchorMax = new Vector2(0f, 0.5f);
        rc.pivot = new Vector2(0f, 0.5f);
        rc.sizeDelta = new Vector2(diametro, diametro);
        rc.anchoredPosition = new Vector2(MarcaUdB.Space2 + 2f, 0f);
        MarcaUdB.Redondear(circulo, diametro * 0.5f);
        TextMeshProUGUI signo = CrearTexto("Signo", rc, "?", MarcaUdB.DisplayExtra, 18f, MarcaUdB.Rojo);
>>>>>>> Juanfe
        signo.alignment = TextAlignmentOptions.Center;
        Estirar((RectTransform)signo.transform, 0f, 0f);
        imagenBotonGuia = img;
        iconoGuiaRT = rc;
        iconoGuia = circulo;
        signoGuia = signo;

        TextMeshProUGUI texto = CrearTexto("Texto", botonRT, etiqueta, MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.InkInverso);
        texto.enableWordWrapping = false;
        Estirar((RectTransform)texto.transform, IzquierdaTextoBoton, MarcaUdB.Space4);
        textoBotonGuia = texto;
        AjustarBoton(false, false);
>>>>>>> Juanfe

        boton.onClick.AddListener(Abrir);
        botonGuia = boton.gameObject;

        Image sombra = MarcaUdB.SombraFlotante(botonRT);
        if (sombra != null)
        {
            sombraBoton = sombra.gameObject;
            desfaseSombra = ((RectTransform)sombra.transform).anchoredPosition - botonRT.anchoredPosition;
        }
        // Orden de dibujo: halo, sombra y botón
        haloRT.SetSiblingIndex(0);

        ConstruirBurbuja();
    }

const float IzquierdaTextoBoton = MarcaUdB.Space2 + DiametroIcono + MarcaUdB.Space2;

// Etiqueta y ancho del botón: «Pregúntale al guía» en pantalla ancha, «Guía» en móvil vertical
void AjustarBoton(bool compacto, bool redondo)
{
    if (textoBotonGuia == null || botonRT == null) return;

    // El sprite redondeado se recalcula con el alto: píldora o círculo
    float alto = redondo ? MarcaUdB.DiametroGuiaMovil : AltoBotonGuia;
    if (imagenBotonGuia != null) MarcaUdB.Pildora(imagenBotonGuia, alto);
    if (halo != null) MarcaUdB.Pildora(halo, alto);
    textoBotonGuia.gameObject.SetActive(!redondo);
    if (sombraBoton != null) sombraBoton.SetActive(!redondo && botonGuia != null && botonGuia.activeSelf);

    if (iconoGuiaRT != null)
    {
        float d = redondo ? 36f : DiametroIcono;
        iconoGuiaRT.anchorMin = iconoGuiaRT.anchorMax = new Vector2(redondo ? 0.5f : 0f, 0.5f);
        // Pivote al centro en los dos modos: el ícono gira sobre sí mismo cuando sale un mensaje
        iconoGuiaRT.pivot = new Vector2(0.5f, 0.5f);
        iconoGuiaRT.sizeDelta = new Vector2(d, d);
        iconoGuiaRT.anchoredPosition = redondo ? Vector2.zero : new Vector2(MarcaUdB.Space2 + 2f + d * 0.5f, 0f);
        MarcaUdB.Redondear(iconoGuia, d * 0.5f);
        if (signoGuia != null) signoGuia.fontSize = redondo ? 26f : 18f;
    }

    if (redondo)
    {
        botonRT.sizeDelta = new Vector2(alto, alto);
        if (haloRT != null) haloRT.sizeDelta = botonRT.sizeDelta;
        return;
    }

    string etiqueta = compacto ? EtiquetaCompacta : etiquetaAncha;
    textoBotonGuia.text = etiqueta;
    float anchoTexto = Mathf.Ceil(textoBotonGuia.GetPreferredValues(etiqueta).x);
    if (anchoTexto <= 1f) anchoTexto = etiqueta.Length * 9f;
    anchoBotonGuia = Mathf.Max(compacto ? 0f : AnchoBoton, IzquierdaTextoBoton + anchoTexto + MarcaUdB.Space4);
    botonRT.sizeDelta = new Vector2(anchoBotonGuia, AltoBotonGuia);
    if (haloRT != null) haloRT.sizeDelta = botonRT.sizeDelta;
    if (sombraBoton != null)
        ((RectTransform)sombraBoton.transform).sizeDelta = botonRT.sizeDelta + new Vector2(16f, 16f);
}

// ------------------------------------------------------------------ mensajes que salen del botón

// El globo nace detrás del botón: su borde derecho queda escondido bajo el botón y se despliega hacia
// la izquierda (recorte que se abre), y al irse se recoge de izquierda a derecha hasta meterse en él.
// Así se lee que el mensaje sale del guía. Tocarlo abre el guía (con la pregunta ya desplegada).
void ConstruirBurbuja()
{
    GameObject goRaiz = new GameObject("MensajeGuia", typeof(RectTransform), typeof(RectMask2D), typeof(CanvasGroup));
    burbujaRT = (RectTransform)goRaiz.transform;
    burbujaRT.SetParent(zonaSegura, false);
    burbujaRT.anchorMin = burbujaRT.anchorMax = new Vector2(1f, 0f);
    burbujaRT.pivot = new Vector2(1f, 0.5f);
    recorteBurbuja = goRaiz.GetComponent<RectMask2D>();
    burbuja = goRaiz.GetComponent<CanvasGroup>();
    burbuja.alpha = 0f;
    burbuja.blocksRaycasts = false;

    Image fondo = CrearImagen("Fondo", burbujaRT, MarcaUdB.Surface100);
    // El fondo deja un margen de SombraMensaje dentro del recorte: ahí cabe su sombra sin cortarse
    RectTransform rtFondo = (RectTransform)fondo.transform;
    rtFondo.anchorMin = Vector2.zero;
    rtFondo.anchorMax = Vector2.one;
    rtFondo.pivot = new Vector2(0.5f, 0.5f);
    rtFondo.offsetMin = new Vector2(SombraMensaje, SombraMensaje);
    rtFondo.offsetMax = new Vector2(-SombraMensaje, -SombraMensaje);
    MarcaUdB.SombraFlotante(rtFondo);
    fondoBurbuja = fondo;
    Button boton = fondo.gameObject.AddComponent<Button>();
    boton.targetGraphic = fondo;
    SinNavegacion(boton);
    MarcaUdB.ColoresBoton(boton, MarcaUdB.Surface100, MarcaUdB.Surface200, MarcaUdB.Surface300);
    boton.onClick.AddListener(AbrirDesdeMensaje);

    // Filete rojo a la izquierda, como en el encabezado de los paneles
    Image filete = CrearImagen("Filete", fondo.transform, MarcaUdB.Rojo);
    filete.raycastTarget = false;
    RectTransform rf = (RectTransform)filete.transform;
    rf.anchorMin = new Vector2(0f, 0f); rf.anchorMax = new Vector2(0f, 1f);
        filete.raycastTarget = false;
        RectTransform rf = (RectTransform)filete.transform;
        rf.anchorMin = new Vector2(0f, 0f); rf.anchorMax = new Vector2(0f, 1f);
        rf.pivot = new Vector2(0f, 0.5f);
        rf.pivot = new Vector2(0f, 0.5f);
        rf.sizeDelta = new Vector2(4f, -MarcaUdB.Space4 * 2f);
        rf.anchoredPosition = new Vector2(MarcaUdB.Space4, 0f);
        MarcaUdB.Redondear(filete, 2f);

        TextMeshProUGUI t = CrearTexto("Texto", fondo.transform, textoBurbuja, MarcaUdB.TextoBold, MarcaUdB.UIControlS, MarcaUdB.Ink);
        t.enableWordWrapping = true;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        textoBurbujaTMP = t;
        textoBurbujaRT = (RectTransform)t.transform;

        // Detrás del botón (y delante del halo): el botón tapa el borde por donde sale el globo
        if (haloRT != null) burbujaRT.SetSiblingIndex(haloRT.GetSiblingIndex() + 1);
        burbujaRT.gameObject.SetActive(false);
    }

    const float AnchoMaximoMensaje = 260f;   // parte visible del globo, sin contar lo que queda bajo el botón
    const float IzquierdaMensaje = MarcaUdB.Space4 + 4f + MarcaUdB.Space3;
    const float SombraMensaje = 8f;          // margen del recorte para la sombra del globo

    // Tamaño y lugar del globo según el texto y el botón actual
    void AcomodarBurbuja()
    {
        if (burbujaRT == null || botonRT == null || textoBurbujaTMP == null || textoBurbujaRT == null) return;
        float margen = MarcaUdB.EsCompacto(canvasRT) ? MarcaUdB.Space4 : MargenAncho;
        float escondido = botonRT.sizeDelta.x * 0.5f;   // lo que queda tapado por el botón
        float disponible = (zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width)
                           - margen * 2f - botonRT.sizeDelta.x - MarcaUdB.Space2;
        float derecha = escondido + MarcaUdB.Space4;
        float anchoTexto = Mathf.Min(AnchoMaximoMensaje, disponible) - IzquierdaMensaje - MarcaUdB.Space4;
        anchoTexto = Mathf.Max(120f, Mathf.Min(anchoTexto, Mathf.Ceil(textoBurbujaTMP.GetPreferredValues(textoBurbujaTMP.text).x) + 2f));
        float altoTexto = Mathf.Ceil(textoBurbujaTMP.GetPreferredValues(textoBurbujaTMP.text, anchoTexto, 0f).y);

        float alto = Mathf.Max(botonRT.sizeDelta.y, altoTexto + MarcaUdB.Space3 * 2f);
        anchoBurbuja = IzquierdaMensaje + anchoTexto + MarcaUdB.Space4 + derecha;
        burbujaRT.sizeDelta = new Vector2(anchoBurbuja + SombraMensaje * 2f, alto + SombraMensaje * 2f);
        // El borde derecho del globo queda en el centro del botón, a su misma altura
        burbujaRT.anchoredPosition = botonRT.anchoredPosition + new Vector2(-escondido + SombraMensaje, botonRT.sizeDelta.y * 0.5f);
        MarcaUdB.Pildora(fondoBurbuja, alto);

        textoBurbujaRT.anchorMin = new Vector2(0f, 0f);
        textoBurbujaRT.anchorMax = new Vector2(1f, 1f);
        textoBurbujaRT.pivot = new Vector2(0.5f, 0.5f);
        textoBurbujaRT.offsetMin = new Vector2(IzquierdaMensaje, 0f);
        textoBurbujaRT.offsetMax = new Vector2(-derecha, 0f);
        AplicarDespliegue(progresoBurbuja);
    }

    // 0 = metido en el botón, 1 = desplegado. Solo cambia el recorte, la opacidad y la posición del texto.
    void AplicarDespliegue(float p)
    {
        if (burbujaRT == null || recorteBurbuja == null || textoBurbujaRT == null) return;
        float e = Mathf.Clamp01(p);
        // Lo que no se ve se recorta desde la izquierda: el globo crece hacia la izquierda desde el botón
        recorteBurbuja.padding = new Vector4((1f - e) * (anchoBurbuja + SombraMensaje * 2f), 0f, 0f, 0f);
        burbuja.alpha = Mathf.Clamp01(e * 1.6f);
        // El texto viaja un poco con el despliegue: acompaña el movimiento de salida del botón
        textoBurbujaRT.anchoredPosition = new Vector2((1f - e) * 24f, 0f);
        // El «?» gira como una rueda que suelta el mensaje: una vuelta a la izquierda mientras sale y de
        // regreso a la derecha mientras se recoge. Va atado al mismo progreso, así que también se interrumpe.
        if (iconoGuiaRT != null) iconoGuiaRT.localEulerAngles = new Vector3(0f, 0f, 360f * e);
    }

    // Animación interrumpible: parte del punto actual. Entra en 280 ms y se recoge en 200 ms.
    IEnumerator Desplegar(bool mostrar)
    {
        if (mostrar) burbujaRT.gameObject.SetActive(true);
        burbuja.blocksRaycasts = mostrar;
        float desde = progresoBurbuja, hasta = mostrar ? 1f : 0f;
        float duracion = (mostrar ? 0.28f : 0.2f) * Mathf.Abs(hasta - desde);
        float t = 0f;
        while (t < 1f && AnimacionesUI.Activas && duracion > 0f)
        {
            t += Time.unscaledDeltaTime / duracion;
            progresoBurbuja = Mathf.Lerp(desde, hasta, AnimacionesUI.SalidaFuerte(Mathf.Clamp01(t)));
            AplicarDespliegue(progresoBurbuja);
            yield return null;
        }
        progresoBurbuja = hasta;
        AplicarDespliegue(hasta);
        if (!mostrar) burbujaRT.gameObject.SetActive(false);
    }

    // El «?» da un pequeño salto cuando sale un mensaje: el ojo va al botón y ve salir el globo de ahí
    IEnumerator Saltar()
    {
        if (iconoGuiaRT == null || !AnimacionesUI.Activas) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.3f;
            float f = Mathf.Clamp01(t);
            // Sube en el primer 40 % (salida fuerte) y vuelve en el resto
            float s = f < 0.4f ? Mathf.Lerp(1f, 1.18f, AnimacionesUI.SalidaFuerte(f / 0.4f))
                               : Mathf.Lerp(1.18f, 1f, AnimacionesUI.SalidaFuerte((f - 0.4f) / 0.6f));
            iconoGuiaRT.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        iconoGuiaRT.localScale = Vector3.one;
    }

    // Halo rojo: dos pulsos mientras el mensaje está afuera
    IEnumerator Pulsar()
    {
        if (halo == null) yield break;
        float inicio = Time.unscaledTime;
        halo.gameObject.SetActive(true);
        while (Time.unscaledTime - inicio < 2f * 2.4f && burbujaRT.gameObject.activeSelf)
        {
            float f = ((Time.unscaledTime - inicio) % 2.4f) / 1.4f;
            if (f <= 1f)
            {
                float e = 1f - Mathf.Pow(1f - f, 2f);
                float ex = 10f / Mathf.Max(1f, haloRT.sizeDelta.x), ey = 10f / Mathf.Max(1f, haloRT.sizeDelta.y);
                haloRT.localScale = new Vector3(1f + ex * 2f * e, 1f + ey * 2f * e, 1f);
                Color c = MarcaUdB.Rojo; c.a = 0.35f * (1f - f);
                halo.color = c;
            }
            else halo.color = new Color(0f, 0f, 0f, 0f);
            yield return null;
        }
        halo.gameObject.SetActive(false);
    }

    // Tocar el mensaje abre el guía; si el mensaje era una pregunta, ya aparece con su respuesta abierta
    void AbrirDesdeMensaje()
    {
        PreguntaGuia p = preguntaMensaje;
        Abrir();
        if (p == null) return;
        foreach (Grupo g in grupos)
            if (g.filas.Exists(f => f.datos == p) && !g.abierto) PonerGrupo(g, true);
        Fila fila = filas.Find(f => f.datos == p);
        if (fila != null && !fila.abierta) Alternar(fila);
    }

    bool BotonALaVista
    {
        get
        {
            return botonGuia != null && !botonOculto && botonGuia.activeInHierarchy && !abierto
                   && !(FadeController.Instance != null && FadeController.Instance.EnTransicion);
        }
    }

    // Elige el mensaje: la invitación general al empezar y cada cinco mensajes; los demás, una pregunta de
    // este lugar si la hay (o una del catálogo), sin repetir hasta haberlas mostrado todas. Devuelve null si
    // el mensaje es la invitación.
    PreguntaGuia ElegirMensaje(int vez)
    {
        if (vez % 5 == 0) return null;
        Material cielo = RenderSettings.skybox;
        string lugar = cielo != null ? cielo.name : "";
        FichaInfo aqui = CatalogoInfo.PorSkybox(cielo);
        List<PreguntaGuia> candidatas = new List<PreguntaGuia>();
        foreach (PreguntaGuia p in CatalogoInfo.Preguntas)
            if (p != null && !string.IsNullOrEmpty(p.pregunta) && !yaMostradas.Contains(p) && EsDeAqui(p, lugar, aqui))
                candidatas.Add(p);
        if (candidatas.Count == 0)
            foreach (PreguntaGuia p in CatalogoInfo.Preguntas)
                if (p != null && !string.IsNullOrEmpty(p.pregunta) && !yaMostradas.Contains(p)) candidatas.Add(p);
        if (candidatas.Count == 0 && yaMostradas.Count > 0)
        {
            // Ya salieron todas: se empieza otra vuelta
            yaMostradas.Clear();
            return ElegirMensaje(vez);
        }
        if (candidatas.Count == 0) return null;
        PreguntaGuia elegida = candidatas[Random.Range(0, candidatas.Count)];
        yaMostradas.Add(elegida);
        return elegida;
    }

    // Llamados del guía: el primero al empezar y después se repiten durante todo el recorrido, cada 25 s o
    // apenas el usuario llega a un lugar nuevo (si ya pasaron 10 s desde el anterior). Cuando el usuario ya
    // abrió el guía alguna vez, siguen saliendo pero más espaciados (60 s, o 30 s al cambiar de lugar).
    // Mientras el guía o un menú están abiertos no sale nada. Entre un mensaje y otro la corrutina solo revisa
    // cuatro veces por segundo: no trabaja cada cuadro.
    IEnumerator AnimarLlamado()
    {
        const float Visible = 8f;
        WaitForSecondsRealtime revisar = new WaitForSecondsRealtime(0.25f);

        yield return new WaitForSecondsRealtime(2.5f);
        // Primero la pista de uso, después el globo: así nunca hay dos avisos a la vez en pantalla
        while (AplicarMarcaUdB.PistaVisible) yield return null;
        yield return new WaitForSecondsRealtime(0.6f);

        for (int vez = 0; ; vez++)
        {
            float desde = Time.unscaledTime;
            Material lugarAnterior = RenderSettings.skybox;
            bool porCambioDeLugar = false;
            while (true)
            {
                float cada = yaAbrioGuia ? 60f : 25f;
                float minimoAlLlegar = yaAbrioGuia ? 30f : 10f;
                float espera = Time.unscaledTime - desde;
                porCambioDeLugar = RenderSettings.skybox != lugarAnterior && espera >= minimoAlLlegar;
                bool toca = vez == 0 || espera >= cada || porCambioDeLugar;
                if (toca && BotonALaVista) break;
                yield return revisar;
            }
            if (porCambioDeLugar) yield return new WaitForSecondsRealtime(1.2f); // deja ver el lugar nuevo primero
            if (!BotonALaVista) { vez--; continue; }

            preguntaMensaje = ElegirMensaje(vez);
            textoBurbujaTMP.text = preguntaMensaje != null ? preguntaMensaje.pregunta : textoBurbuja;
            AcomodarBurbuja();
            StartCoroutine(Saltar());
            StartCoroutine(Pulsar());
            yield return Desplegar(true);

            float inicio = Time.unscaledTime;
            while (Time.unscaledTime - inicio < Visible && BotonALaVista) yield return null;
            yield return Desplegar(false);
        }
    }
>>>>>>> Juanfe
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

    // Las filas se arman con el ancho real del panel: cada una mide lo que mide su texto.
    // Arriba van las preguntas del lugar donde está el usuario; debajo, el resto, en categorías plegables.
    void LlenarLista()
    {
        float ancho = AnchoLista();
        Material cielo = RenderSettings.skybox;
        string lugar = cielo != null ? cielo.name : "";
        if (filas.Count > 0 && Mathf.Abs(ancho - anchoLista) < 0.5f && lugar == lugarLista) return;
        anchoLista = ancho;
        lugarLista = lugar;

        for (int i = contenidoLista.childCount - 1; i >= 0; i--)
        {
            GameObject viejo = contenidoLista.GetChild(i).gameObject;
            viejo.SetActive(false);
            Destroy(viejo);
        }
        filas.Clear();
        grupos.Clear();

        // 1. Preguntas de este lugar (o de este edificio)
        FichaInfo aqui = CatalogoInfo.PorSkybox(cielo);
        List<PreguntaGuia> locales = new List<PreguntaGuia>();
        foreach (PreguntaGuia p in CatalogoInfo.Preguntas)
        {
            if (p == null || string.IsNullOrEmpty(p.pregunta)) continue;
            if (EsDeAqui(p, lugar, aqui)) locales.Add(p);
        }

        if (locales.Count > 0)
        {
            Rotulo("Sobre este lugar", true);
            foreach (PreguntaGuia p in locales) CrearFila(contenidoLista, p, ancho);
            Rotulo("Más preguntas", false);
        }

        // 2. El resto, por categoría. Cada categoría se abre al tocarla.
        Grupo grupo = null;
        string categoria = null;
        foreach (PreguntaGuia p in CatalogoInfo.Preguntas)
        {
            if (p == null || string.IsNullOrEmpty(p.pregunta) || locales.Contains(p)) continue;

            string c = string.IsNullOrEmpty(p.categoria) ? "Otras preguntas" : p.categoria;
            if (grupo == null || c != categoria)
            {
                categoria = c;
                grupo = CrearGrupo(c, ContarCategoria(c, locales), ancho);
            }
            int antes = filas.Count;
            CrearFila(contenidoLista, p, ancho);
            if (filas.Count > antes) grupo.filas.Add(filas[filas.Count - 1]);
        }

        // Si no hay preguntas del lugar (por ejemplo en la vista de inicio), se abre la primera categoría
        for (int i = 0; i < grupos.Count; i++)
            PonerGrupo(grupos[i], locales.Count == 0 && i == 0);
    }

    static bool EsDeAqui(PreguntaGuia p, string lugar, FichaInfo aqui)
    {
        if (!string.IsNullOrEmpty(p.skybox) && p.skybox == lugar) return true;
        if (aqui == null || string.IsNullOrEmpty(p.ficha)) return false;
        if (p.ficha == aqui.id) return true;

        // Mismo edificio o zona que el lugar actual
        FichaInfo f = CatalogoInfo.PorId(p.ficha);
        return f != null && !string.IsNullOrEmpty(aqui.zona) && f.zona == aqui.zona;
    }

    static int ContarCategoria(string categoria, List<PreguntaGuia> excluidas)
    {
        int n = 0;
        foreach (PreguntaGuia p in CatalogoInfo.Preguntas)
        {
            if (p == null || string.IsNullOrEmpty(p.pregunta) || excluidas.Contains(p)) continue;
            string c = string.IsNullOrEmpty(p.categoria) ? "Otras preguntas" : p.categoria;
            if (c == categoria) n++;
        }
        return n;
    }

    void Rotulo(string texto, bool primero)
    {
        TextMeshProUGUI rot = CrearTexto("Rotulo", contenidoLista, texto, null, 0f, Color.white);
        MarcaUdB.EstiloEtiqueta(rot, MarcaUdB.InkMuted);
        rot.margin = new Vector4(MarcaUdB.Space1, 0f, 0f, 0f);
        rot.alignment = TextAlignmentOptions.BottomLeft;
        LayoutElement lr = rot.gameObject.AddComponent<LayoutElement>();
        lr.minHeight = lr.preferredHeight = primero ? 20f : 34f;
    }

    // Encabezado de categoría: nombre, cantidad de preguntas y flecha
    Grupo CrearGrupo(string nombre, int cantidad, float ancho)
    {
        Grupo g = new Grupo();

        Image img;
        Button cabecera = CrearBoton("Categoria", contenidoLista, out img);
        MarcaUdB.Redondear(img, MarcaUdB.RadiusMd);
        MarcaUdB.ColoresBoton(cabecera, MarcaUdB.Surface200, MarcaUdB.Surface300, MarcaUdB.Surface300);
        LayoutElement le = cabecera.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = AltoControl;

        TextMeshProUGUI texto = CrearTexto("Texto", cabecera.transform, nombre, MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.Ink);
        Estirar((RectTransform)texto.transform, MarcaUdB.Space4, MarcaUdB.Space8 + 36f);
        MarcaUdB.AjustarACaja(texto, false, 0.8f);

        TextMeshProUGUI cuenta = CrearTexto("Cantidad", cabecera.transform, cantidad.ToString(), MarcaUdB.Texto, MarcaUdB.CuerpoS, MarcaUdB.InkMuted);
        cuenta.alignment = TextAlignmentOptions.Center;
        RectTransform rc = (RectTransform)cuenta.transform;
        rc.anchorMin = rc.anchorMax = new Vector2(1f, 0.5f);
        rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = new Vector2(32f, 24f);
        rc.anchoredPosition = new Vector2(-MarcaUdB.Space5 - 30f, 0f);

        TextMeshProUGUI flecha = CrearTexto("Flecha", cabecera.transform, ">", MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.Ink);
        flecha.alignment = TextAlignmentOptions.Center;
        RectTransform rtF = (RectTransform)flecha.transform;
        rtF.anchorMin = rtF.anchorMax = new Vector2(1f, 0.5f);
        rtF.pivot = new Vector2(0.5f, 0.5f);
        rtF.sizeDelta = new Vector2(24f, 24f);
        rtF.anchoredPosition = new Vector2(-MarcaUdB.Space5, 0f);
        g.flecha = rtF;

        cabecera.onClick.AddListener(() => AlternarGrupo(g));
        grupos.Add(g);
        return g;
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
        MarcaUdB.AjustarACaja(t, false, 0.75f);
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
