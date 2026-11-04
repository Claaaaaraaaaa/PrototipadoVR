using System;

/// <summary>
/// Canal estatico por el que el resto del juego avisa "esto acaba de pasar".
/// El RecetaManager escucha este canal y marca los pasos. Quien notifica
/// no necesita conocer al manager (si no hay manager en la escena, no pasa nada).
///
/// Uso desde cualquier script:  RecetaEventos.Notificar(RecetaEventos.IngredienteEnOlla);
/// </summary>
public static class RecetaEventos
{
    // Ids de acciones. Cada paso de la receta espera una de estas.
    public const string OllaConAgua = "olla_con_agua";
    public const string IngredienteEnOlla = "ingrediente_en_olla";
    public const string LecheCocoEnOlla = "leche_coco_en_olla";
    public const string EstufaEncendida = "estufa_encendida";
    public const string PlatoServido = "plato_servido";
    public const string PlatoEnMesa = "plato_en_mesa";
    public const string CucharonLleno = "cucharon_lleno";
    public const string CucharonServido = "cucharon_servido";

    public static event Action<string> AccionRealizada;

    public static void Notificar(string idAccion)
    {
        AccionRealizada?.Invoke(idAccion);
    }
}
