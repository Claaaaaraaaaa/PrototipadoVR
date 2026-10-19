using System.Collections;
using UnityEngine;

public class IngredientePicable : MonoBehaviour
{
    [Header("Modelos visuales")]
    [SerializeField] private GameObject modeloEntero;
    [SerializeField] private GameObject modeloPicado;

    private bool yaPicado = false;
    private bool dentroDeOlla = false;

    public bool YaPicado => yaPicado;
    public bool DentroDeOlla => dentroDeOlla;

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

        if (modeloEntero == null || modeloPicado == null)
        {
            Debug.LogWarning("Asigna los modelos entero y picado en " + gameObject.name);
            return;
        }

        yaPicado = true;

        modeloEntero.SetActive(false);
        modeloPicado.SetActive(true);

        Debug.Log("Ingrediente picado: " + gameObject.name);
    }

    public void IncorporarAlaOlla(Transform puntoDestino)
    {
        if (!yaPicado || dentroDeOlla || puntoDestino == null)
            return;

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
    }

    private IEnumerator ColocarEnOlla(Transform puntoDestino)
    {
        // Esperar un frame para que el sistema de agarre termine de mover el objeto
        yield return null;

        // Buscar el Rigidbody también en los padres
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
        Debug.Log("Ingrediente colocado dentro de la olla.");
    }
}