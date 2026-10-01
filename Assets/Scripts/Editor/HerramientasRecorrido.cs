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

    // ------------------------------------------------------------------ 6. Puntos de información

    [MenuItem("Recorrido/6. Crear punto de información donde estoy mirando %&i", false, 50)]
    static void CrearPuntoInfo()
    {
        Material sky = RenderSettings.skybox;
        if (sky == null)
        {
            EditorUtility.DisplayDialog(Titulo, "No hay skybox puesto. Usa primero 'Recorrido > 1. Ver el skybox...'.", "OK");
            return;
        }

        Transform padre = GrupoPuntosInfo(sky);
        if (padre == null)
        {
            EditorUtility.DisplayDialog(Titulo, "No se encontró un Canvas en World Space (CanvasWorld) para colgar el punto.", "OK");
            return;
        }

        // Misma distancia y tamaño aparente que los botones TP de esta foto
        Transform referencia = PrimerBoton(Config(Gestor(false), sky, false));
        if (referencia == null)
        {
            TeleportPoint cualquiera = Object.FindObjectOfType<TeleportPoint>(true);
            if (cualquiera != null) referencia = cualquiera.transform;
        }
        float distancia = referencia != null ? referencia.position.magnitude : DistanciaPorDefecto;
        if (distancia < 1f) distancia = DistanciaPorDefecto;

        GameObject go = new GameObject("Info_" + NombreCorto(sky), typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Crear punto de información");
        go.transform.SetParent(padre, false);
        go.name = NombreUnico(padre, go.name);
        ((RectTransform)go.transform).sizeDelta = new Vector2(80f, 80f);

        float escalaMundo = referencia != null ? Mathf.Abs(referencia.lossyScale.x) : EscalaPorDefecto;
        float escalaPadre = Mathf.Max(0.000001f, Mathf.Abs(padre.lossyScale.x));
        go.transform.localScale = Vector3.one * (escalaMundo / escalaPadre);

        PuntoInfo p = go.AddComponent<PuntoInfo>();
        p.skybox = sky;
        p.zona = AdivinarZona(sky);
        UbicarFrenteALaVista(go.transform, distancia);

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        SceneVisibilityManager.instance.Show(go, true);
        Debug.Log("[Recorrido] Creado " + go.name + " en '" + sky.name + "'. Escribe el título y el texto en el componente PuntoInfo.", go);
    }

    [MenuItem("Recorrido/6. Crear punto de información donde estoy mirando %&i", true)]
    static bool ValidarCrearPuntoInfo()
    {
        return !EditorApplication.isPlaying;
    }

    [MenuItem("Recorrido/7. Seleccionar los puntos de información de este skybox", false, 51)]
    static void SeleccionarPuntosInfo()
    {
        List<Object> lista = new List<Object>();
        foreach (PuntoInfo p in Object.FindObjectsOfType<PuntoInfo>(true))
            if (p.skybox == RenderSettings.skybox) lista.Add(p.gameObject);
        Selection.objects = lista.ToArray();
        Debug.Log("[Recorrido] " + lista.Count + " punto(s) de información en '" + (RenderSettings.skybox != null ? RenderSettings.skybox.name : "sin skybox") + "'.");
    }

    [MenuItem("Recorrido/Revisar puntos de información sin texto", false, 62)]
    static void RevisarPuntosInfo()
    {
        List<Object> malos = new List<Object>();
        foreach (PuntoInfo p in Object.FindObjectsOfType<PuntoInfo>(true))
        {
            bool sinTexto = string.IsNullOrEmpty(p.resumen) && string.IsNullOrEmpty(p.descripcion) &&
                            (p.queEncuentras == null || p.queEncuentras.Length == 0);
            if (p.skybox == null || sinTexto || p.titulo == "Nuevo punto de información")
            {
                malos.Add(p.gameObject);
                Debug.LogWarning("[Recorrido] Punto de información incompleto: " + Ruta(p.transform) +
                                 (p.skybox == null ? " (sin skybox)" : "") + (sinTexto ? " (sin texto)" : ""), p);
            }
        }

        if (malos.Count == 0)
        {
            EditorUtility.DisplayDialog(Titulo, "Todos los puntos de información tienen foto, título y texto.", "OK");
        }
        else
        {
            Selection.objects = malos.ToArray();
            EditorUtility.DisplayDialog(Titulo, malos.Count + " punto(s) incompletos. Quedaron seleccionados y listados en la consola.", "OK");
        }
    }

    // ------------------------------------------------------------------ 8. Nombres oficiales y fichas

    [MenuItem("Recorrido/8. Aplicar nombres oficiales y fichas del catálogo", false, 52)]
    static void AplicarNombresOficiales()
    {
        CatalogoInfo.Recargar();
        int cambios = 0;
        List<string> sinNombre = new List<string>();

        foreach (MenuNavegacion m in Object.FindObjectsOfType<MenuNavegacion>(true))
        {
            if (m.edificios == null) continue;
            Undo.RecordObject(m, "Aplicar nombres oficiales");
            foreach (EdificioRecorrido e in m.edificios)
            {
                if (e == null) continue;
                string ed = NombreEdificioOficial(e.nombreEdificio);
                if (ed != e.nombreEdificio) { e.nombreEdificio = ed; cambios++; }
                if (e.puntos == null) continue;
                foreach (PuntoRecorrido p in e.puntos)
                    if (p != null) cambios += Renombrar(ref p.nombre, p.skybox, "Menú lateral · " + e.nombreEdificio, sinNombre);
            }
            EditorUtility.SetDirty(m);
        }

        foreach (MenusDeZona mz in Object.FindObjectsOfType<MenusDeZona>(true))
        {
            if (mz.menus == null) continue;
            Undo.RecordObject(mz, "Aplicar nombres oficiales");
            foreach (MenuDeZona m in mz.menus)
            {
                if (m == null || m.lugares == null) continue;
                foreach (LugarZona l in m.lugares)
                    if (l != null) cambios += Renombrar(ref l.nombre, l.skybox, "Menú de zona · " + m.titulo, sinNombre);
            }
            EditorUtility.SetDirty(mz);
        }

        foreach (POIController poi in Object.FindObjectsOfType<POIController>(true))
        {
            Undo.RecordObject(poi, "Aplicar nombres oficiales");
            string ed = NombreEdificioOficial(poi.nombreEdificio);
            if (ed != poi.nombreEdificio) { poi.nombreEdificio = ed; cambios++; }
            if (poi.dependencias != null && poi.skyboxes != null)
            {
                for (int i = 0; i < poi.dependencias.Length && i < poi.skyboxes.Length; i++)
                    cambios += Renombrar(ref poi.dependencias[i], poi.skyboxes[i], "Punto " + poi.nombreEdificio, sinNombre);
            }
            EditorUtility.SetDirty(poi);
        }

        // Los puntos de información sin ficha toman la de su foto 360
        int fichas = 0;
        foreach (PuntoInfo p in Object.FindObjectsOfType<PuntoInfo>(true))
        {
            if (!string.IsNullOrEmpty(p.idFicha)) continue;
            FichaInfo f = CatalogoInfo.PorSkybox(p.skybox);
            if (f == null) continue;
            Undo.RecordObject(p, "Asignar ficha");
            p.idFicha = f.id;
            if (string.IsNullOrEmpty(p.zona)) p.zona = f.zona;
            EditorUtility.SetDirty(p);
            fichas++;
        }

        MarcarEscena();
        foreach (string s in sinNombre) Debug.LogWarning("[Recorrido] Sin nombre oficial en el catálogo: " + s);
        EditorUtility.DisplayDialog(Titulo,
            cambios + " nombre(s) actualizados y " + fichas + " punto(s) de información enlazados a su ficha." +
            (sinNombre.Count > 0 ? "\n\n" + sinNombre.Count + " lugar(es) sin nombre en el catálogo: quedan como estaban (ver consola)." : "") +
            "\n\nGuarda la escena con Ctrl+S.", "OK");
    }

    static int Renombrar(ref string nombre, Material sky, string donde, List<string> sinNombre)
    {
        string oficial = CatalogoInfo.NombreOficialDe(sky);
        if (string.IsNullOrEmpty(oficial))
        {
            sinNombre.Add(donde + " · «" + nombre + "» (" + (sky != null ? sky.name : "sin skybox") + ")");
            return 0;
        }
        if (oficial == nombre) return 0;
        nombre = oficial;
        return 1;
    }

    static string NombreEdificioOficial(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return nombre;
        string n = nombre.Trim();
        if (n == "Edificio12") return "Edificio 12";
        return n;
    }

    // Grupo «PuntosInfo/<skybox>» dentro del Canvas del mundo
    static Transform GrupoPuntosInfo(Material sky)
    {
        Canvas mundo = null;
        GameObject tps = GameObject.Find("BotonesTP");
        if (tps != null)
        {
            Canvas c = tps.transform.GetComponentInParent<Canvas>();
            if (c != null) mundo = c.rootCanvas;
        }
        if (mundo == null)
        {
            foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
                if (c.renderMode == RenderMode.WorldSpace) { mundo = c; break; }
        }
        if (mundo == null) return null;

        Transform raiz = mundo.transform.Find("PuntosInfo");
        if (raiz == null)
        {
            GameObject g = new GameObject("PuntosInfo", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(g, "Crear grupo de puntos de información");
            g.transform.SetParent(mundo.transform, false);
            raiz = g.transform;
        }

        string nombre = NombreCorto(sky);
        Transform grupo = raiz.Find(nombre);
        if (grupo == null)
        {
            GameObject g = new GameObject(nombre, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(g, "Crear grupo de puntos de información");
            g.transform.SetParent(raiz, false);
            grupo = g.transform;
        }
        return grupo;
    }

    // La zona (y su color) sale del menú en el que aparece esta foto
    static string AdivinarZona(Material sky)
    {
        foreach (MenuNavegacion m in Object.FindObjectsOfType<MenuNavegacion>(true))
        {
            if (m.edificios == null) continue;
            foreach (EdificioRecorrido e in m.edificios)
            {
                if (e == null || e.puntos == null) continue;
                foreach (PuntoRecorrido p in e.puntos)
                    if (p != null && p.skybox == sky) return e.nombreEdificio;
            }
        }
        foreach (MenusDeZona mz in Object.FindObjectsOfType<MenusDeZona>(true))
        {
            if (mz.menus == null) continue;
            foreach (MenuDeZona m in mz.menus)
            {
                if (m == null || m.lugares == null) continue;
                foreach (LugarZona l in m.lugares)
                    if (l != null && l.skybox == sky) return m.titulo;
            }
        }
        foreach (POIController poi in Object.FindObjectsOfType<POIController>(true))
        {
            if (poi.skyboxes == null) continue;
            foreach (Material m in poi.skyboxes)
                if (m == sky) return poi.nombreEdificio;
        }
        return "";
    }

    // ------------------------------------------------------------------ web y móvil

    [MenuItem("Recorrido/Web y móvil/1. Preparar ajustes para web (plantilla UdB, Gzip)", false, 80)]
    static void PrepararWeb()
    {
        // Plantilla con la marca y adaptada a móvil (Assets/WebGLTemplates/RecorridoUdB)
        PlayerSettings.WebGL.template = "PROJECT:RecorridoUdB";
        // Gzip con respaldo de descompresión: funciona en cualquier servidor (GitHub Pages, itch.io, Live Server)
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        // Pantalla de inicio de Unity sobre blanco, para que empalme con la carga de la página
        // y con la pantalla de carga del recorrido (fondo blanco con el logotipo)
        PlayerSettings.SplashScreen.backgroundColor = Color.white;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(Titulo,
            "Listo: plantilla RecorridoUdB, compresión Gzip, respaldo de descompresión y pantalla de inicio sobre blanco.\n\n" +
            "Para probar en celular, en File > Build Settings > WebGL pon «Texture Compression» en ASTC " +
            "(los celulares no leen el formato de escritorio).", "OK");
    }

    [MenuItem("Recorrido/Web y móvil/2. Panorámicas para web: 1024 por cara (móvil)", false, 81)]
    static void PanoramicasMovil() { AjustarPanoramicasWeb(1024); }

    [MenuItem("Recorrido/Web y móvil/2. Panorámicas para web: 2048 por cara (escritorio)", false, 82)]
    static void PanoramicasEscritorio() { AjustarPanoramicasWeb(2048); }

    [MenuItem("Recorrido/Web y móvil/2. Panorámicas para web: quitar ajuste", false, 83)]
    static void PanoramicasSinAjuste() { AjustarPanoramicasWeb(0); }

    // Solo toca el ajuste de la plataforma WebGL de las texturas cubemap: el editor y Windows no cambian
    static void AjustarPanoramicasWeb(int tamano)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Textures" });
        int n = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string g in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (imp == null || imp.textureShape != TextureImporterShape.TextureCube) continue;

                TextureImporterPlatformSettings web = imp.GetPlatformTextureSettings("WebGL");
                web.name = "WebGL";
                web.overridden = tamano > 0;
                if (tamano > 0)
                {
                    web.maxTextureSize = tamano;
                    web.format = TextureImporterFormat.Automatic;
                    web.textureCompression = TextureImporterCompression.Compressed;
                }
                imp.SetPlatformTextureSettings(web);
                imp.SaveAndReimport();
                n++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        EditorUtility.DisplayDialog(Titulo, n + " panorámicas ajustadas para WebGL" +
            (tamano > 0 ? " a " + tamano + " px por cara." : " (sin ajuste propio).") +
            "\n\nSe nota al construir o al cambiar la plataforma a WebGL.", "OK");
    }

    [MenuItem("Recorrido/Web y móvil/3. Construir para web en Builds/WebGL", false, 84)]
    static void ConstruirWeb()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
            !EditorUtility.DisplayDialog(Titulo,
                "La plataforma activa no es WebGL. Unity la cambiará y reimportará las texturas (puede tardar). ¿Seguimos?",
                "Sí, construir", "Cancelar"))
            return;

        List<string> escenas = new List<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            if (e.enabled) escenas.Add(e.path);

        BuildPlayerOptions opciones = new BuildPlayerOptions();
        opciones.scenes = escenas.ToArray();
        opciones.locationPathName = "Builds/WebGL";
        opciones.target = BuildTarget.WebGL;
        opciones.options = BuildOptions.None;

        UnityEditor.Build.Reporting.BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder("Builds/WebGL/index.html");
            EditorUtility.DisplayDialog(Titulo, "Build listo en Builds/WebGL (" +
                (reporte.summary.totalSize / (1024 * 1024)) + " MB). Sírvelo con Live Server o Python y ábrelo desde el celular.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(Titulo, "El build no terminó: " + reporte.summary.result + ". Revisa la consola.", "OK");
        }
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
            if (go.GetComponent<TeleportPoint>() != null || go.GetComponent<PuntoInfo>() != null) return true;
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
