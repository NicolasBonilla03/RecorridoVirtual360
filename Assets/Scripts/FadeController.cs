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
    }

    public void CambiarSkybox(Material nuevoSkybox)
    {
        StartCoroutine(TransicionSkybox(nuevoSkybox));
    }

    IEnumerator TransicionSkybox(Material nuevoSkybox)
    {
        // Fade a negro
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * velocidadFade;
            panelFade.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Cambiar el skybox
        RenderSettings.skybox = nuevoSkybox;
        DynamicGI.UpdateEnvironment();

        // Fade de vuelta
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * velocidadFade;
            panelFade.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        panelFade.color = new Color(0, 0, 0, 0);
    }
}