using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        "Aula Magna",
        "Entrada 2 Edificio 12"
    };

    [Header("Materiales Skybox")]
    public Material[] skyboxes;

    [Header("Referencias UI")]
    public GameObject panelMenu;
    public Transform contenedorDependencias;
    public GameObject prefabBotonDependencia;

    [Header("Menú abierto")]
    [Tooltip("Filas que avanza la lista por cada paso de la rueda del ratón.")]
    public float filasPorPasoDeRueda = 1f;

    [Tooltip("Opacidad del resto de botones del recorrido mientras este menú está abierto.")]
    [Range(0f, 1f)] public float opacidadDelResto = 0.4f;

    private bool menuAbierto = false;

    // ------------------------------------------------------------------ Un solo menú a la vez
    // Mientras un menú está abierto es lo único que responde: los demás puntos, los hotspots y
    // el botón «Menú» no reciben clics. Se cierra con la X, al elegir un lugar, tocando otra vez
    // el punto, con Esc o con un clic o toque fuera del menú. Arrastrar para mirar alrededor
    // no lo cierra.

    static POIController abierto;
    public static bool HayMenuAbierto { get { return abierto != null; } }

    public static void CerrarAbierto()
    {
        if (abierto != null) abierto.CerrarMenu();
    }

    struct EstadoGrupo { public CanvasGroup grupo; public bool creado; public bool bloqueaba; public float alfa; }
    readonly List<EstadoGrupo> gruposBloqueados = new List<EstadoGrupo>();
    CanvasGroup grupoPropio;
    int indiceOriginal = -1;

    bool presionFuera;
    Vector2 inicioPresion;
    int dedoPresion = -1;
    static readonly List<RaycastResult> impactos = new List<RaycastResult>();

    void Start()
    {
        if (panelMenu != null)
            panelMenu.SetActive(false);

        GenerarBotonesDependencias();
        AjustarRueda();
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
        if (menuAbierto) CerrarMenu();
        else AbrirMenu();
    }

    public void AbrirMenu()
    {
        if (menuAbierto) return;

        if (abierto != null && abierto != this) abierto.CerrarMenu();
        MenuDesplegable.CerrarSiAbierto();

        menuAbierto = true;
        abierto = this;
        if (panelMenu != null)
            panelMenu.SetActive(true);

        // Por encima de todo lo demás: en un lienzo del mundo se dibuja en orden de jerarquía
        indiceOriginal = transform.GetSiblingIndex();
        transform.SetAsLastSibling();

        BloquearResto();
        presionFuera = false;
        dedoPresion = -1;
    }

    public void CerrarMenu()
    {
        bool estaba = menuAbierto;
        menuAbierto = false;
        if (panelMenu != null)
            panelMenu.SetActive(false);

        if (!estaba) return;

        if (abierto == this) abierto = null;
        DesbloquearResto();

        if (indiceOriginal >= 0 && transform.parent != null)
            transform.SetSiblingIndex(Mathf.Min(indiceOriginal, transform.parent.childCount - 1));
        indiceOriginal = -1;
    }

    void OnDisable()
    {
        if (menuAbierto) CerrarMenu();
    }

    void Update()
    {
        if (!menuAbierto) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarMenu();
            return;
        }

        // Clic o toque fuera: se cierra al soltar, siempre que no haya sido un arrastre
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && dedoPresion < 0)
                {
                    dedoPresion = t.fingerId;
                    EmpezarPresion(t.position);
                }
                else if (t.fingerId == dedoPresion)
                {
                    if (Input.touchCount > 1) presionFuera = false; // pellizco para acercar: no es un toque
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        dedoPresion = -1;
                        TerminarPresion(t.position, t.phase == TouchPhase.Ended);
                        return;
                    }
                }
            }
        }
        else
        {
            dedoPresion = -1;
            if (Input.GetMouseButtonDown(0))
                EmpezarPresion(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0))
                TerminarPresion(Input.mousePosition, true);
        }
    }

    void EmpezarPresion(Vector2 posicion)
    {
        inicioPresion = posicion;
        presionFuera = !TocaEsteMenu(posicion);
    }

    void TerminarPresion(Vector2 posicion, bool valida)
    {
        bool cerrar = presionFuera && valida
                      && (posicion - inicioPresion).magnitude <= UmbralToque();
        presionFuera = false;
        if (cerrar) CerrarMenu();
    }

    static float UmbralToque()
    {
        float dpi = Screen.dpi > 0f ? Screen.dpi : 96f;
        return Mathf.Max(10f, dpi * 0.12f);
    }

    // Dentro = sobre el panel o sobre el propio punto (que ya abre y cierra el menú)
    bool TocaEsteMenu(Vector2 posicion)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        PointerEventData datos = new PointerEventData(es);
        datos.position = posicion;
        impactos.Clear();
        es.RaycastAll(datos, impactos);
        foreach (RaycastResult r in impactos)
            if (r.gameObject != null && r.gameObject.transform.IsChildOf(transform))
                return true;
        return false;
    }

    // ------------------------------------------------------------------ Bloqueo del resto

    void BloquearResto()
    {
        DesbloquearResto();

        Canvas propio = GetComponentInParent<Canvas>();
        Canvas raizPropia = propio != null ? propio.rootCanvas : null;

        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            if (c == null || !c.isRootCanvas) continue;

            CanvasGroup g = c.GetComponent<CanvasGroup>();
            bool creado = false;
            if (g == null) { g = c.gameObject.AddComponent<CanvasGroup>(); creado = true; }

            EstadoGrupo e = new EstadoGrupo();
            e.grupo = g; e.creado = creado; e.bloqueaba = g.blocksRaycasts; e.alfa = g.alpha;
            gruposBloqueados.Add(e);

            g.blocksRaycasts = false;
            if (c == raizPropia) g.alpha = Mathf.Min(g.alpha, opacidadDelResto);
        }

        // Este punto y su menú siguen respondiendo y se ven completos
        if (grupoPropio == null) grupoPropio = GetComponent<CanvasGroup>();
        if (grupoPropio == null) grupoPropio = gameObject.AddComponent<CanvasGroup>();
        grupoPropio.ignoreParentGroups = true;
        grupoPropio.alpha = 1f;
        grupoPropio.blocksRaycasts = true;
        grupoPropio.interactable = true;
    }

    void DesbloquearResto()
    {
        foreach (EstadoGrupo e in gruposBloqueados)
        {
            if (e.grupo == null) continue;
            if (e.creado) Destroy(e.grupo);
            else
            {
                e.grupo.blocksRaycasts = e.bloqueaba;
                e.grupo.alpha = e.alfa;
            }
        }
        gruposBloqueados.Clear();

        if (grupoPropio != null) grupoPropio.ignoreParentGroups = false;
    }

    // ------------------------------------------------------------------ Rueda del ratón

    // La lista traía sensibilidad 1: una unidad por paso de rueda, y cada fila mide 55.
    // Ahora cada paso de rueda avanza una fila completa.
    void AjustarRueda()
    {
        if (panelMenu == null) return;

        float paso = 50f;
        LayoutElement le = prefabBotonDependencia != null ? prefabBotonDependencia.GetComponent<LayoutElement>() : null;
        if (le != null && le.preferredHeight > 0f) paso = le.preferredHeight;

        VerticalLayoutGroup vlg = contenedorDependencias != null ? contenedorDependencias.GetComponent<VerticalLayoutGroup>() : null;
        paso += vlg != null ? vlg.spacing : 5f;

        foreach (ScrollRect sr in panelMenu.GetComponentsInChildren<ScrollRect>(true))
            sr.scrollSensitivity = paso * Mathf.Max(0.1f, filasPorPasoDeRueda);
    }
}
