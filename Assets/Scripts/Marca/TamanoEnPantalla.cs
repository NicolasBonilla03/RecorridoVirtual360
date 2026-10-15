using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mantiene un botón del espacio (los «+» para avanzar y los círculos «i» de información) del mismo tamaño en
/// pantalla, MarcaUdB.DiametroHotspot px del sistema, sin importar a qué distancia se puso en la foto 360.
/// Así todos se ven parejos y son fáciles de alcanzar y tocar.
///
/// Además lo gira de frente a la cámara. Los lienzos del mundo ignoran los toques sobre gráficos que le dan la
/// espalda a la cámara (GraphicRaycaster con «ignoreReversedGraphics»), pero la interfaz se dibuja por los dos
/// lados: un botón colocado al revés se veía bien y no respondía. Pasaba con el «+» de «Entrada principal y
/// portería» hacia «Escaleras principales». Girarlos al aparecer arregla ese y cualquier otro, y de paso los
/// círculos se ven redondos en vez de aplastados cuando quedaban en ángulo.
///
/// La cámara solo gira sobre su sitio, así que la distancia a cada botón no cambia: el tamaño solo hay que
/// recalcularlo cuando cambia el campo de visión (zoom, girar el teléfono) o el tamaño de la pantalla, y cuando
/// el botón aparece. Con la pantalla quieta no trabaja. Si el botón tiene ToqueSuave (que también maneja su
/// escala para el toque), le pasa la nueva escala base en vez de pisarla.
/// </summary>
[DisallowMultipleComponent]
public class TamanoEnPantalla : MonoBehaviour
{
    static readonly List<TamanoEnPantalla> activos = new List<TamanoEnPantalla>();
    static float ultimoCampo = -1f;
    static int ultimoAncho, ultimoAlto;
    static int ultimoCuadro = -1;

    /// <summary>Tamaño en pantalla, en px del sistema. Por defecto el de los «+» (MarcaUdB.DiametroHotspot).</summary>
    public float diametro = MarcaUdB.DiametroHotspot;

    RectTransform rt;
    ToqueSuave toque;

    void OnEnable()
    {
        if (rt == null) rt = transform as RectTransform;
        activos.Add(this);
        Ajustar();
    }

    void OnDisable()
    {
        activos.Remove(this);
    }

    // Una sola revisión por cuadro para todos los botones, y solo si cambió algo que afecte su tamaño
    void LateUpdate()
    {
        if (ultimoCuadro == Time.frameCount) return;
        ultimoCuadro = Time.frameCount;

        Camera cam = CameraController.Principal;
        if (cam == null) return;
        if (Mathf.Abs(cam.fieldOfView - ultimoCampo) < 0.01f && Screen.width == ultimoAncho && Screen.height == ultimoAlto)
            return;
        ultimoCampo = cam.fieldOfView;
        ultimoAncho = Screen.width;
        ultimoAlto = Screen.height;
        for (int i = 0; i < activos.Count; i++)
            if (activos[i] != null) activos[i].Ajustar();
    }

    void Ajustar()
    {
        Camera cam = CameraController.Principal;
        if (cam == null || rt == null || Screen.height <= 0) return;

        // De frente a la cámara: su cara mira en la misma dirección en que la cámara lo ve, así recibe los toques
        Vector3 haciaBoton = rt.position - cam.transform.position;
        if (haciaBoton.sqrMagnitude > 0.000001f) rt.rotation = Quaternion.LookRotation(haciaBoton, Vector3.up);

        float distancia = haciaBoton.magnitude;
        float alto = rt.rect.height;
        Transform padre = rt.parent;
        float escalaPadre = padre != null ? Mathf.Abs(padre.lossyScale.y) : 1f;
        if (distancia < 0.0001f || alto <= 0f || escalaPadre <= 0f) return;

        // Alto que debe tener en el mundo para medir DiametroHotspot px del sistema en la pantalla
        float pixeles = diametro * EscalaAdaptable.PixelesPorUnidad;
        float altoMundo = pixeles / Screen.height * 2f * distancia * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float s = altoMundo / (alto * escalaPadre);
        Vector3 escala = new Vector3(s, s, s);

        if (toque == null) toque = GetComponent<ToqueSuave>();
        if (toque != null) toque.CambiarEscalaBase(escala);
        else rt.localScale = escala;
    }
}
