using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Datos del diario y del bienestar, guardados en el propio móvil (diario.json en la carpeta privada de la app).
/// Cada entrada guarda cómo te sentías antes y después de una actividad, lo que escribiste y lo que hiciste.
/// </summary>
[Serializable]
public class EntradaDiario
{
    public string fecha;            // ISO 8601
    public int animoAntes;          // 1 (muy mal) .. 5 (muy bien)
    public int animoDespues;        // 0 si no hizo actividad
    public List<string> emociones = new List<string>();
    public string texto = "";
    public string actividad = "";   // "Respirar", "Caminar", "Leer", "Dibujar" o ""
    public int pasos, paginas, segundos;
    public string foto = "";        // ruta de la foto (página o dibujo)
}

[Serializable]
public class DiarioDatos
{
    public List<EntradaDiario> entradas = new List<EntradaDiario>();
    public int algasDoradas;        // recompensas del Bolsillo

    static DiarioDatos cache;
    static string Ruta => Path.Combine(Application.persistentDataPath, "diario.json");

    public static DiarioDatos Cargar()
    {
        if (cache != null) return cache;
        try { cache = File.Exists(Ruta) ? JsonUtility.FromJson<DiarioDatos>(File.ReadAllText(Ruta)) : new DiarioDatos(); }
        catch { cache = new DiarioDatos(); }
        if (cache == null) cache = new DiarioDatos();
        return cache;
    }

    public void Guardar()
    {
        try { File.WriteAllText(Ruta, JsonUtility.ToJson(this, true)); }
        catch (Exception e) { Debug.LogWarning("No se pudo guardar el diario: " + e.Message); }
    }

    public static readonly string[] Actividades = { "Leer", "Caminar", "Respirar", "Dibujar" };

    /// <summary>Cuánto sube el ánimo de media con cada actividad (null si aún no hay datos).</summary>
    public float? Mejora(string actividad)
    {
        var con = entradas.Where(e => e.actividad == actividad && e.animoAntes > 0 && e.animoDespues > 0).ToList();
        if (con.Count == 0) return null;
        return (float)con.Average(e => e.animoDespues - e.animoAntes);
    }

    /// <summary>Las actividades ordenadas de la que más te ayuda a la que menos (las que no has probado, en medio).</summary>
    public List<string> ActividadesOrdenadas()
        => Actividades.OrderByDescending(a => Mejora(a) ?? 0.5f).ToList();

    /// <summary>¿Lleva varios registros seguidos sintiéndose muy mal? (para ofrecer ayuda)</summary>
    public bool RachaMuyMal(int veces = 3)
    {
        if (entradas.Count < veces) return false;
        return entradas.Skip(entradas.Count - veces).All(e => e.animoAntes == 1);
    }

    public int TotalPaginas => entradas.Sum(e => e.paginas);
    public int TotalPasos => entradas.Sum(e => e.pasos);
    public int MinutosRespirando => entradas.Where(e => e.actividad == "Respirar").Sum(e => e.segundos) / 60;
    public int Dibujos => entradas.Count(e => e.actividad == "Dibujar" && !string.IsNullOrEmpty(e.foto));
}
