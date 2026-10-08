using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica el Manual de Marca UdB (vía el Sistema UdB Digital) a la interfaz del recorrido:
/// tipografías Lato y Raleway, colores institucionales, colores por zona (edificio),
/// hotspots en rojo con halo, paneles en vidrio, logotipo oficial, la nota obligatoria
/// «Vigilada Mineducación» y una pista de uso para la primera vez.
/// Se adapta a móvil: área segura y versión compacta en pantallas angostas.
/// </summary>
public class AplicarMarcaUdB : MonoBehaviour
{
    [Header("Identificación institucional")]
    [Tooltip("Identificador OFICIAL horizontal a color, tal como se descarga del micrositio de marca. " +
             "Si está vacío se usa el que viene en Assets/Marca/Resources/MarcaUdB/Logotipos; " +
             "si tampoco existe, se muestra el nombre en tipografía plana.")]
    public Sprite logotipoHorizontal;

    [Tooltip("Ancho del logotipo en px en pantallas anchas. El manual pide mínimo 152 px para el horizontal.")]
    public float anchoLogotipo = 260f;

    [Tooltip("Ancho del logotipo en móvil vertical (mínimo 152 px).")]
    public float anchoLogotipoCompacto = 168f;

    public string nombreInstitucion = "Universidad de Boyacá";
    public string nombreRecorrido = "Recorrido virtual 360°";
    public bool mostrarCabecera = true;

    [Tooltip("Obligatorio en toda pieza (Resolución 12220 de 2016). No se esconde.")]
    public bool mostrarVigilada = true;

    [Header("Zonas del recorrido")]
    [Tooltip("Color de cada gran parte del recorrido. «Contiene» se busca en el nombre del edificio " +
             "(palabras separadas por |). Colores de la paleta del Manual de Marca.")]
    public List<ZonaRecorrido> zonas = MarcaUdB.ZonasPorDefecto();

    [Header("Pista de uso")]
    public bool mostrarPista = true;
    public string textoPista = "Arrastra para mirar alrededor · toca un punto para avanzar";
    [Tooltip("Pista más corta para pantallas angostas (celular en vertical).")]
    public string textoPistaMovil = "Desliza para mirar · toca un punto para avanzar";
    [Tooltip("Segundos antes de que la pista se esconda sola (también se esconde al primer toque).")]
    public float duracionPista = 8f;

    [Header("Qué se estiliza")]
    public bool aplicarFuentes = true;
    public bool estilizarHotspots = true;
    public bool estilizarPOI = true;
    public int ordenCanvas = 40;

    RectTransform canvasRT;
    RectTransform zonaSegura;
    RectTransform cabecera;
    RectTransform sombraCabecera;
    Vector2 desfaseSombra;
    Image logo;
    Sprite logotipoUsado;
    RectTransform vigilada;
    RectTransform pista;
    CanvasGroup grupoPista;
    Vector2 ultimoTamano;
    Rect ultimaAreaSegura;
    bool compacto;
    float tiempoInicio;
    bool pistaOculta;

    void Awake()
    {
        MarcaUdB.RegistrarZonas(zonas);
    }

    IEnumerator Start()
    {
        AplicarTodo();
        ConstruirChrome();
        tiempoInicio = Time.unscaledTime;

        // POIController y otros scripts crean botones en su Start: segunda pasada un cuadro después
        yield return null;
        AplicarTodo();
    }

    void Update()
    {
        if (canvasRT != null && (canvasRT.rect.size != ultimoTamano || Screen.safeArea != ultimaAreaSegura))
        {
            ultimoTamano = canvasRT.rect.size;
            ultimaAreaSegura = Screen.safeArea;
            AplicarLayout();
        }

        // La pista se va al primer toque o clic, o cuando pasa su tiempo
        if (pista != null && !pistaOculta)
        {
            bool interaccion = Input.GetMouseButtonDown(0) || Input.touchCount > 0;
            if (interaccion || Time.unscaledTime - tiempoInicio > duracionPista)
            {
                pistaOculta = true;
                StartCoroutine(OcultarPista());
            }
        }
    }

    public void AplicarTodo()
    {
        if (estilizarHotspots) EstilizarHotspots();
        if (estilizarPOI) EstilizarPOIs();
        if (aplicarFuentes) AplicarFuentes();
    }

    // ------------------------------------------------------------------ tipografía

