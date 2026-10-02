using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Carga por demanda de las panorámicas en la versión web.
///
/// En el build web las fotos 360 no van dentro del programa: cada una viaja en su propio archivo
/// (StreamingAssets/Panoramas/dxt o astc) y se descarga solo cuando se entra a ese lugar.
/// Así la página abre rápido y la memoria no se llena con las 50 fotos a la vez.
///
/// En el editor y en un build normal los materiales ya traen su textura, y este cargador no hace nada.
/// Los archivos los genera «Recorrido → Web y móvil → 3. Construir para web».
/// </summary>
public static class CargadorPanoramas
{
    const string Propiedad = "_Tex";
    const string Carpeta = "Panoramas";

    /// <summary>Cuántas panorámicas se conservan en memoria (la actual y las últimas visitadas).</summary>
    public static int MaximoEnMemoria = 4;

    /// <summary>Resultado de la última llamada a Preparar.</summary>
    public static bool UltimaCargaOk { get; private set; }

    class Cargada { public Material material; public Texture textura; }
    static readonly List<Cargada> cargadas = new List<Cargada>();

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

        if (!Necesita(m))
        {
            Usar(m);
            yield break;
        }

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
            UltimaCargaOk = false;
            yield break;
        }

        AssetBundleRequest lectura = paquete.LoadAllAssetsAsync<Texture>();
        yield return lectura;
        Texture textura = lectura.allAssets != null && lectura.allAssets.Length > 0 ? lectura.allAssets[0] as Texture : null;
        paquete.Unload(false);

        if (textura == null)
        {
            Debug.LogWarning("[CargadorPanoramas] El archivo " + url + " no trae una panorámica.");
            UltimaCargaOk = false;
            yield break;
        }

        m.SetTexture(Propiedad, textura);
        Cargada c = new Cargada();
        c.material = m;
        c.textura = textura;
        cargadas.Add(c);
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

    /// <summary>Suelta las panorámicas más antiguas. Nunca la que se está viendo.</summary>
    public static void Liberar(Material actual)
    {
        int i = 0;
        while (cargadas.Count > Mathf.Max(1, MaximoEnMemoria) && i < cargadas.Count)
        {
            Cargada c = cargadas[i];
            if (c.material == actual) { i++; continue; }

            if (c.material != null) c.material.SetTexture(Propiedad, null);
            if (c.textura != null) Object.Destroy(c.textura);
            cargadas.RemoveAt(i);
        }
    }
}
