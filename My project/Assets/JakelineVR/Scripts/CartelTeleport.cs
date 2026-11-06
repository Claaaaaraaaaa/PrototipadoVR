using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;

/// <summary>
/// Cuadro que senala un hotspot de teleport. Mira al usuario girando solo en el eje Y,
/// y se elimina cuando el usuario se teletransporta a ese hotspot.
/// </summary>
public class CartelTeleport : MonoBehaviour
{
    [Tooltip("El Teleport Interactable del hotspot que senala este cuadro.")]
    [SerializeField] private TeleportInteractable hotspot;
    [Tooltip("Cabeza del usuario (CenterEyeAnchor).")]
    [SerializeField] private Transform camara;

    private void OnEnable()
    {
        if (hotspot != null)
            hotspot.WhenSelectingInteractorViewAdded += AlTeletransportar;
        else
            Debug.LogWarning("[CartelTeleport] " + name + ": falta el hotspot.", this);
    }

    private void OnDisable()
    {
        if (hotspot != null)
            hotspot.WhenSelectingInteractorViewAdded -= AlTeletransportar;
    }

    private void AlTeletransportar(IInteractorView interactor)
    {
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (camara == null) return;

        Vector3 direccion = transform.position - camara.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direccion);
    }
}
