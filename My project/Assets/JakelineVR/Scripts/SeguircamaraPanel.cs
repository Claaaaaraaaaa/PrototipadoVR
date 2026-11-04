using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

// Se ejecuta antes que los interactores del SDK, asi el rayo usa la posicion final del panel
[DefaultExecutionOrder(-200)]
public class PanelSeguirCamara : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Normalmente CenterEyeAnchor. Si esta vacio usa Camera.main")]
    public Transform camara;
    [Tooltip("Opcional: si una mano esta mas cerca que 'radioCongelar', el panel se congela")]
    public Transform[] manos;

    [Header("Posicion")]
    public float distancia = 1.2f;
    public float offsetY = -0.1f;
    public bool seguirEjeY = false;
    public float suavizadoPosicion = 4f;

    [Header("Evitar paredes y muebles")]
    [Tooltip("Capas de paredes/muebles. Excluye manos, jugador y UI")]
    public LayerMask capasObstaculo = ~0;
    public float distanciaMinima = 0.5f;
    public float margenPared = 0.15f;

    [Header("Seguimiento perezoso")]
    public float anguloActivacion = 60f;   // se reubica solo si sales de este angulo
    public float anguloReposo = 4f;        // deja de moverse cuando queda alineado
    public float radioCongelar = 0.4f;

    [Header("Interaccion")]
    [Tooltip("Si esta vacio, busca los PointableCanvas en este objeto y sus hijos")]
    public PointableCanvas[] pointableCanvases;
    public bool buscarAutomaticamente = true;
    public float tiempoCongeladoExtra = 2f; // sigue quieto un rato tras interactuar

    [Header("Interactores que compiten con el rayo")]
    [Tooltip("Arrastra aqui los DistanceGrabInteractor / DistanceHandGrabInteractor de ambas manos y controles")]
    public Behaviour[] desactivarMientrasApunta;

    [Header("Teletransporte")]
    public float distanciaMaxima = 4f;
    public float esperaEntreReubicaciones = 1f;

    [Header("Rotacion")]
    public float suavizadoRotacion = 5f;
    public bool invertirFrente = false;

    [Header("Opciones")]
    public bool desacoplarDelPadre = false;
    [Tooltip("Coloca el panel delante del usuario al iniciar y cada vez que se activa")]
    public bool reposicionarAlInicio = true;
    public bool bloqueado = false;

    [Header("Debug")]
    public bool mostrarDebug = false;

    Vector3 destino;
    float yReferencia;
    bool moviendo;
    bool iniciado;

    readonly List<PointableCanvas> suscritos = new List<PointableCanvas>();
    int hoversActivos;
    int seleccionesActivas;
    float congeladoHasta;
    float ultimaReubicacion = -99f;
    bool extrasDesactivados;

    // ------------------------------------------------------------------
    // Ciclo de vida
    // ------------------------------------------------------------------

    void OnEnable()
    {
        Suscribir();

        // Al reactivar la zona, colocar el panel delante del usuario de inmediato
        if (iniciado && !bloqueado && reposicionarAlInicio)
            Reposicionar();
    }

    void OnDisable()
    {
        ActualizarInteractoresExtra(false); // nunca dejar el agarre apagado
        Desuscribir();
    }

    void Start()
    {
        if (desacoplarDelPadre)
            transform.SetParent(null, true);

        AsegurarCamara();

        if (suscritos.Count == 0)
            Suscribir();

        if (reposicionarAlInicio && camara != null)
        {
            Reposicionar();
        }
        else
        {
            destino = transform.position;
            yReferencia = transform.position.y;
        }

        iniciado = true;

        if (mostrarDebug && camara != null)
            Debug.Log("[Panel] " + name + " siguiendo a: " + camara.name, this);
    }

    // ------------------------------------------------------------------
    // Suscripcion a eventos del puntero
    // ------------------------------------------------------------------

    public void Suscribir()
    {
        Desuscribir();

        var lista = new List<PointableCanvas>();

        if (pointableCanvases != null)
            foreach (var pc in pointableCanvases)
                if (pc != null && !lista.Contains(pc))
                    lista.Add(pc);

        if (buscarAutomaticamente || lista.Count == 0)
            foreach (var pc in GetComponentsInChildren<PointableCanvas>(true))
                if (!lista.Contains(pc))
                    lista.Add(pc);

        foreach (var pc in lista)
        {
            pc.WhenPointerEventRaised += AlEventoPuntero;
            suscritos.Add(pc);
        }

        if (mostrarDebug)
            Debug.Log("[Panel] " + name + " PointableCanvas suscritos: " + suscritos.Count, this);

        if (suscritos.Count == 0)
            Debug.LogWarning("[Panel] " + name + ": no se encontro ningun PointableCanvas.", this);
    }

    void Desuscribir()
    {
        foreach (var pc in suscritos)
            if (pc != null)
                pc.WhenPointerEventRaised -= AlEventoPuntero;

        suscritos.Clear();
        hoversActivos = 0;
        seleccionesActivas = 0;
    }

    void AlEventoPuntero(PointerEvent e)
    {
        switch (e.Type)
        {
            case PointerEventType.Hover:
                hoversActivos++;
                break;
            case PointerEventType.Unhover:
                hoversActivos = Mathf.Max(0, hoversActivos - 1);
                break;
            case PointerEventType.Select:
                seleccionesActivas++;
                break;
            case PointerEventType.Unselect:
                seleccionesActivas = Mathf.Max(0, seleccionesActivas - 1);
                break;
            case PointerEventType.Cancel:
                hoversActivos = 0;
                seleccionesActivas = 0;
                break;
        }

        congeladoHasta = Time.time + tiempoCongeladoExtra;
    }

    bool Apuntando()
    {
        return hoversActivos > 0 || seleccionesActivas > 0;
    }

    bool Interactuando()
    {
        return Apuntando() || Time.time < congeladoHasta;
    }

    // Apaga el agarre a distancia mientras el rayo esta sobre el panel
    void ActualizarInteractoresExtra(bool apuntando)
    {
        if (desactivarMientrasApunta == null || apuntando == extrasDesactivados)
            return;

        extrasDesactivados = apuntando;

        foreach (var b in desactivarMientrasApunta)
            if (b != null)
                b.enabled = !apuntando;

        if (mostrarDebug)
            Debug.Log("[Panel] " + name + " agarre a distancia " + (apuntando ? "DESACTIVADO" : "activado"), this);
    }

    // ------------------------------------------------------------------
    // Movimiento
    // ------------------------------------------------------------------

    void AsegurarCamara()
    {
        if ((camara == null || !camara.gameObject.activeInHierarchy) && Camera.main != null)
            camara = Camera.main.transform;
    }

    public void Reposicionar()
    {
        AsegurarCamara();
        if (camara == null) return;

        yReferencia = camara.position.y + offsetY;
        destino = CalcularDestino();
        transform.SetPositionAndRotation(destino, CalcularRotacion(destino));
        moviendo = false;
        ultimaReubicacion = Time.time;
    }

    void Update()
    {
        if (!iniciado || bloqueado)
            return;

        ActualizarInteractoresExtra(Apuntando());

        AsegurarCamara();
        if (camara == null)
            return;

        // 1) Congelar mientras se interactua. Tiene prioridad sobre todo.
        if (Interactuando() || ManoCerca())
        {
            moviendo = false;
            return;
        }

        // 2) Teletransporte a otra zona: reubicar de golpe
        float distHorizontal = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(camara.position.x, camara.position.z));

        if (distHorizontal > distanciaMaxima &&
            Time.time - ultimaReubicacion > esperaEntreReubicaciones)
        {
            if (mostrarDebug)
                Debug.Log("[Panel] " + name + " reubicado, estaba a " + distHorizontal.ToString("F1") + " m", this);

            Reposicionar();
            return;
        }

        // 3) Seguimiento perezoso
        float angulo = AnguloConElPanel();

        if (!moviendo && angulo > anguloActivacion)
            moviendo = true;

        if (!moviendo)
            return;

        destino = CalcularDestino();

        // Suavizado independiente de los FPS
        float t = 1f - Mathf.Exp(-suavizadoPosicion * Time.deltaTime);
        float r = 1f - Mathf.Exp(-suavizadoRotacion * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, destino, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, CalcularRotacion(destino), r);

        if (Vector3.Distance(transform.position, destino) < 0.02f && angulo < anguloReposo)
            moviendo = false;
    }

    bool ManoCerca()
    {
        if (manos == null) return false;

        foreach (var m in manos)
            if (m != null && Vector3.Distance(m.position, transform.position) < radioCongelar)
                return true;

        return false;
    }

    float AnguloConElPanel()
    {
        Vector3 haciaPanel = transform.position - camara.position;
        haciaPanel.y = 0f;

        Vector3 frente = camara.forward;
        frente.y = 0f;

        if (haciaPanel.sqrMagnitude < 0.001f || frente.sqrMagnitude < 0.001f)
            return 0f;

        return Vector3.Angle(frente, haciaPanel);
    }

    Vector3 CalcularDestino()
    {
        Vector3 adelante = camara.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 0.001f) adelante = transform.forward;
        adelante.Normalize();

        if (!seguirEjeY && Mathf.Abs(yReferencia - (camara.position.y + offsetY)) > 1.5f)
            yReferencia = camara.position.y + offsetY;

        float altura = seguirEjeY ? camara.position.y + offsetY : yReferencia;

        // Si hay una pared o mueble antes de la distancia deseada, acercar el panel
        float dist = distancia;
        Vector3 origen = new Vector3(camara.position.x, altura, camara.position.z);

        if (Physics.SphereCast(origen, 0.1f, adelante, out RaycastHit hit,
                               distancia + margenPared, capasObstaculo,
                               QueryTriggerInteraction.Ignore))
        {
            dist = Mathf.Max(distanciaMinima, hit.distance - margenPared);
        }

        Vector3 pos = camara.position + adelante * dist;
        pos.y = altura;
        return pos;
    }

    Quaternion CalcularRotacion(Vector3 posicionPanel)
    {
        Vector3 dir = posicionPanel - camara.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return transform.rotation;
        if (invertirFrente) dir = -dir;
        return Quaternion.LookRotation(dir);
    }
}