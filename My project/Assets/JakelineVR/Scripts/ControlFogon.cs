using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fogon de la estufa. Solo enciende si la olla esta encima y todos los
/// ingredientes ya estan en la olla (pasos anteriores de la receta completos).
/// Al encender cocina unos segundos y la olla pasa a "rondon".
/// </summary>
public class ControlFogon : MonoBehaviour
{
    [SerializeField] private GameObject efectoFogon;

    [Header("Olla en la estufa")]
    [Tooltip("El ZonaReceta del detector que esta sobre esta estufa (con Accion = Ninguna)")]
    [SerializeField] private ZonaReceta zonaEstufa;

    [Tooltip("Exigir que todos los pasos anteriores de la receta esten completos")]
    [SerializeField] private bool exigirIngredientes = true;

    [Tooltip("Encender solo al poner la olla (si esta lista), sin tocar la perilla")]
    [SerializeField] private bool encenderAlColocarOlla = false;

    [Header("Coccion")]
    [Tooltip("Segundos que tiene que estar la olla en el fuego para hacerse rondon")]
    [SerializeField] private float segundosCoccion = 10f;

    [Header("Eventos (opcional: sonidos, mensajes...)")]
    public UnityEvent alEncender;
    public UnityEvent alApagar;
    [Tooltip("Se dispara al intentar encender sin olla o con ingredientes pendientes")]
    public UnityEvent alNoPoderEncender;
    public UnityEvent alTerminarCoccion;

    [SerializeField] private bool mostrarDebug = true;

    private bool encendido = false;
    private float tiempoCoccion = 0f;
    private bool cocinado = false;
    private bool hirviendo = false;

    public bool Encendido => encendido;

    private void Awake()
    {
        if (efectoFogon != null)
            efectoFogon.SetActive(false);

        if (zonaEstufa == null)
            Debug.LogWarning("[Fogon] Falta asignar 'Zona Estufa'.", this);
    }

    private void OnEnable()
    {
        if (zonaEstufa != null)
            zonaEstufa.alColocar.AddListener(AlColocarOlla);
    }

    private void OnDisable()
    {
        if (zonaEstufa != null)
            zonaEstufa.alColocar.RemoveListener(AlColocarOlla);

        FijarHervor(false, transform.position);
    }

    // ------------------------------------------------------------------
    // Perilla (lo llama ToquePerilla o un boton)
    // ------------------------------------------------------------------

    public void AlternarFogon()
    {
        if (encendido) Apagar();
        else IntentarEncender();
    }

    public bool IntentarEncender()
    {
        if (encendido) return true;

        OllaEstado olla = zonaEstufa != null ? zonaEstufa.OllaDentro : null;

        if (olla == null)
        {
            NoPuede("No hay olla sobre la estufa.");
            return false;
        }

        if (exigirIngredientes && !IngredientesListos(out string falta))
        {
            NoPuede("Todavia falta: " + falta);
            return false;
        }

        encendido = true;
        if (efectoFogon != null) efectoFogon.SetActive(true);

        SonidosCocina.Sonar(Sonido.PrenderEstufa, transform.position);
        alEncender.Invoke();
        RecetaEventos.Notificar(RecetaEventos.EstufaEncendida);

        if (mostrarDebug) Debug.Log("[Fogon] Encendido. Cocinando...", this);
        return true;
    }

    public void Apagar()
    {
        if (!encendido) return;

        encendido = false;
        if (efectoFogon != null) efectoFogon.SetActive(false);

        alApagar.Invoke();
        if (mostrarDebug) Debug.Log("[Fogon] Apagado.", this);
    }

    // ------------------------------------------------------------------
    // Olla colocada / coccion
    // ------------------------------------------------------------------

    private void AlColocarOlla()
    {
        if (encenderAlColocarOlla && !encendido)
            IntentarEncender();
    }

    private void Update()
    {
        // El agua suena mientras hay fuego, olla encima y todavia no es rondon.
        OllaEstado ollaEncima = zonaEstufa != null ? zonaEstufa.OllaDentro : null;
        FijarHervor(encendido && ollaEncima != null && ollaEncima.EstadoActual != OllaEstado.Estado.Rondon,
            ollaEncima != null ? ollaEncima.transform.position : transform.position);

        if (!encendido || cocinado || zonaEstufa == null) return;

        OllaEstado olla = zonaEstufa.OllaDentro;
        if (olla == null) return; // si quitan la olla, la coccion se pausa

        tiempoCoccion += Time.deltaTime;
        if (tiempoCoccion < segundosCoccion) return;

        cocinado = true;
        olla.HacerRondon();
        alTerminarCoccion.Invoke();

        if (mostrarDebug) Debug.Log("[Fogon] Coccion terminada: rondon listo.", this);
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------

    private void FijarHervor(bool valor, Vector3 posicion)
    {
        if (valor == hirviendo) return;
        hirviendo = valor;
        SonidosCocina.AguaHirviendo(valor, posicion);
    }

    private bool IngredientesListos(out string falta)
    {
        falta = "";
        RecetaManager manager = RecetaManager.Instancia != null
            ? RecetaManager.Instancia
            : FindFirstObjectByType<RecetaManager>();

        if (manager == null)
        {
            Debug.LogWarning("[Fogon] No hay RecetaManager: enciendo sin comprobar.", this);
            return true;
        }

        if (manager.PasosPreviosCompletos(RecetaEventos.EstufaEncendida))
            return true;

        falta = manager.TextoPasoActual();
        return false;
    }

    private void NoPuede(string motivo)
    {
        alNoPoderEncender.Invoke();
        if (mostrarDebug) Debug.Log("[Fogon] No enciende. " + motivo, this);
    }
}