using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

/// <summary>
/// Detector de receta. Ponlo en un objeto con un Collider marcado como "Is Trigger"
/// (por ejemplo un hijo de la snap zone). Cuando la olla queda dentro y suelta,
/// hace la accion elegida.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaReceta : MonoBehaviour
{
    public enum Accion
    {
        Ninguna,        // solo dispara los eventos de abajo
        LlenarOlla,     // lavamanos: llena la olla
        NotificarPaso   // completa un paso de la receta
    }

    public Accion accion = Accion.Ninguna;

    [Tooltip("Segundos que la olla tiene que estar dentro (y suelta) para que cuente")]
    public float segundosDentro = 2f;

    [Tooltip("Solo cuenta si el usuario ya solto la olla (no si pasa por aqui en la mano)")]
    public bool soloSiEstaSuelta = true;

    [Header("Notificar paso")]
    [Tooltip("Id del paso: olla_con_agua, estufa_encendida, plato_en_mesa...")]
    public string idAccion = RecetaEventos.OllaConAgua;
    [Tooltip("Solo cuenta si la olla tiene agua")]
    public bool requiereOllaConAgua = true;

    [Header("Eventos (opcional: sonido, particulas...)")]
    public UnityEvent alColocar;
    public UnityEvent alQuitar;
    public UnityEvent alCompletar;

    public bool mostrarDebug = true;

    // Una olla puede tener varios colliders: se cuentan para saber cuando sale del todo
    readonly Dictionary<OllaEstado, int> collidersDentro = new Dictionary<OllaEstado, int>();

    OllaEstado ollaDentro;
    Grabbable agarreOlla;
    float tiempoQuieta;
    bool hechoEstaVez;
    bool avisoSinAgua;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        Collider c = GetComponent<Collider>();
        if (!c.isTrigger)
        {
            Debug.LogWarning("[ZonaReceta] " + name + ": el Collider no es Trigger. Lo activo.", this);
            c.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        OllaEstado olla = other.GetComponentInParent<OllaEstado>();
        if (olla == null) return;

        collidersDentro.TryGetValue(olla, out int n);
        collidersDentro[olla] = n + 1;

        if (n == 0 && ollaDentro == null)
        {
            ollaDentro = olla;
            agarreOlla = olla.GetComponentInChildren<Grabbable>();
            tiempoQuieta = 0f;
            hechoEstaVez = false;
            avisoSinAgua = false;

            if (mostrarDebug) Debug.Log("[ZonaReceta] " + name + ": entro " + olla.name, this);
            alColocar.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        OllaEstado olla = other.GetComponentInParent<OllaEstado>();
        if (olla == null || !collidersDentro.ContainsKey(olla)) return;

        collidersDentro[olla]--;
        if (collidersDentro[olla] > 0) return;

        collidersDentro.Remove(olla);

        if (olla == ollaDentro)
        {
            if (mostrarDebug) Debug.Log("[ZonaReceta] " + name + ": salio " + olla.name, this);
            ollaDentro = null;
            agarreOlla = null;
            alQuitar.Invoke();
        }
    }

    void Update()
    {
        if (ollaDentro == null || hechoEstaVez) return;

        // Si la olla se desactivo estando dentro, olvidarla
        if (!ollaDentro.isActiveAndEnabled)
        {
            collidersDentro.Remove(ollaDentro);
            ollaDentro = null;
            return;
        }

        // Mientras la tengan en la mano no cuenta el tiempo
        bool enLaMano = agarreOlla != null && agarreOlla.SelectingPointsCount > 0;
        if (soloSiEstaSuelta && enLaMano)
        {
            tiempoQuieta = 0f;
            return;
        }

        tiempoQuieta += Time.deltaTime;
        if (tiempoQuieta < segundosDentro) return;

        Ejecutar();
    }

    void Ejecutar()
    {
        switch (accion)
        {
            case Accion.LlenarOlla:
                ollaDentro.Llenar();
                hechoEstaVez = true;
                alCompletar.Invoke();
                break;

            case Accion.NotificarPaso:
                if (requiereOllaConAgua && !ollaDentro.TieneAgua)
                {
                    if (mostrarDebug && !avisoSinAgua)
                        Debug.Log("[ZonaReceta] " + name + ": la olla no tiene agua, no cuenta.", this);
                    avisoSinAgua = true;
                    return; // sigue esperando por si se llena
                }
                RecetaEventos.Notificar(idAccion);
                hechoEstaVez = true;
                alCompletar.Invoke();
                if (mostrarDebug) Debug.Log("[ZonaReceta] " + name + ": notificado " + idAccion, this);
                break;

            case Accion.Ninguna:
                hechoEstaVez = true;
                alCompletar.Invoke();
                break;
        }
    }
}