using UnityEngine;

public class NavegacionRecetario : MonoBehaviour
{
    [SerializeField] private GameObject pagina1;
    [SerializeField] private GameObject pagina2;

    [Tooltip("Tiempo minimo entre pulsaciones, para que un dedo tembloroso no pase dos paginas")]
    [SerializeField] private float tiempoEntrePulsaciones = 0.5f;

    private bool segundaPagina = false;
    private float ultimaPulsacion = -99f;

    // OnEnable en vez de Start: se ejecuta cada vez que se abre el libro
    private void OnEnable()
    {
        MostrarPagina1();
    }

    public void SiguientePagina()
    {
        if (Time.time - ultimaPulsacion < tiempoEntrePulsaciones)
            return;
        ultimaPulsacion = Time.time;

        segundaPagina = !segundaPagina;

        pagina1.SetActive(!segundaPagina);
        pagina2.SetActive(segundaPagina);

        SonidosCocina.PasarPagina(transform.position);
    }

    public void MostrarPagina1()
    {
        segundaPagina = false;
        pagina1.SetActive(true);
        pagina2.SetActive(false);
    }
}