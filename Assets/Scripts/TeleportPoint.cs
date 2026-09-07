using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeleportPoint : MonoBehaviour
{
    [Header("Configuración del Teletransporte")]
    public string nombreDestino = "Destino";
    public Material skyboxDestino;

    [Header("Referencias UI")]
    public GameObject panelConfirmacion;
    public TextMeshProUGUI textoDestino;

    private bool panelAbierto = false;

    void Start()
    {
        if (panelConfirmacion != null)
        {
            panelConfirmacion.SetActive(false);
            if (textoDestino != null)
                textoDestino.text = "¿Ir a " + nombreDestino + "?";
        }
    }

    public void AlHacerClick()
    {
        panelAbierto = !panelAbierto;
        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(panelAbierto);
    }

    public void Confirmar()
    {
        if (skyboxDestino != null)
        {
            FadeController.Instance.CambiarSkybox(skyboxDestino);
            CerrarPanel();
        }
        else
        {
            Debug.LogWarning("No hay skybox asignado para: "
                           + nombreDestino);
        }
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(false);
    }
}