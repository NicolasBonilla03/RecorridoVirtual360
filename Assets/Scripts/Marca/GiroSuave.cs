using UnityEngine;

/// <summary>
/// Giro corto (150 ms, salida fuerte) para las flechas de las listas plegables: al abrir o cerrar una
/// sección la flecha rota en vez de saltar, lo que indica el cambio de estado sin hacerse esperar.
/// La primera vez que se pone una flecha (al construir la lista) no anima. Solo toca la rotación.
/// </summary>
[DisallowMultipleComponent]
public class GiroSuave : MonoBehaviour
{
    const float Duracion = 0.15f;

    float inicio;
    float objetivo;
    float t = 1f;

    public static void Poner(Transform flecha, float z)
    {
        if (flecha == null) return;
        GiroSuave g = flecha.GetComponent<GiroSuave>();
        bool primeraVez = g == null;
        if (primeraVez) g = flecha.gameObject.AddComponent<GiroSuave>();

        if (primeraVez || !AnimacionesUI.Activas || !flecha.gameObject.activeInHierarchy)
        {
            g.objetivo = z;
            g.t = 1f;
            g.enabled = false;
            flecha.localEulerAngles = new Vector3(0f, 0f, z);
            return;
        }

        // Interrumpible: parte del ángulo actual
        float actual = flecha.localEulerAngles.z;
        if (actual > 180f) actual -= 360f;
        g.inicio = actual;
        g.objetivo = z;
        g.t = 0f;
        g.enabled = true;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime / Duracion;
        float e = AnimacionesUI.SalidaFuerte(Mathf.Clamp01(t));
        transform.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(inicio, objetivo, e));
        if (t >= 1f) enabled = false;
    }

    void OnDisable()
    {
        // Si la lista se esconde a mitad del giro, la flecha queda en su posición final
        if (t < 1f) { t = 1f; transform.localEulerAngles = new Vector3(0f, 0f, objetivo); }
    }
}
