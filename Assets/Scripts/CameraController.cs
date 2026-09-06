using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configuración")]
    public float sensibilidad = 2f;
    public float suavizado = 5f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;
    private bool rotando = false;

    void Start()
    {
        // Inicializar con la rotación actual de la cámara
        rotacionX = transform.eulerAngles.y;
        rotacionY = transform.eulerAngles.x;
    }

    void Update()
    {
        // Activar rotación solo con clic izquierdo mantenido
        if (Input.GetMouseButtonDown(0))
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
    }
}