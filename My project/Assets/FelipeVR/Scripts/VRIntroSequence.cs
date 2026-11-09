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
    [Tooltip("Fase 1: Tiempo en oscuridad total inicial (2 segundos)")]
    [SerializeField] private float initialDarkDuration = 2.0f;

    [Tooltip("Fase 2: Aparición desvanecida del logo")]
    [SerializeField] private float logoFadeInDuration = 0.8f;

    [Tooltip("Fase 3: Tiempo contemplando el logo sobre fondo negro (1.5 segundos)")]
    [SerializeField] private float logoHoldDuration = 1.5f;

    [Tooltip("Fase 4: Desvanecimiento de la oscuridad para revelar el mapa")]
    [SerializeField] private float darkFadeOutDuration = 1.5f;

    [Tooltip("Fase 5: Desvanecimiento final del logo")]
    [SerializeField] private float logoFadeOutDuration = 1.0f;

    private void Awake()
    {
        // 1. Ocultar el logo desde el fotograma 0
        if (logoPanel != null) logoPanel.SetActive(false);
        if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;

        // 2. Colocar la pantalla negra a 1.0m de la cámara (DETRÁS del logo que está a 0.4m)
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.gameObject.SetActive(true);
            blackScreenCanvasGroup.alpha = 1f;

            Transform t = blackScreenCanvasGroup.transform;
            t.localPosition = new Vector3(0f, 0f, 1.0f); // Z = 1.0m para no tapar el logo
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(3f, 3f, 1f); // Escala amplia para cubrir todo el FOV
        }
    }

    private void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }

    // Tiempo maximo que cuenta un frame. Al cargar la escena (sobre todo al reiniciar desde la
    // pantalla final) los primeros frames se congelan y, sin este tope, se "comian" la intro entera.
    private const float MaxDeltaPorFrame = 1f / 30f;

    private static float Delta()
    {
        return Mathf.Min(Time.deltaTime, MaxDeltaPorFrame);
    }

    private static IEnumerator Esperar(float segundos)
    {
        for (float t = 0f; t < segundos; t += Delta())
            yield return null;
    }

    // Espera a que la carga termine: 10 frames seguidos fluidos, o 6 segundos como maximo.
    private static IEnumerator EsperarFramesEstables()
    {
        float limite = Time.realtimeSinceStartup + 6f;
        int estables = 0;
        while (estables < 10 && Time.realtimeSinceStartup < limite)
        {
            estables = Time.unscaledDeltaTime < 0.05f ? estables + 1 : 0;
            yield return null;
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        yield return EsperarFramesEstables();

        // FASE 1: Oscuridad total durante 2 segundos (Escenario oculto, Logo oculto)
        yield return Esperar(initialDarkDuration);

        // FASE 2: Aparece el logo con animación desvanecida sobre el fondo negro
        if (logoPanel != null)
        {
            logoPanel.SetActive(true);
            LazyFollowUI lazyFollow = logoPanel.GetComponent<LazyFollowUI>();
            if (lazyFollow != null) lazyFollow.SnapToCameraFront();
        }

        float elapsed = 0f;
        while (elapsed < logoFadeInDuration)
        {
            elapsed += Delta();
            float t = Mathf.Clamp01(elapsed / logoFadeInDuration);
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            }
            yield return null;
        }
        if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f;

        // FASE 3: Sostener el logo visible sobre el fondo negro
        yield return Esperar(logoHoldDuration);

        // FASE 4: Se desvanece el fondo oscuro (revela el mapa de fondo mientras el logo sigue visible)
        elapsed = 0f;
        while (elapsed < darkFadeOutDuration)
        {
            elapsed += Delta();
            float t = Mathf.Clamp01(elapsed / darkFadeOutDuration);
            if (blackScreenCanvasGroup != null)
            {
                blackScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            yield return null;
        }
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.gameObject.SetActive(false);
        }

        // FASE 5: Se desvanece el logo finalizando la introducción
        elapsed = 0f;
        while (elapsed < logoFadeOutDuration)
        {
            elapsed += Delta();
            float t = Mathf.Clamp01(elapsed / logoFadeOutDuration);
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            yield return null;
        }

        if (logoPanel != null) logoPanel.SetActive(false);
    }
}