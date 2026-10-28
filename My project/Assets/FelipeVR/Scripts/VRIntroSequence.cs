using System.Collections;
using UnityEngine;

public class VRIntroSequence : MonoBehaviour
{
    [Header("Pantalla Negra (Fade Screen)")]
    [Tooltip("CanvasGroup del FadeCanvas ubicado en CenterEyeAnchor")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    [Header("Configuración del Logo")]
    [SerializeField] private GameObject logoPanel;
    [SerializeField] private CanvasGroup logoCanvasGroup;

    [Header("Tiempos de la Secuencia")]
    [Tooltip("Tiempo en oscuridad total al inicio (ej. 1.5s)")]
    [SerializeField] private float initialDarkDuration = 1.5f;

    [Tooltip("Aparición rápida del logo (0.5s)")]
    [SerializeField] private float logoFadeInDuration = 0.5f;

    [Tooltip("Tiempo observando el logo con el fondo negro (2 segundos)")]
    [SerializeField] private float logoHoldDuration = 2.0f;

    [Tooltip("Duración del desvanecimiento final")]
    [SerializeField] private float fadeOutDuration = 1.5f;

    private void Awake()
    {
        // 1. Ocultar el logo antes del primer renderizado
        if (logoPanel != null) logoPanel.SetActive(false);
        if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;

        // 2. Encender pantalla negra pegada al visor
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.gameObject.SetActive(true);
            blackScreenCanvasGroup.alpha = 1f;

            Transform t = blackScreenCanvasGroup.transform;
            t.localPosition = new Vector3(0f, 0f, 0.25f);
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(2f, 2f, 1f);
        }
    }

    private void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        // FASE 1: Oscuridad total estática
        yield return new WaitForSeconds(initialDarkDuration);

        // FASE 2: Aparición rápida del logo
        if (logoPanel != null)
        {
            logoPanel.SetActive(true);
            LazyFollowUI lazyFollow = logoPanel.GetComponent<LazyFollowUI>();
            if (lazyFollow != null) lazyFollow.SnapToCameraFront();
        }

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

        // FASE 3: Mantener logo visible en la oscuridad
        yield return new WaitForSeconds(logoHoldDuration);

        // FASE 4: Desvanecer fondo oscuro y logo al mismo tiempo
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);

            if (blackScreenCanvasGroup != null)
            {
                blackScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            yield return null;
        }

        // Estado final: Se deshabilitan las UIs y empieza el juego normalmente
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.gameObject.SetActive(false);
        }
        if (logoPanel != null) logoPanel.SetActive(false);
    }
}