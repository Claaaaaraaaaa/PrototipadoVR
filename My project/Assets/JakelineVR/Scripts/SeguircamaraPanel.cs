using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class PanelSeguirCamara : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camara;                 // si esta vacio usa la camara activa
    public Transform[] manos;                // opcional: se puede dejar vacio

    [Header("Posicion")]
    public float distancia = 1.2f;
    public float offsetY = -0.1f;
    public bool seguirEjeY = false;
    public float suavizadoPosicion = 4f;

    [Header("Seguimiento perezoso")]
    public float anguloActivacion = 60f;     // se reubica solo si sales de este angulo
    public float anguloReposo = 4f;          // deja de moverse cuando queda alineado
    public float radioCongelar = 0.4f;       // si una mano (opcional) esta mas cerca, se congela

    [Header("Interaccion (mano, control, rayo, poke)")]
    [Tooltip("Si esta vacio, busca TODOS los PointableCanvas en este objeto y sus hijos (incluso inactivos).")]
    public PointableCanvas[] pointableCanvases;
    public bool buscarAutomaticamente = true; // ademas de los asignados, agrega los hijos
    public float tiempoCongeladoExtra = 2f;   // sigue quieto un rato tras interactuar

    [Header("Teletransporte")]
    public float distanciaMaxima = 4f;
    public float esperaEntreReubicaciones = 1f;

    [Header("Rotacion")]
    public float suavizadoRotacion = 5f;
    public bool invertirFrente = false;

    [Header("Opciones")]
    public bool desacoplarDelPadre = false;  // true solo si el panel esta dentro de un padre que se mueve
    public bool reposicionarAlInicio = false; // false = respeta la posicion que tiene el panel en la escena
    public bool bloqueado = false;           // se puede activar desde otros scripts

    [Header("Debug")]
    public bool mostrarDebug = false;

    Vector3 destino;
    float yReferencia;
    bool moviendo = false;

    // Se cuenta por canvas, asi varios canvases no se pisan entre si
    readonly Dictionary<PointableCanvas, int> hoversPorCanvas = new Dictionary<PointableCanvas, int>();
    readonly List<PointableCanvas> suscritos = new List<PointableCanvas>();
    float congeladoHasta;
    float ultimaReubicacion = -99f;

    Vector3 ultimaPos;
    Quaternion ultimaRot;

    void OnEnable()
    {
        void OnEnable()
        {
            // PRUEBA: no suscribirse temporalmente al PointableCanvas.
        }
    }

    void OnDisable()
    {
        foreach (PointableCanvas pc in suscritos)
        {
            if (pc != null)
                pc.WhenPointerEventRaised -= AlEventoPuntero;
        }

        suscritos.Clear();
        hoversPorCanvas.Clear();
    }

    // Llama a esto si agregas o cambias canvases en tiempo de ejecucion
    public void Suscribir()
    {
        OnDisableInterno();

        var lista = new List<PointableCanvas>();

        if (pointableCanvases != null)
        {
            foreach (PointableCanvas pc in pointableCanvases)
                if (pc != null && !lista.Contains(pc))
                    lista.Add(pc);
        }

        if (buscarAutomaticamente || lista.Count == 0)
        {
            foreach (PointableCanvas pc in GetComponentsInChildren<PointableCanvas>(true))
                if (!lista.Contains(pc))
                    lista.Add(pc);
        }

        foreach (PointableCanvas pc in lista)
        {
            pc.WhenPointerEventRaised += AlEventoPuntero;
            suscritos.Add(pc);
            hoversPorCanvas[pc] = 0;
        }

        if (mostrarDebug)
            Debug.Log("[Panel] PointableCanvas suscritos: " + suscritos.Count);

        if (suscritos.Count == 0 && mostrarDebug)
            Debug.LogWarning("[Panel] No se encontro ningun PointableCanvas.");
    }

    void OnDisableInterno()
    {
        foreach (PointableCanvas pc in suscritos)
        {
            if (pc != null)
                pc.WhenPointerEventRaised -= AlEventoPuntero;
        }

        suscritos.Clear();
        hoversPorCanvas.Clear();
    }

    // El evento no dice de que canvas viene, asi que se cuenta en total y se
    // renueva el tiempo de congelado con cualquier evento.
    int interaccionesActivas;

    void AlEventoPuntero(PointerEvent e)
    {
        switch (e.Type)
        {
            case PointerEventType.Hover:
                interaccionesActivas++;
                break;
            case PointerEventType.Unhover:
                interaccionesActivas = Mathf.Max(0, interaccionesActivas - 1);
                break;
            case PointerEventType.Cancel:
                interaccionesActivas = 0;
                break;
        }

        congeladoHasta = Time.time + tiempoCongeladoExtra;
    }

    bool Interactuando()
    {
        return interaccionesActivas > 0 || Time.time < congeladoHasta;
    }

    void Start()
    {
        if (bloqueado)
            return;

        if (desacoplarDelPadre)
            transform.SetParent(null, true);

        if (camara == null || !camara.gameObject.activeInHierarchy)
            camara = Camera.main.transform;

        if (mostrarDebug)
            Debug.Log("[Panel] Siguiendo a: " + camara.name);

        if (reposicionarAlInicio)
            Reposicionar();
        else
        {
            destino = transform.position;
            yReferencia = transform.position.y;
            ultimaPos = transform.position;
            ultimaRot = transform.rotation;
        }
    }

    public void Reposicionar()
    {
        yReferencia = camara.position.y + offsetY;
        destino = CalcularDestino();
        transform.SetPositionAndRotation(destino, CalcularRotacion(destino));
        moviendo = false;
        ultimaReubicacion = Time.time;

        ultimaPos = transform.position;
        ultimaRot = transform.rotation;
    }

    void LateUpdate()
    {
        if (bloqueado)
            return;

        if (!camara.gameObject.activeInHierarchy && Camera.main != null)
            camara = Camera.main.transform;

        if (mostrarDebug &&
            (transform.position != ultimaPos || transform.rotation != ultimaRot))
        {
            Debug.Log("[Panel] Se movio | moviendo=" + moviendo +
                      " | interactuando=" + Interactuando());
        }

        Procesar();

        ultimaPos = transform.position;
        ultimaRot = transform.rotation;
    }

    void Procesar()
    {
        // 1) Congelar si el usuario esta interactuando. Tiene prioridad sobre todo.
        if (bloqueado || Interactuando() || ManoCerca())
        {
            moviendo = false;
            return;
        }

        // 2) Teletransporte a otra zona: reubicar de golpe (con espera para evitar bucles)
        float distHorizontal = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(camara.position.x, camara.position.z));

        if (distHorizontal > distanciaMaxima &&
            Time.time - ultimaReubicacion > esperaEntreReubicaciones)
        {
            if (mostrarDebug)
                Debug.Log("[Panel] Reubicado, estaba a " + distHorizontal.ToString("F1") + " m");

            Reposicionar();
            return;
        }

        // 3) Seguimiento perezoso
        float angulo = AnguloConElPanel();

        if (!moviendo && angulo > anguloActivacion)
        {
            moviendo = true;
            destino = CalcularDestino();
        }

        if (!moviendo)
            return;

        destino = CalcularDestino();

        transform.position = Vector3.Lerp(transform.position, destino,
                                          Time.deltaTime * suavizadoPosicion);
        transform.rotation = Quaternion.Slerp(transform.rotation, CalcularRotacion(destino),
                                              Time.deltaTime * suavizadoRotacion);

        if (Vector3.Distance(transform.position, destino) < 0.02f && angulo < anguloReposo)
            moviendo = false;
    }

    bool ManoCerca()
    {
        if (manos == null) return false;

        foreach (Transform m in manos)
        {
            if (m != null &&
                Vector3.Distance(m.position, transform.position) < radioCongelar)
                return true;
        }
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

        Vector3 pos = camara.position + adelante * distancia;

        if (!seguirEjeY && Mathf.Abs(yReferencia - (camara.position.y + offsetY)) > 1.5f)
            yReferencia = camara.position.y + offsetY;

        pos.y = seguirEjeY ? camara.position.y + offsetY : yReferencia;
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