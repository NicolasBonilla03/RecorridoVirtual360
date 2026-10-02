using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Registro anónimo de uso del recorrido, para los indicadores de la validación:
///  - tasa de completitud: qué espacios visitó cada usuario, sobre el total;
///  - tiempo de navegación por espacio: segundos entre la entrada y la salida de cada foto 360;
///  - uso de las fichas de información y del guía virtual.
///
/// No guarda nombres, correos ni nada que identifique a la persona: cada sesión recibe un
/// identificador al azar y un código corto que el participante escribe en la encuesta, para
/// poder cruzar sus respuestas con su recorrido.
///
/// Los datos se envían a una hoja de Google Sheets (ver Assets/Marca/REGISTRO-DE-USO.md).
/// La dirección se pone en Assets/Resources/RegistroUso.json. Sin dirección, no se envía nada.
/// Se crea solo al iniciar; no hay que añadirlo a la escena.
/// </summary>
public class RegistroUso : MonoBehaviour
{
    [Serializable]
    class Configuracion
    {
        public string url = "";
        public bool enviarDesdeEditor = false;
        public float segundosEntreEnvios = 20f;
    }

    [Serializable]
    class Visita
    {
        public int orden;
        public string espacio;
        public string skybox;
        public string entrada;
        public float segundos;
        [NonSerialized] public bool pendiente;
    }

    [Serializable]
    class Evento
    {
        public string hora;
        public string tipo;
        public string detalle;
        public string espacio;
    }

    [Serializable]
    class Paquete
    {
        public string sesion;
        public string codigo;
        public string inicio;
        public string dispositivo;
        public string pantalla;
        public int totalEspacios;
        public int espaciosVisitados;
        public float segundosTotales;
        public int fichasAbiertas;
        public int preguntasAlGuia;
        public List<Visita> visitas = new List<Visita>();
        public List<Evento> eventos = new List<Evento>();
    }

    static RegistroUso instancia;

    /// <summary>Código corto de la sesión, para escribirlo en la encuesta.</summary>
    public static string Codigo { get { return instancia != null ? instancia.codigo : ""; } }

    /// <summary>true si hay una hoja configurada y los datos se están enviando.</summary>
    public static bool Activo { get { return instancia != null && instancia.PuedeEnviar; } }

    /// <summary>Anota un hecho puntual: abrir una ficha, preguntar al guía…</summary>
    public static void Anotar(string tipo, string detalle)
    {
        if (instancia != null) instancia.AgregarEvento(tipo, detalle);
    }

