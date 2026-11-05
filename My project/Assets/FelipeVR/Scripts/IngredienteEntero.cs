using System.Collections;
using UnityEngine;

public class IngredienteEntero : MonoBehaviour
{

    [Header("Receta")]
    [Tooltip("Id del ingrediente para la receta: cebolla, yuca, caracoles, sal... (sin tildes ni espacios)")]
    [SerializeField] private string idIngrediente = "";
    public string IdIngrediente => idIngrediente;
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

        // Se ha removido la lógica de desactivación de Meta XR y Colliders ya que el objeto será destruido.

        Debug.Log("Ingrediente entero destruido al entrar a la olla: " + gameObject.name, this);
        Destroy(gameObject);

        return true;
    }
}