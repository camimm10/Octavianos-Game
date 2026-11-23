using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Destello: el lenguaje de programación esotérico del equipo (repositorio LYP-2026, carpeta DESTELLO).
/// Este archivo es una traducción fiel a C# del intérprete de destello.py, para que los enigmas
/// del juego sean programas de Destello de verdad.
///
/// Cada destello son 4 linternas. Cada linterna: 0 = rojo ●, 1 = verde ■, 2 = azul ▲.
///  - Linterna 1 = tipo: rojo dato, verde operación, azul control.
///  - Linternas 2-4 = número en base 3 (×9, ×3, ×1), de 0 a 26 (las 27 letras A…Z con Ñ).
/// Es un lenguaje de pila (notación polaca inversa): 7 2 RESTO → 1.
/// </summary>
public static class DestelloVM
{
    public const string ABC = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ"; // A = 0 … Z = 26
    public static readonly string[] Simbolo = { "●", "■", "▲" };
    public static readonly string[] Color = { "rojo", "verde", "azul" };
    public static readonly string[] Forma = { "círculo", "cuadrado", "triángulo" };
    public static readonly string[] Tipo = { "dato", "operación", "control" };

    // Linterna 1 verde = operación
    static readonly Dictionary<(int, int, int), string> Operaciones = new Dictionary<(int, int, int), string>
    {
        { (0, 0, 0), "IMPRIMIR" }, { (1, 1, 1), "LETRA" },   { (2, 2, 2), "ESPACIO" },
        { (0, 0, 1), "SUMAR" },    { (0, 0, 2), "RESTAR" },  { (0, 1, 0), "MULTIPLICAR" },
        { (1, 0, 0), "DUPLICAR" }, { (1, 0, 1), "CAMBIAR" }, { (2, 0, 0), "BORRAR" },
        { (1, 2, 1), "ASCII" },    { (0, 2, 0), "RESTO" },   { (0, 1, 2), "ROTAR" },
        { (1, 1, 2), "PALABRA" },  { (1, 1, 0), "UNIR" },    { (1, 0, 2), "ENCIMA" },
    };

    // Linterna 1 azul = control
    static readonly Dictionary<(int, int, int), string> Control = new Dictionary<(int, int, int), string>
    {
        { (0, 0, 0), "SI" }, { (1, 1, 1), "SI NO" }, { (2, 2, 2), "FIN SI" },
        { (0, 1, 2), "MIENTRAS" }, { (2, 1, 0), "FIN MIENTRAS" }, { (2, 0, 2), "PARAR" },
    };

    public class ErrorDestello : Exception
    {
        public ErrorDestello(string msg) : base(msg) { }
    }

    /// <summary>Resultado de ejecutar un programa.</summary>
    public class Resultado
    {
        public string salida = "";
        public List<int> pila = new List<int>();
        public string error; // null si todo ha ido bien
    }

    // ---------- Lectura de un destello ----------

    public static int Valor(int[] d) => d[1] * 9 + d[2] * 3 + d[3];

    /// <summary>Nombre de la instrucción ("DATO", "RESTO", "SI"…) o null si no significa nada.</summary>
    public static string Nombre(int[] d)
    {
        if (d[0] == 0) return "DATO";
        var tabla = d[0] == 1 ? Operaciones : Control;
        return tabla.TryGetValue((d[1], d[2], d[3]), out var n) ? n : null;
    }

    public static string Simbolos(int[] d) => Simbolo[d[0]] + " " + Simbolo[d[1]] + Simbolo[d[2]] + Simbolo[d[3]];

    public static string Etiqueta(int[] d)
    {
        string n = Nombre(d);
        if (n == "DATO")
        {
            int v = Valor(d);
            return $"dato {v} (letra {ABC[v]})";
        }
        return n ?? "??? no significa nada";
    }

    /// <summary>Destello que hace falta para una instrucción (útil para pistas y para el piloto automático).</summary>
    public static int[] Codificar(string nombre, int valor = 0)
    {
        if (nombre == "DATO") return new[] { 0, valor / 9, valor / 3 % 3, valor % 3 };
        foreach (var kv in Operaciones) if (kv.Value == nombre) return new[] { 1, kv.Key.Item1, kv.Key.Item2, kv.Key.Item3 };
        foreach (var kv in Control) if (kv.Value == nombre) return new[] { 2, kv.Key.Item1, kv.Key.Item2, kv.Key.Item3 };
        throw new ArgumentException("Instrucción desconocida: " + nombre);
    }

    // ---------- Ejecución ----------

