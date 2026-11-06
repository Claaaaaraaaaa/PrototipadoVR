using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

/// <summary>
/// Detector de receta. Ponlo en un objeto con un Collider marcado como "Is Trigger".
/// Cuando la olla queda dentro (y suelta), hace la accion elegida.
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

    public Accion accion = Accion.LlenarOlla;

    [Tooltip("Segundos que la olla tiene que estar dentro (y suelta) para que cuente")]
    public float segundosDentro = 2f;

    [Tooltip("Solo cuenta si el usuario ya solto la olla. Desactivalo si el snap deja la olla 'agarrada'.")]
    public bool soloSiEstaSuelta = false;

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

    readonly Dictionary<OllaEstado, int> collidersDentro = new Dictionary<OllaEstado, int>();

    OllaEstado ollaDentro;
    Grabbable agarreOlla;
    float tiempoQuieta;
    bool hechoEstaVez;
    string ultimoMotivo;

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

        if (mostrarDebug)
            Debug.Log("[ZonaReceta] " + name + " listo. Accion = " + accion, this);
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
            ultimoMotivo = null;

            Log("entro " + olla.name + " (accion: " + accion + ", espera " + segundosDentro + " s)");
            if (accion == Accion.LlenarOlla && !olla.TieneAgua)
                SonidosCocina.LlenandoOlla(true, olla.transform.position);
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
            Log("salio " + olla.name);
            ollaDentro = null;
            agarreOlla = null;
            if (accion == Accion.LlenarOlla)
                SonidosCocina.LlenandoOlla(false, olla.transform.position);
            alQuitar.Invoke();
        }
    }

    void Update()
    {
        if (ollaDentro == null || hechoEstaVez) return;

        if (!ollaDentro.isActiveAndEnabled)
        {
            Log("la olla se desactivo estando dentro");
            collidersDentro.Remove(ollaDentro);
            ollaDentro = null;
            return;
        }

        bool enLaMano = agarreOlla != null && agarreOlla.SelectingPointsCount > 0;
        if (soloSiEstaSuelta && enLaMano)
        {
            tiempoQuieta = 0f;
            Esperando("la olla sigue agarrada (si ya la soltaste, desactiva 'Solo Si Esta Suelta')");
            return;
        }

        tiempoQuieta += Time.deltaTime;
        if (tiempoQuieta < segundosDentro)
        {
            Esperando("contando " + segundosDentro + " s...");
            return;
        }

        Ejecutar();
    }

    void Ejecutar()
    {
        switch (accion)
        {
            case Accion.LlenarOlla:
                if (ollaDentro.TieneAgua)
                    Log("la olla ya tenia agua");
                else
                    ollaDentro.Llenar();
                Completar();
                break;

            case Accion.NotificarPaso:
                if (requiereOllaConAgua && !ollaDentro.TieneAgua)
                {
                    Esperando("la olla no tiene agua, no cuenta");
                    return; // sigue esperando por si se llena
                }
                RecetaEventos.Notificar(idAccion);
                Log("notificado " + idAccion);
                Completar();
                break;

            case Accion.Ninguna:
                Log("Accion = Ninguna, no hago nada (cambiala en el Inspector)");
                Completar();
                break;
        }
    }

    void Completar()
    {
        hechoEstaVez = true;
        alCompletar.Invoke();
    }

    void Esperando(string motivo)
    {
        if (motivo == ultimoMotivo) return;
        ultimoMotivo = motivo;
        Log("esperando: " + motivo);
    }

    void Log(string msg)
    {
        if (mostrarDebug)
            Debug.Log("[ZonaReceta] " + name + ": " + msg, this);
    }

    /// <summary>La olla que esta ahora mismo dentro de la zona (o null).</summary>
    public OllaEstado OllaDentro => ollaDentro;
}