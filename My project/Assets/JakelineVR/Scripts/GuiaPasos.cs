using System.Collections.Generic;
using Oculus.Interaction;
using TMPro;
using UnityEngine;

/// <summary>
/// Guia visual de la receta. Segun el paso actual, pone un contorno a lo que hay que tomar
/// y un disco en la zona a la que hay que llevarlo. Primero resalta el libro; despues sigue la receta,
/// y al final resalta el plato hasta que se agarre para inspeccionarlo.
/// El contorno es una copia de la malla en un hijo, asi no toca los materiales del objeto.
/// </summary>
public class GuiaPasos : MonoBehaviour
{
    private const string NombreContorno = "ContornoGuia";

    private class Contorno
    {
        public Transform objetivo;
        public Renderer origen;
        public GameObject objeto;
        public MeshRenderer renderer;
    }

    [Header("Aspecto")]
    [SerializeField] private Material materialContorno;
    [Tooltip("Metros de diametro del disco que marca una zona.")]
    [SerializeField] private float diametroMarca = 0.28f;
    [Tooltip("Metros por encima de la superficie a los que se coloca el disco.")]
    [SerializeField] private float alturaMarca = 0.012f;

    [Header("Objetos")]
    [SerializeField] private ControlRecetario recetario;
    [SerializeField] private OllaEstado olla;
    [SerializeField] private ZonaReceta zonaEstufa;
    [SerializeField] private Transform cucharon;
    [SerializeField] private Grabbable plato;

    [Header("Zonas (SnapZone)")]
    [SerializeField] private Transform zonaLavamanos;
    [SerializeField] private Transform zonaMeson;
    [SerializeField] private Transform zonaOllaEstufa;
    [SerializeField] private Transform zonaPlato;

    [Tooltip("Resalta una sola unidad por ingrediente (una cebolla de las tres). Menos dibujos y menos ruido visual.")]
    [SerializeField] private bool soloUnaUnidad = true;

    [Tooltip("Escribe en la consola cada cambio de paso de la guia (nunca en una build final).")]
    [SerializeField] private bool mostrarDebug = true;

    private readonly HashSet<Transform> objetivos = new HashSet<Transform>();
    private readonly HashSet<Transform> zonas = new HashSet<Transform>();
    private readonly HashSet<string> pendientes = new HashSet<string>();
    private readonly List<Contorno> contornos = new List<Contorno>();
    private readonly HashSet<Transform> yaPreparados = new HashSet<Transform>();
    private readonly Dictionary<Transform, GameObject> marcas = new Dictionary<Transform, GameObject>();

    private RecetaManager manager;
    private readonly HashSet<string> unidadesVistas = new HashSet<string>();
    private IngredientePicable[] picables;
    private IngredienteEntero[] enteros;
    private string[] accionesPicables;   // se calculan una vez: EnOlla crea textos nuevos en cada llamada
    private string[] accionesEnteros;
    private CuchilloPicador[] cuchillos;
    private Transform perilla;
    private bool libroAbierto;
    private bool platoAgarrado;
    private float siguienteRevision;
    private int ultimoPaso = -1;
    private Material materialMarca;

    private void OnDestroy()
    {
        if (materialMarca != null) Destroy(materialMarca);
    }

    private void Start()
    {
        manager = RecetaManager.Instancia != null
            ? RecetaManager.Instancia
            : FindFirstObjectByType<RecetaManager>();

        picables = FindObjectsByType<IngredientePicable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        enteros = FindObjectsByType<IngredienteEntero>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        cuchillos = FindObjectsByType<CuchilloPicador>(FindObjectsSortMode.None);
        perilla = BuscarPerilla();

        accionesPicables = new string[picables.Length];
        for (int i = 0; i < picables.Length; i++)
            accionesPicables[i] = RecetaEventos.EnOlla(picables[i].IdIngrediente);

        accionesEnteros = new string[enteros.Length];
        for (int i = 0; i < enteros.Length; i++)
            accionesEnteros[i] = RecetaEventos.EnOlla(enteros[i].IdIngrediente);

        if (manager == null || materialContorno == null)
        {
            Debug.LogWarning("[GuiaPasos] Falta el RecetaManager o el material del contorno.", this);
            return;
        }

        // Los discos se ven siempre, aunque algo los tape: marcan a donde hay que ir.
        materialMarca = new Material(materialContorno);
        materialMarca.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);

