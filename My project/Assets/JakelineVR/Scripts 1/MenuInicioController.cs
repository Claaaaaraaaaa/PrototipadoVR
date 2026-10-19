
using UnityEngine;

public class MenuInicioController : MonoBehaviour
{
    [SerializeField] private GameObject menuInicio;

    public void ComenzarExperiencia()
    {
        if (menuInicio != null)
        {
            menuInicio.SetActive(false);
        }
    }
}
