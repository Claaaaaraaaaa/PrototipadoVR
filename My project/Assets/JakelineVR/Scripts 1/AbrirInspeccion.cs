
using UnityEngine;

public class AbrirInspeccion : MonoBehaviour
{
    [SerializeField] private GameObject objetoInspeccion;
    [SerializeField] private Transform camara;
    [SerializeField] private float distancia = 0.8f;

    public void Abrir()
    {
        if (objetoInspeccion == null || camara == null)
            return;

        objetoInspeccion.SetActive(false);

        objetoInspeccion.transform.SetParent(null);

        objetoInspeccion.transform.position =
            camara.position + camara.forward * distancia;

        objetoInspeccion.transform.rotation =
            Quaternion.LookRotation(camara.forward, Vector3.up);

        objetoInspeccion.SetActive(true);
    }

    public void Cerrar()
    {
        if (objetoInspeccion != null)
            objetoInspeccion.SetActive(false);
    }
}
