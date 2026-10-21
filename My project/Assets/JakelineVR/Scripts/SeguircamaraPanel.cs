using UnityEngine;

public class PanelSeguirCamara : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camara; // si lo dejas vacío usa la cámara activa

    [Header("Posición")]
    public float distancia = 1.5f;
    public float offsetY = -0.1f;
    public bool seguirEjeY = false;
    public float suavizadoPosicion = 2f;
    public float umbralMovimiento = 0.25f;

    [Header("Teletransporte")]
    public float distanciaMaxima = 4f;

    [Header("Rotación")]
    public float suavizadoRotacion = 3f;
    public bool invertirFrente = false;

    [Header("Debug")]
    public bool mostrarDebug = true;

    Vector3 destino;
    float yReferencia;

    void Start()
    {
        // Evita que el padre desfase la posición
        transform.SetParent(null, true);

        // Siempre usa la cámara que realmente está renderizando
        if (camara == null || !camara.gameObject.activeInHierarchy)
            camara = Camera.main.transform;

        if (mostrarDebug)
            Debug.Log($"[Panel] Siguiendo a: {camara.name} (ruta: {Ruta(camara)}) | escala: {camara.lossyScale}");

        Reposicionar();
    }

    public void Reposicionar()
    {
        yReferencia = camara.position.y + offsetY;
        destino = CalcularDestino();
        transform.SetPositionAndRotation(destino, CalcularRotacion());
    }

    void LateUpdate()
    {
        // Si la cámara asignada se desactiva, busca la activa
        if (!camara.gameObject.activeInHierarchy && Camera.main != null)
            camara = Camera.main.transform;

        float distHorizontal = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(camara.position.x, camara.position.z));

        if (distHorizontal > distanciaMaxima)
        {
            if (mostrarDebug) Debug.Log($"[Panel] Reubicado, estaba a {distHorizontal:F1} m");
            Reposicionar();
            return;
        }

        Vector3 nuevoDestino = CalcularDestino();
        if (Vector3.Distance(nuevoDestino, destino) > umbralMovimiento)
            destino = nuevoDestino;

        transform.position = Vector3.Lerp(transform.position, destino,
                                          Time.deltaTime * suavizadoPosicion);

        transform.rotation = Quaternion.Slerp(transform.rotation, CalcularRotacion(),
                                              Time.deltaTime * suavizadoRotacion);
    }

    Vector3 CalcularDestino()
    {
        Vector3 adelante = camara.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 0.001f) adelante = transform.forward;
        adelante.Normalize();

        Vector3 pos = camara.position + adelante * distancia; // sin multiplicar por escala

        if (!seguirEjeY && Mathf.Abs(yReferencia - (camara.position.y + offsetY)) > 1.5f)
            yReferencia = camara.position.y + offsetY;

        pos.y = seguirEjeY ? camara.position.y + offsetY : yReferencia;
        return pos;
    }

    Quaternion CalcularRotacion()
    {
        Vector3 dir = transform.position - camara.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return transform.rotation;
        if (invertirFrente) dir = -dir;
        return Quaternion.LookRotation(dir);
    }

    string Ruta(Transform t)
    {
        string r = t.name;
        while (t.parent != null) { t = t.parent; r = t.name + "/" + r; }
        return r;
    }
}