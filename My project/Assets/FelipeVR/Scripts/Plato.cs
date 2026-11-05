using UnityEngine;

public class Plato : MonoBehaviour
{
    [Header("Configuración del Plato")]
    [Tooltip("Nombre exacto del objeto que es solo el plato vacío (según tu imagen, es Cylinder).")]
    [SerializeField] private string nombreBasePlato = "Cylinder";

    private bool estaServido = false;

    private void Start()
    {
        OcultarComida();
    }

    private void OcultarComida()
    {
        // Recorre todos los objetos hijos (Cylinder.001, Vuelta_01, etc.)
        foreach (Transform hijo in transform)
        {
            // Si el hijo NO se llama "Cylinder", lo apaga
            if (hijo.name != nombreBasePlato)
            {
                hijo.gameObject.SetActive(false);
            }
        }
    }

    private void MostrarComida()
    {
        // Enciende todos los objetos hijos para mostrar el rondón completo
        foreach (Transform hijo in transform)
        {
            hijo.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaServido) return;

        // Verifica si lo que entró al trigger del plato fue el cucharón
        Cucharon cucharon = other.GetComponentInParent<Cucharon>();
        if (cucharon == null) cucharon = other.GetComponentInChildren<Cucharon>();

        // Si es el cucharón y además tiene comida
        if (cucharon != null && cucharon.EstaLleno)
        {
            estaServido = true;
            MostrarComida();
            cucharon.Vaciar(); // El cucharón vuelve a quedar vacío después de servir

            Debug.Log("Plato servido con éxito.");

            // Descomenta si usas el recetario:
            // RecetaEventos.Notificar(RecetaEventos.PlatoServido);
        }
    }
}