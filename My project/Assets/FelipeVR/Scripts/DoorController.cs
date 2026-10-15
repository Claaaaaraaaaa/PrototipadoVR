using UnityEngine;

public class DoorController : MonoBehaviour
{
    private bool isOpen = false;

    public void AbrirPuerta()
    {
        isOpen = !isOpen;
        // Rota la puerta 90 grados en el eje Y como prueba temporal
        float targetAngle = isOpen ? 90f : 0f;
        transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
    }
}