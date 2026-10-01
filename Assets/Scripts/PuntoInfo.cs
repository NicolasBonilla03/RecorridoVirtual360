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
    [Tooltip("Ficha del catálogo (Assets/Resources/PuntosInfo/catalogo.json). Lo que escribas abajo reemplaza lo del catálogo; lo que dejes vacío se toma de la ficha.")]
    public string idFicha = "";

    public string titulo = "Nuevo punto de información";
    public TipoPuntoInfo tipo = TipoPuntoInfo.Oficina;

    [Tooltip("Edificio o zona del recorrido. Decide el color: Edificio Central, Edificio Múltiple, Edificio 12, Edificio 3, Exteriores…")]
    public string zona = "";

    [Tooltip("Una o dos frases: qué es y para qué le sirve al estudiante.")]
    [TextArea(2, 4)] public string resumen = "";

    [Tooltip("Texto largo (opcional). Deja una línea en blanco entre párrafos.")]
    [TextArea(4, 12)] public string descripcion = "";

    [Tooltip("Encabezado de la lista. Si está vacío: «Adentro encuentras» para edificios y «Aquí puedes» para lo demás.")]
    public string tituloLista = "";

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

    public const string TituloPorDefecto = "Nuevo punto de información";

    FichaInfo Ficha { get { return CatalogoInfo.PorId(idFicha); } }

    public string Titulo
    {
        get
        {
            if (!string.IsNullOrEmpty(titulo) && titulo != TituloPorDefecto) return titulo;
            FichaInfo f = Ficha;
            return f != null && !string.IsNullOrEmpty(f.titulo) ? f.titulo : titulo;
        }
    }

    public string Zona
    {
        get
        {
            if (!string.IsNullOrEmpty(zona)) return zona;
            FichaInfo f = Ficha;
            return f != null ? f.zona : zona;
        }
    }

    public Color ColorZona { get { string z = Zona; return string.IsNullOrEmpty(z) ? MarcaUdB.Negro : MarcaUdB.ColorZona(z); } }

    /// <summary>Contenido de la tarjeta: la ficha del catálogo con lo escrito aquí encima.</summary>
    public FichaInfo ComoFicha()
    {
        FichaInfo b = Ficha;
        FichaInfo r = new FichaInfo();
        r.id = idFicha;
        r.titulo = Titulo;
        r.tipo = b != null && TipoSinTocar() ? b.tipo : NombreTipo;
        r.zona = Zona;
        r.resumen = Elegir(resumen, b != null ? b.resumen : null);
        r.descripcion = Elegir(descripcion, b != null ? b.descripcion : null);
        r.tituloLista = Elegir(tituloLista, b != null ? b.tituloLista : null);
        r.queEncuentras = queEncuentras != null && queEncuentras.Length > 0 ? queEncuentras : (b != null ? b.queEncuentras : null);
        r.horario = Elegir(horario, b != null ? b.horario : null);
        r.ubicacion = Elegir(ubicacion, b != null ? b.ubicacion : null);
        r.contacto = Elegir(contacto, b != null ? b.contacto : null);
        r.enlace = Elegir(enlace, b != null ? b.enlace : null);
        r.textoEnlace = !string.IsNullOrEmpty(enlace) ? textoEnlace : (b != null && !string.IsNullOrEmpty(b.textoEnlace) ? b.textoEnlace : textoEnlace);
        return r;
    }

    // Con una ficha asignada y el tipo en su valor por defecto, manda el tipo de la ficha
    bool TipoSinTocar() { return tipo == TipoPuntoInfo.Oficina; }

    static string Elegir(string propio, string ficha)
    {
        return !string.IsNullOrEmpty(propio) && propio.Trim().Length > 0 ? propio : ficha;
    }

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
        string nombre = Titulo;
        if (mostrarNombre && !string.IsNullOrEmpty(nombre))
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

            TextMeshProUGUI texto = CrearTexto("Texto", rtP, nombre, MarcaUdB.TextoBold, TamanoNombre, MarcaUdB.Ink);
            texto.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform rtT = (RectTransform)texto.transform;
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.one;
            float izquierda = pad + PuntoZona + 10f;
            rtT.offsetMin = new Vector2(izquierda, 0f);
            rtT.offsetMax = new Vector2(-pad, 0f);

            float ancho = Mathf.Ceil(texto.GetPreferredValues(nombre).x) + izquierda + pad;
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
        UnityEditor.Handles.Label(transform.position + transform.right * radio * 1.4f, "  i  " + Titulo);
    }
#endif
}
