using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajusta el CanvasScaler de una interfaz al tipo de pantalla y lo vuelve a ajustar si la pantalla
/// cambia (por ejemplo, al girar el celular). En escritorio deja la referencia 1280×720 del sistema;
/// en celular hace que un px del sistema se vea casi como un px CSS. MarcaUdB.EscalarSegunPantalla lo añade.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public class EscalaAdaptable : MonoBehaviour
{
    CanvasScaler escalador;
    int ancho;
    int alto;

    void Update()
    {
        if (Screen.width != ancho || Screen.height != alto) Aplicar();
    }

    public void Aplicar()
    {
        if (escalador == null) escalador = GetComponent<CanvasScaler>();
        if (escalador == null) return;
        ancho = Screen.width;
        alto = Screen.height;
        if (ancho <= 0 || alto <= 0) return;

        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        float vertical = (float)alto / ancho;

        if (vertical >= 1.5f)
        {
            // Celular en vertical: el ancho manda
            escalador.referenceResolution = new Vector2(MarcaUdB.AnchoMovil, MarcaUdB.AnchoMovil * vertical);
            escalador.matchWidthOrHeight = 0f;
        }
        else if (EsCelularHorizontal(1f / vertical))
        {
            // Celular en horizontal: la altura manda
            escalador.referenceResolution = new Vector2(MarcaUdB.AltoMovilHorizontal / vertical, MarcaUdB.AltoMovilHorizontal);
            escalador.matchWidthOrHeight = 1f;
        }
        else
        {
            escalador.referenceResolution = MarcaUdB.ResolucionReferencia;
            escalador.matchWidthOrHeight = 0.5f;
        }
    }

    /// <summary>
    /// Píxeles de pantalla que mide un px del sistema, con las mismas reglas que aplica esta escala a los
    /// lienzos de la interfaz. Sirve para que lo que está en el espacio 3D se mida igual que la interfaz.
    /// </summary>
    public static float PixelesPorUnidad
    {
        get
        {
            if (Screen.width <= 0 || Screen.height <= 0) return 1f;
            float vertical = (float)Screen.height / Screen.width;
            if (vertical >= 1.5f) return Screen.width / MarcaUdB.AnchoMovil;
            if (EsCelularHorizontal(1f / vertical)) return Screen.height / MarcaUdB.AltoMovilHorizontal;
            // Escritorio: igual que CanvasScaler con «match» 0.5 entre ancho y alto de la referencia
            Vector2 r = MarcaUdB.ResolucionReferencia;
            float logAncho = Mathf.Log(Screen.width / r.x, 2f), logAlto = Mathf.Log(Screen.height / r.y, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logAncho, logAlto, 0.5f));
        }
    }

    /// <summary>
    /// true si la pantalla tiene forma de celular (vertical, o horizontal muy alargada), con las mismas reglas
    /// que usa la escala. Ahí no hay efectos al pasar el cursor: la interfaz es para el dedo.
    /// </summary>
    public static bool EsPantallaDeCelular
    {
        get
        {
            if (Screen.width <= 0 || Screen.height <= 0) return false;
            float vertical = (float)Screen.height / Screen.width;
            return vertical >= 1.5f || EsCelularHorizontal(1f / vertical);
        }
    }

    static bool EsCelularHorizontal(float proporcion)
    {
        if (proporcion < 1.6f) return false;
        if (Application.isMobilePlatform) return true;
        // En el editor se puede probar con una vista Game muy alargada (por ejemplo 2340×1080)
        return Application.isEditor && proporcion >= 1.95f;
    }
}
