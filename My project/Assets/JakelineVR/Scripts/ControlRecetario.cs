using UnityEngine;
using Oculus.Interaction;

public class ControlRecetario : MonoBehaviour
{
    [Header("Libro en la barra (el que se agarra)")]
    [Tooltip("El Grabbable del libro que esta en la barra")]
    public Grabbable libroBarra;

    [Tooltip("Si esta activo, el recetario se cierra al soltar el libro. " +
             "Si no, sigue abierto hasta pulsar 'Cerrar'.")]
    public bool cerrarAlSoltar = false;

    [Header("Recetario abierto")]
    [Tooltip("AnclaLibro (el libro abierto con el canvas Recetario)")]
    public GameObject libroAbierto;

    [Header("Ocultar mientras el recetario esta abierto")]
    [Tooltip("Arrastra aqui ZonasPaneles (y cualquier otro panel que estorbe)")]
    public GameObject[] ocultarMientrasAbierto;

    [Header("Debug")]
    public bool mostrarDebug = false;

    bool abierto;
    bool[] estadoPrevio;
    int agarresActivos;

    public bool Abierto => abierto;

    void Awake()
    {
        if (libroAbierto != null)
            libroAbierto.SetActive(false);
    }

    void OnEnable()
    {
        if (libroBarra != null)
            libroBarra.WhenPointerEventRaised += AlEventoLibro;
        else
            Debug.LogWarning("[Recetario] Falta asignar 'libroBarra'.", this);
    }

    void OnDisable()
    {
        if (libroBarra != null)
            libroBarra.WhenPointerEventRaised -= AlEventoLibro;
        agarresActivos = 0;
    }

    void AlEventoLibro(PointerEvent e)
    {
        switch (e.Type)
        {
            case PointerEventType.Select:
                agarresActivos++;
                Abrir();
                break;

            case PointerEventType.Unselect:
                agarresActivos = Mathf.Max(0, agarresActivos - 1);
                if (agarresActivos == 0 && cerrarAlSoltar) Cerrar();
                break;

            case PointerEventType.Cancel:
                agarresActivos = 0;
                if (cerrarAlSoltar) Cerrar();
                break;
        }
    }

    public void Abrir()
    {
        if (abierto) return;
        abierto = true;

        estadoPrevio = new bool[ocultarMientrasAbierto.Length];
        for (int i = 0; i < ocultarMientrasAbierto.Length; i++)
        {
            GameObject go = ocultarMientrasAbierto[i];
            if (go == null) continue;
            estadoPrevio[i] = go.activeSelf;
            go.SetActive(false);
        }

        if (libroAbierto != null)
            libroAbierto.SetActive(true); // NavegacionRecetario vuelve a la pagina 1 solo

        if (mostrarDebug) Debug.Log("[Recetario] Abierto", this);
    }

    // Asignar al boton "Cerrar"
    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;

        if (libroAbierto != null)
            libroAbierto.SetActive(false);

        for (int i = 0; i < ocultarMientrasAbierto.Length; i++)
        {
            GameObject go = ocultarMientrasAbierto[i];
            if (go != null && estadoPrevio != null)
                go.SetActive(estadoPrevio[i]);
        }

        if (mostrarDebug) Debug.Log("[Recetario] Cerrado", this);
    }

    public void Alternar()
    {
        if (abierto) Cerrar();
        else Abrir();
    }
}