using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hoja inferior del celular vertical (como la tarjeta de Street View), con tres alturas:
///  - compacta: edificio, título y botón; la descripción queda escondida bajo el borde de la pantalla;
///  - normal: además la descripción corta (la de siempre);
///  - completa: la ficha del lugar (PanelInfo), que se abre aparte.
/// Arrastrar hacia abajo la lleva a compacta; hacia arriba, de compacta a normal y de normal a la ficha.
/// Tocarla en compacta la sube a normal; en normal abre la ficha. Un gesto corto y rápido basta (más de
/// 0.11 px/ms), no hace falta arrastrar hasta un tope.
///
/// La hoja nunca cambia de tamaño: solo se mueve hacia abajo lo que mide la descripción («bajada»).
/// Mientras el dedo arrastra la sigue 1:1; pasado un extremo cede con resistencia. Al soltar va a su sitio
/// con la curva de cajón en 240 ms, partiendo de donde esté (interrumpible). Como atrapa el arrastre,
/// deslizar sobre la hoja nunca gira la cámara.
/// </summary>
[DisallowMultipleComponent]
public class HojaDeslizable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>Abre la ficha (tercera altura). Si es null, la hoja no pasa de la altura normal.</summary>
    public System.Action alAbrir;

    /// <summary>Se llama cada vez que la hoja se mueve, con lo que está bajada (0 = normal).</summary>
    public System.Action<float> alMover;

    const float Distancia = 40f;         // px del canvas para cambiar de altura con un arrastre lento
    const float Velocidad = 0.11f;       // px por ms: un gesto rápido cambia de altura aunque sea corto
    const float Resistencia = 0.35f;     // más allá de un extremo la hoja cede solo un tercio
    const float MaximoSeguimiento = 24f;
    const float Duracion = 0.24f;        // asentarse en una altura (como los paneles)

    RectTransform rt;
    Canvas lienzo;
    float baseY;
    float bajadaMaxima;                  // lo que mide la descripción: cuánto baja en compacta
    bool prefiereCompacta;               // se recuerda al cambiar de lugar
    float bajada;                        // actual (puede salirse un poco de 0..máximo mientras se arrastra)
    float inicioBajada;
    float recorrido;                     // positivo = hacia arriba
    float inicioTiempo;
    bool arrastrando;

    // Animación hacia la altura elegida
    float desde, hasta, t = 1f;

    /// <summary>true si la hoja está (o va hacia) la altura compacta.</summary>
    public bool Compacta { get { return prefiereCompacta && bajadaMaxima > 0f; } }

    /// <summary>
    /// Posición de reposo y cuánto baja en compacta. MenuDesplegable la llama al acomodar la hoja; si la
    /// hoja estaba compacta, sigue compacta con la nueva medida.
    /// </summary>
    public void FijarBase(float y, float maximo)
    {
        if (rt == null) rt = (RectTransform)transform;
        baseY = y;
        bajadaMaxima = Mathf.Max(0f, maximo);
        if (!arrastrando)
        {
            t = 1f;
            bajada = Compacta ? bajadaMaxima : 0f;
        }
        Aplicar();
    }

    void OnDisable()
    {
        // Si se esconde a mitad de un gesto, queda asentada en la altura que tenía elegida
        arrastrando = false;
        t = 1f;
        bajada = Compacta ? bajadaMaxima : 0f;
        Aplicar();
    }

    public void OnPointerClick(PointerEventData datos)
    {
        // Unity también manda el «toque» al soltar después de un arrastre (y antes de OnEndDrag), porque la
        // hoja maneja las dos cosas: si hubo arrastre, aquí no se hace nada y decide OnEndDrag
        if (arrastrando || datos.dragging) return;
        if (Compacta) Ir(false);
        else if (alAbrir != null) alAbrir();
    }

    public void OnBeginDrag(PointerEventData datos)
    {
        arrastrando = true;
        t = 1f; // el dedo toma la hoja donde esté, aunque iba en camino
        inicioBajada = bajada;
        recorrido = 0f;
        inicioTiempo = Time.unscaledTime;
    }

    public void OnDrag(PointerEventData datos)
    {
        if (!arrastrando) return;
        recorrido += datos.delta.y / Escala();
        float b = inicioBajada - recorrido;
        if (!AnimacionesUI.Activas) b = Mathf.Clamp(b, 0f, bajadaMaxima);
        // Fuera de 0..máximo cede con resistencia: avisa que hay un tope (o la ficha, si es hacia arriba)
        else if (b < 0f) b = Mathf.Max(-MaximoSeguimiento, b * Resistencia);
        else if (b > bajadaMaxima) b = bajadaMaxima + Mathf.Min(MaximoSeguimiento * 0.5f, (b - bajadaMaxima) * Resistencia);
        bajada = b;
        Aplicar();
    }

    public void OnEndDrag(PointerEventData datos)
    {
        if (!arrastrando) return;
        arrastrando = false;
        float ms = Mathf.Max(1f, (Time.unscaledTime - inicioTiempo) * 1000f);
        float velocidad = recorrido / ms;
        float umbral = bajadaMaxima > 0f ? Mathf.Min(Distancia, bajadaMaxima * 0.5f) : Distancia;
        bool arriba = recorrido >= umbral || velocidad > Velocidad;
        bool abajo = -recorrido >= umbral || -velocidad > Velocidad;

        if (arriba)
        {
            if (Compacta) Ir(false);                       // compacta → normal
            else
            {
                Ir(false);                                 // normal → ficha (la hoja vuelve a su sitio)
                if (alAbrir != null) alAbrir();
            }
        }
        else if (abajo && bajadaMaxima > 0f) Ir(true);      // normal → compacta
        else Ir(Compacta);                                 // no alcanzó: vuelve a donde estaba
    }

    void Ir(bool compacta)
    {
        prefiereCompacta = compacta;
        desde = bajada;
        hasta = Compacta ? bajadaMaxima : 0f;
        if (!AnimacionesUI.Activas || Mathf.Approximately(desde, hasta))
        {
            t = 1f;
            bajada = hasta;
            Aplicar();
            return;
        }
        t = 0f;
    }

    void Update()
    {
        if (arrastrando || t >= 1f) return;
        t += Time.unscaledDeltaTime / Duracion;
        bajada = Mathf.LerpUnclamped(desde, hasta, AnimacionesUI.Cajon(Mathf.Clamp01(t)));
        Aplicar();
    }

    void Aplicar()
    {
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseY - bajada);
        if (alMover != null) alMover(bajada);
    }

    // El delta del puntero viene en píxeles de pantalla; la hoja se mide en unidades del canvas
    float Escala()
    {
        if (lienzo == null) lienzo = GetComponentInParent<Canvas>();
        float s = lienzo != null ? lienzo.rootCanvas.scaleFactor : 1f;
        return s > 0f ? s : 1f;
    }
}
