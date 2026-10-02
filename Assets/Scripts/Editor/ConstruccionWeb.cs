using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Construcción de la versión web con las fotos 360 por demanda.
///
/// Qué hace «Recorrido → Web y móvil → 2. Construir para web»:
///  1. Empaqueta cada foto 360 en su propio archivo, en dos formatos:
///     computador (DXT, 2048 px por cara) y celular (ASTC, 1024 px por cara).
///  2. Quita las fotos de los materiales solo mientras construye, para que no entren al programa.
///  3. Construye la página en Builds/WebGL.
///  4. Devuelve las fotos a los materiales (pase lo que pase).
///
/// En la página, CargadorPanoramas descarga cada foto cuando se entra a ese lugar.
/// Si un build se interrumpe, las fotos se devuelven solas al reabrir Unity, o con
/// «Recorrido → Web y móvil → Restaurar fotos 360».
/// </summary>
public static class ConstruccionWeb
{
    const string Titulo = "Recorrido";
    const string Propiedad = "_Tex";
    const string CarpetaPanoramas = "Assets/StreamingAssets/Panoramas";
    const string CarpetaSalida = "Builds/WebGL";
    const string ArchivoRespaldo = "Library/RecorridoFotos360.json";

    [System.Serializable]
    class Par { public string material; public string textura; }

    [System.Serializable]
    class Respaldo { public List<Par> pares = new List<Par>(); }

    class Formato
    {
        public string carpeta;
        public WebGLTextureSubtarget subtarget;
        public TextureImporterFormat formato;
        public int tamano;
    }

    // El de computador va de último: así el editor queda con ese ajuste
    static readonly Formato[] Formatos =
    {
        new Formato { carpeta = "astc", subtarget = WebGLTextureSubtarget.ASTC, formato = TextureImporterFormat.ASTC_6x6, tamano = 1024 },
        new Formato { carpeta = "dxt", subtarget = WebGLTextureSubtarget.DXT, formato = TextureImporterFormat.DXT1Crunched, tamano = 2048 },
    };

    // ------------------------------------------------------------------ menú