    /// <summary>Busca qué SI / SI NO / FIN SI y MIENTRAS / FIN MIENTRAS van juntos.</summary>
    static Dictionary<int, (int sino, int fin)> Emparejar(IList<int[]> prog, out Dictionary<int, int> bucles)
    {
        var pareja = new Dictionary<int, (int, int)>();
        bucles = new Dictionary<int, int>();
        var si = new List<int[]>();      // [inicio, sino]
        var mientras = new List<int>();
        for (int i = 0; i < prog.Count; i++)
        {
            string n = Nombre(prog[i]);
            if (n == null) throw new ErrorDestello($"Destello {i + 1} ({Simbolos(prog[i])}): no significa nada.");
            if (n == "SI") si.Add(new[] { i, -1 });
            else if (n == "SI NO")
            {
                if (si.Count == 0) throw new ErrorDestello($"Destello {i + 1}: SI NO sin un SI antes.");
                si[si.Count - 1][1] = i;
            }
            else if (n == "FIN SI")
            {
                if (si.Count == 0) throw new ErrorDestello($"Destello {i + 1}: FIN SI sin un SI antes.");
                var s = si[si.Count - 1]; si.RemoveAt(si.Count - 1);
                pareja[s[0]] = (s[1], i);
                if (s[1] >= 0) pareja[s[1]] = (-1, i);
            }
            else if (n == "MIENTRAS") mientras.Add(i);
            else if (n == "FIN MIENTRAS")
            {
                if (mientras.Count == 0) throw new ErrorDestello($"Destello {i + 1}: FIN MIENTRAS sin un MIENTRAS antes.");
                int ini = mientras[mientras.Count - 1]; mientras.RemoveAt(mientras.Count - 1);
                bucles[ini] = i; bucles[i] = ini;
            }
        }
        if (si.Count > 0) throw new ErrorDestello($"Destello {si[0][0] + 1}: este SI nunca se cierra (falta FIN SI).");
        if (mientras.Count > 0) throw new ErrorDestello($"Destello {mientras[0] + 1}: este MIENTRAS nunca se cierra (falta FIN MIENTRAS).");
        return pareja;
    }

    /// <summary>Escribe la salida con las mismas reglas de espacios que destello.py.</summary>
    class Salida
    {
        public readonly StringBuilder texto = new StringBuilder();
        string ultimo; // null, "num", "letra", "esp"
        public void Numero(int n) { if (ultimo == "num" || ultimo == "letra") texto.Append(' '); texto.Append(n); ultimo = "num"; }
        public void Letras(string s) { if (ultimo == "num") texto.Append(' '); texto.Append(s); ultimo = "letra"; }
        public void Caracter(char c) { texto.Append(c); ultimo = c == ' ' ? "esp" : "letra"; }
        public void Espacio() { texto.Append(' '); ultimo = "esp"; }
    }

    /// <summary>Ejecuta un programa. Nunca lanza excepciones: si hay un error, lo devuelve en Resultado.error.</summary>
    public static Resultado Ejecutar(IList<int[]> prog, int maxPasos = 10000)
    {
        var r = new Resultado();
        var pila = r.pila;
        var outp = new Salida();
        int pc = 0, pasos = 0;
        try
        {
            var pareja = Emparejar(prog, out var bucles);

            while (pc < prog.Count)
            {
                if (++pasos > maxPasos) throw new ErrorDestello("Demasiados pasos: parece un bucle que no termina.");
                var d = prog[pc];
                string n = Nombre(d);
                int sig = pc + 1;
                switch (n)
                {
                    case "DATO": pila.Add(Valor(d)); break;
                    case "IMPRIMIR":
                        if (pila.Count == 0) throw new ErrorDestello($"Destello {pc + 1}: IMPRIMIR con la pila vacía.");
                        outp.Numero(pila[pila.Count - 1]); break;
                    case "LETRA":
                    {
                        int v = Sacar(pila, pc, 1, n)[0];
                        if (v < 0 || v > 26) throw new ErrorDestello($"Destello {pc + 1}: {v} no es una letra (0 a 26).");
                        outp.Letras(ABC[v].ToString()); break;
                    }
                    case "ESPACIO": outp.Espacio(); break;
                    case "PALABRA":
                        if (pila.Count == 0) throw new ErrorDestello($"Destello {pc + 1}: PALABRA con la pila vacía.");
                        if (pila.Any(v => v < 0 || v > 26)) throw new ErrorDestello($"Destello {pc + 1}: hay números que no son letras.");
                        outp.Letras(new string(pila.Select(v => ABC[v]).ToArray())); pila.Clear(); break;
                    case "SUMAR": case "RESTAR": case "MULTIPLICAR": case "RESTO": case "UNIR": case "ASCII":
                    {
                        var ab = Sacar(pila, pc, 2, n); int a = ab[0], b = ab[1];
                        if (n == "SUMAR") pila.Add(a + b);
                        else if (n == "RESTAR") pila.Add(a - b);
                        else if (n == "MULTIPLICAR") pila.Add(a * b);
                        else if (n == "UNIR") pila.Add(a * 10 + b);
                        else if (n == "RESTO")
                        {
                            if (b == 0) throw new ErrorDestello($"Destello {pc + 1}: RESTO entre 0.");
                            pila.Add(((a % b) + b) % b); // como el % de Python (resultado con el signo del divisor)
                        }
                        else
                        {
                            int v = a * 27 + b;
                            if (v < 0 || v > 255) throw new ErrorDestello($"Destello {pc + 1}: {v} no es un código ASCII (0 a 255).");
                            outp.Caracter(v < 128 ? (char)v : '?'); // la tabla extendida 128-255 no se usa en el juego
                        }
                        break;
                    }
                    case "DUPLICAR":
                        if (pila.Count == 0) throw new ErrorDestello($"Destello {pc + 1}: DUPLICAR con la pila vacía.");
                        pila.Add(pila[pila.Count - 1]); break;
                    case "CAMBIAR": { var ab = Sacar(pila, pc, 2, n); pila.Add(ab[1]); pila.Add(ab[0]); break; }
                    case "BORRAR": Sacar(pila, pc, 1, n); break;
                    case "ROTAR": { var abc = Sacar(pila, pc, 3, n); pila.Add(abc[1]); pila.Add(abc[2]); pila.Add(abc[0]); break; }
                    case "ENCIMA":
                        if (pila.Count < 2) throw new ErrorDestello($"Destello {pc + 1}: ENCIMA necesita 2 números.");
                        pila.Add(pila[pila.Count - 2]); break;
                    case "SI":
                    {
                        int v = Sacar(pila, pc, 1, n)[0];
                        if (v == 0) { var p = pareja[pc]; sig = (p.sino >= 0 ? p.sino : p.fin) + 1; }
                        break;
                    }
                    case "SI NO": sig = pareja[pc].fin + 1; break;
                    case "MIENTRAS":
                        if (pila.Count == 0 || pila[pila.Count - 1] == 0) sig = bucles[pc] + 1;
                        break;
                    case "FIN MIENTRAS": sig = bucles[pc]; break;
                    case "PARAR": sig = prog.Count; break;
                }
                pc = sig;
            }
        }
        catch (ErrorDestello e)
        {
            r.error = e.Message;
        }
        r.salida = outp.texto.ToString();
        return r;
    }

