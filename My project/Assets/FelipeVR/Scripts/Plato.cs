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

    /// <summary>Muestra la comida sin pasar por el cucharon (tambien lo usa RecetaDebugInput).</summary>
    public void MostrarComida()
    {
        estaServido = true;
        foreach (Transform hijo in transform)
            hijo.gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        Cucharon cucharon = other.GetComponentInParent<Cucharon>();
        if (cucharon == null) cucharon = other.GetComponentInChildren<Cucharon>();

        if (cucharon == null || !cucharon.EstaLleno)
            return;

        // El cucharon siempre se vacia: si se lleno antes de tiempo, se puede volver a llenar.
        cucharon.Vaciar();
        SonidosCocina.Sonar(Sonido.SopaEnPlato, transform.position);

        if (!estaServido)
        {
            estaServido = true;
            MostrarComida();
        }

        if (mostrarDebug) Debug.Log("[Plato] Plato servido.", this);

        // Solo cuenta para el paso 8 si ya se hizo todo lo anterior.
        if (Cucharon.PasosPreviosListos(RecetaEventos.CucharonServido))
            RecetaEventos.Notificar(RecetaEventos.CucharonServido);
    }
}