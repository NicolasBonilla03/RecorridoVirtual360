using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ConfiguracionSkybox
{
    public string nombreSkybox;
    public Material skybox;
    public List<GameObject> teleportsVisibles;
}

public class GestorTeleports : MonoBehaviour
{
    public static GestorTeleports Instance;

    [Header("Inicio del recorrido")]
    [Tooltip("Skybox con el que arranca siempre el recorrido (vista general).")]
    public Material skyboxInicial;

    [Header("Configuración de Skyboxes y sus TPs")]
    public List<ConfiguracionSkybox> configuraciones;

    void Awake()
    {
        Instance = this;

        // El recorrido siempre arranca en el skybox inicial, aunque en el editor
        // se haya dejado otro skybox puesto en la ventana Lighting.
        if (skyboxInicial != null)
        {
            RenderSettings.skybox = skyboxInicial;
            DynamicGI.UpdateEnvironment();
        }
    }

    void Start()
    {
        // Ocultar todos los TPs y mostrar solo los del skybox con el que se arranca
        OcultarTodos();
        ActivarTeleports(RenderSettings.skybox);
    }

    public void ActivarTeleports(Material skyboxActual)
    {
        // Ocultar todos primero
        OcultarTodos();

        if (configuraciones == null) return;

        // Activar solo los que corresponden al skybox actual
        foreach (ConfiguracionSkybox config in configuraciones)
        {
            if (config != null && config.skybox == skyboxActual)
            {
                if (config.teleportsVisibles == null) break;
                foreach (GameObject tp in config.teleportsVisibles)
                {
                    if (tp != null)
                        tp.SetActive(true);
                }
                break;
            }
        }
    }

    void OcultarTodos()
    {
        GameObject[] tps = GameObject.FindGameObjectsWithTag("TP");
        foreach (GameObject tp in tps)
            tp.SetActive(false);
    }
}