    [MenuItem("Recorrido/Web y móvil/2. Construir para web (fotos 360 por demanda)", false, 84)]
    static void Construir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(Titulo, "Sal del modo Play antes de construir.", "OK");
            return;
        }

        Restaurar(false); // por si quedó algo de un build anterior

        List<Material> materiales = MaterialesDelRecorrido();
        if (materiales.Count == 0)
        {
            EditorUtility.DisplayDialog(Titulo, "No encontré fotos 360 en la escena abierta. Abre RecorridoMain e inténtalo de nuevo.", "OK");
            return;
        }

        // Dos materiales no pueden dar el mismo nombre de archivo
        Dictionary<string, string> nombres = new Dictionary<string, string>();
        foreach (Material m in materiales)
        {
            string n = CargadorPanoramas.NombreArchivo(m.name);
            if (nombres.ContainsKey(n))
            {
                EditorUtility.DisplayDialog(Titulo, "Los materiales «" + nombres[n] + "» y «" + m.name + "» darían el mismo archivo (" + n + "). Cambia el nombre de uno.", "OK");
                return;
            }
            nombres[n] = m.name;
        }

        bool reusar = false;
        if (FotosCompletas(materiales))
        {
            int r = EditorUtility.DisplayDialogComplex(Titulo,
                "Ya hay fotos 360 generadas para la web (" + materiales.Count + " lugares).\n\n" +
                "Si no has cambiado ni agregado fotos, puedes reusarlas y el build tarda mucho menos.",
                "Reusar las fotos", "Cancelar", "Generarlas de nuevo");
            if (r == 1) return;
            reusar = r == 0;
        }
        else if (!EditorUtility.DisplayDialog(Titulo,
            "Se van a generar las fotos 360 de " + materiales.Count + " lugares, para computador y para celular, y luego la página.\n\n" +
            "La primera vez puede tardar entre 20 y 40 minutos. No cierres Unity mientras trabaja.",
            "Construir", "Cancelar"))
        {
            return;
        }

        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog(Titulo, "No se pudo cambiar la plataforma a WebGL. Revisa que el módulo WebGL esté instalado en Unity Hub.", "OK");
            return;
        }

        bool listo = false;
        string mensaje = "";
        try
        {
            if (!reusar && !GenerarFotos(materiales))
            {
                mensaje = "No se pudieron generar las fotos 360. Revisa la consola.";
                return;
            }

            QuitarFotos(materiales);

            List<string> escenas = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
                if (e.enabled) escenas.Add(e.path);

            BuildPlayerOptions opciones = new BuildPlayerOptions();
            opciones.scenes = escenas.ToArray();
            opciones.locationPathName = CarpetaSalida;
            opciones.target = BuildTarget.WebGL;
            opciones.options = BuildOptions.None;

            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
            UnityEditor.Build.Reporting.BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
            listo = reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            mensaje = listo
                ? "Página lista en " + CarpetaSalida + ".\n\nPrograma: " + (reporte.summary.totalSize / (1024 * 1024)) + " MB. Fotos 360: " + TamanoCarpetaMB(CarpetaPanoramas) + " MB en total, pero cada visitante solo descarga las de los lugares que visita."
                : "El build no terminó: " + reporte.summary.result + ". Revisa la consola.";
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            mensaje = "El build se interrumpió: " + ex.Message;
        }
        finally
        {
            Restaurar(false);
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(mensaje)) EditorUtility.DisplayDialog(Titulo, mensaje, "OK");
            if (listo) EditorUtility.RevealInFinder(CarpetaSalida + "/index.html");
        }
    }

    [MenuItem("Recorrido/Web y móvil/Restaurar fotos 360 (si un build se interrumpió)", false, 95)]
    static void RestaurarDesdeMenu()
    {
        int n = Restaurar(true);
        EditorUtility.DisplayDialog(Titulo, n > 0 ? n + " fotos 360 devueltas a sus materiales." : "No había nada que restaurar: los materiales tienen sus fotos.", "OK");
    }

    // Si Unity se cerró a mitad de un build, al volver a abrir se devuelven las fotos
    [InitializeOnLoadMethod]
    static void RevisarAlAbrir()
    {
        if (!File.Exists(ArchivoRespaldo)) return;
        EditorApplication.delayCall += () =>
        {
            if (BuildPipeline.isBuildingPlayer) return;
            int n = Restaurar(true);
            if (n > 0) Debug.Log("[Recorrido] Un build web quedó a medias: se devolvieron " + n + " fotos 360 a sus materiales.");
        };
    }

    // ------------------------------------------------------------------ fotos 360

    // Todos los materiales de foto 360 que usa la escena abierta
    static List<Material> MaterialesDelRecorrido()
    {
        HashSet<Material> unicos = new HashSet<Material>();

        foreach (GestorTeleports g in Object.FindObjectsOfType<GestorTeleports>(true))
        {
            unicos.Add(g.skyboxInicial);
            if (g.configuraciones != null)
                foreach (ConfiguracionSkybox c in g.configuraciones)
                    if (c != null) unicos.Add(c.skybox);
        }
        foreach (TeleportPoint tp in Object.FindObjectsOfType<TeleportPoint>(true))
            unicos.Add(tp.skyboxDestino);
        foreach (MenuNavegacion m in Object.FindObjectsOfType<MenuNavegacion>(true))
            if (m.edificios != null)
                foreach (EdificioRecorrido e in m.edificios)
                    if (e != null && e.puntos != null)
                        foreach (PuntoRecorrido p in e.puntos)
                            if (p != null) unicos.Add(p.skybox);
        foreach (MenusDeZona mz in Object.FindObjectsOfType<MenusDeZona>(true))
            if (mz.menus != null)
                foreach (MenuDeZona m in mz.menus)
                    if (m != null && m.lugares != null)
                        foreach (LugarZona l in m.lugares)
                            if (l != null) unicos.Add(l.skybox);
        foreach (POIController poi in Object.FindObjectsOfType<POIController>(true))
            if (poi.skyboxes != null)
                foreach (Material m in poi.skyboxes) unicos.Add(m);
        unicos.Add(RenderSettings.skybox);

        List<Material> lista = new List<Material>();
        foreach (Material m in unicos)
        {
            if (m == null || !m.HasProperty(Propiedad) || m.GetTexture(Propiedad) == null) continue;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) continue;
            lista.Add(m);
        }
        lista.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return lista;
    }

    static bool FotosCompletas(List<Material> materiales)
    {
        foreach (Formato f in Formatos)
            foreach (Material m in materiales)
                if (!File.Exists(CarpetaPanoramas + "/" + f.carpeta + "/" + CargadorPanoramas.NombreArchivo(m.name)))
                    return false;
        return true;
    }

    static bool GenerarFotos(List<Material> materiales)
    {
        List<AssetBundleBuild> paquetes = new List<AssetBundleBuild>();
        HashSet<string> texturas = new HashSet<string>();
        foreach (Material m in materiales)
        {
            string ruta = AssetDatabase.GetAssetPath(m.GetTexture(Propiedad));
            if (string.IsNullOrEmpty(ruta)) continue;
            texturas.Add(ruta);

            AssetBundleBuild b = new AssetBundleBuild();
            b.assetBundleName = CargadorPanoramas.NombreArchivo(m.name);
            b.assetNames = new[] { ruta };
            paquetes.Add(b);
        }

        if (Directory.Exists(CarpetaPanoramas)) Directory.Delete(CarpetaPanoramas, true);

        for (int i = 0; i < Formatos.Length; i++)
        {
            Formato f = Formatos[i];
            string etapa = "Fotos 360 para " + (f.carpeta == "dxt" ? "computador" : "celular") + " (" + (i + 1) + " de " + Formatos.Length + ")";

            AjustarTexturas(texturas, f, etapa);

            string carpeta = CarpetaPanoramas + "/" + f.carpeta;
            Directory.CreateDirectory(carpeta);

            EditorUtility.DisplayProgressBar(Titulo, etapa + ": empaquetando…", 0.9f);
            EditorUserBuildSettings.webGLBuildSubtarget = f.subtarget;
            AssetBundleManifest manifiesto = BuildPipeline.BuildAssetBundles(carpeta, paquetes.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.WebGL);
            if (manifiesto == null) return false;

            // Unity deja archivos de índice que la página no necesita
            foreach (string archivo in Directory.GetFiles(carpeta))
            {
                string nombre = Path.GetFileName(archivo);
                if (nombre.EndsWith(".manifest") || nombre == f.carpeta || nombre.EndsWith(".meta"))
                    File.Delete(archivo);
            }
        }

        AssetDatabase.Refresh();
        return true;
    }

    // Ajuste de importación solo para WebGL: tamaño por cara, formato y sin mipmaps
    static void AjustarTexturas(HashSet<string> rutas, Formato f, string etapa)
    {
        int n = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string ruta in rutas)
            {
                EditorUtility.DisplayProgressBar(Titulo, etapa + ": preparando " + Path.GetFileNameWithoutExtension(ruta), 0.1f + 0.3f * n++ / Mathf.Max(1, rutas.Count));
                TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (imp == null) continue;

                imp.mipmapEnabled = false; // el cielo se ve casi a escala 1:1; los mipmaps solo pesan

                TextureImporterPlatformSettings web = imp.GetPlatformTextureSettings("WebGL");
                web.name = "WebGL";
                web.overridden = true;
                web.maxTextureSize = f.tamano;
                web.format = f.formato;
                web.compressionQuality = 60;
                imp.SetPlatformTextureSettings(web);
                AssetDatabase.WriteImportSettingsIfDirty(ruta);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        EditorUtility.DisplayProgressBar(Titulo, etapa + ": convirtiendo las fotos (esto es lo que más tarda)…", 0.5f);
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------ quitar y devolver

    static void QuitarFotos(List<Material> materiales)
    {
        Respaldo r = new Respaldo();
        foreach (Material m in materiales)
        {
            Texture t = m.GetTexture(Propiedad);
            if (t == null) continue;
            Par p = new Par();
            p.material = AssetDatabase.GetAssetPath(m);
            p.textura = AssetDatabase.GetAssetPath(t);
            r.pares.Add(p);
        }

        // Primero se guarda el respaldo; después se tocan los materiales
        File.WriteAllText(ArchivoRespaldo, JsonUtility.ToJson(r, true));

        foreach (Material m in materiales)
        {
            m.SetTexture(Propiedad, null);
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
    }

    static int Restaurar(bool avisar)
    {
        if (!File.Exists(ArchivoRespaldo)) return 0;

        int n = 0;
        int fallos = 0;
        try
        {
            Respaldo r = JsonUtility.FromJson<Respaldo>(File.ReadAllText(ArchivoRespaldo));
            if (r != null && r.pares != null)
            {
                foreach (Par p in r.pares)
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(p.material);
                    Texture t = AssetDatabase.LoadAssetAtPath<Texture>(p.textura);
                    if (m == null || t == null)
                    {
                        Debug.LogWarning("[Recorrido] No se pudo restaurar " + p.material + " ← " + p.textura);
                        fallos++;
                        continue;
                    }
                    if (m.GetTexture(Propiedad) == t) continue;
                    m.SetTexture(Propiedad, t);
                    EditorUtility.SetDirty(m);
                    n++;
                }
            }
            AssetDatabase.SaveAssets();
            if (fallos == 0) File.Delete(ArchivoRespaldo); // si algo faltó, el respaldo se conserva
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            if (avisar) Debug.LogError("[Recorrido] No se pudieron restaurar las fotos 360. El respaldo sigue en " + ArchivoRespaldo);
        }
        return n;
    }

    static long TamanoCarpetaMB(string carpeta)
    {
        if (!Directory.Exists(carpeta)) return 0;
        long total = 0;
        foreach (string a in Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories))
            if (!a.EndsWith(".meta")) total += new FileInfo(a).Length;
        return total / (1024 * 1024);
    }
}
