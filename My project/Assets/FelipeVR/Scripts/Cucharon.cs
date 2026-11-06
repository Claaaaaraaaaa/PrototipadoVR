using UnityEngine;

public class Cucharon : MonoBehaviour
{
    [Header("Modelos visuales")]
    [SerializeField] private GameObject modeloVacio;
    [SerializeField] private GameObject modeloLleno;

    [Header("Receta")]
    [Tooltip("Solo se llena si la olla ya tiene el rondon cocinado")]
    [SerializeField] private bool soloSiHayRondon = true;
    [SerializeField] private bool mostrarDebug = true;

    public bool EstaLleno { get; private set; } = false;

    private bool avisoSinRondon;

    private void Start()
    {
        Vaciar();
    }

    public void Llenar()
    {
        if (EstaLleno) return;

        EstaLleno = true;
        if (modeloVacio != null) modeloVacio.SetActive(false);
        if (modeloLleno != null) modeloLleno.SetActive(true);

        if (mostrarDebug) Debug.Log("[Cucharon] Lleno de rondon.", this);

        // El cucharon puede llenarse antes de tiempo. Solo cuenta para el paso
        // "llenar el cucharon" cuando ya se hizo todo lo anterior (plato en la mesa).
        if (PasosPreviosListos(RecetaEventos.CucharonLleno))
            RecetaEventos.Notificar(RecetaEventos.CucharonLleno);
    }

    public void Vaciar()
    {
        EstaLleno = false;
        if (modeloVacio != null) modeloVacio.SetActive(true);
        if (modeloLleno != null) modeloLleno.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (EstaLleno) return;

        // ¿Lo que toco es la olla?
        OllaEstado olla = other.GetComponentInParent<OllaEstado>();
        if (olla == null) return;

        if (soloSiHayRondon && olla.EstadoActual != OllaEstado.Estado.Rondon)
        {
            if (mostrarDebug && !avisoSinRondon)
                Debug.Log("[Cucharon] La olla todavia no tiene rondon (falta cocinar).", this);
            avisoSinRondon = true;
            return;
        }

        avisoSinRondon = false;
        Llenar();
    }

    public static bool PasosPreviosListos(string idAccion)
    {
        RecetaManager m = RecetaManager.Instancia != null
            ? RecetaManager.Instancia
            : FindFirstObjectByType<RecetaManager>();
        return m == null || m.PasosPreviosCompletos(idAccion);
    }
}