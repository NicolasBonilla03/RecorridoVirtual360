using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Herramientas para ubicar botones de teletransporte (TP) sobre cada foto 360.
/// Aparecen en el menú superior de Unity: Recorrido.
/// Solo funcionan en el editor (no entran en el build).
/// </summary>
public static class HerramientasRecorrido
{
    const string RutaPrefabTP = "Assets/Prefabs/BotonTP.prefab";
    const float DistanciaPorDefecto = 226f;
    const float EscalaPorDefecto = 0.368f;
    const string Titulo = "Recorrido";

    // ------------------------------------------------------------------ 1. Ver un skybox

    [MenuItem("Recorrido/1. Ver el skybox seleccionado en Project", false, 1)]
    static void VerSkyboxSeleccionado()
    {
        VerSkybox(Selection.activeObject as Material);
    }

    [MenuItem("Recorrido/1. Ver el skybox seleccionado en Project", true)]
    static bool ValidarVerSkyboxSeleccionado()
    {
        return !EditorApplication.isPlaying && Selection.activeObject is Material;
    }

    [MenuItem("Recorrido/1b. Ver el skybox de inicio", false, 2)]
    static void VerSkyboxInicio()
    {
        GestorTeleports g = Gestor();
        if (g == null) return;
        if (g.skyboxInicial == null)
        {
            EditorUtility.DisplayDialog(Titulo, "GestorTeleports no tiene asignado un Skybox Inicial.", "OK");
            return;
        }
        VerSkybox(g.skyboxInicial);
    }

    // ------------------------------------------------------------------ 2. Crear / mover botones

    [MenuItem("Recorrido/2. Crear botón TP donde estoy mirando %&t", false, 20)]
    static void CrearBotonTP()
    {
        GestorTeleports g = Gestor();
        if (g == null) return;

        Material sky = RenderSettings.skybox;
        if (sky == null)
        {
            EditorUtility.DisplayDialog(Titulo, "No hay skybox puesto. Usa primero 'Recorrido > 1. Ver el skybox...'.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabTP);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(Titulo, "No se encontró el prefab en " + RutaPrefabTP, "OK");
            return;
        }

        // Se usa como referencia (carpeta, distancia y tamaño) otro botón del mismo skybox
        Transform referencia = PrimerBoton(Config(g, sky, false));
        Transform padre = referencia != null ? referencia.parent : CrearGrupoPara(sky);
        if (padre == null)
        {
            EditorUtility.DisplayDialog(Titulo, "No se encontró 'BotonesTP' ni un Canvas en World Space para colgar el botón.", "OK");
            return;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        Undo.RegisterCreatedObjectUndo(go, "Crear botón TP");
        go.name = NombreUnico(padre, "BotonTP_" + NombreCorto(sky) + "_Nuevo");

        float distancia = referencia != null ? referencia.position.magnitude : DistanciaPorDefecto;
        if (distancia < 1f) distancia = DistanciaPorDefecto;
        go.transform.localScale = referencia != null ? referencia.localScale : Vector3.one * EscalaPorDefecto;
        UbicarFrenteALaVista(go.transform, distancia);

        Registrar(g, sky, go);

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        SceneVisibilityManager.instance.Show(go, true);
        Debug.Log("[Recorrido] Creado " + go.name + " en '" + sky.name + "'. Ahora asígnale 'Skybox Destino' en el componente TeleportPoint.", go);
    }

    [MenuItem("Recorrido/2. Crear botón TP donde estoy mirando %&t", true)]
    static bool ValidarCrearBotonTP()
    {
        return !EditorApplication.isPlaying;
    }

    [MenuItem("Recorrido/3. Mover botón seleccionado adonde estoy mirando", false, 21)]
    static void MoverSeleccionado()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            float distancia = go.transform.position.magnitude;
            if (distancia < 1f) distancia = DistanciaPorDefecto;
            UbicarFrenteALaVista(go.transform, distancia);
        }
        MarcarEscena();
    }

    [MenuItem("Recorrido/3. Mover botón seleccionado adonde estoy mirando", true)]
    static bool ValidarMoverSeleccionado()
    {
        return !EditorApplication.isPlaying && HaySeleccionTP();
    }

    // ------------------------------------------------------------------ 4. Registrar / quitar

