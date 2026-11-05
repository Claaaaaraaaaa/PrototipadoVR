using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class IngredientePicable : MonoBehaviour
{
    [Header("Modelos visuales")]
    [SerializeField] private GameObject modeloEntero;
    [SerializeField] private GameObject modeloPicado;

    [Header("Picado")]
    [Tooltip("Cuantos cortes hacen falta para picarlo")]
    [Min(1)][SerializeField] private int cortesNecesarios = 1;
    [Tooltip("Tiempo minimo entre cortes, para que un solo pase del cuchillo no cuente varias veces")]
    [SerializeField] private float tiempoEntreCortes = 0.3f;

    [Header("Eventos (opcional: sonido, particulas...)")]
    public UnityEvent alCortar;
    public UnityEvent alPicarse;

    private bool yaPicado = false;
    private bool dentroDeOlla = false;
    private int cortes = 0;
    private float ultimoCorte = -99f;

    public bool YaPicado => yaPicado;
    public bool DentroDeOlla => dentroDeOlla;

    /// <summary>True si ya esta picado y todavia no esta en la olla.</summary>
    public bool ListoParaOlla => yaPicado && !dentroDeOlla;

    private void Start()
    {
        if (modeloEntero != null)
            modeloEntero.SetActive(true);

        if (modeloPicado != null)
            modeloPicado.SetActive(false);
    }

    public void Picar()
    {
        if (yaPicado || dentroDeOlla)
            return;

        if (Time.time - ultimoCorte < tiempoEntreCortes)
            return;
        ultimoCorte = Time.time;

        if (modeloEntero == null || modeloPicado == null)
        {
            Debug.LogWarning("Asigna los modelos entero y picado en " + gameObject.name, this);
            return;
        }

        cortes++;
        alCortar.Invoke();

        if (cortes < cortesNecesarios)
        {
            Debug.Log("Corte " + cortes + "/" + cortesNecesarios + " en " + gameObject.name, this);
            return;
        }

        yaPicado = true;

        modeloEntero.SetActive(false);
        modeloPicado.SetActive(true);

        alPicarse.Invoke();
        Debug.Log("Ingrediente picado: " + gameObject.name, this);
    }

    /// <summary>
    /// Mete el ingrediente en la olla. Devuelve true si entro.
    /// La notificacion a la receta la hace DetectorOlla.
    /// </summary>
    public bool IncorporarAlaOlla(Transform puntoDestino)
    {
        if (!ListoParaOlla || puntoDestino == null)
            return false;

        dentroDeOlla = true;

        // Desactivar el agarre
        foreach (MonoBehaviour componente in GetComponentsInChildren<MonoBehaviour>(true))
        {
            string nombre = componente.GetType().Name;

            if (nombre.Contains("Grabbable") ||
                nombre.Contains("HandGrabInteractable") ||
                nombre.Contains("GrabInteractable") ||
                nombre.Contains("SnapInteractor"))
            {
                componente.enabled = false;
            }
        }

        // Desactivar colliders
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        StartCoroutine(ColocarEnOlla(puntoDestino));
        return true;
    }

    private IEnumerator ColocarEnOlla(Transform puntoDestino)
    {
        // Esperar un frame para que el sistema de agarre termine de mover el objeto
        yield return null;

        // Buscar el Rigidbody tambien en los padres
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody>();

        // El objeto que realmente hay que mover es el que tiene el Rigidbody
        Transform raiz = rb != null ? rb.transform : transform;

        Vector3 escalaMundo = raiz.lossyScale;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        raiz.SetParent(null, true);
        raiz.rotation = puntoDestino.rotation;

        // Compensar el pivote: alinear el centro visual con el punto destino
        Vector3 offset = Vector3.zero;
        Renderer[] renderers = modeloPicado.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);

            offset = raiz.position - b.center;
        }

        raiz.position = puntoDestino.position + offset;

        // Hacerlo hijo de la olla
        Transform olla = puntoDestino.parent;

        if (olla != null)
        {
            raiz.SetParent(olla, true);

            Vector3 ep = olla.lossyScale;
            raiz.localScale = new Vector3(
                escalaMundo.x / ep.x,
                escalaMundo.y / ep.y,
                escalaMundo.z / ep.z);
        }

        Physics.SyncTransforms();
        Debug.Log("Ingrediente colocado dentro de la olla: " + gameObject.name, this);
    }
}