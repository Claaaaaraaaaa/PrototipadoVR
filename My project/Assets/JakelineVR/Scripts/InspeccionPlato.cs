using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Va en la raiz del plato, junto al Grabbable. Cuando la receta esta completa, agarrar el plato
/// lo lleva a flotar frente al usuario, donde gira sobre su propio centro siguiendo la mano.
/// Al soltarlo espera unos segundos (para poder agarrarlo de nuevo y seguir girando) y vuelve a su sitio.
/// Antes de completar la receta, el plato se agarra de forma normal.
/// </summary>
public class InspeccionPlato : MonoBehaviour, ITransformer
{
    private enum Estado { Reposo, Inspeccionando, Regresando }

    [Tooltip("Cabeza del usuario (CenterEyeAnchor).")]
    [SerializeField] private Transform camara;
    [Tooltip("Metros frente a los ojos a los que flota el plato.")]
    [SerializeField] private float distancia = 0.45f;
    [Tooltip("Metros respecto a la altura de los ojos (negativo = mas abajo).")]
    [SerializeField] private float desnivel = -0.1f;
    [Tooltip("Cuanto gira el plato por cada grado que gira la mano.")]
    [SerializeField] private float sensibilidad = 1.5f;
    [Tooltip("Segundos que tarda en ir y volver.")]
    [SerializeField] private float duracionViaje = 0.5f;
    [Tooltip("Segundos que sigue flotando despues de soltarlo.")]
    [SerializeField] private float segundosAntesDeVolver = 4f;

    [Header("Panel informativo")]
    [Tooltip("Canvas con la historia del plato. Se muestra al lado mientras se inspecciona.")]
    [SerializeField] private GameObject panelInfo;
    [Tooltip("Metros a la derecha del plato (desde el punto de vista del usuario).")]
    [SerializeField] private float panelLado = 0.3f;
    [Tooltip("Metros mas lejos que el plato, para que no lo atraviese al girarlo.")]
    [SerializeField] private float panelFondo = 0.05f;
    [Tooltip("Metros por encima del centro del plato.")]
    [SerializeField] private float panelAltura = 0.05f;

    [Header("Boton cerrar")]
    [Tooltip("Boton poke que devuelve el plato a la mesa. Se muestra junto con el panel.")]
    [SerializeField] private GameObject botonCerrar;
    [Tooltip("Metros a la izquierda del plato.")]
    [SerializeField] private float botonLado = 0.22f;
    [Tooltip("Metros respecto al centro del plato (negativo = mas abajo).")]
    [SerializeField] private float botonAltura = -0.02f;

    private Grabbable agarrable;
    private SnapInteractor snap;
    private RecetaManager manager;
    private bool activo;
    private Estado estado = Estado.Reposo;

    private Vector3 posOrigen;
    private Quaternion rotOrigen;
    private Vector3 centroLocal;    // del pivote al centro visual, sin rotacion
    private Vector3 centroDesde;
    private Vector3 centroObjetivo;
    private Quaternion rotDesde;
    private Quaternion rotActual;
    private Quaternion rotBaseMano;
    private Quaternion rotBasePlato;
    private float progreso;
    private float tiempoSuelto;

    private void Awake()
    {
        agarrable = GetComponent<Grabbable>();
        snap = GetComponentInChildren<SnapInteractor>(true);
        MostrarInfo(false);
        if (agarrable == null)
            Debug.LogWarning("[InspeccionPlato] " + name + ": falta el Grabbable.", this);
    }

    private void Start()
    {
        manager = RecetaManager.Instancia != null
            ? RecetaManager.Instancia
            : FindFirstObjectByType<RecetaManager>();

        if (manager == null)
        {
            Debug.LogWarning("[InspeccionPlato] No hay RecetaManager.", this);
            return;
        }

        manager.CambioEstado += ActualizarActivo;
        ActualizarActivo();
    }

    private void OnDestroy()
    {
        if (manager != null) manager.CambioEstado -= ActualizarActivo;
    }

    private void ActualizarActivo()
    {
        bool completa = manager.RecetaCompleta;
        if (completa == activo || agarrable == null) return;
        activo = completa;

        if (activo)
        {
            // Sin esto, al soltarlo cerca de la mesa el snap se lo llevaria.
            if (snap != null) snap.enabled = false;
            agarrable.InjectOptionalOneGrabTransformer(this);
            agarrable.InjectOptionalTwoGrabTransformer(this);
            return;
        }

        // Solo pasa al deshacer o reiniciar la receta con las teclas de prueba.
        if (estado != Estado.Reposo)
            transform.SetPositionAndRotation(posOrigen, rotOrigen);
        estado = Estado.Reposo;
        MostrarInfo(false);

        GrabFreeTransformer normal = GetComponent<GrabFreeTransformer>();
        agarrable.InjectOptionalOneGrabTransformer(normal);
        agarrable.InjectOptionalTwoGrabTransformer(normal);
        if (snap != null) snap.enabled = true;
    }

    // ------------------------------------------------------------ ITransformer

    public void Initialize(IGrabbable grabbable) { }

