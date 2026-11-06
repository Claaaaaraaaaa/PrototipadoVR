using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Va en el mismo objeto que un SnapInteractable. Cuando el usuario suelta un objeto
/// y este encaja en la zona, notifica un paso de la receta.
/// Los objetos que pueden encajar los decide el filtro de tags de la zona.
/// </summary>
public class SnapNotificarPaso : MonoBehaviour
{
    [Tooltip("Id del paso que se completa al encajar.")]
    [SerializeField] private string idAccion = RecetaEventos.PlatoEnMesa;
    [SerializeField] private bool mostrarDebug = true;

    private SnapInteractable zona;

    private void Awake()
    {
        zona = GetComponent<SnapInteractable>();
        if (zona == null)
            Debug.LogWarning("[SnapNotificarPaso] " + name + ": falta el SnapInteractable.", this);
    }

    private void OnEnable()
    {
        if (zona != null)
            zona.WhenSelectingInteractorViewAdded += AlEncajar;
    }

    private void OnDisable()
    {
        if (zona != null)
            zona.WhenSelectingInteractorViewAdded -= AlEncajar;
    }

    private void AlEncajar(IInteractorView interactor)
    {
        RecetaEventos.Notificar(idAccion);
        if (mostrarDebug) Debug.Log("[SnapNotificarPaso] " + name + ": notificado " + idAccion, this);
    }
}
