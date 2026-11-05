using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Estado de la olla. Ponlo en el objeto raiz de la olla (donde esta el Grabbable).
/// </summary>
public class OllaEstado : MonoBehaviour
{
    public enum Estado { Vacia, ConAgua, Rondon }

    [Header("Aspectos (arrastrar desde la JERARQUIA, no desde Project)")]
    [Tooltip("Piezas de la olla vacia: Cylinder, Torus, Torus.001")]
    public GameObject[] visualVacia;
    [Tooltip("El OLLAagua que esta DENTRO de la olla en la jerarquia")]
    public GameObject visualAgua;
    [Tooltip("El 'rondon en olla' que esta DENTRO de la olla en la jerarquia")]
    public GameObject visualRondon;

    [Header("Eventos (opcional: sonido, particulas...)")]
    public UnityEvent alLlenarse;
    public UnityEvent alHacerseRondon;
    public UnityEvent alVaciarse;

    public bool mostrarDebug = true;

    public Estado EstadoActual { get; private set; } = Estado.Vacia;
    public bool TieneAgua => EstadoActual != Estado.Vacia;

    void Awake()
    {
        Validar();

        DesactivarColliders(visualAgua);
        DesactivarColliders(visualRondon);

        AplicarVisual();
    }

    void Validar()
    {
        if (GetComponents<OllaEstado>().Length > 1 ||
            GetComponentsInChildren<OllaEstado>(true).Length > 1)
            Debug.LogWarning("[Olla] Hay mas de un OllaEstado en " + name + ". Deja solo uno, en la raiz.", this);

        if (visualAgua == null)
            Debug.LogWarning("[Olla] 'Visual Agua' esta vacio.", this);
        else if (!visualAgua.scene.IsValid())
            Debug.LogError("[Olla] 'Visual Agua' es el PREFAB de la carpeta Project. " +
                           "Arrastra el OLLAagua que esta dentro de la olla en la JERARQUIA.", this);
        else if (!visualAgua.transform.IsChildOf(transform))
            Debug.LogWarning("[Olla] 'Visual Agua' no esta dentro de la olla, no se movera con ella.", this);

        if (visualRondon != null && !visualRondon.scene.IsValid())
            Debug.LogError("[Olla] 'Visual Rondon' es el PREFAB de la carpeta Project. " +
                           "Arrastra el que esta dentro de la olla en la JERARQUIA.", this);

        if (visualVacia == null || visualVacia.Length == 0)
            Debug.LogWarning("[Olla] 'Visual Vacia' esta vacio: la olla vacia no se ocultara.", this);
    }

    public void Llenar()
    {
        if (EstadoActual != Estado.Vacia) return;
        CambiarEstado(Estado.ConAgua);
        alLlenarse.Invoke();
        if (mostrarDebug) Debug.Log("[Olla] Llena de agua", this);
    }

    public void HacerRondon()
    {
        if (EstadoActual == Estado.Rondon) return;
        CambiarEstado(Estado.Rondon);
        alHacerseRondon.Invoke();
        if (mostrarDebug) Debug.Log("[Olla] Rondon listo", this);
    }

    public void Vaciar()
    {
        if (EstadoActual == Estado.Vacia) return;
        CambiarEstado(Estado.Vacia);
        alVaciarse.Invoke();
    }

    void CambiarEstado(Estado nuevo)
    {
        EstadoActual = nuevo;
        AplicarVisual();
    }

    void AplicarVisual()
    {
        bool vacia = EstadoActual == Estado.Vacia;

        // Olla vacia: solo renderers (pueden tener los colliders de la olla)
        if (visualVacia != null)
            foreach (GameObject go in visualVacia)
                MostrarRenderers(go, vacia);

        // Aspectos extra: se encienden/apagan enteros (sus colliders ya estan apagados)
        MostrarEntero(visualAgua, EstadoActual == Estado.ConAgua);
        MostrarEntero(visualRondon, EstadoActual == Estado.Rondon);

        if (mostrarDebug)
            Debug.Log("[Olla] Aspecto: " + EstadoActual, this);
    }

    static void MostrarEntero(GameObject go, bool mostrar)
    {
        if (go == null) return;
        go.SetActive(mostrar);
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = mostrar;
    }

    static void MostrarRenderers(GameObject go, bool mostrar)
    {
        if (go == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = mostrar;
    }

    static void DesactivarColliders(GameObject go)
    {
        if (go == null) return;
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
    }
}