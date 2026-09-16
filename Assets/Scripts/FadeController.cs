using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance;
    public Image panelFade;
    public float velocidadFade = 1.5f;

    void Awake()
    {
        Instance = this;
        if (panelFade != null)
            panelFade.raycastTarget = false;
    }

    public void CambiarSkybox(Material nuevoSkybox)
    {
        StartCoroutine(TransicionSkybox(nuevoSkybox));
    }

    IEnumerator TransicionSkybox(Material nuevoSkybox)
    {
        panelFade.raycastTarget = true;

        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * velocidadFade;
            panelFade.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        RenderSettings.skybox = nuevoSkybox;
        DynamicGI.UpdateEnvironment();

        // Activar TPs correspondientes al nuevo skybox
        GestorTeleports.Instance.ActivarTeleports(nuevoSkybox);

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * velocidadFade;
            panelFade.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        panelFade.color = new Color(0, 0, 0, 0);
        panelFade.raycastTarget = false;
    }
}