    [MenuItem("Recorrido/4. Hacer visibles los botones seleccionados en este skybox", false, 40)]
    static void RegistrarSeleccionados()
    {
        GestorTeleports g = Gestor();
        if (g == null || RenderSettings.skybox == null) return;

        int n = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<TeleportPoint>() == null) continue;
            Registrar(g, RenderSettings.skybox, go);
            n++;
        }
        Debug.Log("[Recorrido] " + n + " botón(es) ahora visibles en '" + RenderSettings.skybox.name + "'.");
    }

    [MenuItem("Recorrido/4. Hacer visibles los botones seleccionados en este skybox", true)]
    static bool ValidarRegistrarSeleccionados()
    {
        return !EditorApplication.isPlaying && HaySeleccionTP();
    }

    [MenuItem("Recorrido/5. Quitar los botones seleccionados de este skybox", false, 41)]
    static void QuitarSeleccionados()
    {
        GestorTeleports g = Gestor();
        if (g == null || RenderSettings.skybox == null) return;

        ConfiguracionSkybox cfg = Config(g, RenderSettings.skybox, false);
        if (cfg == null || cfg.teleportsVisibles == null) return;

        Undo.RecordObject(g, "Quitar botones TP");
        foreach (GameObject go in Selection.gameObjects)
            cfg.teleportsVisibles.Remove(go);
        EditorUtility.SetDirty(g);
        MarcarEscena();
    }

    [MenuItem("Recorrido/5. Quitar los botones seleccionados de este skybox", true)]
    static bool ValidarQuitarSeleccionados()
    {
        return !EditorApplication.isPlaying && HaySeleccionTP();
    }

    // ------------------------------------------------------------------ revisión

    [MenuItem("Recorrido/Revisar botones sin destino", false, 60)]
    static void RevisarSinDestino()
    {
        List<Object> malos = new List<Object>();
        foreach (TeleportPoint tp in Object.FindObjectsOfType<TeleportPoint>(true))
        {
            if (tp.skyboxDestino == null)
            {
                malos.Add(tp.gameObject);
                Debug.LogWarning("[Recorrido] Botón sin destino: " + Ruta(tp.transform), tp);
            }
        }

        if (malos.Count == 0)
        {
            EditorUtility.DisplayDialog(Titulo, "Todos los botones tienen destino.", "OK");
        }
        else
        {
            Selection.objects = malos.ToArray();
            EditorUtility.DisplayDialog(Titulo, malos.Count + " botón(es) sin destino. Quedaron seleccionados y listados en la consola.", "OK");
        }
    }

    [MenuItem("Recorrido/Mostrar todos los botones", false, 61)]
    static void MostrarTodos()
    {
        SceneVisibilityManager.instance.ShowAll();
    }

    // ------------------------------------------------------------------ internos

    static void VerSkybox(Material mat)
    {
        if (mat == null) return;

        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
        MarcarEscena();
        MostrarSoloBotonesDe(mat);
        CentrarVista();
        Debug.Log("[Recorrido] Viendo '" + mat.name + "'. Gira la vista de escena con clic derecho y ubica los botones.");
    }

    static void MostrarSoloBotonesDe(Material mat)
    {
        SceneVisibilityManager svm = SceneVisibilityManager.instance;

        List<GameObject> todos = new List<GameObject>();
        foreach (TeleportPoint tp in Object.FindObjectsOfType<TeleportPoint>(true))
            todos.Add(tp.gameObject);
        if (todos.Count > 0)
        {
            svm.Show(todos.ToArray(), true);
            svm.Hide(todos.ToArray(), true);
        }

        ConfiguracionSkybox cfg = Config(Gestor(false), mat, false);
        if (cfg == null || cfg.teleportsVisibles == null) return;

        List<GameObject> visibles = new List<GameObject>();
        foreach (GameObject go in cfg.teleportsVisibles)
            if (go != null) visibles.Add(go);
        if (visibles.Count > 0)
            svm.Show(visibles.ToArray(), true);
    }

    // Pone la cámara de la vista de escena en el centro (donde está la cámara del recorrido)
    static void CentrarVista()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        sv.orthographic = false;
        SceneView.CameraSettings ajustes = sv.cameraSettings;
        ajustes.dynamicClip = false;
        ajustes.nearClip = 0.01f;
        ajustes.farClip = 5000f;
        sv.LookAtDirect(Vector3.zero, sv.rotation, 0.01f);
        sv.Repaint();
    }

    static void UbicarFrenteALaVista(Transform t, float distancia)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        Vector3 dir = sv != null ? (sv.rotation * Vector3.forward) : Vector3.forward;
        dir.Normalize();

        Undo.RecordObject(t, "Ubicar botón TP");
        t.position = dir * distancia;                          // la cámara del recorrido está en (0,0,0)
        t.rotation = Quaternion.LookRotation(dir, Vector3.up);  // el botón queda mirando a la cámara
        MarcarEscena();
    }

    static void Registrar(GestorTeleports g, Material sky, GameObject go)
    {
        Undo.RecordObject(g, "Registrar botón TP");
        ConfiguracionSkybox cfg = Config(g, sky, true);
        if (cfg.teleportsVisibles == null) cfg.teleportsVisibles = new List<GameObject>();
        if (cfg.teleportsVisibles.Contains(go)) return;

        // Reutiliza un espacio vacío de la lista si lo hay
        for (int i = 0; i < cfg.teleportsVisibles.Count; i++)
        {
            if (cfg.teleportsVisibles[i] == null)
            {
                cfg.teleportsVisibles[i] = go;
                EditorUtility.SetDirty(g);
                MarcarEscena();
                return;
            }
        }

        cfg.teleportsVisibles.Add(go);
        EditorUtility.SetDirty(g);
        MarcarEscena();
    }

    static ConfiguracionSkybox Config(GestorTeleports g, Material mat, bool crear)
    {
        if (g == null || mat == null) return null;
        if (g.configuraciones == null) g.configuraciones = new List<ConfiguracionSkybox>();

        foreach (ConfiguracionSkybox c in g.configuraciones)
            if (c != null && c.skybox == mat) return c;

        if (!crear) return null;

        ConfiguracionSkybox nueva = new ConfiguracionSkybox();
        nueva.nombreSkybox = NombreCorto(mat);
        nueva.skybox = mat;
        nueva.teleportsVisibles = new List<GameObject>();
        g.configuraciones.Add(nueva);
        return nueva;
    }

    static Transform PrimerBoton(ConfiguracionSkybox cfg)
    {
        if (cfg == null || cfg.teleportsVisibles == null) return null;
        foreach (GameObject go in cfg.teleportsVisibles)
            if (go != null) return go.transform;
        return null;
    }

    // Crea (o reutiliza) un grupo con el nombre del skybox dentro de BotonesTP
    static Transform CrearGrupoPara(Material sky)
    {
        GameObject raiz = GameObject.Find("BotonesTP");
        if (raiz == null)
        {
            foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
                if (c.renderMode == RenderMode.WorldSpace) { raiz = c.gameObject; break; }
        }
        if (raiz == null) return null;

        string nombre = NombreCorto(sky);
        Transform existente = raiz.transform.Find(nombre);
        if (existente != null) return existente;

        GameObject grupo = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(grupo, "Crear grupo de botones");
        grupo.transform.SetParent(raiz.transform, false);
        return grupo.transform;
    }

    static GestorTeleports Gestor(bool avisar = true)
    {
        GestorTeleports g = Object.FindObjectOfType<GestorTeleports>();
        if (g == null && avisar)
            EditorUtility.DisplayDialog(Titulo, "No hay un GestorTeleports en la escena abierta.", "OK");
        return g;
    }

    static bool HaySeleccionTP()
    {
        foreach (GameObject go in Selection.gameObjects)
            if (go.GetComponent<TeleportPoint>() != null) return true;
        return false;
    }

    static string NombreCorto(Material m)
    {
        return m == null ? "SinSkybox" : m.name.Replace("Skybox_", "").Replace(" ", "");
    }

    static string NombreUnico(Transform padre, string baseNombre)
    {
        string nombre = baseNombre;
        int i = 2;
        while (padre != null && padre.Find(nombre) != null)
            nombre = baseNombre + i++;
        return nombre;
    }

    static string Ruta(Transform t)
    {
        string r = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            r = t.name + "/" + r;
        }
        return r;
    }

    static void MarcarEscena()
    {
        if (!EditorApplication.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
