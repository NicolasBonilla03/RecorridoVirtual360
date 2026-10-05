using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Muestra una pequeña pista (por ejemplo «Ir al inicio») mientras el cursor está sobre un control,
/// con un fundido corto. En pantallas táctiles aparece mientras dura el toque.
/// </summary>
public class PistaAlPasar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CanvasGroup pista;
    const float Rapidez = 10f;
    bool dentro;

    void OnEnable()
    {
        dentro = false;
        if (pista != null) pista.alpha = 0f;
    }

    void Update()
    {
        if (pista == null) return;
        float objetivo = dentro ? 1f : 0f;
        if (Mathf.Approximately(pista.alpha, objetivo)) return;
        pista.alpha = Mathf.MoveTowards(pista.alpha, objetivo, Rapidez * Mathf.Min(Time.unscaledDeltaTime, 0.05f));
    }

    public void OnPointerEnter(PointerEventData datos) { dentro = true; }
    public void OnPointerExit(PointerEventData datos) { dentro = false; }
}
