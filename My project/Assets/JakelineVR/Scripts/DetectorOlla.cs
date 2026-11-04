using UnityEngine;

public class DetectorOlla : MonoBehaviour
{
    [SerializeField] private Transform puntoDestinoIngredientes;

    private void OnTriggerEnter(Collider other)
    {
        if (puntoDestinoIngredientes == null)
        {
            Debug.LogError("Falta asignar PuntoDestinoIngredientes.");
            return;
        }

        // BLOQUE 1: Tu código original para Ingredientes Picados (INTACTO)
        IngredientePicable ingredientePicable = other.GetComponentInParent<IngredientePicable>();
        if (ingredientePicable == null)
            ingredientePicable = other.GetComponentInChildren<IngredientePicable>();

        if (ingredientePicable != null)
        {
            if (ingredientePicable.YaPicado)
            {
                ingredientePicable.IncorporarAlaOlla(puntoDestinoIngredientes);
            }
            return;
        }

        // BLOQUE 2: Lógica para Ingredientes Enteros
        IngredienteEntero ingredienteEntero = other.GetComponentInParent<IngredienteEntero>();
        if (ingredienteEntero == null)
            ingredienteEntero = other.GetComponentInChildren<IngredienteEntero>();

        if (ingredienteEntero != null)
        {
            ingredienteEntero.IncorporarAlaOlla(puntoDestinoIngredientes);
        }
    }
}