
using UnityEngine;

public class ControlFogon : MonoBehaviour
{
    [SerializeField] private GameObject efectoFogon;
    private bool encendido = false;

    public void AlternarFogon()
    {
        encendido = !encendido;

        if (efectoFogon != null)
            efectoFogon.SetActive(encendido);
    }
}
