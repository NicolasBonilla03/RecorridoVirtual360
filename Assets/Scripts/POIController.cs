using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class POIController : MonoBehaviour
{
    [Header("Configuración del POI")]
    public string nombreEdificio = "Edificio Central";
    public string[] dependencias = {
        "Bienestar Universitario",
        "Relaciones Internacionales (DIRI)",
        "Rectoría",
        "Cafetería",
        "Hall de Artes",
        "CENSEI"
    };

    [Header("Referencias UI")]
    public GameObject panelMenu;
    public Transform contenedorDependencias;
    public GameObject prefabBotonDependencia;

    private bool menuAbierto = false;

    void Start()
    {
        if (panelMenu != null)
            panelMenu.SetActive(false);

        GenerarBotonesDependencias();
    }

    void GenerarBotonesDependencias()
    {
        foreach (string dependencia in dependencias)
        {
            GameObject boton = Instantiate(prefabBotonDependencia,
                                           contenedorDependencias);
            TextMeshProUGUI texto = boton.GetComponentInChildren
                                        <TextMeshProUGUI>();
            if (texto != null)
                texto.text = dependencia;

            string nombreDep = dependencia;
            boton.GetComponent<Button>().onClick.AddListener(() =>
            {
                SeleccionarDependencia(nombreDep);
            });
        }
    }

    public void ToggleMenu()
    {
        menuAbierto = !menuAbierto;
        if (panelMenu != null)
            panelMenu.SetActive(menuAbierto);
    }

    void SeleccionarDependencia(string nombre)
    {
        Debug.Log("Navegando a: " + nombre);
        // Aquí después conectaremos el cambio de imagen 360°
        CerrarMenu();
    }

    public void CerrarMenu()
    {
        menuAbierto = false;
        if (panelMenu != null)
            panelMenu.SetActive(false);
    }
}