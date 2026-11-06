using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de sonido de las interacciones. Va en el objeto "Efectos", con un AudioSource hijo
/// por sonido, llamado igual que su valor en el enum Sonido. Los tiempos y volumenes salen de AjustesSonidos.
/// Los scripts de la escena avisan con los metodos estaticos; si no hay Efectos en la escena, no suena nada.
/// La celebracion se dispara sola al completar la receta.
/// </summary>
public class SonidosCocina : MonoBehaviour
{
    private const float SegundosFundido = 0.3f;

    public static SonidosCocina Instancia { get; private set; }

    [SerializeField] private AjustesSonidos ajustes;

    private readonly Dictionary<Sonido, AudioSource> fuentes = new Dictionary<Sonido, AudioSource>();
    private readonly Dictionary<Sonido, Coroutine> rutinas = new Dictionary<Sonido, Coroutine>();
    private RecetaManager manager;
    private int fogonesHirviendo;

    private void Awake()
    {
        Instancia = this;

        foreach (Sonido sonido in Enum.GetValues(typeof(Sonido)))
        {
            Transform hijo = transform.Find(sonido.ToString());
            AudioSource fuente = hijo != null ? hijo.GetComponent<AudioSource>() : null;
            if (fuente == null || fuente.clip == null)
            {
                Debug.LogWarning("[SonidosCocina] Falta el hijo con audio: " + sonido, this);
                continue;
            }

            // Sin esto, cada sonido tardaria en cargarse la primera vez que suena.
            fuente.clip.LoadAudioData();
            fuentes[sonido] = fuente;
        }
    }

    private void Start()
    {
        manager = RecetaManager.Instancia != null
            ? RecetaManager.Instancia
            : FindFirstObjectByType<RecetaManager>();

        if (manager != null)
            manager.onRecetaCompletada.AddListener(AlCompletarReceta);
    }

    private void OnDestroy()
    {
        if (manager != null) manager.onRecetaCompletada.RemoveListener(AlCompletarReceta);
        if (Instancia == this) Instancia = null;
    }

    private void AlCompletarReceta()
    {
        Reproducir(Sonido.Celebracion, transform.position);
    }

    // ------------------------------------------------------------ Reproduccion

    private void Reproducir(Sonido sonido, Vector3 posicion)
    {
        if (!fuentes.TryGetValue(sonido, out AudioSource fuente)) return;

        CancelarRutina(sonido);
        fuente.transform.position = posicion;
        rutinas[sonido] = StartCoroutine(Rutina(sonido, fuente));
    }

    private void Detener(Sonido sonido)
    {
        if (!fuentes.TryGetValue(sonido, out AudioSource fuente)) return;

        CancelarRutina(sonido);
        if (fuente.isPlaying)
            rutinas[sonido] = StartCoroutine(Fundir(fuente));
    }

    private void CancelarRutina(Sonido sonido)
    {
        if (rutinas.TryGetValue(sonido, out Coroutine rutina) && rutina != null)
            StopCoroutine(rutina);
        rutinas.Remove(sonido);
    }

    private IEnumerator Rutina(Sonido sonido, AudioSource fuente)
    {
        AjusteSonido ajuste = ajustes != null ? ajustes.Buscar(sonido) : null;
        if (ajuste == null) ajuste = new AjusteSonido { sonido = sonido };

        if (ajustes != null && ajustes.mostrarDebug && Debug.isDebugBuild)
            Debug.Log("[Sonido] " + sonido + " t=" + Time.time.ToString("F2") + " inicio=" + ajuste.inicio
                + " retraso=" + ajuste.retraso + " volumen=" + ajuste.volumen + " duracion=" + ajuste.duracion, this);

        if (ajuste.retraso > 0f)
            yield return new WaitForSeconds(ajuste.retraso);

        fuente.volume = ajuste.volumen;
        fuente.time = Mathf.Clamp(ajuste.inicio, 0f, Mathf.Max(0f, fuente.clip.length - 0.01f));
        fuente.Play();

        if (ajuste.duracion > 0f)
        {
            yield return new WaitForSeconds(ajuste.duracion);
            yield return Fundir(fuente);
        }
    }

    private static IEnumerator Fundir(AudioSource fuente)
    {
        float volumenInicial = fuente.volume;
        for (float t = 0f; t < SegundosFundido; t += Time.deltaTime)
        {
            fuente.volume = Mathf.Lerp(volumenInicial, 0f, t / SegundosFundido);
            yield return null;
        }

        fuente.Stop();
    }

    /// <summary>Lo usa el boton Probar de AjustesSonidos: suena donde esta el usuario.</summary>
    public void Probar(Sonido sonido)
    {
        AudioListener oyente = FindFirstObjectByType<AudioListener>();
        Reproducir(sonido, oyente != null ? oyente.transform.position : transform.position);
    }

    // ------------------------------------------------------------ Avisos de la escena

    public static void Sonar(Sonido sonido, Vector3 posicion)
    {
        if (Instancia != null) Instancia.Reproducir(sonido, posicion);
    }

    /// <summary>Cada fogon avisa al empezar y al dejar de hervir: suena mientras alguno este hirviendo.</summary>
    public static void AguaHirviendo(bool hirviendo, Vector3 posicion)
    {
        if (Instancia == null) return;

        int antes = Instancia.fogonesHirviendo;
        Instancia.fogonesHirviendo = Mathf.Max(0, antes + (hirviendo ? 1 : -1));

        if (antes == 0 && Instancia.fogonesHirviendo > 0)
            Instancia.Reproducir(Sonido.AguaHirviendo, posicion);
        else if (Instancia.fogonesHirviendo == 0)
            Instancia.Detener(Sonido.AguaHirviendo);
    }

    /// <summary>La llave y el agua cayendo en la olla, juntos, mientras la olla esta en el lavamanos.</summary>
    public static void LlenandoOlla(bool llenando, Vector3 posicion)
    {
        if (Instancia == null) return;

        if (llenando)
        {
            Instancia.Reproducir(Sonido.AguaLlave, posicion);
            Instancia.Reproducir(Sonido.AguaEnOlla, posicion);
        }
        else
        {
            Instancia.Detener(Sonido.AguaLlave);
            Instancia.Detener(Sonido.AguaEnOlla);
        }
    }
}
