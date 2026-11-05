using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Hace que el objeto se quede en su sitio pero gire para mirar siempre al usuario.
/// Mientras el usuario interactua con el (rayo, poke...), deja de girar.
/// Ponlo en AnclaLibro.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MirarAlUsuario : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("CenterEyeAnchor. Si esta vacio usa Camera.main")]
    public Transform camara;

    [Header("Rotacion")]
    [Tooltip("Solo gira de lado a lado (se mantiene derecho)")]
    public bool soloEjeY = true;
    [Tooltip("Activalo si el libro aparece de espaldas")]
    public bool invertirFrente = false;
    [Tooltip("Velocidad del giro. Mas alto = mas rapido")]
    public float suavizado = 4f;
    [Tooltip("No gira si la diferencia es menor a estos grados (evita temblores)")]
    public float anguloMinimo = 2f;

    [Header("No girar mientras se interactua")]
    public bool congelarAlInteractuar = true;
    [Tooltip("Segundos que sigue quieto despues de dejar de interactuar")]
    public float tiempoCongeladoExtra = 1.5f;
    [Tooltip("Si una mano esta mas cerca que esto del libro, no gira (para poder pulsar con el dedo)")]
    public float radioManoCerca = 0.35f;
    [Tooltip("Opcional: LeftHandAnchor y RightHandAnchor (o las puntas de los dedos)")]
    public Transform[] manos;

    public bool mostrarDebug = false;

    readonly List<IPointable> suscritos = new List<IPointable>();
    int hoversActivos;
    int seleccionesActivas;
    float congeladoHasta;

    void OnEnable()
    {
        AsegurarCamara();
        Suscribir();

        // Al abrirse ya aparece mirando al usuario, sin animacion
        if (camara != null)
            transform.rotation = RotacionObjetivo();
    }

    void OnDisable()
    {
        Desuscribir();
    }

    void Update()
    {
        AsegurarCamara();
        if (camara == null) return;

        if (congelarAlInteractuar && Congelado())
            return;

        Quaternion objetivo = RotacionObjetivo();
        if (Quaternion.Angle(transform.rotation, objetivo) < anguloMinimo)
            return;

        float t = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, t);
    }

    Quaternion RotacionObjetivo()
    {
        Vector3 dir = transform.position - camara.position;
        if (soloEjeY) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return transform.rotation;
        if (invertirFrente) dir = -dir;
        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    void AsegurarCamara()
    {
        if ((camara == null || !camara.gameObject.activeInHierarchy) && Camera.main != null)
            camara = Camera.main.transform;
    }

    // ------------------------------------------------------------------
    // Detectar interaccion (rayo, poke...) y manos cerca
    // ------------------------------------------------------------------

    bool Congelado()
    {
        return hoversActivos > 0 || seleccionesActivas > 0 ||
               Time.time < congeladoHasta || ManoCerca();
    }

    bool ManoCerca()
    {
        if (manos == null) return false;
        foreach (Transform m in manos)
            if (m != null && Vector3.Distance(m.position, transform.position) < radioManoCerca)
                return true;
        return false;
    }

    void Suscribir()
    {
        Desuscribir();
        if (!congelarAlInteractuar) return;

        // Todos los interactuables de los hijos: PointableCanvas (rayo), PokeInteractable (poke)...
        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IPointable pointable && !suscritos.Contains(pointable))
            {
                pointable.WhenPointerEventRaised += AlEventoPuntero;
                suscritos.Add(pointable);
            }
        }

        if (mostrarDebug)
            Debug.Log("[MirarAlUsuario] Interactuables suscritos: " + suscritos.Count, this);
    }

    void Desuscribir()
    {
        foreach (IPointable p in suscritos)
            if (p != null) p.WhenPointerEventRaised -= AlEventoPuntero;
        suscritos.Clear();
        hoversActivos = 0;
        seleccionesActivas = 0;
    }

    void AlEventoPuntero(PointerEvent e)
    {
        switch (e.Type)
        {
            case PointerEventType.Hover: hoversActivos++; break;
            case PointerEventType.Unhover: hoversActivos = Mathf.Max(0, hoversActivos - 1); break;
            case PointerEventType.Select: seleccionesActivas++; break;
            case PointerEventType.Unselect: seleccionesActivas = Mathf.Max(0, seleccionesActivas - 1); break;
            case PointerEventType.Cancel: hoversActivos = 0; seleccionesActivas = 0; break;
        }
        congeladoHasta = Time.time + tiempoCongeladoExtra;
    }
}