    void AplicarFuentes()
    {
        TMP_FontAsset regular = MarcaUdB.Texto;
        TMP_FontAsset bold = MarcaUdB.TextoBold;
        if (regular == null) return;

        foreach (TMP_Text t in FindObjectsOfType<TMP_Text>(true))
        {
            if (MarcaUdB.EsFuenteDeMarca(t.font)) continue;

            bool esBold = (t.fontStyle & FontStyles.Bold) == FontStyles.Bold;
            if (esBold && bold != null)
            {
                t.font = bold;
                t.fontStyle &= ~FontStyles.Bold; // el peso lo da la fuente, no una negrita sintética
            }
            else
            {
                t.font = regular;
            }
        }
    }

    // ------------------------------------------------------------------ hotspots

    void EstilizarHotspots()
    {
        foreach (TeleportPoint tp in FindObjectsOfType<TeleportPoint>(true))
        {
            Image img = tp.GetComponent<Image>();
            if (img != null && MarcaUdB.Hotspot != null)
            {
                img.sprite = MarcaUdB.Hotspot;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }

            // Rojo institucional como acento; hover y pressed hacia udb-rojo-fuerte
            Button b = tp.GetComponent<Button>();
            if (b != null)
                MarcaUdB.ColoresBoton(b, MarcaUdB.Rojo, MarcaUdB.RojoFuerte, MarcaUdB.RojoFuerte);
            else if (img != null)
                img.color = MarcaUdB.Rojo;

            foreach (TMP_Text t in tp.GetComponentsInChildren<TMP_Text>(true))
            {
                if (MarcaUdB.TextoBold != null) t.font = MarcaUdB.TextoBold;
                t.fontStyle &= ~FontStyles.Bold;
                t.color = MarcaUdB.InkInverso;
            }
        }
    }

    // ------------------------------------------------------------------ POI y su panel

    void EstilizarPOIs()
    {
        foreach (POIController poi in FindObjectsOfType<POIController>(true))
        {
            Color zona = MarcaUdB.ColorZona(poi.nombreEdificio);

            // El punto de información: círculo con el color de su edificio, texto legible encima
            Image img = poi.GetComponent<Image>();
            if (img != null)
            {
                RectTransform rt = (RectTransform)poi.transform;
                MarcaUdB.Pildora(img, Mathf.Min(rt.rect.width, rt.rect.height));
            }
            Button bp = poi.GetComponent<Button>();
            Color oscuro = Color.Lerp(zona, MarcaUdB.Negro, 0.2f);
            if (bp != null)
                MarcaUdB.ColoresBoton(bp, zona, oscuro, oscuro);
            else if (img != null)
                img.color = zona;

            foreach (Transform hijo in poi.transform)
            {
                if (poi.panelMenu != null && hijo.gameObject == poi.panelMenu) continue;
                TMP_Text t = hijo.GetComponent<TMP_Text>();
                if (t != null)
                {
                    if (MarcaUdB.TextoBold != null) t.font = MarcaUdB.TextoBold;
                    t.fontStyle &= ~FontStyles.Bold;
                    t.color = MarcaUdB.TintaSobre(zona);
                }
            }

            if (poi.panelMenu != null) EstilizarPanel(poi.panelMenu, zona);
        }
    }

    const float AnchoBarraPanel = 10f;

