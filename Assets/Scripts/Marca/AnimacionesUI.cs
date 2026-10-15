using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animaciones sutiles de la interfaz. Se crea solo al iniciar; no hay que añadirlo a la escena.
///
/// A cada botón del recorrido le pone una respuesta al toque (ToqueSuave):
///  - al pasar el cursor crece un poco; al presionar se encoge un poco y vuelve;
///  - los botones que están en el espacio (teletransporte, puntos de edificio y de información)
///    aparecen con un pequeño crecimiento cuando se muestran.
/// Los movimientos son cortos y pequeños a propósito: acompañan, no distraen.
/// Para apagarlas: AnimacionesUI.Activas = false.
/// </summary>
public class AnimacionesUI : MonoBehaviour
{
    /// <summary>Interruptor general de las animaciones.</summary>
    public static bool Activas = true;

    // ------------------------------------------------------------------ curvas (filosofía de Emil Kowalski)
    // Las curvas de fábrica son débiles: estas arrancan rápido (respuesta inmediata) y frenan suave.

    /// <summary>Salida fuerte, cubic-bezier(0.23, 1, 0.32, 1): para todo lo que entra o sale.</summary>
    public static float SalidaFuerte(float t) { return Bezier(0.23f, 1f, 0.32f, 1f, t); }

    /// <summary>Curva de cajón tipo iOS, cubic-bezier(0.32, 0.72, 0, 1): para los paneles laterales.</summary>
    public static float Cajon(float t) { return Bezier(0.32f, 0.72f, 0f, 1f, t); }

    /// <summary>Duraciones: entrar es un poco más lento que salir (el sistema responde rápido al cerrar).</summary>
    public const float DuracionPanelEntrada = 0.24f, DuracionPanelSalida = 0.18f;

    /// <summary>
    /// true solo con mouse real y en la vista de escritorio. En pantallas táctiles el «pasar encima» se
    /// dispara al tocar y queda pegado, así que allí los botones solo responden al presionar. En la vista de
    /// celular tampoco hay efectos al pasar, aunque se pruebe con mouse en el editor: es la versión para el dedo.
    /// </summary>
    public static bool HoverConMouse
    {
        get { return !Application.isMobilePlatform && Input.mousePresent && !EscalaAdaptable.EsPantallaDeCelular; }
    }

    /// <summary>Evalúa una curva cubic-bezier(x1, y1, x2, y2) como las de CSS, en t de 0 a 1.</summary>
    public static float Bezier(float x1, float y1, float x2, float y2, float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        // Bisección sobre el parámetro de la curva: robusta para cualquier curva válida y muy barata
        float bajo = 0f, alto = 1f, t = x;
        for (int i = 0; i < 18; i++)
        {
            t = (bajo + alto) * 0.5f;
            if (Coordenada(x1, x2, t) < x) bajo = t; else alto = t;
        }
        return Coordenada(y1, y2, t);
    }

    static float Coordenada(float a, float b, float t)
    {
        float u = 1f - t;
        return 3f * u * u * t * a + 3f * u * t * t * b + t * t * t;
    }

    // Buscar todos los botones de la escena cuesta (recorre todos los objetos y crea memoria basura),
    // así que no se hace a ritmo fijo: se revisa al arrancar, poco después de cada toque o clic y de
    // cada cambio de lugar (que es cuando los menús crean botones), y como respaldo cada 5 s.
    const float AlArrancar = 3f;     // segundos de revisiones seguidas al inicio, mientras se arma la interfaz
    const float TrasEvento = 0.3f;   // espera tras un toque: deja que el menú termine de crear sus botones
    const float Respaldo = 5f;
    float proxima;
    float revisarTrasEvento = -1f;
    Material ultimoSkybox;
    float creado;

    void Awake() { creado = Time.realtimeSinceStartup; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (FindObjectOfType<AnimacionesUI>() != null) return;
        GameObject go = new GameObject("AnimacionesUI");
        DontDestroyOnLoad(go);
        go.AddComponent<AnimacionesUI>();
    }

    void Update()
    {
        bool huboEvento = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Escape) || RenderSettings.skybox != ultimoSkybox;
        for (int i = 0; i < Input.touchCount && !huboEvento; i++)
            huboEvento = Input.GetTouch(i).phase == TouchPhase.Ended;
        if (huboEvento)
        {
            ultimoSkybox = RenderSettings.skybox;
            revisarTrasEvento = TrasEvento;
        }

        bool revisar = false;
        if (revisarTrasEvento >= 0f)
        {
            revisarTrasEvento -= Time.unscaledDeltaTime;
            if (revisarTrasEvento < 0f) revisar = true;
        }

        proxima -= Time.unscaledDeltaTime;
        if (proxima <= 0f) revisar = true;
        if (!revisar) return;
        proxima = Time.realtimeSinceStartup - creado < AlArrancar ? 0.5f : Respaldo;

        // Los menús crean botones sobre la marcha: se equipan los nuevos
        foreach (Button b in FindObjectsOfType<Button>(true))
        {
            if (b == null || b.transition == Selectable.Transition.None) continue; // los velos de fondo no se animan
            if (b.GetComponent<ToqueSuave>() == null) b.gameObject.AddComponent<ToqueSuave>();
        }
    }
}
