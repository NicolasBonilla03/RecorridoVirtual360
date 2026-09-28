using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TipoPuntoInfo
{
    Oficina,
    Edificio,
    Servicio,
    Espacio,
    Dato
}

/// <summary>
/// Punto de información sobre la foto 360: un círculo con «i» y el nombre del lugar, justo donde
/// se ve la oficina o el edificio. Al tocarlo se abre una tarjeta con lo que hay que saber
/// (qué es, qué hay adentro, horario, ubicación, contacto, enlace).
/// Sirve también para edificios a los que el recorrido no entra: se cuenta qué hay adentro.
///
/// Se crea con «Recorrido → 6. Crear punto de información donde estoy mirando» (Ctrl+Alt+I).
/// Solo se ve en la foto 360 indicada en «Skybox».
/// </summary>
[DisallowMultipleComponent]
public class PuntoInfo : MonoBehaviour
{
    [Header("Dónde aparece")]
    [Tooltip("Foto 360 en la que se ve este punto.")]
    public Material skybox;

    [Header("Contenido de la tarjeta")]
    public string titulo = "Nuevo punto de información";
    public TipoPuntoInfo tipo = TipoPuntoInfo.Oficina;

    [Tooltip("Edificio o zona del recorrido. Decide el color: Edificio Central, Edificio Múltiple, Edificio 12, Edificio 3, Exteriores…")]
    public string zona = "";

    [Tooltip("Una o dos frases: qué es y para qué le sirve al estudiante.")]
    [TextArea(2, 4)] public string resumen = "";

    [Tooltip("Texto largo (opcional). Deja una línea en blanco entre párrafos.")]
    [TextArea(4, 12)] public string descripcion = "";

    [Tooltip("Qué hay adentro o qué servicios ofrece, uno por línea. Útil para edificios a los que el recorrido no entra.")]
    public string[] queEncuentras = new string[0];

    public string horario = "";
    public string ubicacion = "";
    public string contacto = "";

    [Tooltip("Foto opcional para la tarjeta (importada como Sprite).")]
    public Sprite foto;

    [Tooltip("Página con más información (opcional), por ejemplo la del sitio de la universidad.")]
    public string enlace = "";
    public string textoEnlace = "Más información";

    [Header("Botón sobre la foto")]
    [Tooltip("Muestra el nombre junto al círculo.")]
    public bool mostrarNombre = true;

    [Tooltip("El nombre va a la derecha del círculo. Desactívalo si el punto queda cerca del borde derecho del edificio.")]
    public bool nombreALaDerecha = true;

    // Mismas proporciones que el botón de teletransporte (80 × 80, «+» de 48)
    const float Diametro = 80f;
    const float TamanoIcono = 44f;
    const float AltoNombre = 52f;
    const float RadioNombre = 9f;
    const float TamanoNombre = 24f;
    const float PuntoZona = 12f;

    GameObject visual;
    RectTransform circulo;
    bool visible;
    bool construido;

    public Color ColorZona { get { return string.IsNullOrEmpty(zona) ? MarcaUdB.Negro : MarcaUdB.ColorZona(zona); } }

    public string NombreTipo
    {
        get
        {
            switch (tipo)
            {
                case TipoPuntoInfo.Oficina: return "Oficina";
                case TipoPuntoInfo.Edificio: return "Edificio";
                case TipoPuntoInfo.Servicio: return "Servicio";
                case TipoPuntoInfo.Espacio: return "Espacio";
                default: return "Dato";
            }
        }
    }

    void Start()
    {
        Construir();
        visible = !DebeVerse(); // fuerza la primera actualización
    }

    void Update()
    {
        if (!construido) return;

        bool debe = DebeVerse();
        if (debe != visible)
        {
            visible = debe;
            visual.SetActive(debe);
        }
    }

    bool DebeVerse()
    {
        return skybox != null && RenderSettings.skybox == skybox;
    }

    public void Abrir()
    {
        if (FadeController.Instance != null && FadeController.Instance.EnTransicion) return;
        PanelInfo.Mostrar(this);
    }

    // ------------------------------------------------------------------ botón sobre la foto