    Configuracion config = new Configuracion();
    string sesion;
    string codigo;
    string inicio;
    readonly List<Visita> visitas = new List<Visita>();
    readonly List<Evento> eventosPendientes = new List<Evento>();
    readonly HashSet<string> visitados = new HashSet<string>();
    Visita actual;
    Material ultimoSkybox;
    float segundosTotales;
    float proximoEnvio;
    int fichasAbiertas;
    int preguntasAlGuia;
    int totalEspacios;
    bool enviando;
    bool hayCambios;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        GameObject go = new GameObject("RegistroUso");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<RegistroUso>();
    }

    bool PuedeEnviar
    {
        get
        {
            if (string.IsNullOrEmpty(config.url)) return false;
            if (Application.isEditor && !config.enviarDesdeEditor) return false;
            return true;
        }
    }

    void Awake()
    {
        TextAsset texto = Resources.Load<TextAsset>("RegistroUso");
        if (texto != null)
        {
            try { config = JsonUtility.FromJson<Configuracion>(texto.text) ?? new Configuracion(); }
            catch (Exception e) { Debug.LogWarning("[RegistroUso] No se pudo leer RegistroUso.json: " + e.Message); }
        }
        if (config.url != null) config.url = config.url.Trim();
        if (config.segundosEntreEnvios < 5f) config.segundosEntreEnvios = 5f;

        sesion = Guid.NewGuid().ToString("N");
        codigo = CrearCodigo();
        inicio = Ahora();
        proximoEnvio = config.segundosEntreEnvios;
    }

    // Seis caracteres sin letras ni números que se confundan (sin 0, O, 1, I, L)
    static string CrearCodigo()
    {
        const string letras = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        System.Random azar = new System.Random(Guid.NewGuid().GetHashCode());
        char[] c = new char[6];
        for (int i = 0; i < c.Length; i++) c[i] = letras[azar.Next(letras.Length)];
        return new string(c);
    }

    static string Ahora()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    void Update()
    {
        // Un cuadro muy largo (pestaña en segundo plano, descarga) no cuenta como tiempo de visita
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f);
        bool enTransicion = FadeController.Instance != null && FadeController.Instance.EnTransicion;

        Material sky = RenderSettings.skybox;
        if (sky != ultimoSkybox)
        {
            ultimoSkybox = sky;
            CerrarVisita();
            AbrirVisita(sky);
        }

        if (actual != null && !enTransicion)
        {
            actual.segundos += dt;
            actual.pendiente = true;
            segundosTotales += dt;
            hayCambios = true;
        }

        proximoEnvio -= dt;
        if (proximoEnvio <= 0f)
        {
            proximoEnvio = config.segundosEntreEnvios;
            Enviar();
        }
    }

    void AbrirVisita(Material sky)
    {
        if (sky == null) { actual = null; return; }

        if (totalEspacios == 0) totalEspacios = ContarEspacios();

        string nombre = CatalogoInfo.NombreOficialDe(sky);
        if (string.IsNullOrEmpty(nombre)) nombre = sky.name.Replace("Skybox_", "");

        actual = new Visita();
        actual.orden = visitas.Count + 1;
        actual.espacio = nombre;
        actual.skybox = sky.name;
        actual.entrada = Ahora();
        actual.pendiente = true;
        visitas.Add(actual);
        visitados.Add(sky.name);
        hayCambios = true;
    }

    void CerrarVisita()
    {
        if (actual == null) return;
        actual.pendiente = true;
        actual = null;
        proximoEnvio = 0f; // al salir de un espacio se envía de una vez
    }

    // Total de espacios del recorrido: las fotos 360 registradas en el gestor de botones
    static int ContarEspacios()
    {
        GestorTeleports g = GestorTeleports.Instance;
        if (g == null || g.configuraciones == null) return 0;
        HashSet<Material> unicos = new HashSet<Material>();
        foreach (ConfiguracionSkybox c in g.configuraciones)
            if (c != null && c.skybox != null) unicos.Add(c.skybox);
        if (g.skyboxInicial != null) unicos.Add(g.skyboxInicial);
        return unicos.Count;
    }

    void AgregarEvento(string tipo, string detalle)
    {
        Evento e = new Evento();
        e.hora = Ahora();
        e.tipo = tipo;
        e.detalle = detalle;
        e.espacio = actual != null ? actual.espacio : "";
        eventosPendientes.Add(e);
        if (tipo == "ficha") fichasAbiertas++;
        else if (tipo == "guia") preguntasAlGuia++;
        hayCambios = true;
    }

    // Al perder el foco o pasar a segundo plano se envía lo que haya (en la web no hay aviso de cierre)
    void OnApplicationFocus(bool conFoco) { if (!conFoco) Enviar(); }
    void OnApplicationPause(bool pausa) { if (pausa) Enviar(); }

    void Enviar()
    {
        if (!PuedeEnviar || enviando || !hayCambios) return;

        Paquete p = new Paquete();
        p.sesion = sesion;
        p.codigo = codigo;
        p.inicio = inicio;
        p.dispositivo = Application.isMobilePlatform ? "Celular o tableta" : "Computador";
        p.pantalla = Screen.width + "x" + Screen.height;
        p.totalEspacios = totalEspacios;
        p.espaciosVisitados = visitados.Count;
        p.segundosTotales = Mathf.Round(segundosTotales);
        p.fichasAbiertas = fichasAbiertas;
        p.preguntasAlGuia = preguntasAlGuia;

        List<Visita> enviadas = new List<Visita>();
        foreach (Visita v in visitas)
        {
            if (!v.pendiente) continue;
            Visita copia = new Visita();
            copia.orden = v.orden;
            copia.espacio = v.espacio;
            copia.skybox = v.skybox;
            copia.entrada = v.entrada;
            copia.segundos = Mathf.Round(v.segundos * 10f) / 10f;
            p.visitas.Add(copia);
            enviadas.Add(v);
        }
        List<Evento> eventos = new List<Evento>(eventosPendientes);
        p.eventos = eventos;

        hayCambios = false;
        StartCoroutine(Mandar(JsonUtility.ToJson(p), enviadas, eventos));
    }

    IEnumerator Mandar(string json, List<Visita> enviadas, List<Evento> eventos)
    {
        enviando = true;

        // Formulario simple: así el navegador no exige permisos especiales al servidor de Google
        WWWForm formulario = new WWWForm();
        formulario.AddField("datos", json);

        using (UnityWebRequest pedido = UnityWebRequest.Post(config.url, formulario))
        {
            pedido.timeout = 15;
            yield return pedido.SendWebRequest();

            // Un error de conexión deja todo pendiente para el siguiente envío.
            // Cualquier otra respuesta significa que la petición salió: Google la recibe
            // aunque el navegador no deje leer la respuesta.
            if (pedido.result == UnityWebRequest.Result.ConnectionError && pedido.responseCode == 0 && Application.internetReachability == NetworkReachability.NotReachable)
            {
                hayCambios = true;
            }
            else
            {
                foreach (Visita v in enviadas)
                    if (v != actual) v.pendiente = false;
                foreach (Evento e in eventos) eventosPendientes.Remove(e);
            }
        }

        enviando = false;
    }
}