    void EstilizarPanel(GameObject panel, Color zona)
    {
        // Fondo del panel: vidrio-panel, radio grande
        Image fondo = panel.GetComponent<Image>();
        if (fondo != null)
        {
            MarcaUdB.Redondear(fondo, MarcaUdB.RadiusLg);
            fondo.color = MarcaUdB.VidrioPanel;
        }

        // Botones del panel: relleno suave del color del edificio, tinta, hover más intenso
        foreach (Button b in panel.GetComponentsInChildren<Button>(true))
        {
            Image ib = b.targetGraphic as Image;
            if (ib != null) MarcaUdB.Redondear(ib, MarcaUdB.RadiusMd);
            MarcaUdB.ColoresBoton(b, MarcaUdB.Tenue(zona, 0.14f), MarcaUdB.Tenue(zona, 0.26f), MarcaUdB.Tenue(zona, 0.34f));
        }

        // Barra de desplazamiento en el color del edificio: riel suave y manija en píldora,
        // más delgada y separada de la lista
        foreach (Scrollbar sb in panel.GetComponentsInChildren<Scrollbar>(true))
        {
            MarcaUdB.EstilizarBarra(sb, zona, Color.white, AnchoBarraPanel);
            RectTransform rtBarra = (RectTransform)sb.transform;
            rtBarra.anchoredPosition = new Vector2(-MarcaUdB.Space1, rtBarra.anchoredPosition.y);
        }
        foreach (ScrollRect sr in panel.GetComponentsInChildren<ScrollRect>(true))
        {
            sr.verticalScrollbarSpacing = MarcaUdB.Space2;

            // La lista ocupa exactamente el ancho visible: antes era más ancha que su ventana y las
            // filas quedaban cortadas a los lados y debajo de la barra
            RectTransform cont = sr.content;
            if (cont != null)
            {
                cont.anchorMin = new Vector2(0f, 1f);
                cont.anchorMax = new Vector2(1f, 1f);
                cont.pivot = new Vector2(0.5f, 1f);
                cont.sizeDelta = new Vector2(0f, cont.sizeDelta.y);
                cont.anchoredPosition = new Vector2(0f, cont.anchoredPosition.y);

                VerticalLayoutGroup vl = cont.GetComponent<VerticalLayoutGroup>();
                if (vl != null)
                {
                    vl.padding = new RectOffset(6, 6, 4, 8);
                    vl.spacing = 6f;
                    vl.childControlWidth = true;
                    vl.childForceExpandWidth = true;
                }
                LayoutRebuilder.MarkLayoutForRebuild(cont);
            }
        }

        // Área de la lista: sin velo propio, el vidrio del panel ya aísla la lectura
        foreach (ScrollRect sr in panel.GetComponentsInChildren<ScrollRect>(true))
        {
            Image i = sr.GetComponent<Image>();
            if (i != null) i.color = new Color(1f, 1f, 1f, 0f);
        }

        // Textos: tinta; el título (el que no está dentro de un botón) va en la tinta del edificio
        Color tinta = MarcaUdB.TintaLegible(zona);
        foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            bool enBoton = t.GetComponentInParent<Button>() != null;
            t.color = enBoton ? MarcaUdB.Ink : tinta;

            // Ningún texto se sale de su caja: los nombres largos se parten en dos renglones
            // y se encogen lo necesario dentro de la fila; el título se encoge en un renglón
            bool enLista = t.GetComponentInParent<ScrollRect>() != null;
            if (enLista)
            {
                t.margin = new Vector4(16f, 5f, 16f, 5f);
                MarcaUdB.AjustarACaja(t, true, 0.58f);
            }
            else if (!enBoton)
            {
                t.margin = new Vector4(16f, 0f, 16f, 0f);
                MarcaUdB.AjustarACaja(t, false, 0.6f);
            }
        }
    }

    // ------------------------------------------------------------------ cabecera, nota legal y pista

    void ConstruirChrome()
    {
        if (!mostrarCabecera && !mostrarVigilada && !mostrarPista) return;

        GameObject goCanvas = new GameObject("MarcaUdB_UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        goCanvas.transform.SetParent(transform, false);
        Canvas canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvas;
        CanvasScaler escalador = goCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = MarcaUdB.ResolucionReferencia;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        MarcaUdB.EscalarSegunPantalla(escalador);
        canvasRT = (RectTransform)goCanvas.transform;

        GameObject goSegura = new GameObject("AreaSegura", typeof(RectTransform));
        zonaSegura = (RectTransform)goSegura.transform;
        zonaSegura.SetParent(canvasRT, false);
        MarcaUdB.AjustarAreaSegura(zonaSegura);

        // Solo el logotipo recibe clics (vuelve al inicio); el resto no bloquea el arrastre de la cámara
        if (mostrarCabecera) ConstruirCabecera();
        if (mostrarVigilada) ConstruirVigilada();
        if (mostrarPista) ConstruirPista();
    }

    float Margen { get { return compacto ? MarcaUdB.Space4 : MarcaUdB.Space8; } }

    void AplicarLayout()
    {
        MarcaUdB.AjustarAreaSegura(zonaSegura);
        compacto = MarcaUdB.EsCompacto(canvasRT);

        if (cabecera != null)
        {
            if (logo != null && logotipoUsado != null) DimensionarLogo();
            cabecera.anchoredPosition = new Vector2(-Margen, -Margen);
            if (sombraCabecera != null) sombraCabecera.anchoredPosition = cabecera.anchoredPosition + desfaseSombra;
        }

        float bajo = compacto ? MarcaUdB.Space4 : MarcaUdB.Space6;
        if (vigilada != null)
            vigilada.anchoredPosition = new Vector2(Margen, bajo);

        if (pista != null)
        {
            // En móvil la pista va encima de la nota legal; en pantalla ancha, centrada abajo
            float y = compacto ? bajo + 28f + MarcaUdB.Space2 : bajo;
            pista.anchoredPosition = new Vector2(0f, y);
            float disponible = zonaSegura.rect.width > 0f ? zonaSegura.rect.width : canvasRT.rect.width;
            // El primer texto hijo es la flecha: se busca el texto de la pista por su nombre
            Transform hijo = pista.Find("Texto");
            TMP_Text t = hijo != null ? hijo.GetComponent<TMP_Text>() : pista.GetComponentInChildren<TMP_Text>();
            t.text = compacto && !string.IsNullOrEmpty(textoPistaMovil) ? textoPistaMovil : textoPista;
            float ancho = Mathf.Ceil(t.GetPreferredValues(t.text).x) + 18f + MarcaUdB.Space2 + MarcaUdB.Space4 * 2f;
            float maximo = disponible - Margen * 2f;
            if (!compacto) maximo -= 400f; // deja libre la nota legal a la izquierda
            pista.sizeDelta = new Vector2(Mathf.Min(ancho, Mathf.Max(200f, maximo)), 36f);
        }
    }

    void ConstruirCabecera()
    {
        logotipoUsado = logotipoHorizontal != null ? logotipoHorizontal : MarcaUdB.LogotipoHorizontal;

        // Con logotipo, la placa es blanca sólida: el manual pide la versión a color «sobre blanco»
        Image placa = CrearImagen("Cabecera", zonaSegura, logotipoUsado != null ? MarcaUdB.Surface100 : MarcaUdB.VidrioPanel);
        cabecera = (RectTransform)placa.transform;
        cabecera.anchorMin = cabecera.anchorMax = cabecera.pivot = new Vector2(1f, 1f);
        MarcaUdB.Redondear(placa, MarcaUdB.RadiusLg);
        HacerBotonDeInicio(placa);

        if (logotipoUsado != null)
        {
            // Logotipo oficial tal cual: sin recolorear, sin recortar, sin efectos
            logo = CrearImagen("Logotipo", cabecera, Color.white);
            logo.sprite = logotipoUsado;
            logo.preserveAspect = true;
            // El fondo blanco del archivo toma el color de la placa (también al pasar el cursor)
            MarcaUdB.LogoSobreFondo(logo);
            RectTransform rl = (RectTransform)logo.transform;
            rl.anchorMin = rl.anchorMax = rl.pivot = new Vector2(0.5f, 0.5f);
            DimensionarLogo();
        }
        else
        {
            // Sin archivo oficial: nombre en tipografía plana (nunca una imitación del logotipo)
            float padH = MarcaUdB.Space4, padV = MarcaUdB.Space3;
            TMP_Text nombre = CrearTexto("Nombre", cabecera, nombreInstitucion);
            MarcaUdB.Estilo(nombre, MarcaUdB.TextoBold, MarcaUdB.UIControl, MarcaUdB.Negro);
            TMP_Text sub = CrearTexto("Recorrido", cabecera, nombreRecorrido);
            MarcaUdB.EstiloEtiqueta(sub, MarcaUdB.InkMuted);

            Vector2 tn = nombre.GetPreferredValues(nombreInstitucion);
            Vector2 ts = sub.GetPreferredValues(nombreRecorrido);
            float anchoTexto = Mathf.Ceil(Mathf.Max(tn.x, ts.x)) + 2f;

            RectTransform rn = (RectTransform)nombre.transform;
            rn.anchorMin = new Vector2(0f, 1f);
            rn.anchorMax = new Vector2(1f, 1f);
            rn.pivot = new Vector2(0.5f, 1f);
            rn.offsetMin = new Vector2(padH, -padV - 20f);
            rn.offsetMax = new Vector2(-padH, -padV);

            RectTransform rs = (RectTransform)sub.transform;
            rs.anchorMin = new Vector2(0f, 0f);
            rs.anchorMax = new Vector2(1f, 0f);
            rs.pivot = new Vector2(0.5f, 0f);
            rs.offsetMin = new Vector2(padH, padV);
            rs.offsetMax = new Vector2(-padH, padV + 14f);

            cabecera.sizeDelta = new Vector2(anchoTexto + padH * 2f, 20f + 14f + MarcaUdB.Space1 + padV * 2f);
        }

        cabecera.anchoredPosition = new Vector2(-MarcaUdB.Space8, -MarcaUdB.Space8);
        Image sombra = MarcaUdB.SombraFlotante(cabecera);
        if (sombra != null)
        {
            sombraCabecera = (RectTransform)sombra.transform;
            desfaseSombra = sombraCabecera.anchoredPosition - cabecera.anchoredPosition;
        }
    }

    // El logotipo funciona como botón «Ir al inicio». Al pasar el cursor la placa se matiza apenas,
    // aparece el filete rojo de la marca y una pista debajo; todo entra y sale con el mismo fundido.
    void HacerBotonDeInicio(Image placa)
    {
        Color normal = placa.color;
        placa.raycastTarget = true;
        Button boton = placa.gameObject.AddComponent<Button>();
        boton.targetGraphic = placa;
        Navigation nav = boton.navigation;
        nav.mode = Navigation.Mode.None;
        boton.navigation = nav;
        MarcaUdB.ColoresBoton(boton, normal, MarcaUdB.Surface200, MarcaUdB.Surface300);

        boton.onClick.AddListener(IrAlInicioDesdeLogo);

        // Realce: un solo grupo para el filete y la pista, así se funden juntos
        GameObject goRealce = new GameObject("Realce", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform realce = (RectTransform)goRealce.transform;
        realce.SetParent(placa.transform, false);
        realce.anchorMin = Vector2.zero;
        realce.anchorMax = Vector2.one;
        realce.offsetMin = Vector2.zero;
        realce.offsetMax = Vector2.zero;
        CanvasGroup grupo = goRealce.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // Filete rojo de la marca, centrado en el borde inferior de la placa
        Image filete = CrearImagen("Filete", realce, MarcaUdB.Rojo);
        RectTransform rf = (RectTransform)filete.transform;
        rf.anchorMin = rf.anchorMax = new Vector2(0.5f, 0f);
        rf.pivot = new Vector2(0.5f, 0f);
        rf.sizeDelta = new Vector2(40f, 3f);
        rf.anchoredPosition = new Vector2(0f, 5f);
        MarcaUdB.Redondear(filete, 1.5f);

        // Pista «Ir al inicio» bajo la placa, alineada a la derecha
        const string texto = "Ir al inicio";
        float alto = 30f;
        Image chip = CrearImagen("PistaInicio", realce, new Color(MarcaUdB.Negro.r, MarcaUdB.Negro.g, MarcaUdB.Negro.b, 0.92f));
        RectTransform rc = (RectTransform)chip.transform;
        rc.anchorMin = rc.anchorMax = new Vector2(1f, 0f);
        rc.pivot = new Vector2(1f, 1f);
        rc.anchoredPosition = new Vector2(0f, -MarcaUdB.Space2);

        TMP_Text t = CrearTexto("Texto", rc, texto);
        MarcaUdB.Estilo(t, MarcaUdB.TextoBold, MarcaUdB.CuerpoS, MarcaUdB.InkInverso);
        t.alignment = TextAlignmentOptions.Center;
        RectTransform rt = (RectTransform)t.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float ancho = Mathf.Ceil(t.GetPreferredValues(texto).x);
        if (ancho < 4f) ancho = texto.Length * MarcaUdB.CuerpoS * 0.56f;
        rc.sizeDelta = new Vector2(ancho + MarcaUdB.Space4 * 2f, alto);
        MarcaUdB.Pildora(chip, alto);

        placa.gameObject.AddComponent<PistaAlPasar>().pista = grupo;
    }

    void IrAlInicioDesdeLogo()
    {
        if (FadeController.Instance != null && FadeController.Instance.EnTransicion) return;
        if (MenuDesplegable.EnInicio) return;
        POIController.CerrarAbierto();
        PanelInfo.CerrarSiAbierto();
        GuiaVirtual.CerrarSiAbierta();
        MenuDesplegable.IrInicio();
    }

    void DimensionarLogo()
    {
        float proporcion = logotipoUsado.rect.width / Mathf.Max(1f, logotipoUsado.rect.height);
        float anchoLogo = Mathf.Max(152f, compacto ? anchoLogotipoCompacto : anchoLogotipo); // reducción mínima en pantalla
        float altoLogo = anchoLogo / proporcion;

        // Área de protección: 1.5x por los cuatro lados (x = ancho del horizontal / 29.81)
        float proteccion = Mathf.Ceil(1.5f * anchoLogo / 29.81f);
        ((RectTransform)logo.transform).sizeDelta = new Vector2(anchoLogo, altoLogo);
        cabecera.sizeDelta = new Vector2(anchoLogo + proteccion * 2f, altoLogo + proteccion * 2f);

        // La sombra de la placa se reajusta con ella (sombra-flotante: 8 px por lado)
        if (sombraCabecera != null)
            sombraCabecera.sizeDelta = cabecera.sizeDelta + new Vector2(16f, 16f);
    }

    void ConstruirVigilada()
    {
        const string texto = "Vigilada Mineducación";
        float alto = 28f;

        Image chip = CrearImagen("VigiladaMineducacion", zonaSegura, MarcaUdB.VidrioPanel);
        vigilada = (RectTransform)chip.transform;
        vigilada.anchorMin = vigilada.anchorMax = vigilada.pivot = new Vector2(0f, 0f);

        TMP_Text t = CrearTexto("Texto", vigilada, texto);
        MarcaUdB.Estilo(t, MarcaUdB.Texto, MarcaUdB.CuerpoS, MarcaUdB.InkMuted);
        t.alignment = TextAlignmentOptions.Center;
        RectTransform rtt = (RectTransform)t.transform;
        rtt.anchorMin = Vector2.zero;
        rtt.anchorMax = Vector2.one;
        rtt.offsetMin = Vector2.zero;
        rtt.offsetMax = Vector2.zero;

        float ancho = Mathf.Ceil(t.GetPreferredValues(texto).x) + MarcaUdB.Space4 * 2f;
        vigilada.sizeDelta = new Vector2(ancho, alto);
        vigilada.anchoredPosition = new Vector2(MarcaUdB.Space8, MarcaUdB.Space6);
        MarcaUdB.Pildora(chip, alto);
    }

    void ConstruirPista()
    {
        // Píldora oscura (como en las maquetas): negro institucional, texto blanco
        Image chip = CrearImagen("Pista", zonaSegura, new Color(MarcaUdB.Negro.r, MarcaUdB.Negro.g, MarcaUdB.Negro.b, 0.88f));
        pista = (RectTransform)chip.transform;
        pista.anchorMin = pista.anchorMax = pista.pivot = new Vector2(0.5f, 0f);
        MarcaUdB.Pildora(chip, 36f);
        grupoPista = chip.gameObject.AddComponent<CanvasGroup>();
        grupoPista.blocksRaycasts = false;
        grupoPista.interactable = false;

        // Flecha (→) como icono
        TMP_Text flecha = CrearTexto("Icono", pista, "→");
        MarcaUdB.Estilo(flecha, MarcaUdB.TextoBold, MarcaUdB.Cuerpo, MarcaUdB.InkInverso);
        RectTransform rf = (RectTransform)flecha.transform;
        rf.anchorMin = new Vector2(0f, 0f);
        rf.anchorMax = new Vector2(0f, 1f);
        rf.pivot = new Vector2(0f, 0.5f);
        rf.sizeDelta = new Vector2(18f, 0f);
        rf.anchoredPosition = new Vector2(MarcaUdB.Space4, 0f);

        TMP_Text t = CrearTexto("Texto", pista, textoPista);
        MarcaUdB.Estilo(t, MarcaUdB.TextoBold, MarcaUdB.CuerpoS, MarcaUdB.InkInverso);
        t.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rt = (RectTransform)t.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(MarcaUdB.Space4 + 18f + MarcaUdB.Space2, 0f);
        rt.offsetMax = new Vector2(-MarcaUdB.Space4, 0f);

        pista.sizeDelta = new Vector2(420f, 36f);
    }

    IEnumerator OcultarPista()
    {
        float t = 0f;
        while (t < 1f && grupoPista != null)
        {
            t += Time.unscaledDeltaTime / 0.24f;
            grupoPista.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }
        if (pista != null) pista.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ utilidades

    static Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text CrearTexto(string nombre, Transform padre, string texto)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.enableWordWrapping = false;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.raycastTarget = false;
        return t;
    }
}
