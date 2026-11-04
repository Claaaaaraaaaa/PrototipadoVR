using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class CuchilloPicador : MonoBehaviour
{
    [Header("Agarre")]
    [Tooltip("El Grabbable del cuchillo. Si esta vacio, lo busca en este objeto o sus padres.")]
    public Grabbable agarrable;

    [Header("Volver a su posicion")]
    [Tooltip("Segundos que espera despues de soltarlo antes de volver")]
    public float esperaAntesDeVolver = 1f;
    [Tooltip("Segundos que tarda en llegar a su sitio")]
    public float duracionRegreso = 0.6f;

    [Header("Picar")]
    [Tooltip("Solo pica si el usuario lo tiene en la mano (evita picar mientras regresa o si cae encima)")]
    public bool picarSoloAgarrado = true;

    Transform objetivo;          // el objeto que se mueve (el del Grabbable)
    Rigidbody rb;
    Vector3 posInicial;
    Quaternion rotInicial;
    bool kinematicInicial;
    bool gravedadInicial;

    int agarresActivos;
    bool regresando;
    Coroutine rutinaRegreso;

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
            Debug.LogWarning("[Cuchillo] No se encontro un Grabbable. Asignalo en el inspector.", this);
    }

    void OnEnable()
    {
        if (agarrable != null)
            agarrable.WhenPointerEventRaised += AlEventoAgarre;
    }

    void OnDisable()
    {
        if (agarrable != null)
            agarrable.WhenPointerEventRaised -= AlEventoAgarre;

        agarresActivos = 0;
        regresando = false;
    }

    // ------------------------------------------------------------------
    // Agarrar / soltar
    // ------------------------------------------------------------------

    void AlEventoAgarre(PointerEvent e)
    {
        switch (e.Type)
        {
            case PointerEventType.Select:
                agarresActivos++;
                CancelarRegreso();      // si lo vuelven a agarrar mientras regresa, se detiene
                break;

            case PointerEventType.Unselect:
                agarresActivos = Mathf.Max(0, agarresActivos - 1);
                if (agarresActivos == 0)
                    rutinaRegreso = StartCoroutine(Regresar());
                break;

            case PointerEventType.Cancel:
                agarresActivos = 0;
                rutinaRegreso = StartCoroutine(Regresar());
                break;
        }
    }

    void CancelarRegreso()
    {
        if (rutinaRegreso != null)
        {
            StopCoroutine(rutinaRegreso);
            rutinaRegreso = null;
        }

        if (regresando)
        {
            regresando = false;
            RestaurarFisica();
        }
    }

    IEnumerator Regresar()
    {
        // Deja que caiga o se quede donde lo soltaron un momento
        yield return new WaitForSeconds(esperaAntesDeVolver);

        if (agarresActivos > 0)
            yield break;

        regresando = true;

        // Mientras regresa, sin fisica para que no choque con nada
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
        rutinaRegreso = null;
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

    // Llamalo desde otro script si quieres forzar que vuelva (por ejemplo al reiniciar la escena)
    public void VolverAhora()
    {
        CancelarRegreso();
        agarresActivos = 0;
        rutinaRegreso = StartCoroutine(Regresar());
    }

    // ------------------------------------------------------------------
    // Picar
    // ------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
        if (regresando) return;
        if (picarSoloAgarrado && agarresActivos == 0) return;

        IngredientePicable ingrediente = other.GetComponentInParent<IngredientePicable>();
        if (ingrediente != null)
            ingrediente.Picar();
    }
}