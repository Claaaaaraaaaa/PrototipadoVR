using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SOLO PARA PRUEBAS. Permite avanzar los pasos de la receta sin hacer las acciones.
///   - Teclado (Editor / PC):  N = avanzar paso,  B = deshacer paso,  R = reiniciar
///   - Control del Quest:      boton B/Y (OVRInput.Button.Two) = avanzar paso
///   - Boton de UI: conecta su OnClick a RecetaManager.AvanzarPaso
/// Para quitarlo del juego final basta con borrar este componente.
/// </summary>
public class RecetaDebugInput : MonoBehaviour
{
    [SerializeField] private RecetaManager manager;

    [Header("Teclado")]
    [SerializeField] private Key teclaAvanzar = Key.N;
    [SerializeField] private Key teclaRetroceder = Key.B;
    [SerializeField] private Key teclaReiniciar = Key.R;

    [Header("Control VR")]
    [SerializeField] private bool usarControlVR = true;
    [SerializeField] private OVRInput.Button botonAvanzar = OVRInput.Button.Two;

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
        if (!permitirEnBuildFinal && !Application.isEditor && !Debug.isDebugBuild) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado[teclaAvanzar].wasPressedThisFrame) manager.AvanzarPaso();
            if (teclado[teclaRetroceder].wasPressedThisFrame) manager.RetrocederPaso();
            if (teclado[teclaReiniciar].wasPressedThisFrame) manager.Reiniciar();
        }

        if (usarControlVR && OVRInput.GetDown(botonAvanzar))
            manager.AvanzarPaso();
    }
}
