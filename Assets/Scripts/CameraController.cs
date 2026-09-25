using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mirar alrededor en la escena 360°.
/// Escritorio: arrastrar con clic izquierdo y rueda del mouse para acercar.
/// Móvil: arrastrar con un dedo y pellizcar con dos para acercar.
/// El zoom es acotado: acercarse no rompe la sensación de estar parado en un punto.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Configuración")]
    public float sensibilidad = 2f;
    public float suavizado = 5f;

    [Header("Táctil")]
    [Tooltip("1 = la imagen sigue al dedo exactamente.")]
    public float sensibilidadTactil = 1f;

    [Header("Zoom acotado")]
    [Tooltip("Cuánto se puede acercar como máximo (0.5 = la mitad del campo de visión).")]
    [Range(0.3f, 1f)] public float zoomMaximo = 0.55f;
    public float pasoRueda = 0.08f;

    [Header("Móvil vertical")]
    [Tooltip("Campo de visión horizontal que se busca en pantallas verticales, para que no se vea como por un tubo.")]
    public float fovHorizontalVertical = 75f;
    [Tooltip("Tope del campo de visión vertical en pantallas verticales (evita la deformación de un gran angular).")]
    public float fovVerticalMaximo = 95f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;
    private bool rotando = false;

    private Camera cam;
    private float fovBase;      // el campo de visión de la cámara al iniciar
    private float zoom = 1f;    // 1 = sin zoom
    private bool arrastreTactil = false;
    private float ultimoAspecto = -1f;

    void Start()
    {
        // Inicializar con la rotación actual de la cámara
        rotacionX = transform.eulerAngles.y;
        rotacionY = transform.eulerAngles.x;
        if (rotacionY > 180f) rotacionY -= 360f;

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null) fovBase = cam.fieldOfView;
    }

    void Update()
    {
        AjustarCampoDeVision();

        // En móvil se usa el tacto directamente; los toques también generan clics simulados,
        // así que mientras haya dedos en pantalla se ignora el camino del mouse
        if (Input.touchCount > 0)
        {
            ManejarTactil();
            rotando = false;
            return;
        }
        arrastreTactil = false;

        // Activar rotación solo con clic izquierdo mantenido,
        // y no cuando el clic empieza sobre un botón o sobre el menú
        if (Input.GetMouseButtonDown(0) && !PunteroSobreUI())
            rotando = true;

        if (Input.GetMouseButtonUp(0))
            rotando = false;

        if (rotando)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensibilidad;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidad;

            rotacionX -= mouseX;
            rotacionY += mouseY;

            // Limitar la rotación vertical para no dar vuelta completa
            rotacionY = Mathf.Clamp(rotacionY, -80f, 80f);

            Quaternion rotacionObjetivo = Quaternion.Euler(rotacionY,
                                                           rotacionX,
                                                           0f);
            transform.rotation = Quaternion.Lerp(transform.rotation,
                                                  rotacionObjetivo,
                                                  Time.deltaTime * suavizado);
        }

        // Rueda del mouse: zoom acotado
        float rueda = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rueda) > 0.01f && !PunteroSobreUI())
            CambiarZoom(-rueda * pasoRueda);
    }

    void ManejarTactil()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            // Un arrastre que empieza sobre un botón o el menú no mueve la cámara
            if (t.phase == TouchPhase.Began)
                arrastreTactil = !SobreUI(t.fingerId);

            if (arrastreTactil && t.phase == TouchPhase.Moved && cam != null)
            {
                // Grados por píxel según el campo de visión: la imagen sigue al dedo
                float gradosPorPixel = cam.fieldOfView / Mathf.Max(1f, Screen.height) * sensibilidadTactil;
                rotacionX -= t.deltaPosition.x * gradosPorPixel;
                rotacionY += t.deltaPosition.y * gradosPorPixel;
                rotacionY = Mathf.Clamp(rotacionY, -80f, 80f);
                transform.rotation = Quaternion.Euler(rotacionY, rotacionX, 0f);
            }
        }
        else if (Input.touchCount >= 2)
        {
            // Pellizcar: zoom acotado. Al levantar un dedo no se retoma el arrastre hasta el próximo toque
            arrastreTactil = false;
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            float distancia = Vector2.Distance(a.position, b.position);
            float anterior = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
            if (anterior > 1f && distancia > 1f)
                CambiarZoom((anterior - distancia) / Mathf.Max(1f, Screen.height));
        }
    }

    void CambiarZoom(float delta)
    {
        zoom = Mathf.Clamp(zoom + delta, zoomMaximo, 1f);
        AplicarCampoDeVision();
    }

    // En pantallas verticales se abre el campo de visión vertical para conservar un horizontal cómodo
    void AjustarCampoDeVision()
    {
        if (cam == null) return;
        float aspecto = cam.aspect;
        if (Mathf.Approximately(aspecto, ultimoAspecto)) return;
        ultimoAspecto = aspecto;
        AplicarCampoDeVision();
    }

    void AplicarCampoDeVision()
    {
        if (cam == null) return;
        float vertical = fovBase;
        if (cam.aspect < 1f)
        {
            float mitad = Mathf.Tan(fovHorizontalVertical * 0.5f * Mathf.Deg2Rad) / Mathf.Max(0.1f, cam.aspect);
            float necesario = 2f * Mathf.Atan(mitad) * Mathf.Rad2Deg;
            vertical = Mathf.Clamp(necesario, fovBase, fovVerticalMaximo);
        }
        cam.fieldOfView = vertical * zoom;
    }

    bool PunteroSobreUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        return false;
    }

    static bool SobreUI(int dedo)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(dedo);
    }
}
