using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utilidades para hacer mallas "de dibujos animados" por código:
/// superficies de revolución (torno), polígonos extruidos y cubos de aristas redondeadas.
/// </summary>
public static class MallasCartoon
{
    // ---------------------------------------------------------------- Torno

    /// <summary>
    /// Superficie de revolución alrededor del eje Y. perfil: (altura, radio), de abajo arriba.
    /// rot: giro final de la malla (p. ej. para tumbarla a lo largo de Z). escalaY: aplastar después de girar.
    /// </summary>
    public static Mesh Torno(IList<Vector2> perfil, int radial, string nombre, Quaternion? rot = null, Vector3? escala = null, int suavizado = 3)
    {
        var prof = Suavizar(perfil, suavizado);
        var v = new List<Vector3>();
        foreach (var q in prof)
            for (int j = 0; j <= radial; j++)
            {
                float a = j * Mathf.PI * 2f / radial;
                v.Add(new Vector3(Mathf.Sin(a) * q.y, q.x, Mathf.Cos(a) * q.y));
            }
        var t = new List<int>();
        int fila = radial + 1;
        for (int i = 0; i < prof.Count - 1; i++)
            for (int j = 0; j < radial; j++)
            {
                int a = i * fila + j, b = a + 1, c = a + fila, d = c + 1;
                t.AddRange(new[] { a, d, c, a, b, d });   // hacia fuera (comprobado)
            }
        var r = rot ?? Quaternion.identity; var e = escala ?? Vector3.one;
        for (int i = 0; i < v.Count; i++) v[i] = Vector3.Scale(r * v[i], e);
        var m = new Mesh { name = nombre };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        CoserCostura(m, fila, prof.Count);
        return m;
    }

    static List<Vector2> Suavizar(IList<Vector2> p, int pasos)
    {
        if (pasos <= 1) return p.ToList();
        var r = new List<Vector2>();
        for (int i = 0; i < p.Count - 1; i++)
        {
            var p0 = p[Mathf.Max(0, i - 1)]; var p1 = p[i]; var p2 = p[i + 1]; var p3 = p[Mathf.Min(p.Count - 1, i + 2)];
            for (int k = 0; k < pasos; k++)
            {
                float t = k / (float)pasos, t2 = t * t, t3 = t2 * t;
                var q = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                q.y = Mathf.Max(0f, q.y);
                r.Add(q);
            }
        }
        r.Add(p[p.Count - 1]);
        return r;
    }

    static void CoserCostura(Mesh m, int fila, int filas)
    {
        var n = m.normals;
        for (int i = 0; i < filas; i++)
        {
            int a = i * fila, b = a + fila - 1;
            var s = (n[a] + n[b]).normalized;
            n[a] = n[b] = s;
        }
        m.normals = n;
    }

