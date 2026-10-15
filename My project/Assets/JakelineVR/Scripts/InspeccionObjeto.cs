
using UnityEngine;

public class InspeccionObjeto : MonoBehaviour
{
    [Header("Objeto a inspeccionar")]
    [SerializeField] private GameObject objetoInspeccion;
    [SerializeField] private Transform puntoInspeccion;

    [Header("Rotación")]
    [SerializeField] private float velocidadRotacion = 100f;

    private bool inspeccionando = false;

    private void Start()
    {
        if (objetoInspeccion != null)
            objetoInspeccion.SetActive(false);
    }

    public void AbrirInspeccion()
    {
        if (objetoInspeccion == null || puntoInspeccion == null)
        {
            Debug.LogWarning("Asigna el objeto y el punto de inspección.");
            return;
        }

        objetoInspeccion.transform.SetParent(puntoInspeccion, false);
        objetoInspeccion.transform.localPosition = Vector3.zero;
        objetoInspeccion.transform.localRotation = Quaternion.identity;

        objetoInspeccion.SetActive(true);
        inspeccionando = true;
    }

    public void CerrarInspeccion()
    {
        inspeccionando = false;

        if (objetoInspeccion != null)
            objetoInspeccion.SetActive(false);
    }

    public void GirarIzquierda()
    {
        if (inspeccionando && objetoInspeccion != null)
            objetoInspeccion.transform.Rotate(
                Vector3.up, -velocidadRotacion * Time.deltaTime,
                Space.Self);
    }

    public void GirarDerecha()
    {
        if (inspeccionando && objetoInspeccion != null)
            objetoInspeccion.transform.Rotate(
                Vector3.up, velocidadRotacion * Time.deltaTime,
                Space.Self);
    }
}
