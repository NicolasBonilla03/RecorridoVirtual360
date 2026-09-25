using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Fundido entre escenas y pantalla de carga al abrir el recorrido.
/// Crea su propio lienzo por encima de toda la interfaz, a pantalla completa (respeta
/// cualquier tamaño y orientación), así el fundido tapa siempre la pantalla entera.
/// </summary>
public class FadeController : MonoBehaviour
{
    public static FadeController Instance;

    [Tooltip("Panel del fundido anterior. Ya no se usa: el fundido crea su propio lienzo a pantalla completa.")]
    public Image panelFade;
    public float velocidadFade = 1.5f;

    [Tooltip("Color del fundido entre escenas. Negro institucional UdB (#1d1d1b).")]
    public Color colorFade = new Color(29f / 255f, 29f / 255f, 27f / 255f, 1f);

    [Header("Pantalla de carga")]
    [Tooltip("Al abrir el recorrido: fondo blanco con el logotipo de la universidad en el centro.")]
    public bool pantallaDeCarga = true;

    [Tooltip("Segundos mínimos que se ve la pantalla de carga.")]
    public float duracionMinimaCarga = 1.2f;

    public string tituloCarga = "Recorrido virtual 360°";
    public string textoCarga = "Preparando el recorrido…";

    [Tooltip("Muestra también la pantalla blanca con el logotipo en cada cambio de escena, en lugar del fundido de color.")]
    public bool logoEnTransiciones = false;

    // true mientras hay un fundido en curso (evita transiciones encimadas por doble clic)
    public bool EnTransicion { get; private set; }

    const int OrdenCanvas = 1000;          // por encima del menú (50) y de la cabecera (40)
    const float AnchoLogo = 320f;
    const float AnchoBarra = 320f;
    const float AltoBarra = 6f;
    const float AnchoSegmento = 96f;
    const float DuracionSalidaCarga = 0.45f;

    GameObject raiz;
    RectTransform canvasRT;
    Image velo;
    CanvasGroup grupoCarga;
    RectTransform logoRT;
    RectTransform segmento;
    Sprite logotipo;

    void Awake()
    {
        Instance = this;

        // El panel de la escena queda apagado para que no tape solo una parte de la pantalla
        if (panelFade != null)
        {
            panelFade.raycastTarget = false;
            panelFade.color = new Color(colorFade.r, colorFade.g, colorFade.b, 0f);
            panelFade.enabled = false;
        }

        Construir();

        if (pantallaDeCarga)
        {
            MostrarPantalla(Color.white, 1f, true);
            EnTransicion = true;
        }
        else
        {
            Ocultar();
        }
    }

    void Start()
    {
        if (pantallaDeCarga) StartCoroutine(Carga());
    }

    void OnDisable()
    {
        EnTransicion = false;
    }

    void OnDestroy()
    {
        if (raiz != null) Destroy(raiz);
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (raiz == null || !raiz.activeSelf) return;

        // El logotipo no pasa del 72 % del ancho en pantallas angostas
        if (logoRT != null && canvasRT != null)
        {
            float ancho = Mathf.Min(AnchoLogo, canvasRT.rect.width * 0.72f);
            float proporcion = logotipo != null && logotipo.rect.width > 0f ? logotipo.rect.height / logotipo.rect.width : 0.3f;
            logoRT.sizeDelta = new Vector2(ancho, ancho * proporcion);
        }

        // Barra de carga sin fin: un segmento rojo que recorre el riel
        if (segmento != null && grupoCarga != null && grupoCarga.alpha > 0f)
        {
            float t = Mathf.Repeat(Time.unscaledTime * 0.8f, 1f);
            t = t * t * (3f - 2f * t);
            float x = Mathf.Lerp(-AnchoSegmento, AnchoBarra, t);
            segmento.anchoredPosition = new Vector2(x, 0f);
        }
    }

    // ------------------------------------------------------------------ API

    public void CambiarSkybox(Material nuevoSkybox)
    {
        if (nuevoSkybox == null || EnTransicion)
            return;

        StartCoroutine(TransicionSkybox(nuevoSkybox));
    }

    // ------------------------------------------------------------------ secuencias

    IEnumerator Carga()
    {
        float inicio = Time.realtimeSinceStartup;

        // Unos cuadros para que la primera panorámica y la interfaz ya estén dibujadas debajo
        for (int i = 0; i < 3; i++) yield return null;
        while (Time.realtimeSinceStartup - inicio < duracionMinimaCarga) yield return null;

        float a = 1f;
        while (a > 0f)
        {
            a -= Paso() / DuracionSalidaCarga;
            PonerAlfa(Mathf.Clamp01(a));
            yield return null;
        }

        Ocultar();
        EnTransicion = false;
    }

    IEnumerator TransicionSkybox(Material nuevoSkybox)
    {
        EnTransicion = true;

        Color color = logoEnTransiciones ? Color.white : colorFade;
        MostrarPantalla(color, 0f, logoEnTransiciones);

        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Paso() * velocidadFade;
            PonerAlfa(Mathf.Clamp01(alpha));
            yield return null;
        }
        PonerAlfa(1f);

        RenderSettings.skybox = nuevoSkybox;
        DynamicGI.UpdateEnvironment();

