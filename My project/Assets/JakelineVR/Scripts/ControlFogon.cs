
using UnityEngine;

public class ControlFogon : MonoBehaviour
{
    [SerializeField] private GameObject efectoFogon;
    private bool encendido = false;

    public void AlternarFogon()
    {
        encendido = !encendido;
        if (encendido) RecetaEventos.Notificar(RecetaEventos.EstufaEncendida);
        Debug.Log("Fogón encendido: " + encendido);

        if (efectoFogon != null)
            efectoFogon.SetActive(encendido);
    }
}
