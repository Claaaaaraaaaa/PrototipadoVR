
using UnityEngine;

public class NavegacionCocina : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private Transform cameraRig;

    [Header("Destinos")]
    [SerializeField] private Transform destinoBarra;
    [SerializeField] private Transform destinoEstufa;
    [SerializeField] private Transform destinoNevera;

    [Header("Paneles")]
    [SerializeField] private GameObject panelInicio;
    [SerializeField] private GameObject panelBarra;
    [SerializeField] private GameObject panelEstufa;
    [SerializeField] private GameObject panelNevera;

    public void IrBarra()
    {
        Teletransportar(destinoBarra);
        MostrarPanel(panelBarra);
    }

    public void IrEstufa()
    {
        Teletransportar(destinoEstufa);
        MostrarPanel(panelEstufa);
    }

    public void IrNevera()
    {
        Teletransportar(destinoNevera);
        MostrarPanel(panelNevera);
    }

    private void MostrarPanel(GameObject panel)
    {
        if (panelInicio != null)
            panelInicio.SetActive(false);

        if (panelBarra != null)
            panelBarra.SetActive(false);

        if (panelEstufa != null)
            panelEstufa.SetActive(false);

        if (panelNevera != null)
            panelNevera.SetActive(false);

        if (panel != null)
            panel.SetActive(true);
    }

    private void Teletransportar(Transform destino)
    {
        if (cameraRig == null || destino == null)
        {
            Debug.LogError("Falta asignar el Camera Rig o el destino.");
            return;
        }

        Camera cabeza = cameraRig.GetComponentInChildren<Camera>();

        if (cabeza == null)
        {
            Debug.LogError("No se encontró una cámara dentro del Camera Rig.");
            return;
        }

        Vector3 desplazamientoCabeza =
            cabeza.transform.position - cameraRig.position;

        Vector3 nuevaPosicionRig =
            destino.position - desplazamientoCabeza;

        nuevaPosicionRig.y = cameraRig.position.y;
        cameraRig.position = nuevaPosicionRig;
    }
}
