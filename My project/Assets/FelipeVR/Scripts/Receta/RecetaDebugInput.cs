using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SOLO PARA PRUEBAS. Permite avanzar los pasos de la receta sin hacer las acciones.
///   - Teclado (Editor / PC):  N = avanzar,  B = deshacer,  R = reiniciar,
///                             I = completar todo hasta la estufa (agua + ingredientes + leche)
///                             F = completar toda la receta (plato servido, listo para inspeccionar)
///                             P = completar todo y saltar a la pantalla final
///   - Control del Quest:      boton B/Y (OVRInput.Button.Two) = avanzar paso
///   - Inspector:              casilla "Completar Ingredientes Ahora" o menu (⋮)
/// Para quitarlo del juego final basta con borrar este componente.
/// </summary>
public class RecetaDebugInput : MonoBehaviour
{
    [SerializeField] private RecetaManager manager;

    [Header("Teclado")]
    [SerializeField] private Key teclaAvanzar = Key.N;
    [SerializeField] private Key teclaRetroceder = Key.B;
    [SerializeField] private Key teclaReiniciar = Key.R;
    [SerializeField] private Key teclaCompletarIngredientes = Key.I;
    [Tooltip("Completa toda la receta y sirve el plato: queda listo para agarrarlo e inspeccionarlo.")]
    [SerializeField] private Key teclaCompletarTodo = Key.F;
    [Tooltip("Completa toda la receta y salta directo a la pantalla final (creditos).")]
    [SerializeField] private Key teclaPantallaFinal = Key.P;

    [Header("Control VR")]
    [SerializeField] private bool usarControlVR = true;
    [SerializeField] private OVRInput.Button botonAvanzar = OVRInput.Button.Two;

    [Header("Atajos de prueba (marcar en Play)")]
    [Tooltip("Marca esta casilla en Play: completa agua, ingredientes y leche de coco. Se desmarca sola.")]
    [SerializeField] private bool completarIngredientesAhora = false;

    [Header("Seguridad")]
    [Tooltip("Si esta apagado, en un build que no sea de desarrollo estos atajos no hacen nada.")]
    [SerializeField] private bool permitirEnBuildFinal = false;

    private void Awake()
    {
        if (manager == null) manager = GetComponent<RecetaManager>();
        if (manager == null) manager = FindFirstObjectByType<RecetaManager>();
    }

    private void Update()
    {
        if (manager == null) return;

        // Casilla del Inspector
        if (completarIngredientesAhora)
        {
            completarIngredientesAhora = false;
            CompletarIngredientes();
        }

        if (!permitirEnBuildFinal && !Application.isEditor && !Debug.isDebugBuild) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado[teclaAvanzar].wasPressedThisFrame) manager.AvanzarPaso();
            if (teclado[teclaRetroceder].wasPressedThisFrame) manager.RetrocederPaso();
            if (teclado[teclaReiniciar].wasPressedThisFrame) manager.Reiniciar();
            if (teclado[teclaCompletarIngredientes].wasPressedThisFrame) CompletarIngredientes();
            if (teclado[teclaCompletarTodo].wasPressedThisFrame) CompletarTodo();
            if (teclado[teclaPantallaFinal].wasPressedThisFrame) IrAlFinal();
        }

        if (usarControlVR && OVRInput.GetDown(botonAvanzar))
            manager.AvanzarPaso();
    }

    /// <summary>
    /// PRUEBA: deja todo listo para encender la estufa
    /// (paso 1 agua, paso 2 picados, paso 3 enteros, paso 4 leche de coco).
    /// </summary>
    [ContextMenu("PRUEBA: Completar ingredientes (hasta la estufa)")]
    public void CompletarIngredientes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[RecetaDebug] Esto solo funciona con el juego en Play.", this);
            return;
        }

        if (manager == null) manager = FindFirstObjectByType<RecetaManager>();
        if (manager == null)
        {
            Debug.LogWarning("[RecetaDebug] No hay RecetaManager en la escena.", this);
            return;
        }

        manager.CompletarHasta(RecetaEventos.EstufaEncendida);

        // Que la olla tambien se vea con agua
        OllaEstado olla = FindFirstObjectByType<OllaEstado>();
        if (olla != null) olla.Llenar();

        Debug.Log("[RecetaDebug] PRUEBA: pasos completados hasta la estufa. Ya puedes encender el fogon.", this);
    }

    /// <summary>PRUEBA: completa todos los pasos y sirve el plato, listo para inspeccionarlo.</summary>
    [ContextMenu("PRUEBA: Completar toda la receta")]
    public void CompletarTodo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[RecetaDebug] Esto solo funciona con el juego en Play.", this);
            return;
        }

        if (manager == null) manager = FindFirstObjectByType<RecetaManager>();
        if (manager == null)
        {
            Debug.LogWarning("[RecetaDebug] No hay RecetaManager en la escena.", this);
            return;
        }

        // AvanzarPaso marca un paso o sub-paso por llamada; el tope evita un bucle infinito.
        for (int i = 0; i < 200 && !manager.RecetaCompleta; i++)
            manager.AvanzarPaso();

        OllaEstado olla = FindFirstObjectByType<OllaEstado>();
        if (olla != null) olla.Llenar();

        Plato plato = FindFirstObjectByType<Plato>();
        if (plato != null) plato.MostrarComida();

        Debug.Log("[RecetaDebug] PRUEBA: receta completa. Agarra el plato para inspeccionarlo.", this);
    }

    /// <summary>PRUEBA: completa la receta y muestra la pantalla final de una vez.</summary>
    [ContextMenu("PRUEBA: Ir a la pantalla final")]
    public void IrAlFinal()
    {
        CompletarTodo();
        if (!Application.isPlaying) return;

        PantallaFinal final = FindFirstObjectByType<PantallaFinal>();
        if (final != null) final.MostrarAhora();
        else Debug.LogWarning("[RecetaDebug] No hay PantallaFinal en la escena.", this);
    }
}