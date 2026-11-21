using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú "Octavianos" en la barra de Unity.
/// Octavianos → Crear Etapa 1 (Profundidades): monta sola la escena submarina
/// con el fondo, las rocas, las algas, Octavio, un pulpo amigo, la cámara y el ambiente.
/// Puedes volver a ejecutarlo cuando quieras: rehace la escena desde cero.
/// </summary>
public static class EtapaBuilder
{
    const string ScenePath = "Assets/Scenes/Etapa1_Profundidades.unity";
    const string MaterialsFolder = "Assets/Materials";

    [MenuItem("Octavianos/Crear Etapa 1 (Profundidades)")]
    public static void CrearEtapa1()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Etapa 1",
                "La escena Etapa1_Profundidades ya existe. ¿Quieres rehacerla desde cero?",
                "Sí, rehacer", "Cancelar"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Materiales ----------
        var arena    = Mat("Arena",       new Color(0.76f, 0.68f, 0.50f));
        var roca     = Mat("Roca",        new Color(0.35f, 0.37f, 0.40f));
        var alga     = Mat("Alga",        new Color(0.10f, 0.45f, 0.25f));
        var coral    = Mat("Coral",       new Color(0.95f, 0.45f, 0.45f));
        var pielOcta = Mat("PielOctavio", new Color(0.85f, 0.45f, 0.35f));
        var pielAmig = Mat("PielAmigo",   new Color(0.55f, 0.35f, 0.75f));
        var blanco   = Mat("OjoBlanco",   Color.white);
        var negro    = Mat("OjoPupila",   new Color(0.05f, 0.05f, 0.05f));

        // ---------- Fondo marino ----------
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "FondoMarino";
        suelo.transform.localScale = new Vector3(10f, 1f, 10f);   // 100 x 100 m
        suelo.GetComponent<Renderer>().sharedMaterial = arena;

        var decorado = new GameObject("Decorado").transform;
        Random.InitState(42); // siempre el mismo decorado

        for (int i = 0; i < 30; i++)
        {
            var tipo = Random.value < 0.6f ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var r = GameObject.CreatePrimitive(tipo);
            r.name = "Roca_" + i;
            r.transform.SetParent(decorado);
            r.transform.position = PuntoLejosDelCentro(6f, 45f);
            r.transform.localScale = new Vector3(Random.Range(1f, 5f), Random.Range(0.8f, 3f), Random.Range(1f, 5f));
            r.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
            r.GetComponent<Renderer>().sharedMaterial = roca;
        }

        for (int i = 0; i < 60; i++)
        {
            var a = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            a.name = "Alga_" + i;
            a.transform.SetParent(decorado);
            float alto = Random.Range(0.8f, 3f);
            Vector3 p = PuntoLejosDelCentro(3f, 45f);
            p.y = alto; // la cápsula mide 2 * escala
            a.transform.position = p;
            a.transform.localScale = new Vector3(0.15f, alto, 0.15f);
            a.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
            a.GetComponent<Renderer>().sharedMaterial = alga;
            Object.DestroyImmediate(a.GetComponent<Collider>()); // se puede atravesar
        }

        for (int i = 0; i < 15; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.name = "Coral_" + i;
            c.transform.SetParent(decorado);
            Vector3 p = PuntoLejosDelCentro(5f, 40f);
            float s = Random.Range(0.4f, 1.2f);
            p.y = s * 0.4f;
            c.transform.position = p;
            c.transform.localScale = new Vector3(s, s * 0.8f, s);
            c.GetComponent<Renderer>().sharedMaterial = coral;
        }

        // ---------- Octavio ----------
        var octavio = CrearPulpo("Octavio", new Vector3(0f, 2f, 0f), pielOcta, blanco, negro, out var cuerpo, out var piel);
        var rb = octavio.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var col = octavio.AddComponent<SphereCollider>();
        col.radius = 0.6f;

