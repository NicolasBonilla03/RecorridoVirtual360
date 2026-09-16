using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    [Header("Configuración del Teletransporte")]
    public string nombreDestino = "Destino";
    public Material skyboxDestino;

    public void AlHacerClick()
    {
        if (skyboxDestino != null)
        {
            // Cambiar skybox con fade
            FadeController.Instance.CambiarSkybox(skyboxDestino);

            // Ocultar todos los TPs y POIs de la escena
            OcultarTodos();
        }
        else
        {
            Debug.LogWarning("No hay skybox asignado para: "
                           + nombreDestino);
        }
    }

    void OcultarTodos()
    {
        // Ocultar todos los TeleportPoints
        GameObject[] tps = GameObject.FindGameObjectsWithTag("TP");
        foreach (GameObject tp in tps)
            tp.SetActive(false);

        // Ocultar todos los POIs
        GameObject[] pois = GameObject.FindGameObjectsWithTag("POI");
        foreach (GameObject poi in pois)
            poi.SetActive(false);
    }
}