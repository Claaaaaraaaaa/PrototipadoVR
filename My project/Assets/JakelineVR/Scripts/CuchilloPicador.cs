
using UnityEngine;

public class CuchilloPicador : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        IngredientePicable ingrediente =
            other.GetComponentInParent<IngredientePicable>();

        if (ingrediente != null)
        {
            ingrediente.Picar();
        }
    }
}
