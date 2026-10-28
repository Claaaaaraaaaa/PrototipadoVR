using System.Collections;
using UnityEngine;

public class DoorTeleportController : MonoBehaviour
{
    [Header("Configuración de la Puerta")]
    [Tooltip("El pivote o cuerpo de la puerta que va a girar")]
    [SerializeField] private Transform doorTransform;

    [Tooltip("Ángulo de apertura en Y (por defecto 90 grados)")]
    [SerializeField] private float openAngleY = 90f;

    [Tooltip("Duración de la animación de apertura de la puerta")]
    [SerializeField] private float doorOpenDuration = 1.2f;

    [Header("Transición Radial y Teletransporte")]
    [Tooltip("El RectTransform del elemento UI circular usado para el cierre radial")]
    [SerializeField] private RectTransform radialCircleUI;

    [Tooltip("El objeto [BuildingBlock] Camera Rig o XR Origin")]
    [SerializeField] private Transform xrOrigin;

    [Tooltip("Coordenadas destino según la imagen: X=26, Y=1, Z=163")]
    [SerializeField] private Vector3 targetPosition = new Vector3(26f, 1f, 163f);

    [Header("Tiempos Transición Radial")]
    [SerializeField] private float wipeDuration = 0.8f;
    [SerializeField] private float maxCircleScale = 15f; // Escala grande que deja ver toda la pantalla

    private bool isInteracting = false;
    private Quaternion initialRotation;

    private void Start()
    {
        if (doorTransform == null) doorTransform = transform;
        initialRotation = doorTransform.localRotation;

        // Asegurar que la máscara radial empiece abierta (invisible)
        if (radialCircleUI != null)
        {
            radialCircleUI.localScale = Vector3.one * maxCircleScale;
            radialCircleUI.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Conecta esta función al evento UnityEvent de tu Poke Interactable (onSelect / onTrigger)
    /// </summary>
    public void OpenDoorAndTeleport()
    {
        if (isInteracting) return;
        StartCoroutine(DoorSequenceRoutine());
    }

    private IEnumerator DoorSequenceRoutine()
    {
        isInteracting = true;

        // 1. ANIMACIÓN DE PUERTA CON BOUNCE OUT
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0f, openAngleY, 0f);
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);

            // Aplicar Easing Bounce Out
            float bounceT = EaseOutBounce(t);
            doorTransform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, bounceT);

            yield return null;
        }
        doorTransform.localRotation = targetRotation;

        // 2. CERRAR TRANSICIÓN RADIAL (Iris Wipe In)
        if (radialCircleUI != null)
        {
            radialCircleUI.gameObject.SetActive(true);
            elapsed = 0f;

            while (elapsed < wipeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / wipeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Escala de grande (pantalla visible) a 0 (pantalla negra)
                radialCircleUI.localScale = Vector3.one * Mathf.Lerp(maxCircleScale, 0f, smoothT);
                yield return null;
            }
            radialCircleUI.localScale = Vector3.zero;
        }

        // 3. TELETRANSPORTE A LA POSICIÓN (26, 1, 163)
        if (xrOrigin != null)
        {
            xrOrigin.position = targetPosition;
        }

        yield return new WaitForSeconds(0.2f); // Pausa breve en negro

        // 4. ABRIR TRANSICIÓN RADIAL (Iris Wipe Out)
        if (radialCircleUI != null)
        {
            elapsed = 0f;

            while (elapsed < wipeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / wipeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Escala de 0 (pantalla negra) a grande (pantalla visible)
                radialCircleUI.localScale = Vector3.one * Mathf.Lerp(0f, maxCircleScale, smoothT);
                yield return null;
            }

            radialCircleUI.localScale = Vector3.one * maxCircleScale;
            radialCircleUI.gameObject.SetActive(false);
        }

        isInteracting = false;
    }

    // Fórmula matemática estándar de Easing Bounce Out
    private float EaseOutBounce(float x)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (x < 1f / d1)
        {
            return n1 * x * x;
        }
        else if (x < 2f / d1)
        {
            return n1 * (x -= 1.5f / d1) * x + 0.75f;
        }
        else if (x < 2.625f / d1)
        {
            return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        }
        else
        {
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }
    }
}