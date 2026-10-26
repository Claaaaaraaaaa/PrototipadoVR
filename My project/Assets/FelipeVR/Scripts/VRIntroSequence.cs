using System.Collections;
using UnityEngine;

public class VRIntroSequence : MonoBehaviour
{
    [Header("Referencias XR")]
    [Tooltip("El objeto [BuildingBlock] Camera Rig")]
    [SerializeField] private Transform xrOrigin;

    [Header("Pantalla Negra (Fade Screen)")]
    [Tooltip("CanvasGroup del FadeCanvas ubicado en CenterEyeAnchor")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    [Tooltip("Distancia súper cercana para el arranque sin atravesar el suelo")]
    [SerializeField] private float closeDistance = 0.15f;

    [Tooltip("Distancia normal del panel detrás del logo durante la animación")]
    [SerializeField] private float normalDistance = 1.0f;

    [Header("Configuración del Logo")]
    [SerializeField] private GameObject logoPanel;
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private float logoFadeInDuration = 1.0f;
    [SerializeField] private float logoHoldDuration = 0.5f;

    [Header("Configuración de Descenso")]
    [SerializeField] private float startHeight = 5.0f;
    [SerializeField] private float descentDuration = 6.0f;

    private Vector3 groundPosition;
    private Vector3 elevatedPosition;

    private void Awake()
    {
        // 1. Iniciar en total oscuridad
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 1f;
            blackScreenCanvasGroup.gameObject.SetActive(true);

            // Pegar la pantalla negra pegada a los ojos (Z = 0.15m) para evitar que toque el suelo
            Vector3 localPos = blackScreenCanvasGroup.transform.localPosition;
            blackScreenCanvasGroup.transform.localPosition = new Vector3(localPos.x, localPos.y, closeDistance);
        }

        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        // 2. Espera técnica en oscuridad pegada a la cara para que el SDK de Meta estabilice el tracking
        yield return new WaitForSeconds(0.4f);

        // Elevar la cámara en penumbra
        if (xrOrigin != null)
        {
            groundPosition = xrOrigin.position;
            elevatedPosition = groundPosition + Vector3.up * startHeight;
            xrOrigin.position = elevatedPosition;
        }

        // 3. Ya en la altura, alejar la pantalla negra a su distancia normal (Z = 1.0m) detrás del logo
        if (blackScreenCanvasGroup != null)
        {
            Vector3 localPos = blackScreenCanvasGroup.transform.localPosition;
            blackScreenCanvasGroup.transform.localPosition = new Vector3(localPos.x, localPos.y, normalDistance);
        }

        if (logoPanel != null) logoPanel.SetActive(true);

        // 4. Aparecer el logo suavemente frente a la pantalla negra
        float elapsed = 0f;
        while (elapsed < logoFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / logoFadeInDuration);
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            }
            yield return null;
        }
        if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f;

        // Pausa breve para observar el logo
        if (logoHoldDuration > 0)
        {
            yield return new WaitForSeconds(logoHoldDuration);
        }

        // 5. Descenso de la cámara + Desvanecimiento en paralelo del mapa y del logo
        elapsed = 0f;
        while (elapsed < descentDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / descentDuration);

            // Transición S doble para evitar vértigo
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            smoothT = Mathf.SmoothStep(0f, 1f, smoothT);

            // Bajar la cámara paulatinamente
            if (xrOrigin != null)
            {
                xrOrigin.position = Vector3.Lerp(elevatedPosition, groundPosition, smoothT);
            }

            // Desvanecer el fondo negro revelando el escenario
            if (blackScreenCanvasGroup != null)
            {
                blackScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            }

            // Desvanecer el logo al mismo tiempo
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            }

            yield return null;
        }

        // 6. Finalización en el suelo
        if (xrOrigin != null) xrOrigin.position = groundPosition;

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.gameObject.SetActive(false);
        }

        if (logoPanel != null)
        {
            logoPanel.SetActive(false);
        }
    }
}