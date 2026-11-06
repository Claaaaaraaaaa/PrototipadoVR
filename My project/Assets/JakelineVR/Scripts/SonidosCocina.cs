using UnityEngine;

/// <summary>
/// Efectos de sonido de las interacciones. Va en el objeto "Efectos", con un AudioSource hijo por sonido.
/// Los scripts de la escena avisan con los metodos estaticos; si no hay Efectos en la escena, no suena nada.
/// La celebracion se dispara sola al completar la receta.
/// </summary>
public class SonidosCocina : MonoBehaviour
{
    public static SonidosCocina Instancia { get; private set; }

    [SerializeField] private AudioSource cortar;
    [SerializeField] private AudioSource prenderEstufa;
    [SerializeField] private AudioSource aguaHirviendo;
    [SerializeField] private AudioSource sopaEnPlato;
    [SerializeField] private AudioSource pasarPagina;
    [SerializeField] private AudioSource celebracion;

    private RecetaManager manager;
    private int fogonesHirviendo;

    private void Awake()
    {
        Instancia = this;

        // Sin esto, cada sonido tardaria en cargarse la primera vez que suena.
        foreach (AudioSource fuente in GetComponentsInChildren<AudioSource>())
            if (fuente.clip != null) fuente.clip.LoadAudioData();
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
        if (celebracion != null) celebracion.Play();
    }

    private static void Sonar(AudioSource fuente, Vector3 posicion)
    {
        if (fuente == null || fuente.clip == null) return;
        fuente.transform.position = posicion;
        fuente.PlayOneShot(fuente.clip);
    }

    public static void Cortar(Vector3 posicion)
    {
        if (Instancia != null) Sonar(Instancia.cortar, posicion);
    }

    public static void PrenderEstufa(Vector3 posicion)
    {
        if (Instancia != null) Sonar(Instancia.prenderEstufa, posicion);
    }

    public static void SopaEnPlato(Vector3 posicion)
    {
        if (Instancia != null) Sonar(Instancia.sopaEnPlato, posicion);
    }

    public static void PasarPagina(Vector3 posicion)
    {
        if (Instancia != null) Sonar(Instancia.pasarPagina, posicion);
    }

    /// <summary>Cada fogon avisa al empezar y al dejar de hervir: suena mientras alguno este hirviendo.</summary>
    public static void AguaHirviendo(bool hirviendo, Vector3 posicion)
    {
        if (Instancia == null || Instancia.aguaHirviendo == null) return;

        AudioSource fuente = Instancia.aguaHirviendo;
        Instancia.fogonesHirviendo = Mathf.Max(0, Instancia.fogonesHirviendo + (hirviendo ? 1 : -1));

        if (Instancia.fogonesHirviendo == 0)
        {
            fuente.Stop();
        }
        else if (!fuente.isPlaying)
        {
            fuente.transform.position = posicion;
            fuente.Play();
        }
    }
}