    public void BeginTransform()
    {
        if (estado == Estado.Reposo)
        {
            posOrigen = transform.position;
            rotOrigen = transform.rotation;
            centroLocal = Quaternion.Inverse(rotOrigen) * (CalcularCentroVisual() - posOrigen);
            rotActual = rotOrigen;
        }

        if (estado != Estado.Inspeccionando)
        {
            centroDesde = CentroActual();
            centroObjetivo = CalcularPuntoInspeccion();
            progreso = 0f;
            estado = Estado.Inspeccionando;
            MostrarPanel();
        }

        tiempoSuelto = 0f;
        rotBaseMano = agarrable.GrabPoints[0].rotation;
        rotBasePlato = rotActual;
    }

    public void UpdateTransform()
    {
        Quaternion delta = agarrable.GrabPoints[0].rotation * Quaternion.Inverse(rotBaseMano);
        delta.ToAngleAxis(out float angulo, out Vector3 eje);
        if (angulo > 180f) angulo -= 360f;
        if (Mathf.Approximately(angulo, 0f)) return;

        rotActual = Quaternion.AngleAxis(angulo * sensibilidad, eje) * rotBasePlato;
    }

    public void EndTransform() { }

    // ------------------------------------------------------------ Movimiento

    private void LateUpdate()
    {
        if (estado == Estado.Reposo) return;

        progreso = Mathf.Min(1f, progreso + Time.deltaTime / Mathf.Max(0.01f, duracionViaje));
        float s = Mathf.SmoothStep(0f, 1f, progreso);

        if (estado == Estado.Inspeccionando)
        {
            ColocarPorCentro(Vector3.Lerp(centroDesde, centroObjetivo, s), rotActual);

            if (agarrable.SelectingPointsCount > 0)
            {
                tiempoSuelto = 0f;
            }
            else
            {
                tiempoSuelto += Time.deltaTime;
                if (tiempoSuelto >= segundosAntesDeVolver)
                {
                    EmpezarRegreso();
                }
            }
            return;
        }

        rotActual = Quaternion.Slerp(rotDesde, rotOrigen, s);
        ColocarPorCentro(Vector3.Lerp(centroDesde, posOrigen + rotOrigen * centroLocal, s), rotActual);

        if (progreso >= 1f)
        {
            transform.SetPositionAndRotation(posOrigen, rotOrigen);
            estado = Estado.Reposo;
        }
    }

    private void ColocarPorCentro(Vector3 centro, Quaternion rotacion)
    {
        transform.SetPositionAndRotation(centro - rotacion * centroLocal, rotacion);
    }

    private Vector3 CentroActual()
    {
        return transform.position + transform.rotation * centroLocal;
    }

    // El pivote del plato no esta en su centro: se gira sobre el centro de lo que se ve.
    private Vector3 CalcularCentroVisual()
    {
        Renderer[] visuales = GetComponentsInChildren<Renderer>();
        if (visuales.Length == 0) return transform.position;

        Bounds caja = visuales[0].bounds;
        for (int i = 1; i < visuales.Length; i++)
            caja.Encapsulate(visuales[i].bounds);
        return caja.center;
    }

    /// <summary>Lo llama el boton "Cerrar": el plato vuelve a la mesa sin esperar.</summary>
    public void Cerrar()
    {
        if (estado == Estado.Inspeccionando) EmpezarRegreso();
    }

    private void EmpezarRegreso()
    {
        centroDesde = CentroActual();
        rotDesde = rotActual;
        progreso = 0f;
        estado = Estado.Regresando;
        MostrarInfo(false);
    }

    private void MostrarInfo(bool visible)
    {
        if (panelInfo != null) panelInfo.SetActive(visible);
        if (botonCerrar != null) botonCerrar.SetActive(visible);
    }

    // El panel (derecha) y el boton (izquierda) se colocan una vez al empezar la inspeccion,
    // al lado del punto donde flota el plato, mirando al usuario.
    private void MostrarPanel()
    {
        Transform ojos = camara != null ? camara : Camera.main != null ? Camera.main.transform : null;
        if (ojos == null) return;

        Vector3 frente = Vector3.ProjectOnPlane(centroObjetivo - ojos.position, Vector3.up);
        if (frente.sqrMagnitude < 0.0001f) frente = Vector3.ProjectOnPlane(ojos.forward, Vector3.up);
        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente);

        Colocar(panelInfo, centroObjetivo + derecha * panelLado + frente * panelFondo + Vector3.up * panelAltura, ojos, frente);
        Colocar(botonCerrar, centroObjetivo - derecha * botonLado + Vector3.up * botonAltura, ojos, frente);
        MostrarInfo(true);
    }

    private static void Colocar(GameObject objeto, Vector3 posicion, Transform ojos, Vector3 frente)
    {
        if (objeto == null) return;
        Vector3 mirada = Vector3.ProjectOnPlane(posicion - ojos.position, Vector3.up);
        objeto.transform.SetPositionAndRotation(posicion,
            Quaternion.LookRotation(mirada.sqrMagnitude > 0.0001f ? mirada : frente));
    }

    private Vector3 CalcularPuntoInspeccion()
    {
        Transform ojos = camara != null ? camara : Camera.main != null ? Camera.main.transform : null;
        if (ojos == null)
        {
            Debug.LogWarning("[InspeccionPlato] Sin camara asignada: el plato no se mueve.", this);
            return CentroActual();
        }

        Vector3 frente = Vector3.ProjectOnPlane(ojos.forward, Vector3.up);
        if (frente.sqrMagnitude < 0.001f) frente = Vector3.ProjectOnPlane(ojos.up, Vector3.up);
        return ojos.position + frente.normalized * distancia + Vector3.up * desnivel;
    }
}