        var swim = octavio.AddComponent<OctavioSwimController>();
        SetRef(swim, "visual", cuerpo);

        var skin = octavio.AddComponent<OctavioSkinColor>();
        var so = new SerializedObject(skin);
        var arr = so.FindProperty("skinRenderers");
        arr.arraySize = piel.Count;
        for (int i = 0; i < piel.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = piel[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Brisa, la amiga que le dice "Octavio, ¿qué haces aquí?" ----------
        var amigo = CrearPulpo("Brisa", new Vector3(6f, 1.5f, 8f), pielAmig, blanco, negro, out _, out var pielBrisa);
        amigo.transform.rotation = Quaternion.Euler(0f, 215f, 0f); // mirando hacia Octavio

        var skinBrisa = amigo.AddComponent<OctavioSkinColor>();
        var sb = new SerializedObject(skinBrisa);
        sb.FindProperty("readKeyboard").boolValue = false;
        sb.FindProperty("neutral").colorValue = new Color(0.55f, 0.35f, 0.75f);
        var arrB = sb.FindProperty("skinRenderers");
        arrB.arraySize = pielBrisa.Count;
        for (int i = 0; i < pielBrisa.Count; i++) arrB.GetArrayElementAtIndex(i).objectReferenceValue = pielBrisa[i];
        sb.ApplyModifiedPropertiesWithoutUndo();

        // Ruta hacia la futura Octópolis
        var ruta = new GameObject("RutaOctopolis").transform;
        Vector3[] puntos =
        {
            new Vector3(10f, 2f, 16f),
            new Vector3(8f, 3f, 25f),
            new Vector3(14f, 2.5f, 33f),
            new Vector3(20f, 2f, 41f),
        };
        var wps = new Transform[puntos.Length];
        for (int i = 0; i < puntos.Length; i++)
        {
            var w = new GameObject("Punto_" + (i + 1)).transform;
            w.SetParent(ruta);
            w.position = puntos[i];
            wps[i] = w;
        }

        // Entrada de Octópolis (arco provisional)
        var arco = new GameObject("EntradaOctopolis").transform;
        arco.position = new Vector3(22f, 0f, 44f);
        Primitiva(PrimitiveType.Cube, "PilarIzq", arco, new Vector3(-2.5f, 2.5f, 0f), new Vector3(1f, 5f, 1f), roca);
        Primitiva(PrimitiveType.Cube, "PilarDer", arco, new Vector3(2.5f, 2.5f, 0f), new Vector3(1f, 5f, 1f), roca);
        Primitiva(PrimitiveType.Cube, "Dintel", arco, new Vector3(0f, 5.3f, 0f), new Vector3(6.5f, 0.8f, 1.2f), coral);
        arco.rotation = Quaternion.Euler(0f, 35f, 0f);

        var guia = amigo.AddComponent<NPCGuide>();
        guia.waypoints = wps;

        var dialogo = amigo.AddComponent<NPCDialogue>();
        dialogo.lines = LineasEtapa1();
        dialogo.guideAfter = guia;

        // ---------- Cámara ----------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 4f, -6f);
            cam.transform.LookAt(octavio.transform);
            cam.farClipPlane = 200f;
            var tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            SetRef(tpc, "target", octavio.transform);
        }

        // ---------- Luz ----------
        var luz = Object.FindAnyObjectByType<Light>();
        if (luz != null)
        {
            luz.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
            luz.color = new Color(0.6f, 0.85f, 1f);
            luz.intensity = 0.8f;
        }

        // ---------- Ambiente submarino ----------
        new GameObject("Ambiente").AddComponent<UnderwaterAmbience>();

        // ---------- Guardar ----------
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AnadirABuild(ScenePath);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = octavio;
        Debug.Log("[Octavianos] Etapa 1 creada. Pulsa Play y nada con WASD, Espacio/Ctrl, Shift. Colores: 1, 2, 3, 0.");
    }

