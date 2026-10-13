using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hoja inferior del celular vertical (como la tarjeta de Street View): tocarla o deslizarla hacia arriba
/// abre la ficha del lugar. Un gesto corto y rápido basta; no hace falta arrastrar hasta un tope.
/// Mientras el dedo arrastra, la hoja sube un poco con resistencia para que se sienta que responde.
/// Como atrapa el arrastre, deslizar sobre la hoja nunca gira la cámara.
/// </summary>
[DisallowMultipleComponent]
public class HojaDeslizable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>Se llama al tocar la hoja o deslizarla hacia arriba. Si es null, la hoja solo bloquea el arrastre.</summary>
    public System.Action alAbrir;

    const float Distancia = 40f;         // px del canvas hacia arriba para abrir
    const float Velocidad = 0.11f;       // px por ms: un toque rápido hacia arriba abre aunque sea corto
    const float Resistencia = 0.35f;     // la hoja sigue al dedo solo un tercio: avisa sin despegarse
    const float MaximoSeguimiento = 24f;
    const float Regreso = 12f;           // rapidez con la que vuelve a su sitio al soltar

    RectTransform rt;
    Canvas lienzo;
    float baseY;
    float desplazamiento;
    float recorrido;
    float inicioTiempo;
    bool arrastrando;

    /// <summary>Posición de reposo de la hoja. MenuDesplegable la fija al acomodarla.</summary>
    public void FijarBase(float y)
    {
        if (rt == null) rt = (RectTransform)transform;
        baseY = y;
        if (!arrastrando) desplazamiento = 0f;
        Aplicar();
    }

    void OnDisable()
    {
        arrastrando = false;
        desplazamiento = 0f;
        Aplicar();
    }

    public void OnPointerClick(PointerEventData datos)
    {
        // Unity no manda el clic si hubo arrastre: aquí solo llegan los toques
        if (alAbrir != null) alAbrir();
    }

    public void OnBeginDrag(PointerEventData datos)
    {
        arrastrando = true;
        recorrido = 0f;
        inicioTiempo = Time.unscaledTime;
    }

    public void OnDrag(PointerEventData datos)
    {
        if (!arrastrando) return;
        recorrido += datos.delta.y / Escala();
        // Hacia arriba sigue al dedo con resistencia; hacia abajo no se mueve (no hay a dónde ir)
        desplazamiento = AnimacionesUI.Activas ? Mathf.Clamp(recorrido * Resistencia, 0f, MaximoSeguimiento) : 0f;
        Aplicar();
    }

    public void OnEndDrag(PointerEventData datos)
    {
        if (!arrastrando) return;
        arrastrando = false;
        float ms = Mathf.Max(1f, (Time.unscaledTime - inicioTiempo) * 1000f);
        if (alAbrir != null && recorrido > 0f && (recorrido >= Distancia || recorrido / ms > Velocidad))
            alAbrir();
    }

    void Update()
    {
        if (arrastrando || desplazamiento <= 0f) return;
        // Vuelve a su sitio partiendo de donde quedó (interrumpible si el dedo la toma otra vez)
        desplazamiento = Mathf.Lerp(desplazamiento, 0f, 1f - Mathf.Exp(-Regreso * Mathf.Min(Time.unscaledDeltaTime, 0.05f)));
        if (desplazamiento < 0.25f) desplazamiento = 0f;
        Aplicar();
    }

    void Aplicar()
    {
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseY + desplazamiento);
    }

    // El delta del puntero viene en píxeles de pantalla; la hoja se mide en unidades del canvas
    float Escala()
    {
        if (lienzo == null) lienzo = GetComponentInParent<Canvas>();
        float s = lienzo != null ? lienzo.rootCanvas.scaleFactor : 1f;
        return s > 0f ? s : 1f;
    }
}
