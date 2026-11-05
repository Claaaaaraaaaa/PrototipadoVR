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
                Registrar(picable.gameObject, picable.IdIngrediente);
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
                Registrar(entero.gameObject, entero.IdIngrediente);
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

    private void Registrar(GameObject ingrediente, string idIngrediente)
    {
        yaIncorporados.Add(ingrediente);

        // Aviso general (por si algun paso cuenta "cualquier ingrediente")
        RecetaEventos.Notificar(RecetaEventos.IngredienteEnOlla);

        // Aviso del ingrediente concreto: marca su paso en el libro
        if (string.IsNullOrWhiteSpace(idIngrediente))
        {
            Debug.LogWarning("[DetectorOlla] " + ingrediente.name +
                             " no tiene 'Id Ingrediente'. Asignalo en el Inspector.", ingrediente);
        }
        else
        {
            RecetaEventos.Notificar(RecetaEventos.EnOlla(idIngrediente));
        }

        if (mostrarDebug)
            Debug.Log("[DetectorOlla] En la olla: " + ingrediente.name +
                      " (id: " + idIngrediente + ")", this);
    }

    public void Reiniciar()
    {
        yaIncorporados.Clear();
        avisadosSinPicar.Clear();
    }
}