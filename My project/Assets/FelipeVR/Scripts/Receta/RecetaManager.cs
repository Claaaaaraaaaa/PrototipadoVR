using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Un sub-paso (por ejemplo un ingrediente dentro de "Pica y agrega...").</summary>
[Serializable]
public class SubPasoReceta
{
    public string texto;

    [Tooltip("Id de la accion que marca este sub-paso (ej: olla_cebolla).")]
    public string accionRequerida;

    [NonSerialized] public bool completado;

    public SubPasoReceta() { }

    public SubPasoReceta(string texto, string accionRequerida)
    {
        this.texto = texto;
        this.accionRequerida = accionRequerida;
    }
}

/// <summary>Un paso de la receta. Se edita desde el Inspector del RecetaManager.</summary>
[Serializable]
public class PasoReceta
{
    [TextArea(1, 3)] public string texto;

    [Tooltip("Id de la accion que completa este paso. Se ignora si el paso tiene sub-pasos.")]
    public string accionRequerida;

    [Min(1)]
    [Tooltip("Cuantas veces hay que notificar la accion. Se ignora si el paso tiene sub-pasos.")]
    public int accionesRequeridas = 1;

    [Tooltip("Si tiene sub-pasos, el paso se completa cuando TODOS estan hechos.")]
    public List<SubPasoReceta> subpasos = new List<SubPasoReceta>();

    // Estado en tiempo de ejecucion (no se guarda en la escena).
    [NonSerialized] public int accionesHechas;
    [NonSerialized] public bool completado;

    public bool TieneSubpasos => subpasos != null && subpasos.Count > 0;

    public int SubpasosHechos
    {
        get
        {
            int n = 0;
            if (subpasos != null)
                foreach (SubPasoReceta s in subpasos)
                    if (s.completado) n++;
            return n;
        }
    }

    public PasoReceta() { }

    public PasoReceta(string texto, string accionRequerida, int accionesRequeridas = 1)
    {
        this.texto = texto;
        this.accionRequerida = accionRequerida;
        this.accionesRequeridas = accionesRequeridas;
    }

    public static PasoReceta ConSubpasos(string texto, params SubPasoReceta[] subs)
    {
        PasoReceta p = new PasoReceta();
        p.texto = texto;
        p.accionRequerida = "";
        p.subpasos = new List<SubPasoReceta>(subs);
        return p;
    }

    public void Resetear()
    {
        accionesHechas = 0;
        completado = false;
        if (subpasos != null)
            foreach (SubPasoReceta s in subpasos)
                s.completado = false;
    }
}

