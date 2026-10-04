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

    const float CadaCuanto = 0.75f; // segundos entre revisiones de botones nuevos
    float proxima;

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
        proxima -= Time.unscaledDeltaTime;
        if (proxima > 0f) return;
        proxima = CadaCuanto;

        // Los menús crean botones sobre la marcha: se revisa cada cierto tiempo y se equipan los nuevos
        foreach (Button b in FindObjectsOfType<Button>(true))
        {
            if (b == null || b.transition == Selectable.Transition.None) continue; // los velos de fondo no se animan
            if (b.GetComponent<ToqueSuave>() == null) b.gameObject.AddComponent<ToqueSuave>();
        }
    }
}
