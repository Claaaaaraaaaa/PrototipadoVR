using UnityEngine;

public class Plato : MonoBehaviour
{
    [Header("Configuracion del Plato")]
    [Tooltip("Nombre exacto del hijo que es solo el plato vacio (Cylinder).")]
    [SerializeField] private string nombreBasePlato = "Cylinder";
    [SerializeField] private bool mostrarDebug = true;

    private bool estaServido = false;
    public bool EstaServido => estaServido;

    private void Start()
    {
        OcultarComida();
    }

    private void OcultarComida()
    {
        foreach (Transform hijo in transform)
            if (hijo.name != nombreBasePlato)
                hijo.gameObject.SetActive(false);
    }

    private void MostrarComida()
    {
        foreach (Transform hijo in transform)
            hijo.gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        Cucharon cucharon = other.GetComponentInParent<Cucharon>();
        if (cucharon == null) cucharon = other.GetComponentInChildren<Cucharon>();

        if (cucharon == null || !cucharon.EstaLleno)
            return;

        // Primera vez: servir el plato desde la olla (paso 6)
        if (!estaServido)
        {
            estaServido = true;
            MostrarComida();
            cucharon.Vaciar();

            RecetaEventos.Notificar(RecetaEventos.PlatoServido);
            if (mostrarDebug) Debug.Log("[Plato] Plato servido.", this);
            return;
        }

        // Segunda vez: servir de nuevo en la mesa (paso 9)
        if (Cucharon.PasosPreviosListos(RecetaEventos.CucharonServido))
        {
            cucharon.Vaciar();
            RecetaEventos.Notificar(RecetaEventos.CucharonServido);
            if (mostrarDebug) Debug.Log("[Plato] Servido otra vez en la mesa.", this);
        }
    }
}