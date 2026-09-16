using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class PuntoRecorrido
{
    public string nombre;
    public Material skybox;
}

[System.Serializable]
public class EdificioRecorrido
{
    public string nombreEdificio;
    public List<PuntoRecorrido> puntos;
    public bool desplegado = false;
}

public class MenuNavegacion : MonoBehaviour
{
    [Header("Edificios y Puntos")]
    public List<EdificioRecorrido> edificios;

    [Header("Referencias UI")]
    public GameObject panelMenu;
    public Transform contenedor;
    public GameObject prefabEncabezadoEdificio;
    public GameObject prefabBotonPunto;
    public Button botonToggleMenu;

    private bool menuAbierto = false;

    void Start()
    {
        if (panelMenu != null)
            panelMenu.SetActive(false);

        GenerarMenu();
    }

    void GenerarMenu()
    {
        // Limpiar contenedor
        foreach (Transform hijo in contenedor)
            Destroy(hijo.gameObject);

        foreach (EdificioRecorrido edificio in edificios)
        {
            // Crear encabezado del edificio
            GameObject encabezado = Instantiate(prefabEncabezadoEdificio,
                                                contenedor);
            TextMeshProUGUI textoEncabezado = encabezado
                                             .GetComponentInChildren
                                             <TextMeshProUGUI>();
            if (textoEncabezado != null)
                textoEncabezado.text = (edificio.desplegado ? "▼ " : "► ")
                                       + edificio.nombreEdificio;

            EdificioRecorrido edRef = edificio;
            GameObject encRef = encabezado;

            encabezado.GetComponent<Button>().onClick.AddListener(() =>
            {
                edRef.desplegado = !edRef.desplegado;
                TextMeshProUGUI txt = encRef.GetComponentInChildren
                                           <TextMeshProUGUI>();
                if (txt != null)
                    txt.text = (edRef.desplegado ? "▼ " : "► ")
                               + edRef.nombreEdificio;
                GenerarMenu();
            });

            // Crear botones de puntos si está desplegado
            if (edificio.desplegado)
            {
                foreach (PuntoRecorrido punto in edificio.puntos)
                {
                    GameObject boton = Instantiate(prefabBotonPunto,
                                                   contenedor);
                    TextMeshProUGUI textoPunto = boton
                                               .GetComponentInChildren
                                               <TextMeshProUGUI>();
                    if (textoPunto != null)
                        textoPunto.text = "   " + punto.nombre;

                    Material skyboxRef = punto.skybox;
                    boton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (skyboxRef != null)
                            FadeController.Instance.CambiarSkybox(skyboxRef);
                        else
                            Debug.LogWarning("Sin skybox: " + punto.nombre);

                        // Ocultar todos los POI de la escena
                        GameObject[] pois = GameObject.FindGameObjectsWithTag("POI");
                        foreach (GameObject poi in pois)
                            poi.SetActive(false);

                        ToggleMenu();
                    });
                }
            }
        }
    }

    public void ToggleMenu()
    {
        menuAbierto = !menuAbierto;
        if (panelMenu != null)
            panelMenu.SetActive(menuAbierto);
    }
}