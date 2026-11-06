using System;

/// <summary>
/// Canal estatico por el que el resto del juego avisa "esto acaba de pasar".
/// Uso: RecetaEventos.Notificar(RecetaEventos.OllaConAgua);
///      RecetaEventos.Notificar(RecetaEventos.EnOlla("cebolla"));
/// </summary>
public static class RecetaEventos
{
    public const string OllaConAgua = "olla_con_agua";
    public const string IngredienteEnOlla = "ingrediente_en_olla"; // cualquier ingrediente (general)
    public const string LecheCocoEnOlla = "leche_coco_en_olla";
    public const string EstufaEncendida = "estufa_encendida";
    public const string PlatoEnMesa = "plato_en_mesa";
    public const string CucharonLleno = "cucharon_lleno";
    public const string CucharonServido = "cucharon_servido";

    /// <summary>Id de "este ingrediente concreto entro en la olla". EnOlla("cebolla") = "olla_cebolla"</summary>
    public static string EnOlla(string idIngrediente)
    {
        return "olla_" + idIngrediente.Trim().ToLowerInvariant();
    }

    public static event Action<string> AccionRealizada;

    public static void Notificar(string idAccion)
    {
        AccionRealizada?.Invoke(idAccion);
    }
}