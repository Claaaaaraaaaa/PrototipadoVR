
using UnityEngine;

public class DetectorOlla : MonoBehaviour
{
    [SerializeField] private Transform puntoDestinoIngredientes;

    private void OnTriggerEnter(Collider other)
    {
        IngredientePicable ingrediente =
            other.GetComponentInParent<IngredientePicable>();

        if (ingrediente == null)
            ingrediente = other.GetComponentInChildren<IngredientePicable>();

        if (ingrediente == null || !ingrediente.YaPicado)
            return;

        if (puntoDestinoIngredientes == null)
        {
            Debug.LogError("Falta asignar PuntoDestinoIngredientes.");
            return;
        }

        ingrediente.IncorporarAlaOlla(puntoDestinoIngredientes);
    }
}
