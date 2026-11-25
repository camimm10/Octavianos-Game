using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Datos del diario y del bienestar, guardados en el propio móvil (diario.json en la carpeta privada de la app).
/// Cada entrada guarda cómo te sentías antes y después de una actividad, lo que escribiste y lo que hiciste.
/// La colección guarda las fotos de libros, cuadros, campos limpios y dibujos.
/// </summary>
[Serializable]
public class EntradaDiario
{
    public string fecha;            // ISO 8601
    public int animoAntes;          // 1 (muy mal) .. 5 (muy bien); 0 si se fue directo a una actividad
    public int animoDespues;        // 0 si no hizo actividad
    public List<string> emociones = new List<string>();
    public string texto = "";
    public string actividad = "";   // "Leer", "Caminar", "Respirar", "Dibujar", "Idioma"…
    public int pasos, paginas, segundos;
    public float metros;            // distancia caminando (GPS)
    public string foto = "";        // foto principal (página donde empiezas, dibujo…)
    public string fotoFin = "";     // página donde terminas
    public string titulo = "";      // título del libro
    public int paginaInicio, paginaFin;
    public string nota = "";        // opinión o apuntes ("comparte tu opinión con Octavio")

    public DateTime Fecha => DiarioDatos.LeerFecha(fecha);
}

[Serializable]
public class ItemColeccion
{
    public string tipo;             // "Libro", "Cuadro", "Campo", "Dibujo"
    public string titulo = "";
    public string autor = "";
    public string nota = "";
    public string foto = "";        // portada / cuadro / antes / dibujo
    public string foto2 = "";       // página final / después
    public string fecha;
    public int numero;              // páginas leídas (libros) o minutos (campos)
    public bool verificado;         // campos: comprobado con GPS y tiempo
}

[Serializable]
public class DiarioDatos
{
    public List<EntradaDiario> entradas = new List<EntradaDiario>();
    public List<ItemColeccion> coleccion = new List<ItemColeccion>();
    public int algasDoradas;        // recompensas del Bolsillo

    static DiarioDatos cache;
    static string Ruta => Path.Combine(Application.persistentDataPath, "diario.json");

    public static DiarioDatos Cargar()
    {
        if (cache != null) return cache;
        try { cache = File.Exists(Ruta) ? JsonUtility.FromJson<DiarioDatos>(File.ReadAllText(Ruta)) : new DiarioDatos(); }
        catch { cache = new DiarioDatos(); }
        if (cache == null) cache = new DiarioDatos();
        if (cache.entradas == null) cache.entradas = new List<EntradaDiario>();
        if (cache.coleccion == null) cache.coleccion = new List<ItemColeccion>();
        return cache;
    }

    public void Guardar()
    {
        try { File.WriteAllText(Ruta, JsonUtility.ToJson(this, true)); }
        catch (Exception e) { Debug.LogWarning("No se pudo guardar el diario: " + e.Message); }
    }

    public static DateTime LeerFecha(string s)
    {
        if (!string.IsNullOrEmpty(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
            return d.Kind == DateTimeKind.Utc ? d.ToLocalTime() : d;
        return DateTime.MinValue;
    }

    public static readonly string[] Actividades =
        { "Leer", "Caminar", "Respirar", "Dibujar", "Idioma", "Instrumento", "Meditar", "Cocinar", "Llamar" };

    /// <summary>Cuánto sube el ánimo de media con cada actividad (null si aún no hay datos).</summary>
    public float? Mejora(string actividad)
    {
        var con = entradas.Where(e => e.actividad == actividad && e.animoAntes > 0 && e.animoDespues > 0).ToList();
        if (con.Count == 0) return null;
        return (float)con.Average(e => e.animoDespues - e.animoAntes);
    }

    /// <summary>Las actividades ordenadas de la que más te ayuda a la que menos (las que no has probado, en medio).</summary>
    public List<string> ActividadesOrdenadas()
        => Actividades.Select((a, i) => (a, i)).OrderByDescending(x => Mejora(x.a) ?? 0.5f).ThenBy(x => x.i).Select(x => x.a).ToList();

    /// <summary>¿Lleva varios registros seguidos sintiéndose muy mal? (para ofrecer ayuda)</summary>
    public bool RachaMuyMal(int veces = 3)
    {
        var con = entradas.Where(e => e.animoAntes > 0).ToList();
        if (con.Count < veces) return false;
        return con.Skip(con.Count - veces).All(e => e.animoAntes == 1);
    }

    // ---------- Totales
    public int TotalPaginas => entradas.Sum(e => e.paginas);
    public int TotalPasos => entradas.Sum(e => e.pasos);
    public float TotalKm => entradas.Sum(e => e.metros) / 1000f;
    public int MinutosRespirando => entradas.Where(e => e.actividad == "Respirar" || e.actividad == "Meditar").Sum(e => e.segundos) / 60;
    public int Cuantos(string tipo) => coleccion.Count(c => c.tipo == tipo);

    // ---------- Pasos por día
    public int PasosDia(DateTime dia) => entradas.Where(e => e.pasos > 0 && e.Fecha.Date == dia.Date).Sum(e => e.pasos);
    public float MetrosDia(DateTime dia) => entradas.Where(e => e.Fecha.Date == dia.Date).Sum(e => e.metros);
    public int PasosSemana()
    {
        var hoy = DateTime.Now.Date;
        return Enumerable.Range(0, 7).Sum(i => PasosDia(hoy.AddDays(-i)));
    }
    public int MejorDia(out DateTime dia)
    {
        dia = DateTime.MinValue;
        var g = entradas.Where(e => e.pasos > 0).GroupBy(e => e.Fecha.Date).Select(x => (x.Key, x.Sum(e => e.pasos))).OrderByDescending(x => x.Item2).FirstOrDefault();
        dia = g.Key;
        return g.Item2;
    }
    public int SesionesCaminando => entradas.Count(e => e.actividad == "Caminar");

    // ---------- Colección
    public ItemColeccion Libro(string titulo) =>
        string.IsNullOrWhiteSpace(titulo) ? null
        : coleccion.FirstOrDefault(c => c.tipo == "Libro" && string.Equals(c.titulo.Trim(), titulo.Trim(), StringComparison.OrdinalIgnoreCase));

    public string UltimoLibro => entradas.LastOrDefault(e => e.actividad == "Leer" && !string.IsNullOrWhiteSpace(e.titulo))?.titulo ?? "";
}
