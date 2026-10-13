using UnityEngine;

/// <summary>
/// Fundido del velo oscuro que va detrás de los paneles (menú, guía, ficha). Antes aparecía de golpe;
/// ahora entra en 160 ms y sale en 140 ms con la curva de salida fuerte. Es interrumpible: si el panel
/// se vuelve a abrir a mitad del cierre, el velo sigue desde donde va, sin saltos.
/// Solo cambia la opacidad de un CanvasGroup (no recalcula la interfaz) y se apaga al terminar.
/// Se usa también para los controles flotantes (botón de menú, chip, hoja del lugar y botón del guía), que se
/// funden al abrir un panel en lugar de desaparecer de golpe.
/// </summary>
[DisallowMultipleComponent]
public class VeloSuave : MonoBehaviour
{
    const float Entrada = 0.16f;
    const float Salida = 0.14f;

    CanvasGroup grupo;
    float progreso;      // 0 = invisible, 1 = visible
    bool visible;

    public static void Mostrar(GameObject velo)
    {
        if (velo != null) Obtener(velo).Poner(true);
    }

    public static void Ocultar(GameObject velo)
    {
        if (velo != null && velo.activeSelf) Obtener(velo).Poner(false);
    }

    static VeloSuave Obtener(GameObject velo)
    {
        VeloSuave v = velo.GetComponent<VeloSuave>();
        if (v == null)
        {
            v = velo.AddComponent<VeloSuave>();
            // También sirve para botones que ya están en pantalla: arranca del estado en que los encuentra
            v.visible = velo.activeSelf;
            v.progreso = velo.activeSelf ? 1f : 0f;
            v.enabled = false;
        }
        return v;
    }

    void Preparar()
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
    }

    void Poner(bool mostrar)
    {
        Preparar();
        visible = mostrar;
        grupo.blocksRaycasts = mostrar; // mientras se va, ya no atrapa toques
        if (mostrar && !gameObject.activeSelf)
        {
            progreso = 0f;
            Aplicar();
            gameObject.SetActive(true);
        }

        if (!AnimacionesUI.Activas)
        {
            progreso = mostrar ? 1f : 0f;
            Aplicar();
            if (!mostrar) gameObject.SetActive(false);
            enabled = false;
            return;
        }
        enabled = true;
    }

    void Update()
    {
        float destino = visible ? 1f : 0f;
        progreso = Mathf.MoveTowards(progreso, destino, Time.unscaledDeltaTime / (visible ? Entrada : Salida));
        Aplicar();
        if (Mathf.Approximately(progreso, destino))
        {
            enabled = false;
            if (!visible) gameObject.SetActive(false);
        }
    }

    void Aplicar()
    {
        if (grupo == null) return;
        // Entrando: arranca rápido. Saliendo: se va rápido al principio y termina suave.
        grupo.alpha = visible
            ? AnimacionesUI.SalidaFuerte(progreso)
            : 1f - AnimacionesUI.SalidaFuerte(1f - progreso);
    }
}
