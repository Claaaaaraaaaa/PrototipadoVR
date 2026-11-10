using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de cierre: cuando el plato vuelve a la mesa despues de inspeccionarlo, funde a negro,
/// oculta la cocina (solo quedan el rig del jugador y esta pantalla), deja sonando solo la musica
/// caribena y muestra los creditos con un boton para reiniciar la experiencia desde el inicio.
/// </summary>
public class PantallaFinal : MonoBehaviour
{
    [Tooltip("InspeccionPlato del plato servido. La pantalla aparece cuando termina su inspeccion.")]
    [SerializeField] private InspeccionPlato plato;
    [Tooltip("Cabeza del usuario (CenterEyeAnchor).")]
    [SerializeField] private Transform camara;
    [Tooltip("CanvasGroup negro pegado a la camara (el FadeCanvas de la intro).")]
    [SerializeField] private CanvasGroup fundido;
    [Tooltip("Creditos y boton. Empieza oculto.")]
    [SerializeField] private GameObject contenido;
    [Tooltip("Musica caribena del ambiente. Es lo unico que sigue sonando.")]
    [SerializeField] private AudioSource musica;

    [Header("Tiempos y posicion")]
    [SerializeField] private float volumenMusica = 0.6f;
    [Tooltip("Segundos entre que el plato vuelve a la mesa y empieza el fundido.")]
    [SerializeField] private float esperaAntes = 1.5f;
    [SerializeField] private float duracionFundido = 1.2f;
    [Tooltip("Metros frente a los ojos a los que aparecen los creditos (al alcance de la mano).")]
    [SerializeField] private float distancia = 0.55f;
    [Tooltip("Metros respecto a la altura de los ojos (negativo = mas abajo).")]
    [SerializeField] private float desnivel = -0.05f;
    [Tooltip("Color del vacio alrededor de la pantalla negra de creditos.")]
    [SerializeField] private Color colorFondo = new Color(0.12f, 0.12f, 0.12f, 1f);

    private bool mostrada;
    private bool reiniciando;

    private void Awake()
    {
        if (contenido != null) contenido.SetActive(false);
        // El negro del FadeCanvas usa el material UI/FundidoEncima (asignado en la escena), que tapa
        // todo sin importar la distancia; el logo y los creditos van encima por sorting order (101 > 100).
    }

    private void Start()
    {
        if (plato != null) plato.InspeccionTerminada += AlTerminarInspeccion;
        else Debug.LogWarning("[PantallaFinal] Falta asignar el plato.", this);
    }

    private void OnDestroy()
    {
        if (plato != null) plato.InspeccionTerminada -= AlTerminarInspeccion;
        AudioListener.pause = false;
    }

    private void AlTerminarInspeccion()
    {
        if (mostrada) return;
        mostrada = true;
        StartCoroutine(Mostrar());
    }

    /// <summary>PRUEBA: muestra la pantalla final sin esperar (menu del componente o tecla P de RecetaDebugInput).</summary>
    [ContextMenu("Mostrar ahora (prueba)")]
    public void MostrarAhora()
    {
        if (!Application.isPlaying || mostrada) return;
        mostrada = true;
        esperaAntes = 0f;
        StartCoroutine(Mostrar());
    }

    private IEnumerator Mostrar()
    {
        if (esperaAntes > 0f) yield return new WaitForSeconds(esperaAntes);

        yield return Fundir(0f, 1f, t =>
        {
            AudioListener.volume = 1f - t;
        });

        Transform ojos = Ojos();
        OcultarEscena(ojos);

        // Todo lo demas queda en pausa; la musica la ignora y sigue.
        AudioListener.pause = true;
        AudioListener.volume = 1f;
        if (musica != null)
        {
            musica.ignoreListenerPause = true;
            musica.volume = 0f;
            if (!musica.isPlaying) musica.Play();
        }

        if (contenido != null)
        {
            if (ojos != null)
            {
                Vector3 frente = Vector3.ProjectOnPlane(ojos.forward, Vector3.up);
                if (frente.sqrMagnitude < 0.001f) frente = Vector3.ProjectOnPlane(ojos.up, Vector3.up);
                frente.Normalize();
                contenido.transform.SetPositionAndRotation(
                    ojos.position + frente * distancia + Vector3.up * desnivel,
                    Quaternion.LookRotation(frente));
            }
            contenido.SetActive(true);
        }

        yield return Fundir(1f, 0f, t =>
        {
            if (musica != null) musica.volume = Mathf.Lerp(0f, volumenMusica, 1f - t);
        });
        if (fundido != null) fundido.gameObject.SetActive(false);
    }

    /// <summary>Lo llama el boton "Reiniciar": vuelve a cargar la escena, con su intro.</summary>
    public void Reiniciar()
    {
        if (reiniciando) return;
        reiniciando = true;
        StartCoroutine(RecargarEscena());
    }

    private IEnumerator RecargarEscena()
    {
        float volumen = musica != null ? musica.volume : 0f;
        yield return Fundir(0f, 1f, t =>
        {
            if (musica != null) musica.volume = volumen * (1f - t);
        });

        AudioListener.pause = false;
        AudioListener.volume = 1f;

        Scene escena = SceneManager.GetActiveScene();
        if (escena.buildIndex >= 0)
        {
            SceneManager.LoadScene(escena.buildIndex);
            yield break;
        }
#if UNITY_EDITOR
        // La escena no esta en Build Settings: en el editor se puede recargar igual.
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            escena.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("[PantallaFinal] Agrega " + escena.name + " a Build Settings para poder reiniciar.", this);
#endif
    }

    // Anima el alpha del fundido; el callback recibe ese alpha (0..1) en cada frame.
    private IEnumerator Fundir(float desde, float hasta, System.Action<float> cadaFrame)
    {
        if (fundido != null)
        {
            fundido.gameObject.SetActive(true);
            fundido.alpha = desde;
        }

        float duracion = Mathf.Max(0.01f, duracionFundido);
        for (float tiempo = 0f; tiempo < duracion; tiempo += Time.deltaTime)
        {
            float a = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, tiempo / duracion));
            if (fundido != null) fundido.alpha = a;
            cadaFrame?.Invoke(a);
            yield return null;
        }

        if (fundido != null) fundido.alpha = hasta;
        cadaFrame?.Invoke(hasta);
    }

    // Apaga todo lo que se ve menos el rig del jugador (manos incluidas) y esta pantalla.
    private void OcultarEscena(Transform ojos)
    {
        Transform rig = ojos != null ? ojos.root : null;

        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (!Conservar(r.transform, rig)) r.enabled = false;

        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (!Conservar(c.transform, rig)) c.enabled = false;

        Camera cam = ojos != null ? ojos.GetComponent<Camera>() : Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = colorFondo;
        }
        RenderSettings.fog = false;
    }

    private bool Conservar(Transform t, Transform rig)
    {
        return t.IsChildOf(transform) || (rig != null && t.IsChildOf(rig));
    }

    private Transform Ojos()
    {
        if (camara != null) return camara;
        return Camera.main != null ? Camera.main.transform : null;
    }
}
