using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Cierra un panel arrastrándolo hacia abajo: el gesto contrario al que lo abrió (la hoja del celular se
/// desliza hacia arriba). El panel sigue al dedo 1:1 hacia abajo y apenas cede hacia arriba (resistencia).
/// Al soltar se cierra si bajó una cuarta parte de su alto o si el gesto fue rápido; si no, vuelve a su sitio.
/// Va en la zona por donde se agarra (el encabezado), no en la lista, para no pelear con el desplazamiento.
/// </summary>
[DisallowMultipleComponent]
public class DeslizarParaCerrar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform objetivo;
    /// <summary>Si devuelve false, el gesto no hace nada (por ejemplo, fuera del celular vertical).</summary>
    public System.Func<bool> activo;
    public System.Action alEmpezar;        // detener cualquier animación del panel
    public System.Action alCerrar;
    public System.Action alVolver;

    const float Velocidad = 0.11f;         // px por ms: un gesto rápido cierra aunque sea corto
    const float FraccionDelAlto = 0.25f;
    const float ResistenciaArriba = 0.15f;
    const float MaximoArriba = 12f;

    Canvas lienzo;
    float inicioY;
    float recorrido;                       // positivo = hacia abajo
    float inicioTiempo;
    bool arrastrando;

    public void OnBeginDrag(PointerEventData datos)
    {
        arrastrando = objetivo != null && (activo == null || activo());
        if (!arrastrando) return;
        if (alEmpezar != null) alEmpezar();
        inicioY = objetivo.anchoredPosition.y;
        recorrido = 0f;
        inicioTiempo = Time.unscaledTime;
    }

    public void OnDrag(PointerEventData datos)
    {
        if (!arrastrando) return;
        recorrido -= datos.delta.y / Escala();
        float y = recorrido >= 0f ? recorrido : Mathf.Max(-MaximoArriba, recorrido * ResistenciaArriba);
        objetivo.anchoredPosition = new Vector2(objetivo.anchoredPosition.x, inicioY - y);
    }

    public void OnEndDrag(PointerEventData datos)
    {
        if (!arrastrando) return;
        arrastrando = false;
        float ms = Mathf.Max(1f, (Time.unscaledTime - inicioTiempo) * 1000f);
        bool cerrar = recorrido > 0f && (recorrido >= objetivo.rect.height * FraccionDelAlto || recorrido / ms > Velocidad);
        if (cerrar) { if (alCerrar != null) alCerrar(); }
        else if (alVolver != null) alVolver();
    }

    // El delta del puntero viene en píxeles de pantalla; el panel se mide en unidades del canvas
    float Escala()
    {
        if (lienzo == null) lienzo = GetComponentInParent<Canvas>();
        float s = lienzo != null ? lienzo.rootCanvas.scaleFactor : 1f;
        return s > 0f ? s : 1f;
    }
}
