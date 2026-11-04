using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra los pasos de la receta como texto con un check al lado.
/// Ponlo en el objeto que tiene el TextMeshPro (o asigna uno en "texto").
/// Se refresca solo cuando cambia algun paso.
///
///   [ ] 1. Paso pendiente
///   [X] 2. Paso completado (verde y tachado)
/// </summary>
public class RecetaPasosUI : MonoBehaviour
{
    [SerializeField] private TMP_Text texto;
    [SerializeField] private string titulo = "";

    [Header("Que pasos muestra (para repartir en varias paginas)")]
    [Tooltip("Primer paso que muestra este texto (1 = el primero).")]
    [Min(1)] [SerializeField] private int primerPaso = 1;
    [Tooltip("Cuantos pasos muestra. 0 = todos desde el primero indicado.")]
    [Min(0)] [SerializeField] private int cantidadPasos = 0;

    [Header("Estilo")]

    [SerializeField] private Color colorPendiente = new Color(0.15f, 0.1f, 0.05f);
    [SerializeField] private Color colorCompletado = new Color(0.1f, 0.45f, 0.15f);
    [SerializeField] private bool tacharCompletados = true;

    private RecetaManager manager;

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

        string hexPendiente = ColorUtility.ToHtmlStringRGB(colorPendiente);
        string hexCompletado = ColorUtility.ToHtmlStringRGB(colorCompletado);

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(titulo))
            sb.Append(titulo).Append("\n\n");

        var pasos = manager.Pasos;
        int desde = Mathf.Max(0, primerPaso - 1);
        int hasta = cantidadPasos > 0 ? Mathf.Min(pasos.Count, desde + cantidadPasos) : pasos.Count;

        for (int i = desde; i < hasta; i++)
        {
            PasoReceta p = pasos[i];

            if (p.completado)
            {
                sb.Append("<color=#").Append(hexCompletado).Append(">[X] ").Append(i + 1).Append(". ");
                if (tacharCompletados) sb.Append("<s>").Append(p.texto).Append("</s>");
                else sb.Append(p.texto);
                sb.Append("</color>");
            }
            else
            {
                sb.Append("<color=#").Append(hexPendiente).Append(">[  ] ").Append(i + 1).Append(". ")
                  .Append(p.texto).Append("</color>");
            }

            if (i < hasta - 1) sb.Append("\n\n");
        }

        texto.text = sb.ToString();
    }
}
