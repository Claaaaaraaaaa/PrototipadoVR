using System.Collections;
using UnityEngine;

public class IngredienteEntero : MonoBehaviour
{
    private bool dentroDeOlla = false;
    public bool DentroDeOlla => dentroDeOlla;

    /// <summary>
    /// Mete el ingrediente en la olla. Devuelve true si entro.
    /// La notificacion a la receta la hace DetectorOlla.
    /// </summary>
    public bool IncorporarAlaOlla(Transform puntoDestino)
    {
        if (dentroDeOlla || puntoDestino == null)
            return false;

        dentroDeOlla = true;

        // 1. Desactivar el agarre de Meta XR
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

        // 2. Desactivar colliders
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        StartCoroutine(ColocarEnOlla(puntoDestino));
        return true;
    }

    private IEnumerator ColocarEnOlla(Transform puntoDestino)
    {
        yield return null;

        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody>();

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

        Vector3 offset = Vector3.zero;
        Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);

            offset = raiz.position - b.center;
        }

        raiz.position = puntoDestino.position + offset;
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
        Debug.Log("Ingrediente entero colocado dentro de la olla: " + gameObject.name, this);
    }
}