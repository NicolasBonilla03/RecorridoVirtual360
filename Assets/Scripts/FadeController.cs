using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance;
    public Image panelFade;
    public float velocidadFade = 1.5f;

    // true mientras hay un fundido en curso (evita transiciones encimadas por doble clic)
    public bool EnTransicion { get; private set; }

    void Awake()
    {
        Instance = this;
        if (panelFade != null)
            panelFade.raycastTarget = false;
    }

    void OnDisable()
    {
        EnTransicion = false;
    }

    public void CambiarSkybox(Material nuevoSkybox)
    {
        if (nuevoSkybox == null || EnTransicion)
            return;

        StartCoroutine(TransicionSkybox(nuevoSkybox));
    }

    IEnumerator TransicionSkybox(Material nuevoSkybox)
    {
        EnTransicion = true;
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
        if (GestorTeleports.Instance != null)
            GestorTeleports.Instance.ActivarTeleports(nuevoSkybox);

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * velocidadFade;
            panelFade.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        panelFade.color = new Color(0, 0, 0, 0);
        panelFade.raycastTarget = false;
        EnTransicion = false;
    }
}