    void Construir()
    {
        if (construido) return;
        if (!(transform is RectTransform) || GetComponentInParent<Canvas>() == null)
        {
            Debug.LogWarning("[PuntoInfo] '" + name + "' debe estar dentro del Canvas del mundo (CanvasWorld). Créalo con Recorrido → 6.", this);
            return;
        }

        Color color = ColorZona;

        visual = new GameObject("Visual", typeof(RectTransform));
        RectTransform rtV = (RectTransform)visual.transform;
        rtV.SetParent(transform, false);
        rtV.sizeDelta = new Vector2(Diametro, Diametro);

        // Círculo con halo: el mismo hotspot de los botones de teletransporte, pero en el color
        // de la zona y con «i» en lugar de «+» (el rojo queda para ir a otro lugar)
        Image imgCirculo = CrearImagen("Circulo", rtV, Color.white);
        circulo = (RectTransform)imgCirculo.transform;
        circulo.sizeDelta = new Vector2(Diametro, Diametro);
        if (MarcaUdB.Hotspot != null)
        {
            imgCirculo.sprite = MarcaUdB.Hotspot;
            imgCirculo.type = Image.Type.Simple;
            imgCirculo.preserveAspect = true;
        }
        else
        {
            MarcaUdB.Redondear(imgCirculo, Diametro * 0.5f);
        }
        Button bCirculo = imgCirculo.gameObject.AddComponent<Button>();
        bCirculo.targetGraphic = imgCirculo;
        MarcaUdB.ColoresBoton(bCirculo, color, Color.Lerp(color, MarcaUdB.Negro, 0.18f), Color.Lerp(color, MarcaUdB.Negro, 0.32f));
        SinNavegacion(bCirculo);
        bCirculo.onClick.AddListener(Abrir);

        TextMeshProUGUI icono = CrearTexto("Icono", circulo, "i", MarcaUdB.TextoBold, TamanoIcono, MarcaUdB.TintaSobre(color));
        icono.alignment = TextAlignmentOptions.Center;
        RectTransform rtIcono = (RectTransform)icono.transform;
        rtIcono.anchorMin = Vector2.zero;
        rtIcono.anchorMax = Vector2.one;
        rtIcono.sizeDelta = Vector2.zero;

        // Nombre en un chip de vidrio con el punto de la zona, como el chip «Estás en» del menú
        if (mostrarNombre && !string.IsNullOrEmpty(titulo))
        {
            Image chip = CrearImagen("Nombre", rtV, MarcaUdB.VidrioPanel);
            RectTransform rtP = (RectTransform)chip.transform;
            float lado = nombreALaDerecha ? 1f : -1f;
            rtP.anchorMin = rtP.anchorMax = new Vector2(0.5f, 0.5f);
            rtP.pivot = new Vector2(nombreALaDerecha ? 0f : 1f, 0.5f);
            rtP.anchoredPosition = new Vector2(lado * (Diametro * 0.5f + 8f), 0f);
            MarcaUdB.Redondear(chip, RadioNombre);

            float pad = 18f;
            Image punto = CrearImagen("Zona", rtP, color);
            punto.raycastTarget = false;
            RectTransform rtZ = (RectTransform)punto.transform;
            rtZ.anchorMin = rtZ.anchorMax = new Vector2(0f, 0.5f);
            rtZ.pivot = new Vector2(0f, 0.5f);
            rtZ.sizeDelta = new Vector2(PuntoZona, PuntoZona);
            rtZ.anchoredPosition = new Vector2(pad, 0f);
            MarcaUdB.Redondear(punto, PuntoZona * 0.5f);

            TextMeshProUGUI texto = CrearTexto("Texto", rtP, titulo, MarcaUdB.TextoBold, TamanoNombre, MarcaUdB.Ink);
            texto.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform rtT = (RectTransform)texto.transform;
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.one;
            float izquierda = pad + PuntoZona + 10f;
            rtT.offsetMin = new Vector2(izquierda, 0f);
            rtT.offsetMax = new Vector2(-pad, 0f);

            float ancho = Mathf.Ceil(texto.GetPreferredValues(titulo).x) + izquierda + pad;
            rtP.sizeDelta = new Vector2(Mathf.Min(ancho, 560f), AltoNombre);

            Button bNombre = chip.gameObject.AddComponent<Button>();
            bNombre.targetGraphic = chip;
            MarcaUdB.ColoresBoton(bNombre, MarcaUdB.VidrioPanel, MarcaUdB.Tenue(color, 0.14f), MarcaUdB.Tenue(color, 0.24f));
            SinNavegacion(bNombre);
            bNombre.onClick.AddListener(Abrir);

            MarcaUdB.SombraFlotante(rtP);
        }

        visual.SetActive(false);
        construido = true;
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
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    static void SinNavegacion(Selectable s)
    {
        Navigation nav = s.navigation;
        nav.mode = Navigation.Mode.None;
        s.navigation = nav;
    }

#if UNITY_EDITOR
    // En la vista de escena: un disco del color de la zona con el nombre, solo en la foto que se está viendo
    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        bool seleccionado = UnityEditor.Selection.activeGameObject == gameObject;
        if (!seleccionado && RenderSettings.skybox != skybox) return;

        float radio = Diametro * 0.5f * Mathf.Abs(transform.lossyScale.x);
        Color c = ColorZona;
        Gizmos.color = new Color(c.r, c.g, c.b, 0.9f);
        Gizmos.DrawSphere(transform.position, radio);
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.DrawWireDisc(transform.position, transform.forward, radio * 1.15f);
        UnityEditor.Handles.Label(transform.position + transform.right * radio * 1.4f, "  i  " + titulo);
    }
#endif
}
