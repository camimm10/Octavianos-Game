using UnityEngine;

/// <summary>
/// CORAZÓN · Es solo un juego (Etapa 5).
/// Misma pantalla que "¿Qué te pasa, Octavio?" (Etapa 3). Los pequeños están asustados y Octavio
/// tiene que mantenerlos tranquilos convirtiendo la huida en un juego del escondite.
/// Cada respuesta: +2 si los calma y los guía, 0 si no ayuda mucho, -1 si los asusta más.
/// Hace falta 5 de 8: contestando siempre con calma se gana en cualquier partida (8 puntos).
/// La respuesta que calma es siempre la primera de cada lista (el piloto automático la usa);
/// en pantalla aparecen barajadas.
/// </summary>
public class MinijuegoCalma : MinijuegoConfianza
{
    static readonly Color Lapa = new Color(0.95f, 0.6f, 0.4f);
    static readonly Color Coralina = new Color(1f, 0.55f, 0.75f);
    static readonly Color Burbuja = new Color(0.5f, 0.8f, 1f);
    static readonly Color Perlita = new Color(1f, 0.88f, 0.45f);

    protected override string Title => "CORAZÓN · Es solo un juego";
    protected override string Instructions =>
        "Los pequeños están asustados y no saben qué pasa.\n" +
        "Elige qué decirle a cada uno con 1, 2, 3 (o con clic). Si están tranquilos, se esconderán sin hacer ruido.\n" +
        "Necesitas " + needed + " puntos de calma.";
    protected override string MensajeVictoria => "Los pequeños se esconden en silencio, riéndose bajito.";
    protected override string MensajeRendirse => "Los pequeños siguen asustados…";
    protected override string MensajeDerrota(int c) => $"Calma {c} de {needed}… los pequeños siguen asustados.";
    protected override string NombreMarcador => "Calma";

    protected override Pregunta[] CrearPreguntas() => new[]
    {
        new Pregunta
        {
            quien = "Lapa", color = Lapa,
            frase = "Abuelo Octavio, ¿por qué hay tanto ruido? Tengo miedo…",
            respuestas = new[]
            {
                new Respuesta("Vamos a jugar al escondite más grande del océano. El que haga menos ruido, gana.", 2,
                              "Lapa se tapa la boca con un tentáculo, muy concentrada. «¡Voy a ganar yo!»"),
                new Respuesta("No es nada. Tú no te preocupes.", 0,
                              "Lapa asiente, pero sigue temblando."),
                new Respuesta("Son las orcas. Han venido a por nosotros.", -1,
                              "Lapa rompe a llorar."),
            },
        },
        new Pregunta
        {
            quien = "Coralina", color = Coralina,
            frase = "¿Y mi mamá? ¿Ella también juega?",
            respuestas = new[]
            {
                new Respuesta("Tu mamá también está escondida. Cuando acabe el juego, iremos a buscarla juntos.", 2,
                              "Coralina sonríe un poco y te da la mano."),
                new Respuesta("Ahora no puedo hablar de eso.", 0,
                              "Coralina se queda callada, sin entender."),
                new Respuesta("No lo sé. Puede que no vuelvas a verla.", -1,
                              "Coralina se queda pálida de miedo."),
            },
        },
        new Pregunta
        {
            quien = "Burbuja", color = Burbuja,
            frase = "¡Yo quiero salir a ver a las orcas! ¡Son enormes!",
            respuestas = new[]
            {
                new Respuesta("Ellas son las que buscan. Si te ven, pierdes. ¡Ya verás qué bien te escondes!", 2,
                              "Burbuja se esconde detrás de una roca, muy serio. «¡No me encontrarán!»"),
                new Respuesta("No, quédate aquí.", 0,
                              "Burbuja protesta, pero se queda… por ahora."),
                new Respuesta("¡Si sales te comerán!", -1,
                              "Burbuja grita de miedo. Demasiado alto."),
            },
        },
        new Pregunta
        {
            quien = "Perlita", color = Perlita,
            frase = "¿Cuánto dura el juego? Ya me quiero ir a casa.",
            respuestas = new[]
            {
                new Respuesta("Un ratito. Cuenta las burbujas del techo de la cueva y, cuando acabes, te vengo a buscar.", 2,
                              "Perlita empieza a contar burbujas en voz muy bajita: «una, dos, tres…»"),
                new Respuesta("No lo sé, espera.", 0,
                              "Perlita suspira y se acurruca."),
                new Respuesta("A lo mejor ya no hay casa a la que volver.", -1,
                              "Perlita se echa a llorar en silencio."),
            },
        },
    };
}
