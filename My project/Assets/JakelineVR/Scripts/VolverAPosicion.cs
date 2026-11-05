using System.Collections;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Hace que un objeto agarrable vuelva a su posicion original cuando el usuario lo suelta.
/// Ponlo en el objeto que tiene el Grabbable (o en un hijo).
/// </summary>
public class VolverAPosicion : MonoBehaviour
{
    [Tooltip("El Grabbable del objeto. Si esta vacio, lo busca en este objeto o sus padres.")]
    public Grabbable agarrable;

    [Tooltip("Segundos que espera despues de soltarlo antes de volver (0 = inmediato)")]
    public float esperaAntesDeVolver = 0.5f;

    [Tooltip("Segundos que tarda en llegar a su sitio")]
    public float duracionRegreso = 0.6f;

    public bool Agarrado => agarresActivos > 0;
    public bool Regresando => regresando;

    Transform objetivo;
    Rigidbody rb;
    Vector3 posInicial;
    Quaternion rotInicial;
    bool kinematicInicial;
    bool gravedadInicial;

    int agarresActivos;
    bool regresando;
    Coroutine rutina;

    void Awake()
    {
        if (agarrable == null)
            agarrable = GetComponentInParent<Grabbable>();

        objetivo = agarrable != null ? agarrable.transform : transform;
        rb = objetivo.GetComponent<Rigidbody>();

        posInicial = objetivo.position;
        rotInicial = objetivo.rotation;

        if (rb != null)
        {
            kinematicInicial = rb.isKinematic;
            gravedadInicial = rb.useGravity;
        }

        if (agarrable == null)
            Debug.LogWarning("[VolverAPosicion] No se encontro un Grabbable en " + name, this);
    }

    void OnEnable()
    {
        if (agarrable != null)
            agarrable.WhenPointerEventRaised += AlEvento;
    }

    void OnDisable()
    {
        if (agarrable != null)
            agarrable.WhenPointerEventRaised -= AlEvento;

        if (rutina != null) StopCoroutine(rutina);
        rutina = null;

        if (regresando)
        {
            objetivo.SetPositionAndRotation(posInicial, rotInicial);
            RestaurarFisica();
        }

        regresando = false;
        agarresActivos = 0;
    }

    void AlEvento(PointerEvent e)
    {
        switch (e.Type)
        {
            case PointerEventType.Select:
                agarresActivos++;
                Cancelar();
                break;

            case PointerEventType.Unselect:
                agarresActivos = Mathf.Max(0, agarresActivos - 1);
                if (agarresActivos == 0) Iniciar();
                break;

            case PointerEventType.Cancel:
                agarresActivos = 0;
                Iniciar();
                break;
        }
    }

    void Iniciar()
    {
        if (!isActiveAndEnabled) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Regresar());
    }

    void Cancelar()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }

        if (regresando)
        {
            regresando = false;
            RestaurarFisica();
        }
    }

    IEnumerator Regresar()
    {
        if (esperaAntesDeVolver > 0f)
            yield return new WaitForSeconds(esperaAntesDeVolver);

        if (agarresActivos > 0) yield break;

        regresando = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 posDesde = objetivo.position;
        Quaternion rotDesde = objetivo.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duracionRegreso);
            float s = Mathf.SmoothStep(0f, 1f, t);

            objetivo.SetPositionAndRotation(
                Vector3.Lerp(posDesde, posInicial, s),
                Quaternion.Slerp(rotDesde, rotInicial, s));

            yield return null;
        }

        objetivo.SetPositionAndRotation(posInicial, rotInicial);
        RestaurarFisica();

        regresando = false;
        rutina = null;
    }

    void RestaurarFisica()
    {
        if (rb == null) return;

        rb.isKinematic = kinematicInicial;
        rb.useGravity = gravedadInicial;

        if (!rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>Fuerza el regreso (por ejemplo al reiniciar la receta).</summary>
    public void VolverAhora()
    {
        agarresActivos = 0;
        Iniciar();
    }
}