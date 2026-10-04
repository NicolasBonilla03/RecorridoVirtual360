using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Respuesta al toque de un botón: escala suave al pasar, al presionar y al aparecer.</summary>
public class ToqueSuave : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    const float Rapidez = 16f;        // qué tan rápido llega a su tamaño
    const float InicioAparicion = 0.82f;

    Vector3 escalaBase;
    bool tieneBase;
    bool enElEspacio;                 // botón dentro de un lienzo del mundo
    bool medido;
    float alPasar = 1.04f;
    float alPresionar = 0.95f;

    float actual = 1f;
    bool dentro;
    bool presionado;
    Selectable control;

    void OnEnable()
    {
        if (!tieneBase)
        {
            escalaBase = transform.localScale;
            tieneBase = true;
            control = GetComponent<Selectable>();
            Canvas lienzo = GetComponentInParent<Canvas>();
            enElEspacio = lienzo != null && lienzo.rootCanvas.renderMode == RenderMode.WorldSpace;
        }

        dentro = false;
        presionado = false;
        actual = AnimacionesUI.Activas && enElEspacio ? InicioAparicion : 1f;
        transform.localScale = escalaBase * actual;
    }

    void OnDisable()
    {
        if (tieneBase) transform.localScale = escalaBase;
    }

    // Las filas anchas de las listas se mueven menos que los botones pequeños
    void Medir()
    {
        if (medido) return;
        medido = true;
        RectTransform rt = transform as RectTransform;
        float ancho = rt != null ? rt.rect.width : 0f;
        if (enElEspacio) { alPasar = 1.08f; alPresionar = 0.94f; }
        else if (ancho > 220f) { alPasar = 1f; alPresionar = 0.985f; }
        else { alPasar = 1.04f; alPresionar = 0.95f; }
    }

    void Update()
    {
        float objetivo = 1f;
        if (AnimacionesUI.Activas && (control == null || control.IsInteractable()))
            objetivo = presionado ? alPresionar : (dentro ? alPasar : 1f);

        if (Mathf.Abs(actual - objetivo) < 0.0005f)
        {
            if (actual != objetivo) { actual = objetivo; transform.localScale = escalaBase * actual; }
            return;
        }

        actual = Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-Rapidez * Mathf.Min(Time.unscaledDeltaTime, 0.05f)));
        transform.localScale = escalaBase * actual;
    }

    public void OnPointerEnter(PointerEventData datos) { Medir(); dentro = true; }
    public void OnPointerExit(PointerEventData datos) { dentro = false; presionado = false; }
    public void OnPointerDown(PointerEventData datos) { Medir(); presionado = true; }
    public void OnPointerUp(PointerEventData datos) { presionado = false; }
}
