using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mirar alrededor en la escena 360°.
/// Escritorio: arrastrar con clic izquierdo y rueda del mouse para acercar.
/// Móvil: arrastrar con un dedo (con suavizado e inercia) y pellizcar con dos para acercar.
/// El zoom es acotado: acercarse no rompe la sensación de estar parado en un punto.
/// </summary>
public class CameraController : MonoBehaviour
{
    static Camera principal;

    /// <summary>
    /// La cámara del recorrido. No usa solo Camera.main porque el objeto «MainCamera» de la escena no tiene la
    /// etiqueta MainCamera (y Camera.main volvía vacío): toma la cámara de este controlador y, si no hay,
    /// Camera.main o la primera cámara de la escena.
    /// </summary>
    public static Camera Principal
    {
        get
        {
            if (principal == null)
            {
                CameraController c = FindObjectOfType<CameraController>();
                if (c != null) principal = c.GetComponent<Camera>();
                if (principal == null) principal = Camera.main;
                if (principal == null) principal = FindObjectOfType<Camera>();
            }
            return principal;
        }
    }

    [Header("Mouse")]
    public float sensibilidad = 2f;
    [Tooltip("Qué tan rápido la cámara alcanza el objetivo con el mouse.")]
    public float suavizado = 8f;

    [Header("Táctil")]
    [Tooltip("1 = la imagen sigue al dedo exactamente (estilo Google Maps). Súbelo para girar más rápido.")]
    public float sensibilidadTactil = 1.3f;
    [Tooltip("Marcado: la vista gira hacia donde mueves el dedo. Desmarcado: la imagen sigue al dedo (como Google Maps).")]
    public bool invertirTactilX = true;
    [Tooltip("Marcado: deslizar hacia arriba mira hacia arriba. Desmarcado: deslizar hacia arriba mira hacia abajo.")]
    public bool invertirTactilY = true;
    [Tooltip("Qué tan directo responde al dedo. Más alto = más pegado al dedo; más bajo = más suave.")]
    public float suavizadoTactil = 20f;

    [Header("Inercia táctil")]
    public bool usarInercia = true;
    [Tooltip("Qué tan rápido se frena al soltar el dedo. Más alto = se detiene antes.")]
    public float frenadoInercia = 4f;

    [Header("Zoom acotado")]
    [Tooltip("Cuánto se puede acercar como máximo (0.5 = la mitad del campo de visión).")]
    [Range(0.3f, 1f)] public float zoomMaximo = 0.55f;
    public float pasoRueda = 0.08f;

    [Header("Móvil vertical")]
    [Tooltip("Campo de visión horizontal que se busca en pantallas verticales, para que no se vea como por un tubo.")]
    public float fovHorizontalVertical = 75f;
    [Tooltip("Tope del campo de visión vertical en pantallas verticales (evita la deformación de un gran angular).")]
    public float fovVerticalMaximo = 95f;

    // Rotación objetivo; la cámara real la persigue con suavizado
    private float rotacionX = 0f;
    private float rotacionY = 0f;

    private bool rotando = false;           // arrastre con mouse
    private bool arrastreTactil = false;
    private bool usandoTactil = false;
    private bool animando = false;          // la cámara aún no alcanzó el objetivo
    private Vector2 velocidad = Vector2.zero; // grados/segundo, para la inercia

    private Camera cam;
    private float fovBase;      // el campo de visión de la cámara al iniciar
    private float zoom = 1f;    // 1 = sin zoom
    private float distanciaPellizco; // distancia entre los dos dedos en el cuadro anterior (0 = sin pellizco)
    private float ultimoAspecto = -1f;

    void Start()
    {
        SincronizarDesdeTransform();

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
        AplicarInercia();
        ManejarMouse();
    }

    void LateUpdate()
    {
        if (!animando) return;

        Quaternion objetivo = Quaternion.Euler(rotacionY, rotacionX, 0f);
        float k = usandoTactil ? suavizadoTactil : suavizado;
        float t = 1f - Mathf.Exp(-k * Time.deltaTime); // independiente de los FPS
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, t);

