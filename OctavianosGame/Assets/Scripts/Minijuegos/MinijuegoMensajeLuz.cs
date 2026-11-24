using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MENTE · El mensaje de luz (Etapa 5).
/// Misma pantalla que "El lenguaje de la luz" (Etapa 3), con dos retos nuevos:
///  1. Escribir CUEVA para avisar a los pequeños de dónde esconderse.
///  2. La cuenta atrás del escondite: 3 2 1, con un bucle MIENTRAS (es el ejemplo while.c del repositorio de Destello).
/// No se puede perder (solo rendirse con ESC): cada reto tiene solución y la pista la explica entera.
/// </summary>
public class MinijuegoMensajeLuz : MinijuegoDestello
{
    protected override string Title => "MENTE · El mensaje de luz";
    protected override string Instructions =>
        "Las orcas no entienden la luz de los Octavianos: es el único mensaje que no pueden interceptar.\n" +
        "Gira las linternas con 1, 2, 3, 4 (o con clic): rojo ●  →  verde ■  →  azul ▲.\n" +
        "ENTER emite el destello · RETROCESO borra el último · R empieza de nuevo · H abre la chuleta.";
    protected override string MensajeVictoria => "¡Los pequeños han visto el mensaje y corren a la cueva!";

    protected override List<Reto> CrearRetos() => new List<Reto>
    {
        new Reto
        {
            titulo = "Reto 1 · ¿Dónde nos escondemos?",
            enunciado = "Escribe CUEVA con destellos para que los pequeños sepan adónde ir.",
            pista = "C = 2, U = 21, E = 4, V = 22, A = 0 (la Ñ también cuenta).  21 = 2·9 + 1·3 + 0  →  ● ▲ ■ ●\n" +
                    "Apila las cinco letras en orden y termina con PALABRA (verde ■ y luego ■ ■ ▲).",
            superado = r => r.error == null && r.salida.Trim() == "CUEVA",
            solucion = new[] { new[] { 0, 0, 0, 2 }, new[] { 0, 2, 1, 0 }, new[] { 0, 0, 1, 1 }, new[] { 0, 2, 1, 1 }, new[] { 0, 0, 0, 0 }, new[] { 1, 1, 1, 2 } },
        },
        new Reto
        {
            titulo = "Reto 2 · La cuenta atrás del escondite",
            enunciado = "Antes de esconderse, los pequeños cuentan hacia atrás.\nHaz que el programa escriba 3 2 1.",
            pista = "Con un bucle:  3   MIENTRAS   IMPRIMIR   1   RESTAR   FIN MIENTRAS\n" +
                    "MIENTRAS es azul ▲ y luego ● ■ ▲.   FIN MIENTRAS es azul ▲ y luego ▲ ■ ●.   RESTAR es verde ■ y luego ● ● ▲.",
            superado = r => r.error == null && r.salida.Trim() == "3 2 1",
            solucion = new[] { new[] { 0, 0, 1, 0 }, new[] { 2, 0, 1, 2 }, new[] { 1, 0, 0, 0 }, new[] { 0, 0, 0, 1 }, new[] { 1, 0, 0, 2 }, new[] { 2, 2, 1, 0 } },
        },
    };
}