/// <summary>
/// Lleva la cuenta de los pasos de la receta. Pon UNO en la escena.
/// - El juego avisa con RecetaEventos.Notificar(id) y el paso o sub-paso correspondiente se marca.
/// - Para pruebas: AvanzarPaso() marca el siguiente paso (o sub-paso) pendiente.
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
    [Tooltip("Se dispara al completar un sub-paso. Recibe (indice del paso, indice del sub-paso).")]
    public UnityEvent<int, int> onSubpasoCompletado = new UnityEvent<int, int>();
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
            p.Resetear();
    }

    // ---------------------------------------------------------------- API

    /// <summary>El juego avisa de que paso una accion (normalmente via RecetaEventos).</summary>
    public void RegistrarAccion(string idAccion)
    {
        Inicializar();
        if (string.IsNullOrEmpty(idAccion)) return;

        int desde = 0;
        int hasta = pasos.Count;

        if (ordenEstricto)
        {
            desde = PasoActual;
            hasta = Mathf.Min(desde + 1, pasos.Count);
        }

        for (int i = desde; i < hasta; i++)
        {
            PasoReceta paso = pasos[i];
            if (paso.completado) continue;

            if (paso.TieneSubpasos)
            {
                for (int j = 0; j < paso.subpasos.Count; j++)
                {
                    SubPasoReceta sub = paso.subpasos[j];
                    if (!sub.completado && sub.accionRequerida == idAccion)
                    {
                        CompletarSubpaso(i, j);
                        return;
                    }
                }
            }
            else if (paso.accionRequerida == idAccion)
            {
                paso.accionesHechas++;

                if (paso.accionesHechas >= paso.accionesRequeridas)
                    CompletarPaso(i);
                else
                    CambioEstado?.Invoke();
                return;
            }
        }
    }

    public void CompletarSubpaso(int indicePaso, int indiceSub)
    {
        Inicializar();
        if (indicePaso < 0 || indicePaso >= pasos.Count) return;

        PasoReceta paso = pasos[indicePaso];
        if (!paso.TieneSubpasos || indiceSub < 0 || indiceSub >= paso.subpasos.Count) return;

        SubPasoReceta sub = paso.subpasos[indiceSub];
        if (sub.completado) return;

        sub.completado = true;

        if (registrarEnConsola)
            Debug.Log($"[Receta] Paso {indicePaso + 1}: {sub.texto} listo " +
                      $"({paso.SubpasosHechos}/{paso.subpasos.Count})");

        onSubpasoCompletado.Invoke(indicePaso, indiceSub);

        if (paso.SubpasosHechos >= paso.subpasos.Count)
            CompletarPaso(indicePaso);   // ya invoca CambioEstado
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

        if (paso.TieneSubpasos)
            foreach (SubPasoReceta s in paso.subpasos)
                s.completado = true;

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

    /// <summary>PRUEBA: marca el siguiente pendiente (si el paso tiene sub-pasos, uno a uno).</summary>
    public void AvanzarPaso()
    {
        int actual = PasoActual;
        if (actual >= pasos.Count) return;

        PasoReceta paso = pasos[actual];
        if (paso.TieneSubpasos)
        {
            for (int j = 0; j < paso.subpasos.Count; j++)
            {
                if (!paso.subpasos[j].completado)
                {
                    CompletarSubpaso(actual, j);
                    return;
                }
            }
        }

        CompletarPaso(actual);
    }

    /// <summary>PRUEBA: deshace lo ultimo completado (sub-paso o paso).</summary>
    public void RetrocederPaso()
    {
        Inicializar();
        for (int i = pasos.Count - 1; i >= 0; i--)
        {
            PasoReceta p = pasos[i];

            if (p.TieneSubpasos && p.SubpasosHechos > 0)
            {
                for (int j = p.subpasos.Count - 1; j >= 0; j--)
                {
                    if (!p.subpasos[j].completado) continue;
                    p.subpasos[j].completado = false;
                    p.completado = false;
                    if (registrarEnConsola)
                        Debug.Log($"[Receta] Paso {i + 1}: {p.subpasos[j].texto} deshecho.");
                    CambioEstado?.Invoke();
                    return;
                }
            }
            else if (p.completado)
            {
                p.Resetear();
                if (registrarEnConsola) Debug.Log($"[Receta] Paso {i + 1} deshecho.");
                CambioEstado?.Invoke();
                return;
            }
        }
    }

    public void Reiniciar()
    {
        Inicializar();
        foreach (PasoReceta p in pasos)
            p.Resetear();
        CambioEstado?.Invoke();
    }

    /// <summary>Cambia cuantas acciones necesita el paso que espera 'idAccion'.</summary>
    public void FijarAccionesRequeridas(string idAccion, int cantidad)
    {
        Inicializar();
        foreach (PasoReceta p in pasos)
            if (!p.TieneSubpasos && p.accionRequerida == idAccion)
                p.accionesRequeridas = Mathf.Max(1, cantidad);
        CambioEstado?.Invoke();
    }

    /// <summary>
    /// True si todos los pasos ANTERIORES al paso que espera 'idAccion' estan completos.
    /// Ej: PasosPreviosCompletos(EstufaEncendida) = agua, ingredientes y leche de coco listos.
    /// </summary>
    public bool PasosPreviosCompletos(string idAccion)
    {
        Inicializar();
        for (int i = 0; i < pasos.Count; i++)
        {
            if (!pasos[i].TieneSubpasos && pasos[i].accionRequerida == idAccion)
                return true;            // llegamos al paso sin encontrar ninguno pendiente

            if (!pasos[i].completado)
                return false;           // hay un paso anterior sin hacer
        }
        return true;
    }

    /// <summary>PRUEBA: completa todos los pasos ANTERIORES al paso que espera 'idAccion'.</summary>
    public void CompletarHasta(string idAccion)
    {
        Inicializar();
        for (int i = 0; i < pasos.Count; i++)
        {
            if (!pasos[i].TieneSubpasos && pasos[i].accionRequerida == idAccion)
                return;          // llegamos al paso objetivo: este no se completa

            CompletarPaso(i);    // si ya estaba completo no hace nada
        }
    }

    /// <summary>Texto del primer paso pendiente (para avisar al usuario de que falta).</summary>
    public string TextoPasoActual()
    {
        int actual = PasoActual;
        return actual < pasos.Count ? pasos[actual].texto : "";
    }

    [ContextMenu("Cargar pasos por defecto")]
    private void CargarPasosPorDefecto()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Cargar pasos por defecto");
#endif
        pasos = PasosPorDefecto();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[Receta] Cargados " + pasos.Count + " pasos en el Inspector.");
    }

    // ------------------------------------------------------- Pasos iniciales

    // Se usan solo si la lista del Inspector esta vacia.
    private static List<PasoReceta> PasosPorDefecto()
    {
        return new List<PasoReceta>
        {
            // 1
            new PasoReceta("Llenar la olla de agua y ponerla en el mesón",
                RecetaEventos.OllaConAgua),

            // 2
            PasoReceta.ConSubpasos("Pica y agrega a la olla los siguientes ingredientes:",
                new SubPasoReceta("Cebolla",       RecetaEventos.EnOlla("cebolla")),
                new SubPasoReceta("Fruta de pan",  RecetaEventos.EnOlla("fruta_de_pan")),
                new SubPasoReceta("Yuca",          RecetaEventos.EnOlla("yuca")),
                new SubPasoReceta("Plátano",       RecetaEventos.EnOlla("platano")),
                new SubPasoReceta("Ñame",          RecetaEventos.EnOlla("name")),
                new SubPasoReceta("Ajo",           RecetaEventos.EnOlla("ajo_picado")),
                new SubPasoReceta("Cola de cerdo", RecetaEventos.EnOlla("cola_de_cerdo")),
                new SubPasoReceta("Pescado",       RecetaEventos.EnOlla("pescado"))),

            // 3
            PasoReceta.ConSubpasos("Agrega enteros a la olla los siguientes ingredientes:",
                new SubPasoReceta("Caracoles",      RecetaEventos.EnOlla("caracoles")),
                new SubPasoReceta("Sal",            RecetaEventos.EnOlla("sal")),
                new SubPasoReceta("Tomillo",        RecetaEventos.EnOlla("tomillo")),
                new SubPasoReceta("Pimienta",       RecetaEventos.EnOlla("pimienta")),
                new SubPasoReceta("Chile panameño", RecetaEventos.EnOlla("chile"))),

            // 4 a 9
            new PasoReceta("Echar la leche de coco", RecetaEventos.EnOlla("leche_coco")),
            new PasoReceta("Llevar la olla a la estufa y encenderla", RecetaEventos.EstufaEncendida),
            new PasoReceta("Tomar el plato y servir desde la olla",   RecetaEventos.PlatoServido),
            new PasoReceta("Llevar el plato a la mesa",               RecetaEventos.PlatoEnMesa),
            new PasoReceta("Volver a la estufa y llenar el cucharón", RecetaEventos.CucharonLleno),
            new PasoReceta("Volver a la mesa a servir",               RecetaEventos.CucharonServido),
        };
    }
}