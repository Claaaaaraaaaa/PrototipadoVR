
using UnityEngine;

public class NavegacionRecetario : MonoBehaviour
{
    [SerializeField] private GameObject pagina1;
    [SerializeField] private GameObject pagina2;

    private bool segundaPagina = false;

    private void Start()
    {
        MostrarPagina1();
    }

    public void SiguientePagina()
    {
        segundaPagina = !segundaPagina;

        pagina1.SetActive(!segundaPagina);
        pagina2.SetActive(segundaPagina);
    }

    private void MostrarPagina1()
    {
        segundaPagina = false;
        pagina1.SetActive(true);
        pagina2.SetActive(false);
    }
}
