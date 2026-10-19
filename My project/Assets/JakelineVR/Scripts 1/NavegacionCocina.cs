
using System;
using System.Collections.Generic;
using UnityEngine;

public class NavegacionCocina : MonoBehaviour
{
    [Serializable]
    public class Zona
    {
        public string nombre;
        public Transform destino;
        public GameObject panel;
    }

    [Header("Jugador")]
    [SerializeField] private Transform cameraRig;

    [Header("Panel inicial")]
    [SerializeField] private GameObject panelInicio;

    [Header("Zonas de navegación")]
    [SerializeField] private List<Zona> zonas = new List<Zona>();

    public void IrABarra() => IrAZona("Barra");
    public void IrAEstufa() => IrAZona("Estufa");
    public void IrANevera() => IrAZona("Nevera");
    public void IrAAlacena() => IrAZona("Alacena");

    public void IrAZona(string nombreZona)
    {
        Zona zona = zonas.Find(z =>
            string.Equals(z.nombre, nombreZona,
                StringComparison.OrdinalIgnoreCase));

        if (zona == null)
        {
            Debug.LogError("No existe una zona llamada: " + nombreZona);
            return;
        }

        if (zona.destino == null)
        {
            Debug.LogError("Falta asignar el destino de: " + nombreZona);
            return;
        }

        Teletransportar(zona.destino);
        MostrarPanel(zona.panel);
    }

    private void MostrarPanel(GameObject panelActual)
    {
        if (panelInicio != null)
            panelInicio.SetActive(false);

        foreach (Zona zona in zonas)
        {
            if (zona.panel != null)
                zona.panel.SetActive(zona.panel == panelActual);
        }
    }

    private void Teletransportar(Transform destino)
    {
        if (cameraRig == null)
        {
            Debug.LogError("Falta asignar el Camera Rig.");
            return;
        }

        Camera cabeza = cameraRig.GetComponentInChildren<Camera>();

        if (cabeza == null)
        {
            Debug.LogError("No se encontró una cámara dentro del Camera Rig.");
            return;
        }

        Vector3 desplazamientoCabeza =
            cabeza.transform.position - cameraRig.position;

        Vector3 nuevaPosicionRig =
            destino.position - desplazamientoCabeza;

        nuevaPosicionRig.y = cameraRig.position.y;
        cameraRig.position = nuevaPosicionRig;
    }
}
