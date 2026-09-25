using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class LugarZona
{
    public string nombre;
    public Material skybox;
}

[System.Serializable]
public class MenuDeZona
{
    [Tooltip("Nombre de la zona. También decide su color (ver Zonas en Aplicar Marca UdB).")]
    public string titulo;

    [Tooltip("Botón que se puso en la escena para marcar dónde va este menú. Se reemplaza por el punto de información.")]
    public GameObject boton;

    [Tooltip("Lugares que aparecen en el menú, en orden.")]
    public List<LugarZona> lugares = new List<LugarZona>();
}

/// <summary>
/// Crea, al iniciar, un punto de información igual al del Edificio 12 (POIController) para cada zona:
/// un círculo que al tocarlo abre la lista de lugares de esa zona y lleva a cada uno con el fundido.
/// El punto aparece donde está el botón de referencia, con el mismo tamaño aparente que el modelo,
/// y el botón de referencia se oculta.
/// </summary>
public class MenusDeZona : MonoBehaviour
{
    [Tooltip("Punto de información que sirve de modelo (el del Edificio 12). Si está vacío se busca en la escena.")]
    public POIController plantilla;

    public List<MenuDeZona> menus = new List<MenuDeZona>();

    [Tooltip("Oculta el botón de referencia y lo saca de los botones visibles del skybox.")]
    public bool ocultarBotonOriginal = true;

    void Awake()
    {
        if (plantilla == null) plantilla = FindObjectOfType<POIController>(true);
        if (plantilla == null)
        {
            Debug.LogWarning("[MenusDeZona] No hay un punto de información (POIController) que sirva de modelo.");
            return;
        }

        Camera cam = Camera.main;
        Vector3 centro = cam != null ? cam.transform.position : Vector3.zero;
        GestorTeleports gestor = FindObjectOfType<GestorTeleports>();

        foreach (MenuDeZona m in menus)
        {
            if (m == null || m.boton == null) continue;
            CrearPunto(m, centro);

            if (ocultarBotonOriginal)
            {
                QuitarDeGestor(gestor, m.boton);
                m.boton.SetActive(false);
            }
        }
    }

    void CrearPunto(MenuDeZona m, Vector3 centro)
    {
        Transform b = m.boton.transform;
        Transform modelo = plantilla.transform;

        POIController poi = Instantiate(plantilla, b.parent, false);
        poi.name = "POI_" + (string.IsNullOrEmpty(m.titulo) ? m.boton.name : m.titulo.Replace(" ", ""));

        // Donde está el botón, mirando a la cámara y con el mismo tamaño aparente que el modelo
        float distanciaModelo = Mathf.Max(0.01f, (modelo.position - centro).magnitude);
        float distanciaBoton = Mathf.Max(0.01f, (b.position - centro).magnitude);
        Vector3 direccion = (b.position - centro).normalized;

        poi.transform.position = b.position;
        poi.transform.rotation = Quaternion.LookRotation(direccion, Vector3.up);
        float escalaMundo = modelo.lossyScale.x * distanciaBoton / distanciaModelo;
        float escalaPadre = poi.transform.parent != null ? poi.transform.parent.lossyScale.x : 1f;
        poi.transform.localScale = Vector3.one * (escalaMundo / Mathf.Max(0.000001f, escalaPadre));

        // Datos del menú
        poi.nombreEdificio = m.titulo;
        List<string> nombres = new List<string>();
        List<Material> skyboxes = new List<Material>();
        foreach (LugarZona l in m.lugares)
        {
            if (l == null || l.skybox == null || string.IsNullOrEmpty(l.nombre)) continue;
            nombres.Add(l.nombre);
            skyboxes.Add(l.skybox);
        }
        poi.dependencias = nombres.ToArray();
        poi.skyboxes = skyboxes.ToArray();

        // Título del panel
        if (poi.panelMenu != null)
        {
            foreach (TMP_Text t in poi.panelMenu.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.gameObject.name == "TituloMenu")
                    t.text = m.titulo;
            }
        }

        // Los botones de lugares los crea POIController en su Start; se borran los que el modelo
        // ya hubiera creado (si el modelo arrancó antes) para no duplicarlos
        if (poi.contenedorDependencias != null)
        {
            for (int i = poi.contenedorDependencias.childCount - 1; i >= 0; i--)
                Destroy(poi.contenedorDependencias.GetChild(i).gameObject);
        }

        poi.gameObject.SetActive(true);
    }

    static void QuitarDeGestor(GestorTeleports gestor, GameObject boton)
    {
        if (gestor == null || gestor.configuraciones == null) return;
        foreach (ConfiguracionSkybox c in gestor.configuraciones)
            if (c != null && c.teleportsVisibles != null)
                c.teleportsVisibles.RemoveAll(go => go == boton);
    }
}
