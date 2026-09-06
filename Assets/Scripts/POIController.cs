using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class POIController : MonoBehaviour
{
    [Header("Configuración del POI")]
    public string nombreEdificio = "Edificio 12";
    public string[] dependencias = {
        "Entrada Edificio 12",
        "Interior Primer Piso",
        "Piso 4",
        "Piso 5",
        "Piso 7 Entrada",
        "Piso 7 Interior",
        "Piso 10",
        "Piso 10 Interior",
        "Aula Magna",
        "Entrada 2 Edificio 12"
    };

    [Header("Materiales Skybox")]
    public Material[] skyboxes;

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
        for (int i = 0; i < dependencias.Length; i++)
        {
            GameObject boton = Instantiate(prefabBotonDependencia,
                                           contenedorDependencias);
            TextMeshProUGUI texto = boton.GetComponentInChildren
                                        <TextMeshProUGUI>();
            if (texto != null)
                texto.text = dependencias[i];

            int indice = i;
            boton.GetComponent<Button>().onClick.AddListener(() =>
            {
                SeleccionarDependencia(indice);
            });
        }
    }

    void SeleccionarDependencia(int indice)
    {
        if (indice < skyboxes.Length && skyboxes[indice] != null)
        {
            FadeController.Instance.CambiarSkybox(skyboxes[indice]);
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No hay skybox asignado para: "
                           + dependencias[indice]);
        }

        CerrarMenu();
    }

    public void ToggleMenu()
    {
        menuAbierto = !menuAbierto;
        if (panelMenu != null)
            panelMenu.SetActive(menuAbierto);
    }

    public void CerrarMenu()
    {
        menuAbierto = false;
        if (panelMenu != null)
            panelMenu.SetActive(false);
    }
}