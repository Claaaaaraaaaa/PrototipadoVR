using UnityEngine;

public class Cucharon : MonoBehaviour
{
    [Header("Modelos visuales")]
    [SerializeField] private GameObject modeloVacio;
    [SerializeField] private GameObject modeloLleno;

    [Header("Configuración")]
    [Tooltip("Escribe aquí el nombre exacto del objeto en la olla")]
    [SerializeField] private string nombreComidaOlla = "rondon en olla";

    public bool EstaLleno { get; private set; } = false;

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

        Debug.Log("Cucharón lleno de rondón.");

        // Descomenta esta línea si estás usando el sistema de eventos de la receta
        // RecetaEventos.Notificar(RecetaEventos.CucharonLleno);
    }

    public void Vaciar()
    {
        EstaLleno = false;
        if (modeloVacio != null) modeloVacio.SetActive(true);
        if (modeloLleno != null) modeloLleno.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que tocó el cucharón contiene el nombre configurado
        if (other.gameObject.name.Contains(nombreComidaOlla))
        {
            Llenar();
        }
    }
}