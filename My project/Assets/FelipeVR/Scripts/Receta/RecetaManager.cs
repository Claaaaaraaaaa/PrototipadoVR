using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Un paso de la receta. Se edita desde el Inspector del RecetaManager.</summary>
[Serializable]
public class PasoReceta
{
    [TextArea(1, 3)] public string texto;

    [Tooltip("Id de la accion que completa este paso (ver RecetaEventos).")]
    public string accionRequerida;

    [Min(1)]
    [Tooltip("Cuantas veces hay que notificar la accion para completar el paso.")]
    public int accionesRequeridas = 1;

    // Estado en tiempo de ejecucion (no se guarda en la escena).
    [NonSerialized] public int accionesHechas;
    [NonSerialized] public bool completado;

    public PasoReceta() { }

    public PasoReceta(string texto, string accionRequerida, int accionesRequeridas = 1)
    {
        this.texto = texto;
        this.accionRequerida = accionRequerida;
        this.accionesRequeridas = accionesRequeridas;
    }
}

/// <summary>
/// Lleva la cuenta de los pasos de la receta. Pon UNO en la escena.
/// - El juego avisa con RecetaEventos.Notificar(id) y el paso correspondiente se completa.
/// - Para pruebas: AvanzarPaso() completa el siguiente paso pendiente
///   (lo llama RecetaDebugInput o cualquier boton de UI).
/// </summary>
public class RecetaManager : MonoBehaviour
{
    public static RecetaManager Instancia { get; private set; }

    [SerializeField] private List<PasoReceta> pasos = new List<PasoReceta>();

    [Tooltip("Si esta activo, una accion solo cuenta si pertenece al paso actual. " +
             "Si no, cada accion marca su paso en cualquier orden.")]
    [SerializeField] private bool ordenEstricto = false;

    [SerializeField] private bool registrarEnConsola = true;

    [Header("Eventos")]
    [Tooltip("Se dispara al completar un paso. Recibe el indice del paso (0, 1, 2...).")]
    public UnityEvent<int> onPasoCompletado = new UnityEvent<int>();
    public UnityEvent onRecetaCompletada = new UnityEvent();

    /// <summary>Lo usa la UI para refrescarse.</summary>
    public event Action CambioEstado;

    private bool inicializado;

    public IReadOnlyList<PasoReceta> Pasos
    {
        get { Inicializar(); return pasos; }
    }

    /// <summary>Indice del primer paso pendiente, o Pasos.Count si ya estan todos.</summary>
    public int PasoActual
    {
        get
        {
            Inicializar();
            for (int i = 0; i < pasos.Count; i++)
                if (!pasos[i].completado) return i;
            return pasos.Count;
        }
    }

    public bool RecetaCompleta => PasoActual >= Pasos.Count;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
            Debug.LogWarning("Hay mas de un RecetaManager en la escena.");
        Instancia = this;
        Inicializar();
    }

    private void OnEnable()
    {
        RecetaEventos.AccionRealizada += RegistrarAccion;
    }

    private void OnDisable()
    {
        RecetaEventos.AccionRealizada -= RegistrarAccion;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Inicializar()
    {
        if (inicializado) return;
        inicializado = true;

        if (pasos == null || pasos.Count == 0)
            pasos = PasosPorDefecto();

        foreach (PasoReceta p in pasos)
        {
            p.accionesHechas = 0;
            p.completado = false;
        }
    }

    // ---------------------------------------------------------------- API

    /// <summary>El juego avisa de que paso una accion (normalmente via RecetaEventos).</summary>
    public void RegistrarAccion(string idAccion)
    {
        Inicializar();
        if (string.IsNullOrEmpty(idAccion)) return;

        int indice = -1;

        if (ordenEstricto)
        {
            int actual = PasoActual;
            if (actual < pasos.Count && pasos[actual].accionRequerida == idAccion)
                indice = actual;
        }
        else
        {
            for (int i = 0; i < pasos.Count; i++)
            {
                if (!pasos[i].completado && pasos[i].accionRequerida == idAccion)
                {
                    indice = i;
                    break;
                }
            }
        }

        if (indice < 0) return;

        PasoReceta paso = pasos[indice];
        paso.accionesHechas++;

        if (paso.accionesHechas >= paso.accionesRequeridas)
            CompletarPaso(indice);
        else
            CambioEstado?.Invoke();
    }

    public void CompletarPaso(int indice)
    {
        Inicializar();
        if (indice < 0 || indice >= pasos.Count) return;

        PasoReceta paso = pasos[indice];
        if (paso.completado) return;

        paso.completado = true;
        paso.accionesHechas = Mathf.Max(paso.accionesHechas, paso.accionesRequeridas);

        if (registrarEnConsola)
            Debug.Log($"[Receta] Paso {indice + 1}/{pasos.Count} completado: {paso.texto}");

        onPasoCompletado.Invoke(indice);
        CambioEstado?.Invoke();

        if (RecetaCompleta)
        {
            if (registrarEnConsola) Debug.Log("[Receta] Receta completa.");
            onRecetaCompletada.Invoke();
        }
    }

    /// <summary>PRUEBA: completa el siguiente paso pendiente.</summary>
    public void AvanzarPaso()
    {
        int actual = PasoActual;
        if (actual < pasos.Count) CompletarPaso(actual);
    }

    /// <summary>PRUEBA: deshace el ultimo paso completado.</summary>
    public void RetrocederPaso()
    {
        Inicializar();
        for (int i = pasos.Count - 1; i >= 0; i--)
        {
            if (!pasos[i].completado) continue;

            pasos[i].completado = false;
            pasos[i].accionesHechas = 0;
            if (registrarEnConsola) Debug.Log($"[Receta] Paso {i + 1} deshecho.");
            CambioEstado?.Invoke();
            return;
        }
    }

    public void Reiniciar()
    {
        Inicializar();
        foreach (PasoReceta p in pasos)
        {
            p.accionesHechas = 0;
            p.completado = false;
        }
        CambioEstado?.Invoke();
    }

    // ------------------------------------------------------- Pasos iniciales

    // Basados en la referencia enviada por el equipo.
    // Se usan solo si la lista del Inspector esta vacia; se pueden editar ahi.
    private static List<PasoReceta> PasosPorDefecto()
    {
        return new List<PasoReceta>
        {
            new PasoReceta("Tomar la olla, llenarla de agua y ponerla de nuevo en el mesón",
                RecetaEventos.OllaConAgua),
            new PasoReceta("Agarrar los ingredientes, picarlos y echarlos a la olla",
                RecetaEventos.IngredienteEnOlla),
            new PasoReceta("Echar la leche de coco",
                RecetaEventos.LecheCocoEnOlla),
            new PasoReceta("Llevar la olla a la estufa, encender la estufa y dejar que cocine",
                RecetaEventos.EstufaEncendida),
            new PasoReceta("Tomar el plato y servir desde la olla",
                RecetaEventos.PlatoServido),
            new PasoReceta("Llevar el plato a la mesa",
                RecetaEventos.PlatoEnMesa),
            new PasoReceta("Volver a la estufa y llenar el cucharón",
                RecetaEventos.CucharonLleno),
            new PasoReceta("Volver a la mesa a servir",
                RecetaEventos.CucharonServido),
        };
    }
}
