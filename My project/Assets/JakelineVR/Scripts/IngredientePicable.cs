using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class IngredientePicable : MonoBehaviour
{
    [Header("Receta")]
    [Tooltip("Id del ingrediente para la receta: cebolla, yuca, caracoles, sal... (sin tildes ni espacios)")]
    [SerializeField] private string idIngrediente = "";
    public string IdIngrediente => idIngrediente;
    [Header("Modelos visuales")]
    [SerializeField] private GameObject modeloEntero;
    [SerializeField] private GameObject modeloPicado;

    [Header("Picado")]
    [Tooltip("Cuantos cortes hacen falta para picarlo")]
    [Min(1)][SerializeField] private int cortesNecesarios = 1;
    [Tooltip("Tiempo minimo entre cortes, para que un solo pase del cuchillo no cuente varias veces")]
    [SerializeField] private float tiempoEntreCortes = 0.3f;

    [Header("Eventos (opcional: sonido, particulas...)")]
    public UnityEvent alCortar;
    public UnityEvent alPicarse;

    private bool yaPicado = false;
    private bool dentroDeOlla = false;
    private int cortes = 0;
    private float ultimoCorte = -99f;

    public bool YaPicado => yaPicado;
    public bool DentroDeOlla => dentroDeOlla;

    /// <summary>True si ya esta picado y todavia no esta en la olla.</summary>
    public bool ListoParaOlla => yaPicado && !dentroDeOlla;

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

        if (Time.time - ultimoCorte < tiempoEntreCortes)
            return;
        ultimoCorte = Time.time;

        if (modeloEntero == null || modeloPicado == null)
        {
            Debug.LogWarning("Asigna los modelos entero y picado en " + gameObject.name, this);
            return;
        }

        cortes++;
        alCortar.Invoke();

        if (cortes < cortesNecesarios)
        {
            Debug.Log("Corte " + cortes + "/" + cortesNecesarios + " en " + gameObject.name, this);
            return;
        }

        yaPicado = true;

        modeloEntero.SetActive(false);
        modeloPicado.SetActive(true);

        alPicarse.Invoke();
        Debug.Log("Ingrediente picado: " + gameObject.name, this);
    }

    /// <summary>
    /// Mete el ingrediente en la olla. Devuelve true si entro.
    /// La notificacion a la receta la hace DetectorOlla.
    /// </summary>
    public bool IncorporarAlaOlla(Transform puntoDestino)
    {
        if (!ListoParaOlla || puntoDestino == null)
            return false;

        dentroDeOlla = true;

        // Se ha removido la lógica de desactivación de Meta XR y Colliders ya que el objeto será destruido.

        Debug.Log("Ingrediente picable destruido al entrar a la olla: " + gameObject.name, this);
        Destroy(gameObject);

        return true;
    }
}