        // Activar TPs correspondientes al nuevo skybox
        if (GestorTeleports.Instance != null)
            GestorTeleports.Instance.ActivarTeleports(nuevoSkybox);

        // Un cuadro con la pantalla tapada: ahí se sube la panorámica nueva a la tarjeta de video
        yield return null;

        while (alpha > 0f)
        {
            alpha -= Paso() * velocidadFade;
            PonerAlfa(Mathf.Clamp01(alpha));
            yield return null;
        }

        Ocultar();
        EnTransicion = false;
    }

    // Tiempo del cuadro sin saltos: si un cuadro tarda mucho (cargando una panorámica),
    // el fundido no se salta la mitad del recorrido
    static float Paso()
    {
        return Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
    }

    // ------------------------------------------------------------------ estado del lienzo

    void MostrarPantalla(Color color, float alfa, bool conLogo)
    {
        if (raiz == null) return;
        raiz.SetActive(true);
        velo.color = new Color(color.r, color.g, color.b, 1f);
        velo.raycastTarget = true; // bloquea clics mientras dura
        grupoCarga.gameObject.SetActive(conLogo);
        PonerAlfa(alfa);
    }

    void PonerAlfa(float a)
    {
        if (velo == null) return;
        Color c = velo.color;
        c.a = a;
        velo.color = c;
        if (grupoCarga != null) grupoCarga.alpha = a;
    }

    void Ocultar()
    {
        if (raiz == null) return;
        PonerAlfa(0f);
        velo.raycastTarget = false;
        raiz.SetActive(false);
    }

    // ------------------------------------------------------------------ construcción

    void Construir()
    {
        raiz = new GameObject("Transiciones_UdB", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OrdenCanvas;
        CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = MarcaUdB.ResolucionReferencia;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        canvasRT = (RectTransform)raiz.transform;

        // Velo a pantalla completa
        velo = CrearImagen("Velo", canvasRT, Color.white);
        Estirar((RectTransform)velo.transform);

        // Contenido de la pantalla de carga, centrado
        GameObject goCarga = new GameObject("PantallaCarga", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rtCarga = (RectTransform)goCarga.transform;
        rtCarga.SetParent(canvasRT, false);
        Estirar(rtCarga);
        grupoCarga = goCarga.GetComponent<CanvasGroup>();
        grupoCarga.blocksRaycasts = false;
        grupoCarga.interactable = false;

        GameObject goColumna = new GameObject("Columna", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform columna = (RectTransform)goColumna.transform;
        columna.SetParent(rtCarga, false);
        columna.anchorMin = columna.anchorMax = columna.pivot = new Vector2(0.5f, 0.5f);
        VerticalLayoutGroup vl = goColumna.GetComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.spacing = MarcaUdB.Space5;
        vl.childControlWidth = false;
        vl.childControlHeight = false;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;
        ContentSizeFitter ajuste = goColumna.GetComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Logotipo oficial, tal cual (sobre blanco va la versión a color)
        logotipo = MarcaUdB.LogotipoHorizontal;
        if (logotipo != null)
        {
            Image logo = CrearImagen("Logotipo", columna, Color.white);
            logo.sprite = logotipo;
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            logoRT = (RectTransform)logo.transform;
            float proporcion = logotipo.rect.width > 0f ? logotipo.rect.height / logotipo.rect.width : 0.3f;
            logoRT.sizeDelta = new Vector2(AnchoLogo, AnchoLogo * proporcion);
        }

        if (!string.IsNullOrEmpty(tituloCarga))
        {
            TextMeshProUGUI titulo = CrearTexto("Titulo", columna, tituloCarga, MarcaUdB.Display, MarcaUdB.DisplayM, MarcaUdB.Ink);
            ((RectTransform)titulo.transform).sizeDelta = new Vector2(AnchoBarra + 80f, 32f);
        }

        // Riel de la barra con un segmento rojo que lo recorre
        Image riel = CrearImagen("Barra", columna, MarcaUdB.Surface300);
        riel.raycastTarget = false;
        RectTransform rtRiel = (RectTransform)riel.transform;
        rtRiel.sizeDelta = new Vector2(AnchoBarra, AltoBarra);
        MarcaUdB.Redondear(riel, AltoBarra * 0.5f);
        riel.gameObject.AddComponent<RectMask2D>();

        Image seg = CrearImagen("Avance", rtRiel, MarcaUdB.Rojo);
        seg.raycastTarget = false;
        segmento = (RectTransform)seg.transform;
        segmento.anchorMin = new Vector2(0f, 0f);
        segmento.anchorMax = new Vector2(0f, 1f);
        segmento.pivot = new Vector2(0f, 0.5f);
        segmento.sizeDelta = new Vector2(AnchoSegmento, 0f);
        segmento.anchoredPosition = new Vector2(-AnchoSegmento, 0f);
        MarcaUdB.Redondear(seg, AltoBarra * 0.5f);

        if (!string.IsNullOrEmpty(textoCarga))
        {
            TextMeshProUGUI estado = CrearTexto("Estado", columna, textoCarga, MarcaUdB.Texto, MarcaUdB.Cuerpo, MarcaUdB.InkMuted);
            ((RectTransform)estado.transform).sizeDelta = new Vector2(AnchoBarra + 80f, 24f);
        }
    }

    static Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, TMP_FontAsset fuente, float tamano, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tamano;
        t.color = color;
        t.text = texto;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
