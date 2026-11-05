using System.Collections.Generic;
using UnityEngine;

public class DetectorOlla : MonoBehaviour
{
    [SerializeField] private Transform puntoDestinoIngredientes;

    [Header("Receta")]
    [Tooltip("Solo acepta ingredientes si la olla ya tiene agua (paso 1 hecho)")]
    [SerializeField] private bool soloSiTieneAgua = true;
    [SerializeField] private bool mostrarDebug = true;

    private OllaEstado olla;
    private readonly HashSet<GameObject> yaIncorporados = new HashSet<GameObject>();
    private readonly HashSet<GameObject> avisadosSinPicar = new HashSet<GameObject>();

    private void Awake()
    {
        olla = GetComponentInParent<OllaEstado>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (puntoDestinoIngredientes == null)
        {
            Debug.LogError("Falta asignar PuntoDestinoIngredientes.", this);
            return;
        }

        // BLOQUE 1: Ingredientes que hay que picar
        IngredientePicable picable = other.GetComponentInParent<IngredientePicable>();
        if (picable == null)
            picable = other.GetComponentInChildren<IngredientePicable>();

        if (picable != null)
        {
            if (picable.DentroDeOlla)
                return;

            if (!picable.ListoParaOlla)
            {
                if (mostrarDebug && avisadosSinPicar.Add(picable.gameObject))
                    Debug.Log("[DetectorOlla] " + picable.name + " aun no esta picado.", this);
                return;
            }

            if (PuedeAceptar(picable.gameObject) &&
                picable.IncorporarAlaOlla(puntoDestinoIngredientes))
            {
                Registrar(picable.gameObject);
            }
            return;
        }

        // BLOQUE 2: Ingredientes que van enteros
        IngredienteEntero entero = other.GetComponentInParent<IngredienteEntero>();
        if (entero == null)
            entero = other.GetComponentInChildren<IngredienteEntero>();

        if (entero != null && !entero.DentroDeOlla)
        {
            if (PuedeAceptar(entero.gameObject) &&
                entero.IncorporarAlaOlla(puntoDestinoIngredientes))
            {
                Registrar(entero.gameObject);
            }
        }
    }

    private bool PuedeAceptar(GameObject ingrediente)
    {
        if (yaIncorporados.Contains(ingrediente))
            return false;

        if (soloSiTieneAgua && olla != null && !olla.TieneAgua)
        {
            if (mostrarDebug)
                Debug.Log("[DetectorOlla] La olla no tiene agua, no acepta " + ingrediente.name, this);
            return false;
        }

        return true;
    }

    private void Registrar(GameObject ingrediente)
    {
        yaIncorporados.Add(ingrediente);
        RecetaEventos.Notificar(RecetaEventos.IngredienteEnOlla);

        if (mostrarDebug)
            Debug.Log("[DetectorOlla] Ingrediente en la olla: " + ingrediente.name +
                      " (total: " + yaIncorporados.Count + ")", this);
    }

    public void Reiniciar()
    {
        yaIncorporados.Clear();
        avisadosSinPicar.Clear();
    }
}