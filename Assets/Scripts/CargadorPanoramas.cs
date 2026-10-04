using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Carga de las panorámicas en la versión web.
///
/// En el build web las fotos 360 no van dentro del programa: cada una viaja en su propio archivo
/// (StreamingAssets/Panoramas/dxt o astc). Para que cambiar de lugar no tenga espera:
///  - al llegar a un lugar se descargan en segundo plano las fotos de los lugares a los que se
///    puede ir desde ahí (sus botones de teletransporte), empezando por el que el usuario está mirando;
///  - al abrir el menú de un edificio se adelantan las fotos de sus lugares;
///  - al pulsar un botón, la descarga empieza de inmediato, mientras la pantalla se oscurece.
/// En memoria se conservan el lugar actual, sus vecinos y los últimos visitados; el resto se suelta.
///
/// En el editor y en un build normal los materiales ya traen su textura, y este cargador no hace nada.
/// Los archivos los genera «Recorrido → Web y móvil → 2. Construir para web».
/// </summary>
public static class CargadorPanoramas
{
    const string Propiedad = "_Tex";
    const string Carpeta = "Panoramas";

    /// <summary>Panorámicas que se conservan en memoria en computador (actual, vecinas y recientes).</summary>
    public static int MaximoEnMemoria = 10;

    /// <summary>Lo mismo en celulares y tabletas, que tienen menos memoria.</summary>
    public static int MaximoEnMemoriaMovil = 6;

    /// <summary>Cuántos lugares vecinos se adelantan como máximo desde cada lugar.</summary>
    public static int VecinosPorAdelantar = 6;
    public static int VecinosPorAdelantarMovil = 4;

    /// <summary>Resultado de la última llamada a Preparar.</summary>
    public static bool UltimaCargaOk { get; private set; }

    class Cargada { public Material material; public Texture textura; }
    static readonly List<Cargada> cargadas = new List<Cargada>();

    // Descargas en curso y su resultado, para no pedir dos veces la misma foto
    static readonly HashSet<Material> enCurso = new HashSet<Material>();
    static readonly HashSet<Material> fallidas = new HashSet<Material>();

    // Cola de fotos por adelantar y lugares que no se deben soltar
    static readonly List<Material> cola = new List<Material>();
    static readonly HashSet<Material> protegidas = new HashSet<Material>();
    static bool trabajando;

