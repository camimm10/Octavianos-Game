using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Decide qué tipo de realidad aumentada puede usar este móvil:
///  - Completa: el móvil es compatible con ARCore (o ARKit en iPhone). Los objetos se quedan fijos en el suelo.
///  - Cámara: cualquier otro móvil. Se ve la cámara de fondo y los objetos encima; con el giroscopio,
///    los objetos se quedan en su dirección cuando giras el móvil (pero no se anclan al suelo).
/// ARCore está marcado como OPCIONAL en el proyecto, así que la app se instala en cualquier Android.
/// </summary>
public static class ModoRA
{
    public enum Tipo { Comprobando, Completa, Camara }

    public static Tipo Actual { get; private set; } = Tipo.Comprobando;

    /// <summary>Comprueba el móvil y llama a alListo con el resultado (como mucho tarda unos segundos).</summary>
    public static IEnumerator Detectar(Action<Tipo> alListo)
    {
#if UNITY_EDITOR
        // En el ordenador no hay ARCore: se prueba el modo cámara (con la webcam si la hay)
        Actual = Tipo.Camara;
        alListo?.Invoke(Actual);
        yield break;
#else
        if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
        {
            float limite = Time.realtimeSinceStartup + 8f;
            var comprobacion = ARSession.CheckAvailability();
            while (comprobacion.MoveNext())
            {
                if (Time.realtimeSinceStartup > limite) break;
                yield return comprobacion.Current;
            }
        }
        // Si el móvil es compatible pero le falta el servicio de RA, Google lo ofrece para instalar;
        // si el usuario dice que no, seguimos en modo cámara.
        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            var instalar = ARSession.Install();
            while (instalar.MoveNext()) yield return instalar.Current;
        }
        Actual = ARSession.state == ARSessionState.Ready || ARSession.state == ARSessionState.SessionInitializing
                 || ARSession.state == ARSessionState.SessionTracking
            ? Tipo.Completa
            : Tipo.Camara;
        alListo?.Invoke(Actual);
#endif
    }
}