        if (Quaternion.Angle(transform.rotation, objetivo) < 0.01f)
        {
            transform.rotation = objetivo;
            animando = false;
        }
    }

    // Si otro script movió la cámara mientras estaba quieta, se retoma desde ahí
    void SincronizarDesdeTransform()
    {
        rotacionX = transform.eulerAngles.y;
        rotacionY = transform.eulerAngles.x;
        if (rotacionY > 180f) rotacionY -= 360f;
    }

    void Girar(float dX, float dY)
    {
        rotacionX += dX;
        // Limitar la rotación vertical para no dar vuelta completa
        rotacionY = Mathf.Clamp(rotacionY + dY, -80f, 80f);
        animando = true;
    }

    // ---------------- Mouse ----------------

    void ManejarMouse()
    {
        // Rotación solo con clic izquierdo mantenido,
        // y no cuando el clic empieza sobre un botón o sobre el menú
        if (Input.GetMouseButtonDown(0) && !PunteroSobreUI())
        {
            rotando = true;
            if (!animando) SincronizarDesdeTransform();
        }

        if (Input.GetMouseButtonUp(0))
            rotando = false;

        if (rotando)
        {
            usandoTactil = false;
            Girar(-Input.GetAxis("Mouse X") * sensibilidad,
                   Input.GetAxis("Mouse Y") * sensibilidad);
        }

        // Rueda del mouse: zoom acotado
        float rueda = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rueda) > 0.01f && !PunteroSobreUI())
            CambiarZoom(-rueda * pasoRueda);
    }

    // ---------------- Táctil ----------------

    void ManejarTactil()
    {
        usandoTactil = true;
        if (Input.touchCount < 2) distanciaPellizco = 0f; // el próximo pellizco empieza de cero

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            // Un arrastre que empieza sobre un botón o el menú no mueve la cámara
            if (t.phase == TouchPhase.Began)
            {
                arrastreTactil = !SobreUI(t.fingerId);
                velocidad = Vector2.zero;
                if (arrastreTactil && !animando) SincronizarDesdeTransform();
            }

            if (!arrastreTactil || cam == null) return;

            if (t.phase == TouchPhase.Moved)
            {
                // Grados por píxel según el campo de visión
                float gradosPorPixel = cam.fieldOfView / Mathf.Max(1f, Screen.height) * sensibilidadTactil;

                // Sin invertir = la imagen sigue al dedo. Invertido = la vista va hacia donde va el dedo.
                float signoX = invertirTactilX ? 1f : -1f;
                float signoY = invertirTactilY ? -1f : 1f;

                float dX = signoX * t.deltaPosition.x * gradosPorPixel;
                float dY = signoY * t.deltaPosition.y * gradosPorPixel;
                Girar(dX, dY);

                // Velocidad para la inercia (promediada para evitar tirones)
                float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                velocidad = Vector2.Lerp(velocidad, new Vector2(dX, dY) / dt, 0.4f);
            }
            else if (t.phase == TouchPhase.Stationary)
            {
                // Si se queda quieto antes de soltar, no hay inercia
                velocidad = Vector2.zero;
            }
        }
        else if (Input.touchCount >= 2)
        {
            // Pellizcar: zoom acotado. Al levantar un dedo no se retoma el arrastre hasta el próximo toque
            arrastreTactil = false;
            velocidad = Vector2.zero;

            // La distancia anterior se guarda de un cuadro al siguiente en vez de calcularla con deltaPosition:
            // en el navegador ese valor puede llegar con el signo al revés (por eso el arrastre de un dedo
            // necesita invertirTactil) y el pellizco quedaba invertido. Separar los dedos acerca; juntarlos aleja.
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            float distancia = Vector2.Distance(a.position, b.position);
            bool empieza = a.phase == TouchPhase.Began || b.phase == TouchPhase.Began || distanciaPellizco <= 0f;
            if (!empieza && distancia > 1f)
                CambiarZoom((distanciaPellizco - distancia) / Mathf.Max(1f, Screen.height));
            distanciaPellizco = distancia;
        }
    }

    // Sigue girando un poco después de soltar el dedo y se va frenando
    void AplicarInercia()
    {
        if (!usarInercia || velocidad.sqrMagnitude < 0.25f)
        {
            velocidad = Vector2.zero;
            return;
        }

        Girar(velocidad.x * Time.deltaTime, velocidad.y * Time.deltaTime);
        velocidad *= Mathf.Exp(-frenadoInercia * Time.deltaTime);
    }

    // ---------------- Zoom y campo de visión ----------------

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

    // ---------------- UI ----------------

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