    // Al iniciar (o al volver a dar Play en el editor) se parte de cero
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        cargadas.Clear(); enCurso.Clear(); fallidas.Clear(); cola.Clear(); protegidas.Clear();
        trabajando = false;
    }

    static bool Movil { get { return Application.isMobilePlatform; } }
    static int Maximo { get { return Mathf.Max(2, Movil ? MaximoEnMemoriaMovil : MaximoEnMemoria); } }
    static int MaximoVecinos { get { return Mathf.Max(0, Movil ? VecinosPorAdelantarMovil : VecinosPorAdelantar); } }

    /// <summary>true si la foto de este material hay que descargarla antes de mostrarla.</summary>
    public static bool Necesita(Material m)
    {
        return m != null && m.HasProperty(Propiedad) && m.GetTexture(Propiedad) == null;
    }

    /// <summary>Nombre del archivo de una panorámica: el del material, en minúsculas y sin símbolos.</summary>
    public static string NombreArchivo(string nombreMaterial)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in (nombreMaterial ?? "").ToLowerInvariant())
            sb.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ? c : '_');
        return sb.ToString();
    }

    // Computadores: DXT. Celulares: ASTC. Cada formato tiene su carpeta.
    static string Formato()
    {
        if (SystemInfo.SupportsTextureFormat(TextureFormat.DXT1)) return "dxt";
        if (SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_6x6)) return "astc";
        return "dxt";
    }

    /// <summary>Deja lista la foto de un material. Consultar UltimaCargaOk al terminar.</summary>
    public static IEnumerator Preparar(Material m)
    {
        UltimaCargaOk = true;
        if (m == null) yield break;

        // Si ya se estaba adelantando, se espera a que termine en vez de pedirla otra vez
        while (enCurso.Contains(m)) yield return null;

        if (!Necesita(m))
        {
            Usar(m);
            yield break;
        }

        yield return Descargar(m);
        UltimaCargaOk = !Necesita(m);
    }

    /// <summary>
    /// Empieza a descargar una foto sin esperar a que termine. Sirve para ganar tiempo
    /// mientras la pantalla se oscurece. Preparar() la recoge cuando llegue.
    /// </summary>
    public static void Adelantar(MonoBehaviour anfitrion, Material m)
    {
        if (anfitrion == null || !Necesita(m) || enCurso.Contains(m)) return;
        cola.Remove(m);
        fallidas.Remove(m);
        anfitrion.StartCoroutine(Descargar(m));
    }

    /// <summary>
    /// Define los lugares a los que se puede ir desde el actual y los descarga en segundo plano,
    /// en el orden recibido. Reemplaza la cola anterior. Esos lugares no se sueltan de la memoria.
    /// </summary>
    public static void AdelantarVecinos(MonoBehaviour anfitrion, Material actual, IList<Material> vecinos)
    {
        cola.Clear();
        protegidas.Clear();
        fallidas.Clear();
        if (actual != null) protegidas.Add(actual);

        if (vecinos != null)
        {
            int limite = MaximoVecinos;
            foreach (Material v in vecinos)
            {
                if (v == null || v == actual || protegidas.Contains(v)) continue;
                if (protegidas.Count - 1 >= limite) break;
                protegidas.Add(v);
                if (Necesita(v)) cola.Add(v);
                else Usar(v);
            }
        }

        Liberar(actual);
        Arrancar(anfitrion);
    }

    /// <summary>
    /// Añade fotos al frente de la cola (por ejemplo las del menú de un edificio que se acaba de abrir).
    /// No cambia los vecinos protegidos.
    /// </summary>
    public static void AdelantarAlFrente(MonoBehaviour anfitrion, IList<Material> materiales, int cuantas)
    {
        if (materiales == null) return;
        int puesto = 0;
        foreach (Material m in materiales)
        {
            if (puesto >= cuantas) break;
            if (!Necesita(m) || enCurso.Contains(m)) continue;
            cola.Remove(m);
            cola.Insert(puesto, m);
            puesto++;
        }
        Arrancar(anfitrion);
    }

    static void Arrancar(MonoBehaviour anfitrion)
    {
        if (trabajando || cola.Count == 0 || anfitrion == null || !anfitrion.isActiveAndEnabled) return;
        anfitrion.StartCoroutine(Trabajar());
    }

    // Una foto a la vez, para no competir con la descarga del lugar que el usuario acaba de elegir
    static IEnumerator Trabajar()
    {
        trabajando = true;
        while (cola.Count > 0)
        {
            Material m = cola[0];
            cola.RemoveAt(0);
            if (m == null || !Necesita(m) || enCurso.Contains(m) || fallidas.Contains(m)) continue;

            // Si no cabe sin soltar algo que se necesita, no se adelanta más
            if (cargadas.Count >= Maximo && !HayLibreParaSoltar()) break;

            yield return Descargar(m);
            Liberar(RenderSettings.skybox);
            yield return null;
        }
        trabajando = false;
    }

    static bool HayLibreParaSoltar()
    {
        foreach (Cargada c in cargadas)
            if (!protegidas.Contains(c.material) && c.material != RenderSettings.skybox) return true;
        return false;
    }

    static IEnumerator Descargar(Material m)
    {
        if (m == null || enCurso.Contains(m) || !Necesita(m)) yield break;
        enCurso.Add(m);

        string url = Application.streamingAssetsPath + "/" + Carpeta + "/" + Formato() + "/" + NombreArchivo(m.name);
        AssetBundle paquete = null;

        for (int intento = 0; intento < 2 && paquete == null; intento++)
        {
            using (UnityWebRequest pedido = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return pedido.SendWebRequest();
                if (pedido.result == UnityWebRequest.Result.Success)
                    paquete = DownloadHandlerAssetBundle.GetContent(pedido);
                else
                    Debug.LogWarning("[CargadorPanoramas] No se pudo descargar " + url + ": " + pedido.error);
            }
        }

        if (paquete == null)
        {
            fallidas.Add(m);
            enCurso.Remove(m);
            yield break;
        }

        AssetBundleRequest lectura = paquete.LoadAllAssetsAsync<Texture>();
        yield return lectura;
        Texture textura = lectura.allAssets != null && lectura.allAssets.Length > 0 ? lectura.allAssets[0] as Texture : null;
        paquete.Unload(false);

        if (textura == null)
        {
            Debug.LogWarning("[CargadorPanoramas] El archivo " + url + " no trae una panorámica.");
            fallidas.Add(m);
            enCurso.Remove(m);
            yield break;
        }

        if (m != null && Necesita(m))
        {
            m.SetTexture(Propiedad, textura);
            Cargada c = new Cargada();
            c.material = m;
            c.textura = textura;
            cargadas.Add(c);
        }
        else
        {
            Object.Destroy(textura);
        }
        enCurso.Remove(m);
    }

    // La más reciente va al final de la lista
    static void Usar(Material m)
    {
        for (int i = 0; i < cargadas.Count; i++)
        {
            if (cargadas[i].material != m) continue;
            Cargada c = cargadas[i];
            cargadas.RemoveAt(i);
            cargadas.Add(c);
            return;
        }
    }

    /// <summary>Suelta las panorámicas más antiguas. Nunca la que se está viendo ni sus vecinas.</summary>
    public static void Liberar(Material actual)
    {
        int i = 0;
        while (cargadas.Count > Maximo && i < cargadas.Count)
        {
            Cargada c = cargadas[i];
            if (c.material == actual || c.material == RenderSettings.skybox || protegidas.Contains(c.material)) { i++; continue; }

            if (c.material != null) c.material.SetTexture(Propiedad, null);
            if (c.textura != null) Object.Destroy(c.textura);
            cargadas.RemoveAt(i);
        }
    }
}