    static int[] Sacar(List<int> pila, int pc, int cuantos, string quien)
    {
        if (pila.Count < cuantos) throw new ErrorDestello($"Destello {pc + 1}: {quien} necesita {cuantos} número(s) en la pila.");
        var res = pila.GetRange(pila.Count - cuantos, cuantos).ToArray();
        pila.RemoveRange(pila.Count - cuantos, cuantos);
        return res;
    }

    // ---------- Transcripción a C (una línea por destello, como destello.py) ----------

    public static string LineaC(int[] d)
    {
        const string bin = "{{ int b = desapilar(), a = desapilar(); {0} }}";
        switch (Nombre(d))
        {
            case "DATO": return $"apilar({Valor(d)});";
            case "IMPRIMIR": return "imprimir();";
            case "LETRA": return "letra();";
            case "ESPACIO": return "espacio();";
            case "PALABRA": return "palabra();";
            case "SUMAR": return string.Format(bin, "apilar(a + b);");
            case "RESTAR": return string.Format(bin, "apilar(a - b);");
            case "MULTIPLICAR": return string.Format(bin, "apilar(a * b);");
            case "RESTO": return string.Format(bin, "apilar(a % b);");
            case "UNIR": return string.Format(bin, "apilar(a * 10 + b);");
            case "ASCII": return string.Format(bin, "ascii(a * 27 + b);");
            case "DUPLICAR": return "apilar(cima());";
            case "CAMBIAR": return string.Format(bin, "apilar(b); apilar(a);");
            case "BORRAR": return "desapilar();";
            case "ROTAR": return "{ int c = desapilar(), b = desapilar(), a = desapilar(); apilar(b); apilar(c); apilar(a); }";
            case "ENCIMA": return "apilar(pila[cima_pila - 2]);";
            case "SI": return "if (desapilar() != 0) {";
            case "SI NO": return "} else {";
            case "FIN SI": return "}";
            case "MIENTRAS": return "while (cima() != 0) {";
            case "FIN MIENTRAS": return "}";
            case "PARAR": return "goto fin;";
            default: return "/* no significa nada */";
        }
    }

    /// <summary>Lista de todas las instrucciones (para la chuleta del juego).</summary>
    public static IEnumerable<(int[] destello, string nombre)> Instrucciones()
    {
        foreach (var kv in Operaciones) yield return (new[] { 1, kv.Key.Item1, kv.Key.Item2, kv.Key.Item3 }, kv.Value);
        foreach (var kv in Control) yield return (new[] { 2, kv.Key.Item1, kv.Key.Item2, kv.Key.Item3 }, kv.Value);
    }
}