    /// <summary>Se queda solo con los triángulos que cumplen la condición (sobre su centro). Para "parches" como la barriga.</summary>
    public static Mesh Filtrar(Mesh origen, System.Func<Vector3, bool> cond, float inflar, string nombre)
    {
        var v = origen.vertices; var n = origen.normals; var t = origen.triangles;
        var nt = new List<int>();
        for (int k = 0; k < t.Length; k += 3)
        {
            var c = (v[t[k]] + v[t[k + 1]] + v[t[k + 2]]) / 3f;
            if (cond(c)) { nt.Add(t[k]); nt.Add(t[k + 1]); nt.Add(t[k + 2]); }
        }
        var nv = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++) nv[i] = v[i] + n[i] * inflar;
        var m = new Mesh { name = nombre };
        m.vertices = nv; m.normals = n; m.triangles = nt.ToArray(); m.RecalculateBounds();
        return m;
    }

    // ---------------------------------------------------------------- Extrusión de un polígono

    /// <summary>
    /// Extruye un polígono plano (u, v) con un grosor a lo largo de w. eu, ev, ew: hacia dónde va cada eje en la malla.
    /// </summary>
    public static Mesh Extruir(IList<Vector2> poligono, float grosor, Vector3 eu, Vector3 ev, Vector3 ew, Vector3 desplazamiento, string nombre)
    {
        var p = poligono.ToList();
        float area = 0f;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) area += p[j].x * p[i].y - p[i].x * p[j].y;
        if (area < 0) p.Reverse();
        int n = p.Count;
        var tris = Triangular(p);
        float g = grosor / 2f;
        var loc = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < n; i++) loc.Add(new Vector3(p[i].x, p[i].y, -g));
        for (int i = 0; i < n; i++) loc.Add(new Vector3(p[i].x, p[i].y, g));
        for (int k = 0; k < tris.Count; k += 3)
        {
            int a = tris[k], b = tris[k + 1], c = tris[k + 2];
            t.AddRange(new[] { a, c, b });
            t.AddRange(new[] { n + a, n + b, n + c });
        }
        for (int i = 0; i < n; i++)
        {
            var a = p[i]; var b = p[(i + 1) % n];
            int s = loc.Count;
            loc.Add(new Vector3(a.x, a.y, -g)); loc.Add(new Vector3(b.x, b.y, -g));
            loc.Add(new Vector3(b.x, b.y, g)); loc.Add(new Vector3(a.x, a.y, g));
            t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }
        // Pasar a los ejes pedidos; si la base es "zurda", hay que dar la vuelta a los triángulos
        var v = loc.Select(q => eu * q.x + ev * q.y + ew * q.z + desplazamiento).ToList();
        if (Vector3.Dot(Vector3.Cross(eu, ev), ew) < 0f)
            for (int k = 0; k < t.Count; k += 3) { int x = t[k + 1]; t[k + 1] = t[k + 2]; t[k + 2] = x; }
        var m = new Mesh { name = nombre };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static List<int> Triangular(List<Vector2> v)
    {
        var res = new List<int>();
        var idx = Enumerable.Range(0, v.Count).ToList();
        int guarda = 0;
        while (idx.Count > 3 && guarda++ < 10000)
        {
            bool corte = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                Vector2 a = v[ia], b = v[ib], c = v[ic];
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) <= 1e-7f) continue;
                bool vacio = true;
                foreach (int k in idx)
                {
                    if (k == ia || k == ib || k == ic) continue;
                    if (Dentro(v[k], a, b, c)) { vacio = false; break; }
                }
                if (!vacio) continue;
                res.AddRange(new[] { ia, ib, ic });
                idx.RemoveAt(i);
                corte = true;
                break;
            }
            if (!corte) break;
        }
        if (idx.Count == 3) res.AddRange(new[] { idx[0], idx[1], idx[2] });
        return res;
    }

    static bool Dentro(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    // ---------------------------------------------------------------- Cubo redondeado

    /// <summary>Cubo de lado 1 (como el de Unity) con las aristas redondeadas.</summary>
    public static Mesh CuboRedondeado(float radio, int div = 8)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        Vector3[] normales = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var nor in normales)
        {
            var a = Mathf.Abs(nor.y) > 0.5f ? Vector3.right : Vector3.up;
            var b = Vector3.Cross(nor, a);
            int s = v.Count;
            for (int i = 0; i <= div; i++)
                for (int j = 0; j <= div; j++)
                {
                    var p = nor * 0.5f + a * (i / (float)div - 0.5f) + b * (j / (float)div - 0.5f);
                    var dentro = new Vector3(Mathf.Clamp(p.x, -0.5f + radio, 0.5f - radio), Mathf.Clamp(p.y, -0.5f + radio, 0.5f - radio), Mathf.Clamp(p.z, -0.5f + radio, 0.5f - radio));
                    var d = (p - dentro).normalized;
                    v.Add(dentro + d * radio); n.Add(d);
                }
            for (int i = 0; i < div; i++)
                for (int j = 0; j < div; j++)
                {
                    int q0 = s + i * (div + 1) + j, q1 = q0 + 1, q2 = q0 + div + 1, q3 = q2 + 1;
                    Orientado(v, n, t, q0, q2, q3);
                    Orientado(v, n, t, q0, q3, q1);
                }
        }
        var m = new Mesh { name = "CuboRedondeado" };
        m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    static void Orientado(List<Vector3> v, List<Vector3> n, List<int> t, int a, int b, int c)
    {
        var cara = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (cara.sqrMagnitude < 1e-12f) return;
        if (Vector3.Dot(cara, n[a] + n[b] + n[c]) < 0f) { t.Add(a); t.Add(c); t.Add(b); }
        else { t.Add(a); t.Add(b); t.Add(c); }
    }
}
