using System;
using UnityEngine;

/// <summary>Cada valor corresponde a un hijo del objeto "Efectos" con el mismo nombre.</summary>
public enum Sonido
{
    Cortar,
    PrenderEstufa,
    AguaHirviendo,
    SopaEnPlato,
    PasarPagina,
    Celebracion,
    AguaLlave,
    AguaEnOlla
}

[Serializable]
public class AjusteSonido
{
    public Sonido sonido;

    [Tooltip("Segundos del inicio del audio que se saltan. Sube este valor si el sonido llega tarde.")]
    [Range(0f, 2f)] public float inicio = 0f;

    [Tooltip("Segundos de espera antes de sonar. Sube este valor si el sonido llega demasiado pronto.")]
    [Range(0f, 2f)] public float retraso = 0f;

    [Range(0f, 1f)] public float volumen = 1f;

    [Tooltip("Segundos que suena antes de apagarse con un fundido. 0 = el audio completo.")]
    [Range(0f, 30f)] public float duracion = 0f;
}

/// <summary>
/// Tiempos y volumenes de los efectos de sonido. Al ser un asset, lo que se cambie
/// durante el Play se conserva al salir, y se aplica la siguiente vez que suene cada sonido.
/// </summary>
[CreateAssetMenu(fileName = "AjustesSonidos", menuName = "Cocina/Ajustes de sonidos")]
public class AjustesSonidos : ScriptableObject
{
    [Tooltip("Escribe en la consola cada sonido que se reproduce.")]
    public bool mostrarDebug = true;

    public AjusteSonido[] ajustes = new AjusteSonido[0];

    public AjusteSonido Buscar(Sonido sonido)
    {
        foreach (AjusteSonido ajuste in ajustes)
            if (ajuste.sonido == sonido) return ajuste;
        return null;
    }
}