    // Primera conversación del juego. Cada línea lleva el color con el que "habla" la piel.
    static DialogueLine[] LineasEtapa1()
    {
        var azul     = new Color(0.15f, 0.40f, 0.95f);
        var verde    = new Color(0.15f, 0.80f, 0.30f);
        var rojo     = new Color(0.90f, 0.15f, 0.15f);
        var amarillo = new Color(0.95f, 0.85f, 0.20f);

        return new[]
        {
            new DialogueLine("Brisa",   "¡Octavio! ¿Qué haces aquí? ¿Te has perdido?", false, azul),
            new DialogueLine("Octavio", "Eh… creo que sí. Estaba siguiendo un banco de peces y, cuando me di cuenta, ya no veía las rocas de casa.", true, amarillo),
            new DialogueLine("Brisa",   "¡Estás muy lejos! Estas aguas no son seguras. Dicen que por aquí cerca está el géiser DESTELLO.", false, rojo),
            new DialogueLine("Octavio", "¿El géiser de las historias? ¿El que se traga todo lo que se acerca?", true, verde),
            new DialogueLine("Brisa",   "Ese mismo. Tus padres te lo han dicho mil veces… Venga, sígueme. Te llevo de vuelta a casa.", false, azul),
            new DialogueLine("Octavio", "Vale, vale… ¡Voy detrás de ti!", true, verde),
        };
    }

    // Crea un pulpo provisional: cabeza, 2 ojos y 8 tentáculos.
    static GameObject CrearPulpo(string nombre, Vector3 pos, Material piel, Material blanco, Material negro,
                                 out Transform cuerpo, out List<Renderer> renderersPiel)
    {
        renderersPiel = new List<Renderer>();

        var raiz = new GameObject(nombre);
        raiz.transform.position = pos;

        var c = new GameObject("Cuerpo").transform;
        c.SetParent(raiz.transform, false);
        cuerpo = c;

        var cabeza = Primitiva(PrimitiveType.Sphere, "Cabeza", c, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 1.1f, 0.9f), piel);
        renderersPiel.Add(cabeza.GetComponent<Renderer>());

        for (int lado = -1; lado <= 1; lado += 2)
        {
            Primitiva(PrimitiveType.Sphere, lado < 0 ? "OjoIzq" : "OjoDer", c,
                      new Vector3(0.18f * lado, 0.15f, 0.38f), Vector3.one * 0.2f, blanco);
            Primitiva(PrimitiveType.Sphere, lado < 0 ? "PupilaIzq" : "PupilaDer", c,
                      new Vector3(0.18f * lado, 0.15f, 0.47f), Vector3.one * 0.09f, negro);
        }

        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            Vector3 fuera = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            var t = Primitiva(PrimitiveType.Capsule, "Tentaculo_" + (i + 1), c,
                              fuera * 0.3f + Vector3.down * 0.55f, new Vector3(0.12f, 0.35f, 0.12f), piel);
            t.transform.localRotation = Quaternion.AngleAxis(-30f, Vector3.Cross(Vector3.up, fuera));
            renderersPiel.Add(t.GetComponent<Renderer>());
        }

        return raiz;
    }

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 posLocal, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // el collider lo lleva el padre
        return go;
    }

    static Vector3 PuntoLejosDelCentro(float min, float max)
    {
        Vector2 d = Random.insideUnitCircle.normalized * Random.Range(min, max);
        return new Vector3(d.x, 0f, d.y);
    }

    static Material Mat(string nombre, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = $"{MaterialsFolder}/{nombre}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void SetRef(Object componente, string campo, Object valor)
    {
        var so = new SerializedObject(componente);
        so.FindProperty(campo).objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AnadirABuild(string path)
    {
        var lista = EditorBuildSettings.scenes.ToList();
        if (lista.Any(s => s.path == path)) return;
        lista.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
