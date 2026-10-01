using System.Collections.Generic;
using UnityEngine;

/// <summary>Ficha de información de un lugar o dependencia (la que muestra PanelInfo).</summary>
[System.Serializable]
public class FichaInfo
{
    public string id;
    public string titulo;
    public string tipo;            // Oficina, Edificio, Servicio, Espacio o Dato
    public string zona;
    public string resumen;
    public string descripcion;
    public string tituloLista;     // encabezado de la lista (si está vacío se elige según el tipo)
    public string[] queEncuentras;
    public string horario;
    public string ubicacion;
    public string contacto;
    public string enlace;
    public string textoEnlace;
    public string fuente;          // de dónde salió la información (no se muestra)
    public string[] skyboxes;      // fotos 360 en las que aplica esta ficha

    public TipoPuntoInfo Tipo
    {
        get
        {
            switch ((tipo ?? "").Trim().ToLowerInvariant())
            {
                case "oficina": return TipoPuntoInfo.Oficina;
                case "edificio": return TipoPuntoInfo.Edificio;
                case "servicio": return TipoPuntoInfo.Servicio;
                case "espacio": return TipoPuntoInfo.Espacio;
                default: return TipoPuntoInfo.Dato;
            }
        }
    }

    public string NombreTipo
    {
        get
        {
            switch (Tipo)
            {
                case TipoPuntoInfo.Oficina: return "Oficina";
                case TipoPuntoInfo.Edificio: return "Edificio";
                case TipoPuntoInfo.Servicio: return "Servicio";
                case TipoPuntoInfo.Espacio: return "Espacio";
                default: return "Dato";
            }
        }
    }

    public Color ColorZona { get { return string.IsNullOrEmpty(zona) ? MarcaUdB.Negro : MarcaUdB.ColorZona(zona); } }
}

/// <summary>Nombre oficial de un lugar del recorrido, por foto 360.</summary>
[System.Serializable]
public class NombreOficial
{
    public string skybox;
    public string nombre;
}

[System.Serializable]
public class ArchivoCatalogo
{
    public FichaInfo[] fichas;
    public NombreOficial[] nombres;
}

/// <summary>
/// Catálogo de fichas y nombres oficiales: Assets/Resources/PuntosInfo/catalogo.json.
/// Se edita sin abrir Unity (cualquier editor de texto). Lo usan los puntos de información,
/// el botón «i» del chip «Estás en» y la herramienta «Recorrido → Aplicar nombres oficiales».
/// </summary>
public static class CatalogoInfo
{
    public const string Ruta = "PuntosInfo/catalogo";

    static ArchivoCatalogo datos;

    static ArchivoCatalogo Datos
    {
        get
        {
            if (datos == null)
            {
                TextAsset texto = Resources.Load<TextAsset>(Ruta);
                if (texto != null)
                {
                    try { datos = JsonUtility.FromJson<ArchivoCatalogo>(texto.text); }
                    catch (System.Exception e) { Debug.LogWarning("[CatalogoInfo] No se pudo leer el catálogo: " + e.Message); }
                }
                if (datos == null) datos = new ArchivoCatalogo();
                if (datos.fichas == null) datos.fichas = new FichaInfo[0];
                if (datos.nombres == null) datos.nombres = new NombreOficial[0];
            }
            return datos;
        }
    }

    /// <summary>Vuelve a leer el archivo (útil en el editor después de editarlo).</summary>
    public static void Recargar() { datos = null; }

    public static IEnumerable<FichaInfo> Fichas { get { return Datos.fichas; } }
    public static IEnumerable<NombreOficial> Nombres { get { return Datos.nombres; } }

    public static FichaInfo PorId(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (FichaInfo f in Datos.fichas)
            if (f != null && f.id == id) return f;
        return null;
    }

    public static FichaInfo PorSkybox(Material sky)
    {
        if (sky == null) return null;
        return PorSkybox(sky.name);
    }

    public static FichaInfo PorSkybox(string nombreSkybox)
    {
        if (string.IsNullOrEmpty(nombreSkybox)) return null;
        foreach (FichaInfo f in Datos.fichas)
        {
            if (f == null || f.skyboxes == null) continue;
            foreach (string s in f.skyboxes)
                if (s == nombreSkybox) return f;
        }
        return null;
    }

    public static string NombreOficialDe(Material sky)
    {
        if (sky == null) return null;
        foreach (NombreOficial n in Datos.nombres)
            if (n != null && n.skybox == sky.name && !string.IsNullOrEmpty(n.nombre)) return n.nombre;
        return null;
    }
}
