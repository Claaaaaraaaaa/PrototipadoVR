using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Estado de la olla. Ponlo en el objeto raiz de la olla (donde esta el Grabbable).
/// Cambia el aspecto encendiendo/apagando solo los Renderers, para no tocar los colliders.
/// </summary>
public class OllaEstado : MonoBehaviour
{
    public enum Estado { Vacia, ConAgua, Rondon }

    [Header("Aspectos (modelos dentro de la olla)")]
    [Tooltip("Los modelos de la olla vacia (Olla, OLLA_COLOR (1))")]
    public GameObject[] visualVacia;
    [Tooltip("Copia del prefab OLLAagua dentro de la olla")]
    public GameObject visualAgua;
    [Tooltip("Copia del prefab 'rondon en olla' dentro de la olla (para pasos siguientes)")]
    public GameObject visualRondon;

    [Header("Eventos (opcional: sonido, particulas...)")]
    public UnityEvent alLlenarse;
    public UnityEvent alHacerseRondon;
    public UnityEvent alVaciarse;

    public Estado EstadoActual { get; private set; } = Estado.Vacia;

    // Compatibilidad con ZonaReceta: rondon tambien cuenta como "tiene liquido"
    public bool TieneAgua => EstadoActual != Estado.Vacia;

    void Awake()
    {
        // Los aspectos extra son solo visuales: sin colliders para no chocar con la olla
        DesactivarColliders(visualAgua);
        DesactivarColliders(visualRondon);

        AplicarVisual();
    }

    public void Llenar()
    {
        if (EstadoActual != Estado.Vacia) return;
        CambiarEstado(Estado.ConAgua);
        alLlenarse.Invoke();
        Debug.Log("[Olla] Llena de agua", this);
    }

    public void HacerRondon()
    {
        if (EstadoActual == Estado.Rondon) return;
        CambiarEstado(Estado.Rondon);
        alHacerseRondon.Invoke();
        Debug.Log("[Olla] Rondon listo", this);
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

        if (visualVacia != null)
            foreach (GameObject go in visualVacia)
                MostrarRenderers(go, vacia);

        MostrarRenderers(visualAgua, EstadoActual == Estado.ConAgua);
        MostrarRenderers(visualRondon, EstadoActual == Estado.Rondon);
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