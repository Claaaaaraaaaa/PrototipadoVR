
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
            Debug.LogWarning(
                "Asigna los modelos entero y picado en " + gameObject.name
            );
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

        // Guardar la escala mundial antes de cambiar de padre.
        Vector3 escalaMundo = transform.lossyScale;

        // Desactivar el agarre del ingrediente.
        foreach (MonoBehaviour componente in
                 GetComponentsInChildren<MonoBehaviour>(true))
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

        // Detener la física.
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Desactivar los colliders del ingrediente.
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        // Colocar el ingrediente en el destino.
        transform.SetParent(null, true);
        transform.position = puntoDestino.position;
        transform.rotation = puntoDestino.rotation;

        // Hacer que siga a la olla al moverla.
        Transform olla = puntoDestino.parent;
        transform.SetParent(olla, true);

        // Conservar el tamaño visual.
        Vector3 escalaPadre = transform.parent.lossyScale;

        transform.localScale = new Vector3(
            escalaMundo.x / escalaPadre.x,
            escalaMundo.y / escalaPadre.y,
            escalaMundo.z / escalaPadre.z
        );

        Debug.Log("Ingrediente colocado dentro de la olla.");
    }

}
