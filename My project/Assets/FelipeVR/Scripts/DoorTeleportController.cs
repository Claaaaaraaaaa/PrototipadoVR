using System.Collections;
using UnityEngine;

public class DoorTeleportController : MonoBehaviour
{
    [Header("Configuración de la Puerta")]
    [Tooltip("El pivote o cuerpo de la puerta que va a girar")]
    [SerializeField] private Transform doorTransform;

    [Tooltip("Ángulo de rotación objetivo en grados (X, Y, Z)")]
    [SerializeField] private Vector3 openRotationAngle = new Vector3(90f, 0f, 0f);

    [Tooltip("Duración de la animación de apertura con rebote")]
    [SerializeField] private float doorOpenDuration = 1.2f;

    [Header("Transición Radial (Iris Wipe)")]
    [Tooltip("RectTransform del objeto UI con la textura del agujero")]
    [SerializeField] private RectTransform radialCircleUI;

    [Tooltip("CanvasGroup del FadeCanvas / FadePanel negro")]
    [SerializeField] private CanvasGroup fadePanelCanvasGroup;

    [Header("Escalas del Agujero Radial")]
    [Tooltip("Escala donde el agujero es gigante y deja ver la pantalla completa")]
    [SerializeField] private float maxCircleScale = 15f;

    [Tooltip("Escala donde el agujero se cierra (NUNCA 0 para evitar que desaparezca la textura)")]
    [SerializeField] private float minCircleScale = 0.05f;

    [Header("Teletransporte y Tiempos")]
    [Tooltip("El objeto [BuildingBlock] Camera Rig o XR Origin")]
    [SerializeField] private Transform xrOrigin;

    [Tooltip("Coordenadas destino (X: 26, Y: 1, Z: 163)")]
    [SerializeField] private Vector3 targetPosition = new Vector3(26f, 1f, 163f);

    [Tooltip("Duración del encogimiento y crecimiento del agujero")]
    [SerializeField] private float wipeDuration = 1.2f;

    [Tooltip("Tiempo de delay en oscuridad total antes de abrir la vista")]
    [SerializeField] private float blackScreenDelay = 1.0f;

    private bool isInteracting = false;
    private Quaternion initialRotation;

    private void Start()
    {
        if (doorTransform == null) doorTransform = transform;
        initialRotation = doorTransform.localRotation;

        // Asegurar que la máscara radial inicie desactivada
        if (radialCircleUI != null)
        {
            radialCircleUI.localScale = Vector3.one * maxCircleScale;
            radialCircleUI.gameObject.SetActive(false);
        }

        // NOTA: No modificamos fadePanelCanvasGroup aquí para no interrumpir la intro inicial.
    }

    public void OpenDoorAndTeleport()
    {
        if (isInteracting) return;
        StartCoroutine(DoorSequenceRoutine());
    }

    private IEnumerator DoorSequenceRoutine()
    {
        isInteracting = true;

        // 1. ANIMACIÓN DE LA PUERTA (BOUNCE OUT)
        Quaternion targetRotation = initialRotation * Quaternion.Euler(openRotationAngle);
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            float bounceT = EaseOutBounce(t);

            doorTransform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, bounceT);
            yield return null;
        }
        doorTransform.localRotation = targetRotation;

        // 2. ENCOGIMIENTO DE LA MÁSCARA RADIAL (MaxScale -> MinScale)
        if (radialCircleUI != null)
        {
            radialCircleUI.gameObject.SetActive(true);
            elapsed = 0f;

            while (elapsed < wipeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / wipeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                radialCircleUI.localScale = Vector3.one * Mathf.Lerp(maxCircleScale, minCircleScale, smoothT);
                yield return null;
            }
            radialCircleUI.localScale = Vector3.one * minCircleScale;
        }

        // 3. ENCENDER FADE PANEL AL FINALIZAR EL ENCOGIMIENTO
        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.gameObject.SetActive(true);
            fadePanelCanvasGroup.alpha = 1f;
        }

        // 4. TELETRANSPORTE
        if (xrOrigin != null)
        {
            xrOrigin.position = targetPosition;
        }

        // 5. PAUSA DE DELAY EN OSCURIDAD
        yield return new WaitForSeconds(blackScreenDelay);

        // 6. APAGAR EL FADE PANEL ANTES DE REABRIR LA VISTA
        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.alpha = 0f;
            fadePanelCanvasGroup.gameObject.SetActive(false);
        }

        // 7. CRECIMIENTO DE LA MÁSCARA RADIAL (MinScale -> MaxScale)
        if (radialCircleUI != null)
        {
            elapsed = 0f;

            while (elapsed < wipeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / wipeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                radialCircleUI.localScale = Vector3.one * Mathf.Lerp(minCircleScale, maxCircleScale, smoothT);
                yield return null;
            }

            radialCircleUI.localScale = Vector3.one * maxCircleScale;
            radialCircleUI.gameObject.SetActive(false);
        }

        isInteracting = false;
    }

    private float EaseOutBounce(float x)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (x < 1f / d1) return n1 * x * x;
        if (x < 2f / d1) return n1 * (x -= 1.5f / d1) * x + 0.75f;
        if (x < 2.625f / d1) return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        return n1 * (x -= 2.625f / d1) * x + 0.984375f;
    }
}