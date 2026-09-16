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

    [Header("Configuración de Skyboxes y sus TPs")]
    public List<ConfiguracionSkybox> configuraciones;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ocultar todos los TPs al inicio
        OcultarTodos();
    }

    public void ActivarTeleports(Material skyboxActual)
    {
        // Ocultar todos primero
        OcultarTodos();

        // Activar solo los que corresponden al skybox actual
        foreach (ConfiguracionSkybox config in configuraciones)
        {
            if (config.skybox == skyboxActual)
            {
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