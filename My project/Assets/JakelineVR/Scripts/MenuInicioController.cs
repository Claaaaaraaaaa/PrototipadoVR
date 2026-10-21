
using UnityEngine;

public class MenuInicioController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject menuInicio;
    [SerializeField] private GameObject panelZonas;

    public void ComenzarExperiencia()
    {
        if (menuInicio != null)
        {
            menuInicio.SetActive(false);
        }

        if (panelZonas != null)
        {
            panelZonas.SetActive(true);
        }
    }
}