        CrearMarca(zonaLavamanos);
        CrearMarca(zonaMeson);
        CrearMarca(zonaOllaEstufa);
        CrearMarca(zonaPlato);
    }

    private void Update()
    {
        if (manager == null || materialContorno == null || Time.time < siguienteRevision) return;
        siguienteRevision = Time.time + 0.2f;

        objetivos.Clear();
        zonas.Clear();
        CalcularObjetivos();
        AplicarResaltado();
    }

    // ------------------------------------------------------------ Que se resalta en cada paso

    private void CalcularObjetivos()
    {
        if (!libroAbierto && recetario != null)
        {
            libroAbierto = recetario.Abierto;
            if (!libroAbierto)
            {
                Agregar(recetario.libroBarra != null ? recetario.libroBarra.Transform : null);
                return;
            }
        }

        int actual = manager.PasoActual;
        if (mostrarDebug && Debug.isDebugBuild && actual != ultimoPaso)
            Debug.Log("[GuiaPasos] Paso " + (actual + 1) + ": " + manager.TextoPasoActual(), this);
        ultimoPaso = actual;

        if (actual >= manager.Pasos.Count)
        {
            CalcularFinal();
            return;
        }

        PasoReceta paso = manager.Pasos[actual];
        switch (paso.accionRequerida)
        {
            case RecetaEventos.OllaConAgua:
                Agregar(olla != null ? olla.transform : null);
                zonas.Add(olla != null && olla.TieneAgua ? zonaMeson : zonaLavamanos);
                break;

            case RecetaEventos.EstufaEncendida:
                if (zonaEstufa != null && zonaEstufa.OllaDentro != null)
                {
                    Agregar(perilla);
                }
                else
                {
                    Agregar(olla != null ? olla.transform : null);
                    zonas.Add(zonaOllaEstufa);
                }
                break;

            case RecetaEventos.PlatoEnMesa:
                Agregar(plato != null ? plato.transform : null);
                zonas.Add(zonaPlato);
                break;

            case RecetaEventos.CucharonLleno:
                Agregar(olla != null ? olla.transform : null);
                // Mientras se cocina solo se resalta la olla: el cucharon aun no se puede llenar.
                if (olla != null && olla.EstadoActual == OllaEstado.Estado.Rondon)
                    Agregar(cucharon);
                break;

            case RecetaEventos.CucharonServido:
                Agregar(plato != null ? plato.transform : null);
                Agregar(cucharon);
                break;

            default:
                CalcularIngredientes(paso);
                break;
        }
    }

    private void CalcularIngredientes(PasoReceta paso)
    {
        pendientes.Clear();
        if (paso.TieneSubpasos)
        {
            foreach (SubPasoReceta sub in paso.subpasos)
                if (!sub.completado) pendientes.Add(sub.accionRequerida);
        }
        else
        {
            pendientes.Add(paso.accionRequerida);
        }

        unidadesVistas.Clear();
        bool porPicar = false;
        bool paraLaOlla = false;

        // Primero las unidades ya picadas: si hay una, es la que hay que llevar a la olla.
        for (int i = 0; i < picables.Length; i++)
        {
            IngredientePicable picable = picables[i];
            if (picable != null && picable.YaPicado && pendientes.Contains(accionesPicables[i])
                && ResaltarUnidad(picable.transform, accionesPicables[i]))
                paraLaOlla = true;
        }

        for (int i = 0; i < picables.Length; i++)
        {
            IngredientePicable picable = picables[i];
            if (picable != null && !picable.YaPicado && pendientes.Contains(accionesPicables[i])
                && ResaltarUnidad(picable.transform, accionesPicables[i]))
                porPicar = true;
        }

        for (int i = 0; i < enteros.Length; i++)
        {
            IngredienteEntero entero = enteros[i];
            if (entero != null && pendientes.Contains(accionesEnteros[i])
                && ResaltarUnidad(entero.transform, accionesEnteros[i]))
                paraLaOlla = true;
        }

        if (porPicar)
            foreach (CuchilloPicador cuchillo in cuchillos)
                if (cuchillo != null)
                    Agregar(cuchillo.agarrable != null ? cuchillo.agarrable.transform : cuchillo.transform);

        if (paraLaOlla && olla != null)
            Agregar(olla.transform);
    }

    private bool ResaltarUnidad(Transform unidad, string accion)
    {
        if (soloUnaUnidad && !unidadesVistas.Add(accion)) return false;
        Agregar(unidad);
        return true;
    }

    private void CalcularFinal()
    {
        if (platoAgarrado || plato == null) return;

        platoAgarrado = plato.SelectingPointsCount > 0;
        if (!platoAgarrado)
            Agregar(plato.transform);
    }

    private void Agregar(Transform objetivo)
    {
        if (objetivo != null) objetivos.Add(objetivo);
    }

    // De las perillas, la del fogon que queda debajo de la olla.
    private Transform BuscarPerilla()
    {
        if (zonaOllaEstufa == null) return null;

        Transform mejor = null;
        float menor = float.MaxValue;
        foreach (ControlFogon fogon in FindObjectsByType<ControlFogon>(FindObjectsSortMode.None))
        {
            float distancia = (fogon.PosicionFuego - zonaOllaEstufa.position).sqrMagnitude;
            if (distancia < menor)
            {
                menor = distancia;
                mejor = fogon.transform;
            }
        }
        return mejor;
    }

    // ------------------------------------------------------------ Dibujo

    private void AplicarResaltado()
    {
        foreach (Transform objetivo in objetivos)
            if (yaPreparados.Add(objetivo))
                CrearContornos(objetivo);

        for (int i = contornos.Count - 1; i >= 0; i--)
        {
            Contorno contorno = contornos[i];
            if (contorno.objeto == null || contorno.origen == null)
            {
                contornos.RemoveAt(i);  // el ingrediente ya se destruyo al entrar en la olla
                continue;
            }

            // Se comprueba siempre: OllaEstado y Plato tambien tocan a los hijos de sus modelos.
            bool visible = contorno.objetivo != null && objetivos.Contains(contorno.objetivo);
            if (contorno.objeto.activeSelf != visible) contorno.objeto.SetActive(visible);
            if (visible) contorno.renderer.enabled = contorno.origen.enabled;
        }

        foreach (KeyValuePair<Transform, GameObject> marca in marcas)
            marca.Value.SetActive(zonas.Contains(marca.Key));
    }

    private void CrearContornos(Transform objetivo)
    {
        foreach (MeshRenderer origen in objetivo.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filtro = origen.GetComponent<MeshFilter>();
            if (origen.name == NombreContorno || origen.isPartOfStaticBatch || origen.GetComponent<TMP_Text>() != null)
                continue;
            if (filtro == null || filtro.sharedMesh == null) continue;

            GameObject objeto = new GameObject(NombreContorno);
            objeto.layer = origen.gameObject.layer;
            objeto.transform.SetParent(origen.transform, false);
            objeto.AddComponent<MeshFilter>().sharedMesh = filtro.sharedMesh;

            MeshRenderer renderer = objeto.AddComponent<MeshRenderer>();
            ConfigurarRenderer(renderer);

            // Un material por submalla: si no, solo se contornearia la ultima.
            Material[] materiales = new Material[filtro.sharedMesh.subMeshCount];
            for (int i = 0; i < materiales.Length; i++) materiales[i] = materialContorno;
            renderer.sharedMaterials = materiales;

            contornos.Add(new Contorno { objetivo = objetivo, origen = origen, objeto = objeto, renderer = renderer });
        }
    }

    private void CrearMarca(Transform zona)
    {
        if (zona == null) return;

        GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marca.name = "Marca " + zona.name;

        Collider colisionador = marca.GetComponent<Collider>();
        colisionador.enabled = false;
        Destroy(colisionador);

        marca.transform.SetParent(transform, false);
        marca.transform.position = BuscarSuperficie(zona) + Vector3.up * alturaMarca;
        marca.transform.localScale = new Vector3(diametroMarca, 0.0025f, diametroMarca);

        MeshRenderer renderer = marca.GetComponent<MeshRenderer>();
        ConfigurarRenderer(renderer);
        renderer.sharedMaterial = materialMarca;

        marca.SetActive(false);
        marcas[zona] = marca;
    }

    // La altura del snap no siempre coincide con el mueble: se busca la superficie real debajo de la zona.
    private Vector3 BuscarSuperficie(Transform zona)
    {
        Vector3 punto = zona.position;
        float menor = float.MaxValue;
        bool encontrada = false;

        foreach (RaycastHit golpe in Physics.RaycastAll(punto + Vector3.up * 0.3f, Vector3.down, 0.6f,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (olla != null && golpe.collider.transform.IsChildOf(olla.transform)) continue;
            if (plato != null && golpe.collider.transform.IsChildOf(plato.transform)) continue;
            if (golpe.distance >= menor) continue;

            menor = golpe.distance;
            punto = golpe.point;
            encontrada = true;
        }

        if (mostrarDebug && Debug.isDebugBuild)
            Debug.Log("[GuiaPasos] Disco de " + zona.name + (encontrada
                ? " sobre " + punto.y.ToString("F3") + " m"
                : ": sin superficie debajo, uso la altura del snap (" + punto.y.ToString("F3") + " m)"), this);

        return punto;
    }

    private static void ConfigurarRenderer(MeshRenderer renderer)
    {
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }
}
