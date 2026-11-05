using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra los pasos de la receta como texto con un check al lado.
///   [  ] 2. Pica y agrega a la olla los siguientes ingredientes: (1/8)
///         [X] Cebolla
///         [  ] Yuca
/// </summary>
public class RecetaPasosUI : MonoBehaviour
{
    [SerializeField] private TMP_Text texto;
    [SerializeField] private string titulo = "";

    [Header("Que pasos muestra (para repartir en varias paginas)")]
    [Tooltip("Primer paso que muestra este texto (1 = el primero).")]
    [Min(1)][SerializeField] private int primerPaso = 1;
    [Tooltip("Cuantos pasos muestra. 0 = todos desde el primero indicado.")]
    [Min(0)][SerializeField] private int cantidadPasos = 0;

    [Header("Estilo")]
    [SerializeField] private Color colorPendiente = new Color(0.15f, 0.1f, 0.05f);
    [SerializeField] private Color colorCompletado = new Color(0.1f, 0.45f, 0.15f);
    [SerializeField] private bool tacharCompletados = true;
    [Tooltip("Pone en negrita el paso en el que va el usuario")]
    [SerializeField] private bool resaltarPasoActual = true;
    [Tooltip("Muestra (3/8) junto al paso principal")]
    [SerializeField] private bool mostrarProgreso = true;
    [Tooltip("Sangria de los sub-pasos, en % del ancho")]
    [SerializeField] private float sangriaSubpasos = 8f;
    [Tooltip("Deja una linea en blanco entre pasos")]
    [SerializeField] private bool lineaEnBlancoEntrePasos = true;
    [Tooltip("Deja una linea en blanco entre el paso principal y su lista de sub-pasos")]
    [SerializeField] private bool lineaEnBlancoAntesDeSubpasos = true;

    private RecetaManager manager;
    private string hexPendiente;
    private string hexCompletado;

    private void Awake()
    {
        if (texto == null) texto = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        manager = RecetaManager.Instancia;
        if (manager == null) manager = FindFirstObjectByType<RecetaManager>();

        if (manager == null)
        {
            Debug.LogWarning("RecetaPasosUI: no hay RecetaManager en la escena.", this);
            return;
        }

        manager.CambioEstado += Refrescar;
        Refrescar();
    }

    private void OnDisable()
    {
        if (manager != null) manager.CambioEstado -= Refrescar;
        manager = null;
    }

    public void Refrescar()
    {
        if (texto == null || manager == null) return;

        hexPendiente = ColorUtility.ToHtmlStringRGB(colorPendiente);
        hexCompletado = ColorUtility.ToHtmlStringRGB(colorCompletado);

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(titulo))
            sb.Append(titulo).Append("\n\n");

        var pasos = manager.Pasos;
        int actual = manager.PasoActual;
        int desde = Mathf.Max(0, primerPaso - 1);
        int hasta = cantidadPasos > 0 ? Mathf.Min(pasos.Count, desde + cantidadPasos) : pasos.Count;

        for (int i = desde; i < hasta; i++)
        {
            PasoReceta p = pasos[i];
            bool esActual = resaltarPasoActual && i == actual;

            // Linea principal
            string linea = (i + 1) + ". " + p.texto;
            if (mostrarProgreso && !p.completado)
            {
                if (p.TieneSubpasos)
                    linea += " (" + p.SubpasosHechos + "/" + p.subpasos.Count + ")";
                else if (p.accionesRequeridas > 1)
                    linea += " (" + p.accionesHechas + "/" + p.accionesRequeridas + ")";
            }
            AgregarLinea(sb, p.completado, linea, esActual);

            // Sub-pasos
            if (p.TieneSubpasos)
            {
                if (lineaEnBlancoAntesDeSubpasos)
                    sb.Append("\n");

                foreach (SubPasoReceta sub in p.subpasos)
                {
                    sb.Append("\n<indent=").Append(sangriaSubpasos.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("%>");
                    AgregarLinea(sb, sub.completado, sub.texto, false);
                    sb.Append("</indent>");
                }
            }

            if (i < hasta - 1)
                sb.Append(lineaEnBlancoEntrePasos ? "\n\n" : "\n");
        }

        texto.text = sb.ToString();
    }

    private void AgregarLinea(StringBuilder sb, bool hecho, string contenido, bool negrita)
    {
        sb.Append("<color=#").Append(hecho ? hexCompletado : hexPendiente).Append(">");
        if (negrita) sb.Append("<b>");

        sb.Append(hecho ? "[X] " : "[  ] ");

        if (hecho && tacharCompletados) sb.Append("<s>").Append(contenido).Append("</s>");
        else sb.Append(contenido);

        if (negrita) sb.Append("</b>");
        sb.Append("</color>");
    }
}