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
        guia.objectiveArrived = ""; // al llegar empieza la prueba

        // ---------- El humano (cae al mar al llegar a Octópolis) ----------
        var pielHumano = Mat("PielHumano", new Color(0.95f, 0.78f, 0.62f));
        var camiseta   = Mat("Camiseta",   new Color(0.92f, 0.92f, 0.96f));
        var pantalon   = Mat("Pantalon",   new Color(0.20f, 0.25f, 0.45f));
        var humano = new GameObject("Humano");
        var hc = humano.transform;
        Primitiva(PrimitiveType.Capsule, "Torso",     hc, Vector3.zero,                    new Vector3(0.6f, 0.55f, 0.4f),  camiseta);
        Primitiva(PrimitiveType.Sphere,  "Cabeza",    hc, new Vector3(0f, 0.85f, 0f),      Vector3.one * 0.45f,             pielHumano);
        Primitiva(PrimitiveType.Capsule, "BrazoIzq",  hc, new Vector3(-0.42f, 0.1f, 0f),   new Vector3(0.18f, 0.45f, 0.18f), pielHumano);
        Primitiva(PrimitiveType.Capsule, "BrazoDer",  hc, new Vector3(0.42f, 0.1f, 0f),    new Vector3(0.18f, 0.45f, 0.18f), pielHumano);
        Primitiva(PrimitiveType.Capsule, "PiernaIzq", hc, new Vector3(-0.15f, -0.9f, 0f),  new Vector3(0.22f, 0.5f, 0.22f),  pantalon);
        Primitiva(PrimitiveType.Capsule, "PiernaDer", hc, new Vector3(0.15f, -0.9f, 0f),   new Vector3(0.22f, 0.5f, 0.22f),  pantalon);
        hc.position = new Vector3(22f, 12f, 40f);
        humano.SetActive(false);

        // ---------- Prueba de la etapa 1 (3 caminos) ----------
        var prueba = new GameObject("PruebaEtapa1").AddComponent<Etapa1Prueba>();
        prueba.guide = guia;
        prueba.brisa = amigo.transform;
        prueba.humano = humano;

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

    // =====================================================================
    //  ETAPA 2 · EL GÉISER DESTELLO
    // =====================================================================

    const string ScenePath2 = "Assets/Scenes/Etapa2_Geiser.unity";

    [MenuItem("Octavianos/Crear Etapa 2 (Géiser)")]
    public static void CrearEtapa2()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath2) != null &&
            !EditorUtility.DisplayDialog("Etapa 2",
                "La escena Etapa2_Geiser ya existe. ¿Quieres rehacerla desde cero?", "Sí, rehacer", "Cancelar"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Materiales ----------
        var arenaOscura = Mat("ArenaOscura",    new Color(0.32f, 0.27f, 0.22f));
        var volcanica   = Mat("RocaVolcanica",  new Color(0.14f, 0.12f, 0.12f));
        var crater      = Mat("Crater",         new Color(0.05f, 0.04f, 0.04f));
        var lava        = MatEmisiva("BrilloGeiser", new Color(1f, 0.45f, 0.1f), 3f);
        var burbuja     = MatEmisiva("Burbuja",      new Color(0.75f, 0.92f, 1f), 0.6f);
        var guia        = MatEmisiva("BrilloGuia",   new Color(1f, 0.55f, 0.2f), 1.5f);
        var arenaIsla   = Mat("ArenaIsla",      new Color(0.86f, 0.78f, 0.55f));
        var marca       = MatEmisiva("MarcaBusqueda", new Color(0.6f, 0.95f, 1f), 1.2f);
        var simRojo     = MatEmisiva("SimboloRojo",  new Color(0.9f, 0.15f, 0.15f), 2f);
        var simVerde    = MatEmisiva("SimboloVerde", new Color(0.15f, 0.85f, 0.3f), 2f);
        var simAzul     = MatEmisiva("SimboloAzul",  new Color(0.1f, 0.3f, 1f), 2.2f); // azul intenso (con más brillo se veía blanco)
        var pielOcta    = Mat("PielOctavio", new Color(0.85f, 0.45f, 0.35f));
        var blanco      = Mat("OjoBlanco", Color.white);
        var negro       = Mat("OjoPupila", new Color(0.05f, 0.05f, 0.05f));
        var pielHumano  = Mat("PielHumano", new Color(0.95f, 0.78f, 0.62f));
        var camiseta    = Mat("Camiseta",   new Color(0.92f, 0.92f, 0.96f));
        var pantalon    = Mat("Pantalon",   new Color(0.20f, 0.25f, 0.45f));

        // ---------- Fondo ----------
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "FondoVolcanico";
        suelo.transform.localScale = new Vector3(16f, 1f, 16f); // 160 x 160 m
        suelo.GetComponent<Renderer>().sharedMaterial = arenaOscura;

        Vector3 geiserPos = new Vector3(0f, 0f, 35f);
        Vector3 islaCentro = new Vector3(70f, -2f, 75f);

        var decorado = new GameObject("Decorado").transform;
        Random.InitState(7);
        for (int i = 0; i < 45; i++)
        {
            Vector3 p = PuntoLejosDelCentro(8f, 70f);
            if (DistanciaAlSegmento(p, Vector3.zero, geiserPos) < 7f) continue;   // deja libre el camino al géiser
            if ((p - new Vector3(islaCentro.x, 0f, islaCentro.z)).magnitude < 32f) continue; // y la isla
            var r = GameObject.CreatePrimitive(Random.value < 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            r.name = "RocaVolcanica_" + i;
            r.transform.SetParent(decorado);
            r.transform.position = p;
            r.transform.localScale = new Vector3(Random.Range(1.5f, 6f), Random.Range(1f, 5f), Random.Range(1.5f, 6f));
            r.transform.rotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            r.GetComponent<Renderer>().sharedMaterial = volcanica;
        }

        // Pequeñas grietas brillantes que marcan el camino hacia el géiser
        for (int i = 1; i <= 6; i++)
        {
            float k = i / 7f;
            Vector3 p = Vector3.Lerp(new Vector3(0f, 0f, 4f), geiserPos, k) + new Vector3((i % 2 == 0 ? 2.5f : -2.5f), 0.05f, 0f);
            var g = Primitiva(PrimitiveType.Sphere, "Grieta_" + i, decorado, p, new Vector3(0.8f, 0.1f, 0.5f), guia);
            g.transform.SetParent(decorado, true);
        }

        // ---------- El géiser ----------
        var geiser = new GameObject("GeiserDestello").transform;
        geiser.position = geiserPos;
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            float h = Random.Range(1f, 2.5f);
            Primitiva(PrimitiveType.Cylinder, "Borde_" + i, geiser, new Vector3(Mathf.Cos(a) * 4f, h * 0.5f, Mathf.Sin(a) * 4f), new Vector3(1.6f, h, 1.6f), volcanica);
        }
        Primitiva(PrimitiveType.Cylinder, "Crater", geiser, new Vector3(0f, 0.05f, 0f), new Vector3(7f, 0.05f, 7f), crater);
        Primitiva(PrimitiveType.Cylinder, "Brillo", geiser, new Vector3(0f, 0.12f, 0f), new Vector3(3f, 0.05f, 3f), lava);

        var luz = new GameObject("LuzGeiser").AddComponent<Light>();
        luz.transform.SetParent(geiser, false);
        luz.transform.localPosition = new Vector3(0f, 3f, 0f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.55f, 0.2f);
        luz.range = 30f;
        luz.intensity = 4f;

        var fx = geiser.gameObject.AddComponent<GeyserFX>();
        fx.bubbleMaterial = burbuja;
        fx.glow = luz;

        // ---------- La isla ----------
        var isla = Primitiva(PrimitiveType.Sphere, "Isla", null, islaCentro, Vector3.one * 50f, arenaIsla);
        isla.AddComponent<SphereCollider>();
        var islaSpawn = new GameObject("DespertarIsla").transform;
        islaSpawn.position = new Vector3(50f, 14f, 56f);
        islaSpawn.rotation = Quaternion.LookRotation(new Vector3(islaCentro.x, 0f, islaCentro.z) - new Vector3(50f, 0f, 56f));

        var busqueda = new GameObject("PuntosBusqueda").transform;
        var puntos = new Transform[3];
        float[] angulos = { 200f, 235f, 270f };
        for (int i = 0; i < 3; i++)
        {
            float a = angulos[i] * Mathf.Deg2Rad;
            Vector3 p = new Vector3(islaCentro.x + Mathf.Cos(a) * 29f, 12f, islaCentro.z + Mathf.Sin(a) * 29f);
            var m = Primitiva(PrimitiveType.Sphere, "Busca_" + (i + 1), busqueda, p, Vector3.one * 0.8f, marca);
            // Haz de luz vertical para que se vea desde lejos a pesar de la niebla
            var haz = Primitiva(PrimitiveType.Cylinder, "Haz", m.transform, new Vector3(0f, 5f, 0f), new Vector3(0.25f, 5f, 0.25f), marca);
            haz.transform.localScale = new Vector3(0.25f, 5f / 0.8f, 0.25f); // compensa la escala de la esfera
            puntos[i] = m.transform;
        }

        // Símbolos de Destello (círculo rojo, cuadrado verde, triángulo azul) en las rocas de la orilla
        var simbolos = new GameObject("SimbolosDestello").transform;
        Vector3 baseSim = new Vector3(islaCentro.x + Mathf.Cos(250f * Mathf.Deg2Rad) * 27f, 11f, islaCentro.z + Mathf.Sin(250f * Mathf.Deg2Rad) * 27f);
        simbolos.position = baseSim;
        simbolos.rotation = Quaternion.LookRotation(baseSim - new Vector3(islaCentro.x, baseSim.y, islaCentro.z));
        Primitiva(PrimitiveType.Cylinder, "Circulo", simbolos, new Vector3(-2.2f, 1.2f, 0f), new Vector3(1.2f, 0.05f, 1.2f), simRojo)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Primitiva(PrimitiveType.Cube, "Cuadrado", simbolos, new Vector3(0f, 1.2f, 0f), new Vector3(1.1f, 1.1f, 0.1f), simVerde);
        var tri = new GameObject("Triangulo").transform;
        tri.SetParent(simbolos, false);
        tri.localPosition = new Vector3(2.2f, 1.2f, 0f);
        Primitiva(PrimitiveType.Cube, "Base", tri, new Vector3(0f, -0.45f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul);
        Primitiva(PrimitiveType.Cube, "LadoIzq", tri, new Vector3(-0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, 60f);
        Primitiva(PrimitiveType.Cube, "LadoDer", tri, new Vector3(0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, -60f);

        // ---------- Octavio con el humano ----------
        var octavio = CrearPulpo("Octavio", new Vector3(0f, 2f, 0f), pielOcta, blanco, negro, out var cuerpo, out var piel);
        var rb = octavio.AddComponent<Rigidbody>();
        rb.useGravity = false;
        octavio.AddComponent<SphereCollider>().radius = 0.6f;
        var swim = octavio.AddComponent<OctavioSwimController>();
        SetRef(swim, "visual", cuerpo);
        var skin = octavio.AddComponent<OctavioSkinColor>();
        var so = new SerializedObject(skin);
        var arr = so.FindProperty("skinRenderers");
        arr.arraySize = piel.Count;
        for (int i = 0; i < piel.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = piel[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        var humano = new GameObject("Humano");
        var hc = humano.transform;
        Primitiva(PrimitiveType.Capsule, "Torso",     hc, Vector3.zero,                   new Vector3(0.6f, 0.55f, 0.4f),  camiseta);
        Primitiva(PrimitiveType.Sphere,  "Cabeza",    hc, new Vector3(0f, 0.85f, 0f),     Vector3.one * 0.45f,             pielHumano);
        Primitiva(PrimitiveType.Capsule, "BrazoIzq",  hc, new Vector3(-0.42f, 0.1f, 0f),  new Vector3(0.18f, 0.45f, 0.18f), pielHumano);
        Primitiva(PrimitiveType.Capsule, "BrazoDer",  hc, new Vector3(0.42f, 0.1f, 0f),   new Vector3(0.18f, 0.45f, 0.18f), pielHumano);
        Primitiva(PrimitiveType.Capsule, "PiernaIzq", hc, new Vector3(-0.15f, -0.9f, 0f), new Vector3(0.22f, 0.5f, 0.22f),  pantalon);
        Primitiva(PrimitiveType.Capsule, "PiernaDer", hc, new Vector3(0.15f, -0.9f, 0f),  new Vector3(0.22f, 0.5f, 0.22f),  pantalon);
        hc.SetParent(octavio.transform, false);
        hc.localPosition = new Vector3(0f, -1.2f, 0.8f);
        hc.localRotation = Quaternion.Euler(80f, 0f, 0f);

        // ---------- Cámara, luz y ambiente ----------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 4f, -6f);
            cam.transform.LookAt(octavio.transform);
            cam.farClipPlane = 300f;
            SetRef(cam.gameObject.AddComponent<ThirdPersonCamera>(), "target", octavio.transform);
        }
        var sol = Object.FindAnyObjectByType<Light>();
        if (sol != null && sol != luz)
        {
            sol.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
            sol.color = new Color(0.5f, 0.6f, 0.8f);
            sol.intensity = 0.45f;
        }
        var amb = new GameObject("Ambiente").AddComponent<UnderwaterAmbience>();
        var sa = new SerializedObject(amb);
        sa.FindProperty("waterColor").colorValue = new Color(0.02f, 0.10f, 0.16f);
        sa.FindProperty("fogDensity").floatValue = 0.05f;
        sa.FindProperty("ambientColor").colorValue = new Color(0.10f, 0.20f, 0.28f);
        sa.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Controlador de la etapa ----------
        var ctrl = new GameObject("Etapa2").AddComponent<Etapa2Controller>();
        ctrl.geyser = geiser;
        ctrl.fx = fx;
        ctrl.humano = humano;
        ctrl.islandSpawn = islaSpawn;
        ctrl.searchPoints = puntos;
        ctrl.symbols = simbolos.gameObject;
        ctrl.nextScene = "Etapa3_Casa";

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath2);
        AnadirABuild(ScenePath2);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = octavio;
        Debug.Log("[Octavianos] Etapa 2 creada. Pulsa Play. En el editor, F9 te lleva directo al géiser.");
    }

    // =====================================================================
    //  ETAPA 3 · ALGO HA CAMBIADO (la colonia)
    // =====================================================================

    const string ScenePath3 = "Assets/Scenes/Etapa3_Casa.unity";

    [MenuItem("Octavianos/Crear Etapa 3 (Casa)")]
    public static void CrearEtapa3()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath3) != null &&
            !EditorUtility.DisplayDialog("Etapa 3",
                "La escena Etapa3_Casa ya existe. ¿Quieres rehacerla desde cero?", "Sí, rehacer", "Cancelar"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Materiales ----------
        var arenaClara = Mat("ArenaClara",   new Color(0.86f, 0.80f, 0.62f));
        var roca       = Mat("Roca",         new Color(0.35f, 0.37f, 0.40f));
        var rocaCasa   = Mat("RocaCasa",     new Color(0.55f, 0.52f, 0.48f));
        var puertaMat  = Mat("PuertaPiedra", new Color(0.42f, 0.40f, 0.38f));
        var oscuro     = Mat("Hueco",        new Color(0.03f, 0.04f, 0.05f));
        var alga       = Mat("Alga",         new Color(0.10f, 0.45f, 0.25f));
        Material[] corales =
        {
            Mat("CoralRosa",     new Color(1f, 0.45f, 0.65f)),
            Mat("CoralNaranja",  new Color(1f, 0.55f, 0.2f)),
            Mat("CoralAmarillo", new Color(1f, 0.85f, 0.3f)),
            Mat("CoralMorado",   new Color(0.6f, 0.4f, 0.9f)),
            Mat("CoralTurquesa", new Color(0.25f, 0.85f, 0.8f)),
        };
        var algasGuardadas = MatEmisiva("AlgasDespensa", new Color(0.3f, 0.95f, 0.4f), 1.2f);
        var marca    = MatEmisiva("MarcaBusqueda", new Color(0.6f, 0.95f, 1f), 1.2f);
        var simRojo  = MatEmisiva("SimboloRojo",  new Color(0.9f, 0.15f, 0.15f), 2f);
        var simVerde = MatEmisiva("SimboloVerde", new Color(0.15f, 0.85f, 0.3f), 2f);
        var simAzul  = MatEmisiva("SimboloAzul",  new Color(0.1f, 0.3f, 1f), 2.2f);
        var pielOcta = Mat("PielOctavio", new Color(0.85f, 0.45f, 0.35f));
        var blanco   = Mat("OjoBlanco", Color.white);
        var negro    = Mat("OjoPupila", new Color(0.05f, 0.05f, 0.05f));

        // ---------- Fondo ----------
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "FondoColonia";
        suelo.transform.localScale = new Vector3(12f, 1f, 12f); // 120 x 120 m
        suelo.GetComponent<Renderer>().sharedMaterial = arenaClara;

        Vector3 plaza = new Vector3(-5f, 0f, 32f);
        Vector3 despCentro = new Vector3(18f, 0f, 40f);
        Vector3 dirPuerta = plaza - despCentro; dirPuerta.y = 0f; dirPuerta.Normalize(); // la puerta mira a la plaza
        Vector3 lateral = Vector3.Cross(Vector3.up, dirPuerta);
        Vector3 frentePuerta = despCentro + dirPuerta * 9f;

        // ---------- Casas de roca (cúpulas) ----------
        var colonia = new GameObject("Colonia").transform;
        Vector3[] casas = { new Vector3(-8f, 0f, 20f), new Vector3(-18f, 0f, 30f), new Vector3(-12f, 0f, 44f), new Vector3(0f, 0f, 52f), new Vector3(-22f, 0f, 14f) };
        for (int i = 0; i < casas.Length; i++)
        {
            var casa = new GameObject(i == 0 ? "CasaOctavio" : "Casa_" + i).transform;
            casa.SetParent(colonia);
            casa.position = casas[i];
            Vector3 mira = plaza - casas[i]; mira.y = 0f;
            casa.rotation = Quaternion.LookRotation(mira.normalized);
            var cupula = Primitiva(PrimitiveType.Sphere, "Cupula", casa, Vector3.zero, new Vector3(6f, 5f, 6f), rocaCasa);
            cupula.AddComponent<SphereCollider>();
            Primitiva(PrimitiveType.Cylinder, "Entrada", casa, new Vector3(0f, 0.9f, 2.85f), new Vector3(1.4f, 0.05f, 1.6f), oscuro)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int k = 0; k < 3; k++) // coral de adorno junto a cada casa
            {
                float a = (k * 120f + i * 37f) * Mathf.Deg2Rad;
                Primitiva(PrimitiveType.Sphere, "Adorno_" + k, casa, new Vector3(Mathf.Cos(a) * 3.6f, 0.4f, Mathf.Sin(a) * 3.6f),
                          Vector3.one * 0.9f, corales[(i + k) % corales.Length]);
            }
        }

        // Gran coral en la plaza
        var granCoral = new GameObject("GranCoral").transform;
        granCoral.SetParent(colonia);
        granCoral.position = plaza;
        for (int i = 0; i < 7; i++)
        {
            float a = i * 51f * Mathf.Deg2Rad;
            float h = 1.5f + (i % 3) * 0.8f;
            var rama = Primitiva(PrimitiveType.Capsule, "Rama_" + i, granCoral, new Vector3(Mathf.Cos(a) * 0.7f, h, Mathf.Sin(a) * 0.7f),
                                 new Vector3(0.5f, h, 0.5f), corales[i % corales.Length]);
            rama.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 20f, 0f, -Mathf.Cos(a) * 20f);
        }

        // ---------- Decorado ----------
        var decorado = new GameObject("Decorado").transform;
        Random.InitState(33);
        for (int i = 0; i < 70; i++)
        {
            Vector3 p = PuntoLejosDelCentro(4f, 55f);
            bool libre = (p - despCentro).magnitude > 9f && DistanciaAlSegmento(p, Vector3.zero, new Vector3(-5f, 0f, 17f)) > 3f
                      && DistanciaAlSegmento(p, new Vector3(-5f, 0f, 17f), frentePuerta) > 3f;
            foreach (var c in casas) if ((p - c).magnitude < 5f) libre = false;
            if (!libre) continue;
            if (i % 3 == 0)
            {
                var a = Primitiva(PrimitiveType.Capsule, "Alga_" + i, decorado, Vector3.zero, Vector3.one, alga);
                float alto = Random.Range(0.8f, 2.5f);
                a.transform.position = new Vector3(p.x, alto, p.z);
                a.transform.localScale = new Vector3(0.15f, alto, 0.15f);
            }
            else
            {
                var tipo = Random.value < 0.5f ? PrimitiveType.Sphere : PrimitiveType.Cylinder;
                var coral = Primitiva(tipo, "Coral_" + i, decorado, Vector3.zero, Vector3.one, corales[Random.Range(0, corales.Length)]);
                float t = Random.Range(0.5f, 1.6f);
                coral.transform.position = new Vector3(p.x, t * 0.5f, p.z);
                coral.transform.localScale = tipo == PrimitiveType.Sphere ? Vector3.one * t : new Vector3(t * 0.4f, t, t * 0.4f);
            }
        }
        for (int i = 0; i < 14; i++) // rocas grandes en las afueras
        {
            var r = GameObject.CreatePrimitive(Random.value < 0.6f ? PrimitiveType.Sphere : PrimitiveType.Cube);
            r.name = "Roca_" + i;
            r.transform.SetParent(decorado);
            r.transform.position = PuntoLejosDelCentro(42f, 58f) + new Vector3(0f, 0f, 20f);
            r.transform.localScale = new Vector3(Random.Range(2f, 6f), Random.Range(1.5f, 4f), Random.Range(2f, 6f));
            r.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            r.GetComponent<Renderer>().sharedMaterial = roca;
        }

        // ---------- La despensa ----------
        var despensa = new GameObject("Despensa").transform;
        despensa.position = despCentro;
        despensa.rotation = Quaternion.LookRotation(dirPuerta);
        var rocaGrande = Primitiva(PrimitiveType.Sphere, "RocaDespensa", despensa, new Vector3(0f, 1f, 0f), new Vector3(11f, 9f, 11f), roca);
        rocaGrande.AddComponent<SphereCollider>();
        Primitiva(PrimitiveType.Cube, "Hueco", despensa, new Vector3(0f, 2f, 5.35f), new Vector3(2.8f, 3.8f, 0.3f), oscuro);
        Primitiva(PrimitiveType.Cube, "AlgasDentro", despensa, new Vector3(0f, 1f, 5.52f), new Vector3(2.4f, 1.6f, 0.2f), algasGuardadas);
        var puerta = Primitiva(PrimitiveType.Cube, "PuertaDePiedra", despensa, new Vector3(0f, 2.1f, 5.85f), new Vector3(3.4f, 4.2f, 0.6f), puertaMat);
        puerta.AddComponent<BoxCollider>();

        // Símbolos de Destello sobre la puerta
        var simbolos = new GameObject("SimbolosPuerta").transform;
        simbolos.SetParent(despensa, false);
        simbolos.localPosition = new Vector3(0f, 4.9f, 5.6f);
        Primitiva(PrimitiveType.Cylinder, "Circulo", simbolos, new Vector3(1.5f, 0f, 0f), new Vector3(0.8f, 0.05f, 0.8f), simRojo)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Primitiva(PrimitiveType.Cube, "Cuadrado", simbolos, Vector3.zero, new Vector3(0.75f, 0.75f, 0.1f), simVerde);
        var tri = new GameObject("Triangulo").transform;
        tri.SetParent(simbolos, false);
        tri.localPosition = new Vector3(-1.5f, 0f, 0f); // visto de frente: ● ■ ▲ (el eje X local queda invertido)
        tri.localScale = Vector3.one * 0.7f;
        Primitiva(PrimitiveType.Cube, "Base", tri, new Vector3(0f, -0.45f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul);
        Primitiva(PrimitiveType.Cube, "LadoIzq", tri, new Vector3(-0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, 60f);
        Primitiva(PrimitiveType.Cube, "LadoDer", tri, new Vector3(0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, -60f);

        var luzDespensa = new GameObject("LuzDespensa").AddComponent<Light>();
        luzDespensa.transform.SetParent(despensa, false);
        luzDespensa.transform.localPosition = new Vector3(0f, 5f, 7f);
        luzDespensa.type = LightType.Point;
        luzDespensa.color = new Color(0.7f, 0.9f, 1f);
        luzDespensa.range = 12f;
        luzDespensa.intensity = 2f;

        // Punto delante de la puerta y haz de luz que lo señala
        var frente = new GameObject("FrenteDespensa").transform;
        frente.position = frentePuerta + Vector3.up * 1.5f;
        var haz = Primitiva(PrimitiveType.Cylinder, "HazDespensa", null, frentePuerta + Vector3.up * 6f, new Vector3(0.35f, 6f, 0.35f), marca);

        // ---------- Personajes ----------
        GameObject Npc(string nombre, Vector3 pos, Color color, float escala, Vector3 mirarA)
        {
            var piel = Mat("Piel" + nombre.Replace(" ", ""), color);
            var go = CrearPulpo(nombre, pos, piel, blanco, negro, out var cuerpoNpc, out var pielNpc);
            go.transform.localScale = Vector3.one * escala;
            Vector3 m = mirarA - pos; m.y = 0f;
            if (m.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(m);
            var s = go.AddComponent<OctavioSkinColor>();
            var so2 = new SerializedObject(s);
            so2.FindProperty("readKeyboard").boolValue = false;
            so2.FindProperty("neutral").colorValue = color;
            var arr2 = so2.FindProperty("skinRenderers");
            arr2.arraySize = pielNpc.Count;
            for (int i = 0; i < pielNpc.Count; i++) arr2.GetArrayElementAtIndex(i).objectReferenceValue = pielNpc[i];
            so2.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<NPCIdle>().visual = cuerpoNpc;
            return go;
        }

        Vector3 inicio = new Vector3(0f, 2f, 0f);
        var mama   = Npc("Mamá",  new Vector3(-5f, 1.5f, 17f), new Color(0.95f, 0.55f, 0.7f), 1.1f, inicio);
        var brisa  = Npc("Brisa", new Vector3(-2f, 1.5f, 18.5f), new Color(0.55f, 0.35f, 0.75f), 1f, inicio);
        var abuelo = Npc("Abuelo Coral", despCentro + dirPuerta * 7f + lateral * 2.5f + Vector3.up * 1.8f,
                         new Color(0.55f, 0.62f, 0.72f), 1.35f, frentePuerta + dirPuerta * 5f);
        Color[] coloresVecinos = { new Color(1f, 0.6f, 0.25f), new Color(0.2f, 0.75f, 0.7f), new Color(0.95f, 0.8f, 0.3f), new Color(0.4f, 0.7f, 0.35f) };
        string[] nombresVecinos = { "Vecina", "Vecino", "Vecina_2", "Vecino_2" };
        float[] lados = { -6.5f, 6.5f, -9.5f, 9.5f }; // a los lados, para no tapar la cámara
        var vecinos = new Transform[4];
        for (int i = 0; i < 4; i++)
            vecinos[i] = Npc(nombresVecinos[i], despCentro + dirPuerta * (7.5f + (i / 2) * 2f) + lateral * lados[i] + Vector3.up * 1.4f,
                             coloresVecinos[i], 0.95f, despCentro + dirPuerta * 5f).transform;

        var puntoMama = new GameObject("PuntoMamaDespensa").transform;
        puntoMama.position = frentePuerta + lateral * 3f + dirPuerta * 1.5f + Vector3.up * 1.5f;
        var puntoBrisa = new GameObject("PuntoBrisaDespensa").transform;
        puntoBrisa.position = frentePuerta - lateral * 3f + dirPuerta * 1.5f + Vector3.up * 1.5f;

        // ---------- Octavio ----------
        var octavio = CrearPulpo("Octavio", inicio, pielOcta, blanco, negro, out var cuerpo, out var pielO);
        var rb = octavio.AddComponent<Rigidbody>();
        rb.useGravity = false;
        octavio.AddComponent<SphereCollider>().radius = 0.6f;
        var swim = octavio.AddComponent<OctavioSwimController>();
        SetRef(swim, "visual", cuerpo);
        var skin = octavio.AddComponent<OctavioSkinColor>();
        var so = new SerializedObject(skin);
        var arr = so.FindProperty("skinRenderers");
        arr.arraySize = pielO.Count;
        for (int i = 0; i < pielO.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pielO[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Cámara, luz y ambiente (más claro: estamos en casa) ----------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 4f, -6f);
            cam.transform.LookAt(octavio.transform);
            cam.farClipPlane = 250f;
            SetRef(cam.gameObject.AddComponent<ThirdPersonCamera>(), "target", octavio.transform);
        }
        var sol = Object.FindAnyObjectByType<Light>();
        if (sol != null && sol != luzDespensa)
        {
            sol.transform.rotation = Quaternion.Euler(65f, 20f, 0f);
            sol.color = new Color(0.85f, 0.95f, 1f);
            sol.intensity = 1.1f;
        }
        var amb = new GameObject("Ambiente").AddComponent<UnderwaterAmbience>();
        var sa = new SerializedObject(amb);
        sa.FindProperty("waterColor").colorValue = new Color(0.07f, 0.32f, 0.42f);
        sa.FindProperty("fogDensity").floatValue = 0.022f;
        sa.FindProperty("ambientColor").colorValue = new Color(0.35f, 0.45f, 0.5f);
        sa.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Controlador de la etapa ----------
        var ctrl = new GameObject("Etapa3").AddComponent<Etapa3Controller>();
        ctrl.mama = mama.transform;
        ctrl.brisa = brisa.transform;
        ctrl.abuelo = abuelo.transform;
        ctrl.vecinos = vecinos;
        ctrl.despensa = frente;
        ctrl.puerta = puerta.transform;
        ctrl.marcaDespensa = haz;
        ctrl.puntoMama = puntoMama;
        ctrl.puntoBrisa = puntoBrisa;
        ctrl.nextScene = "Etapa4_Octopolis";

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath3);
        AnadirABuild(ScenePath3);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = octavio;
        Debug.Log("[Octavianos] Etapa 3 creada. Pulsa Play. En el editor, F9 se salta los recorridos.");
    }

    // =====================================================================
    //  ETAPA 4 · EL NACIMIENTO DE OCTÓPOLIS
    // =====================================================================

    const string ScenePath4 = "Assets/Scenes/Etapa4_Octopolis.unity";

    [MenuItem("Octavianos/Crear Etapa 4 (Octópolis)")]
    public static void CrearEtapa4()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath4) != null &&
            !EditorUtility.DisplayDialog("Etapa 4",
                "La escena Etapa4_Octopolis ya existe. ¿Quieres rehacerla desde cero?", "Sí, rehacer", "Cancelar"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Materiales ----------
        var arenaClara = Mat("ArenaClara",   new Color(0.86f, 0.80f, 0.62f));
        var roca       = Mat("Roca",         new Color(0.35f, 0.37f, 0.40f));
        var rocaCasa   = Mat("RocaCasa",     new Color(0.55f, 0.52f, 0.48f));
        var piedraCorte = Mat("PiedraCortada", new Color(0.70f, 0.66f, 0.58f));
        var madera     = Mat("Andamio",      new Color(0.45f, 0.33f, 0.22f));
        var alga       = Mat("Alga",         new Color(0.10f, 0.45f, 0.25f));
        var oscuro     = Mat("Hueco",        new Color(0.03f, 0.04f, 0.05f));
        Material[] corales =
        {
            Mat("CoralRosa",     new Color(1f, 0.45f, 0.65f)),
            Mat("CoralNaranja",  new Color(1f, 0.55f, 0.2f)),
            Mat("CoralAmarillo", new Color(1f, 0.85f, 0.3f)),
            Mat("CoralMorado",   new Color(0.6f, 0.4f, 0.9f)),
            Mat("CoralTurquesa", new Color(0.25f, 0.85f, 0.8f)),
        };
        Material[] fachadas =
        {
            Mat("FachadaArena",  new Color(0.85f, 0.78f, 0.62f)),
            Mat("FachadaCoral",  new Color(0.95f, 0.62f, 0.55f)),
            Mat("FachadaMar",    new Color(0.55f, 0.75f, 0.85f)),
            Mat("FachadaLila",   new Color(0.72f, 0.62f, 0.88f)),
        };
        var ventana  = MatEmisiva("VentanaOctopolis", new Color(1f, 0.85f, 0.45f), 2f);
        var marca    = MatEmisiva("MarcaBusqueda", new Color(0.6f, 0.95f, 1f), 1.2f);
        var simRojo  = MatEmisiva("SimboloRojo",  new Color(0.9f, 0.15f, 0.15f), 2f);
        var simVerde = MatEmisiva("SimboloVerde", new Color(0.15f, 0.85f, 0.3f), 2f);
        var simAzul  = MatEmisiva("SimboloAzul",  new Color(0.1f, 0.3f, 1f), 2.2f);
        var pielOcta = Mat("PielOctavio", new Color(0.85f, 0.45f, 0.35f));
        var blanco   = Mat("OjoBlanco", Color.white);
        var negro    = Mat("OjoPupila", new Color(0.05f, 0.05f, 0.05f));

        // ---------- Fondo ----------
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "FondoSector9";
        suelo.transform.localScale = new Vector3(14f, 1f, 14f); // 140 x 140 m
        suelo.GetComponent<Renderer>().sharedMaterial = arenaClara;

        Vector3 inicio = new Vector3(0f, 2f, 0f);
        Vector3 plaza = new Vector3(0f, 0f, 40f);
        Vector3 centroCiudad = plaza + new Vector3(0f, 0f, 12f);
        Vector3[] obras = { new Vector3(-30f, 0f, 24f), new Vector3(28f, 0f, 18f), new Vector3(40f, 0f, 50f) }; // fuera del sitio de la ciudad

        // Las rocas de la vieja colonia (cerca del inicio)
        var vieja = new GameObject("ViejaColonia").transform;
        Vector3[] casasViejas = { new Vector3(-10f, 0f, -4f), new Vector3(10f, 0f, -6f), new Vector3(-16f, 0f, 8f), new Vector3(15f, 0f, 9f) };
        foreach (var c in casasViejas)
        {
            var cup = Primitiva(PrimitiveType.Sphere, "CasaRoca", vieja, c, new Vector3(6f, 5f, 6f), rocaCasa);
            cup.AddComponent<SphereCollider>();
        }

        // Decorado: corales y algas, sin tapar caminos ni obras ni la ciudad
        var decorado = new GameObject("Decorado").transform;
        Random.InitState(44);
        for (int i = 0; i < 80; i++)
        {
            Vector3 p = PuntoLejosDelCentro(6f, 66f);
            bool libre = (p - centroCiudad).magnitude > 32f;
            foreach (var o in obras) if ((p - o).magnitude < 9f) libre = false;
            foreach (var c in casasViejas) if ((p - c).magnitude < 5f) libre = false;
            if (!libre) continue;
            if (i % 3 == 0)
            {
                float alto = Random.Range(0.8f, 2.5f);
                Primitiva(PrimitiveType.Capsule, "Alga_" + i, decorado, new Vector3(p.x, alto, p.z), new Vector3(0.15f, alto, 0.15f), alga);
            }
            else
            {
                float t = Random.Range(0.5f, 1.6f);
                Primitiva(PrimitiveType.Sphere, "Coral_" + i, decorado, new Vector3(p.x, t * 0.5f, p.z), Vector3.one * t, corales[Random.Range(0, corales.Length)]);
            }
        }

        // ---------- Las tres obras ----------
        var obrasRaiz = new GameObject("Obras").transform;
        var puntosObra = new Transform[3];
        var marcas = new GameObject[3];
        string[] nombres = { "EscuelaDeLaLuz", "GranjaDeAlgas", "TallerDePiedra" };
        for (int i = 0; i < 3; i++)
        {
            var o = new GameObject(nombres[i]).transform;
            o.SetParent(obrasRaiz);
            o.position = obras[i];
            Vector3 mira = plaza - obras[i]; mira.y = 0f;
            o.rotation = Quaternion.LookRotation(mira.normalized);

            // andamio de 4 postes
            for (int k = 0; k < 4; k++)
            {
                float sx = (k % 2 == 0 ? -1f : 1f) * 2.5f, sz = (k < 2 ? -1f : 1f) * 2.5f;
                Primitiva(PrimitiveType.Cylinder, "Poste_" + k, o, new Vector3(sx, 2f, sz), new Vector3(0.25f, 2f, 0.25f), madera);
            }
            Primitiva(PrimitiveType.Cube, "Viga", o, new Vector3(0f, 4f, 0f), new Vector3(5.5f, 0.2f, 0.3f), madera);

            if (i == 0) // Escuela: círculo de piedras con los símbolos de Destello
            {
                for (int k = 0; k < 8; k++)
                {
                    float a = k * 45f * Mathf.Deg2Rad;
                    Primitiva(PrimitiveType.Cube, "Asiento_" + k, o, new Vector3(Mathf.Cos(a) * 3.2f, 0.3f, Mathf.Sin(a) * 3.2f), new Vector3(0.9f, 0.6f, 0.9f), piedraCorte);
                }
                Primitiva(PrimitiveType.Cylinder, "Circulo", o, new Vector3(1.2f, 2.4f, 0f), new Vector3(0.6f, 0.05f, 0.6f), simRojo).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Primitiva(PrimitiveType.Cube, "Cuadrado", o, new Vector3(0f, 2.4f, 0f), new Vector3(0.55f, 0.55f, 0.08f), simVerde);
                Primitiva(PrimitiveType.Cube, "Rombo", o, new Vector3(-1.2f, 2.4f, 0f), new Vector3(0.5f, 0.5f, 0.08f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            else if (i == 1) // Granja: filas de algas plantadas en orden
            {
                for (int fx = -3; fx <= 3; fx++)
                    for (int fz = -1; fz <= 1; fz++)
                    {
                        float alto = 0.6f + ((fx + fz + 10) % 3) * 0.3f;
                        Primitiva(PrimitiveType.Capsule, "Cultivo", o, new Vector3(fx * 0.9f, alto, fz * 1.6f), new Vector3(0.18f, alto, 0.18f), alga);
                    }
            }
            else // Taller: bloques cortados apilados
            {
                for (int k = 0; k < 6; k++)
                    Primitiva(PrimitiveType.Cube, "Bloque_" + k, o, new Vector3((k % 3 - 1) * 1.3f, 0.4f + (k / 3) * 0.8f, 0f), new Vector3(1.2f, 0.75f, 1.2f), piedraCorte);
            }

            var punto = new GameObject("Punto_" + nombres[i]).transform;
            punto.position = obras[i] + (plaza - obras[i]).normalized * 5f + Vector3.up * 1.5f;
            puntosObra[i] = punto;
            marcas[i] = Primitiva(PrimitiveType.Cylinder, "Haz_" + nombres[i], null, obras[i] + Vector3.up * 7f, new Vector3(0.35f, 7f, 0.35f), marca);
        }

        // Plaza: anillo de piedras y haz de luz
        var plazaT = new GameObject("PlazaSector9").transform;
        plazaT.position = plaza;
        for (int k = 0; k < 12; k++)
        {
            float a = k * 30f * Mathf.Deg2Rad;
            Primitiva(PrimitiveType.Cylinder, "Losa_" + k, plazaT, new Vector3(Mathf.Cos(a) * 7f, 0.05f, Mathf.Sin(a) * 7f), new Vector3(1.6f, 0.05f, 1.6f), piedraCorte);
        }
        var puntoPlaza = new GameObject("PuntoPlaza").transform;
        puntoPlaza.position = plaza + Vector3.up * 1.5f;
        var hazPlaza = Primitiva(PrimitiveType.Cylinder, "HazPlaza", null, plaza + new Vector3(0f, 7f, 0f), new Vector3(0.35f, 7f, 0.35f), marca);

        // ---------- La ciudad (escondida hasta el final) ----------
        var ciudad = new GameObject("Octopolis").transform;
        ciudad.position = centroCiudad;
        Random.InitState(9);
        int nEdificio = 0;
        // Torre central
        CrearEdificio(ciudad, centroCiudad + new Vector3(0f, 0f, 8f), 3f, 16f, fachadas[0], ventana, rocaCasa, ref nEdificio);
        // Anillos de edificios alrededor (dejando libre el lado de la plaza)
        for (int anillo = 0; anillo < 2; anillo++)
        {
            float radio = 16f + anillo * 10f;
            int cuantos = 8 + anillo * 4;
            for (int k = 0; k < cuantos; k++)
            {
                float ang = (k + 0.5f * anillo) * (360f / cuantos);
                if (ang > 140f && ang < 220f) continue; // hueco hacia la plaza (sur)
                float a = (ang + 90f) * Mathf.Deg2Rad;
                Vector3 p = centroCiudad + new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio);
                float alto = Random.Range(5f, 11f) - anillo * 1.5f;
                CrearEdificio(ciudad, p, Random.Range(1.8f, 2.6f), alto, fachadas[Random.Range(0, fachadas.Length)], ventana, rocaCasa, ref nEdificio);
            }
        }
        // Gran arco de entrada con los símbolos de Destello
        var arco = new GameObject("PuertaOctopolis").transform;
        arco.SetParent(ciudad);
        arco.position = plaza + new Vector3(0f, 0f, 7.5f);
        Primitiva(PrimitiveType.Cube, "PilarIzq", arco, new Vector3(-4f, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f), piedraCorte);
        Primitiva(PrimitiveType.Cube, "PilarDer", arco, new Vector3(4f, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f), piedraCorte);
        Primitiva(PrimitiveType.Cube, "Dintel", arco, new Vector3(0f, 7.5f, 0f), new Vector3(9.5f, 1.2f, 1.4f), piedraCorte);
        // visto desde la plaza (mirando a +Z): ● ■ ▲ de izquierda a derecha
        Primitiva(PrimitiveType.Cylinder, "Circulo", arco, new Vector3(-2f, 7.5f, -0.75f), new Vector3(0.8f, 0.05f, 0.8f), simRojo).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Primitiva(PrimitiveType.Cube, "Cuadrado", arco, new Vector3(0f, 7.5f, -0.75f), new Vector3(0.75f, 0.75f, 0.1f), simVerde);
        var triA = new GameObject("Triangulo").transform;
        triA.SetParent(arco, false);
        triA.localPosition = new Vector3(2f, 7.5f, -0.78f);
        triA.localScale = Vector3.one * 0.7f;
        Primitiva(PrimitiveType.Cube, "Base", triA, new Vector3(0f, -0.45f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul);
        Primitiva(PrimitiveType.Cube, "LadoIzq", triA, new Vector3(-0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, 60f);
        Primitiva(PrimitiveType.Cube, "LadoDer", triA, new Vector3(0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, -60f);

        // ---------- Personajes ----------
        GameObject Npc(string nombre, Vector3 pos, Color color, float escala, Vector3 mirarA)
        {
            var piel = Mat("Piel" + nombre.Replace(" ", ""), color);
            var go = CrearPulpo(nombre, pos, piel, blanco, negro, out var cuerpoNpc, out var pielNpc);
            go.transform.localScale = Vector3.one * escala;
            Vector3 m = mirarA - pos; m.y = 0f;
            if (m.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(m);
            var s = go.AddComponent<OctavioSkinColor>();
            var so2 = new SerializedObject(s);
            so2.FindProperty("readKeyboard").boolValue = false;
            so2.FindProperty("neutral").colorValue = color;
            var arr2 = so2.FindProperty("skinRenderers");
            arr2.arraySize = pielNpc.Count;
            for (int i = 0; i < pielNpc.Count; i++) arr2.GetArrayElementAtIndex(i).objectReferenceValue = pielNpc[i];
            so2.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<NPCIdle>().visual = cuerpoNpc;
            return go;
        }

        var perla = Npc("Perla", inicio + new Vector3(2.5f, -0.5f, 4f), new Color(1f, 0.85f, 0.35f), 0.8f, inicio);
        var brisa = Npc("Brisa", puntosObra[1].position + new Vector3(0f, 0f, 0f) + (obras[1] - puntosObra[1].position).normalized * 2f, new Color(0.55f, 0.35f, 0.75f), 1.15f, puntosObra[1].position);
        var rocaNpc = Npc("Roca", puntosObra[2].position + (obras[2] - puntosObra[2].position).normalized * 2f, new Color(1f, 0.55f, 0.25f), 1.2f, puntosObra[2].position);
        var mama = Npc("Mamá", plaza + new Vector3(-2f, 1.5f, 3f), new Color(0.95f, 0.55f, 0.7f), 1.1f, plaza + new Vector3(0f, 0f, -10f));
        Npc("Vecina", plaza + new Vector3(4f, 1.4f, 4f), new Color(1f, 0.6f, 0.25f), 0.95f, plaza);
        Npc("Vecino", plaza + new Vector3(-5f, 1.4f, 1f), new Color(0.2f, 0.75f, 0.7f), 0.95f, plaza);
        Npc("Pequeño", plaza + new Vector3(3f, 1.1f, -3f), new Color(0.95f, 0.5f, 0.6f), 0.6f, plaza);

        var puntoPerla = new GameObject("PuntoPerlaEscuela").transform;
        puntoPerla.position = puntosObra[0].position + (obras[0] - puntosObra[0].position).normalized * 2f;
        var reunion = new Transform[4];
        Vector3[] offs = { new Vector3(2.5f, 1.2f, 2.5f), new Vector3(-3f, 1.5f, 3.5f), new Vector3(-1f, 1.5f, 4f), new Vector3(3.5f, 1.5f, 0.5f) };
        for (int i = 0; i < 4; i++)
        {
            reunion[i] = new GameObject("Reunion_" + i).transform;
            reunion[i].position = plaza + offs[i];
        }

        // ---------- Octavio (ya adulto) ----------
        var octavio = CrearPulpo("Octavio", inicio, pielOcta, blanco, negro, out var cuerpo, out var pielO);
        octavio.transform.localScale = Vector3.one * 1.25f;
        var rb = octavio.AddComponent<Rigidbody>();
        rb.useGravity = false;
        octavio.AddComponent<SphereCollider>().radius = 0.6f;
        var swim = octavio.AddComponent<OctavioSwimController>();
        SetRef(swim, "visual", cuerpo);
        var skin = octavio.AddComponent<OctavioSkinColor>();
        var so = new SerializedObject(skin);
        var arr = so.FindProperty("skinRenderers");
        arr.arraySize = pielO.Count;
        for (int i = 0; i < pielO.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pielO[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Cámara, luz y ambiente ----------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 4f, -6f);
            cam.transform.LookAt(octavio.transform);
            cam.farClipPlane = 300f;
            SetRef(cam.gameObject.AddComponent<ThirdPersonCamera>(), "target", octavio.transform);
        }
        var sol = Object.FindAnyObjectByType<Light>();
        if (sol != null)
        {
            sol.transform.rotation = Quaternion.Euler(60f, 25f, 0f);
            sol.color = new Color(0.9f, 0.97f, 1f);
            sol.intensity = 1.2f;
        }
        var amb = new GameObject("Ambiente").AddComponent<UnderwaterAmbience>();
        var sa = new SerializedObject(amb);
        sa.FindProperty("waterColor").colorValue = new Color(0.08f, 0.34f, 0.45f);
        sa.FindProperty("fogDensity").floatValue = 0.018f;
        sa.FindProperty("ambientColor").colorValue = new Color(0.38f, 0.48f, 0.52f);
        sa.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Controlador ----------
        var ctrl = new GameObject("Etapa4").AddComponent<Etapa4Controller>();
        ctrl.perla = perla.transform;
        ctrl.brisa = brisa.transform;
        ctrl.mama = mama.transform;
        ctrl.roca = rocaNpc.transform;
        ctrl.obras = puntosObra;
        ctrl.marcasObras = marcas;
        ctrl.plaza = puntoPlaza;
        ctrl.marcaPlaza = hazPlaza;
        ctrl.puntoPerlaEscuela = puntoPerla;
        ctrl.reunion = reunion;
        ctrl.ciudad = ciudad;
        ctrl.nextScene = "Etapa5_Traicion";

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath4);
        AnadirABuild(ScenePath4);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = octavio;
        Debug.Log("[Octavianos] Etapa 4 creada. Pulsa Play. En el editor, F9 te lleva a la siguiente obra o a la plaza.");
    }

    // Un edificio de Octópolis: cuerpo cilíndrico, cúpula y ventanas que brillan. El pivote está en la arena para que pueda "crecer".
    static void CrearEdificio(Transform ciudad, Vector3 pos, float radio, float alto, Material fachada, Material ventana, Material cupula, ref int n)
    {
        var b = new GameObject("Edificio_" + (n++)).transform;
        b.SetParent(ciudad);
        b.position = pos;
        var cuerpoE = Primitiva(PrimitiveType.Cylinder, "Cuerpo", b, new Vector3(0f, alto / 2f, 0f), new Vector3(radio * 2f, alto / 2f, radio * 2f), fachada);
        cuerpoE.AddComponent<CapsuleCollider>();
        Primitiva(PrimitiveType.Sphere, "Cupula", b, new Vector3(0f, alto, 0f), new Vector3(radio * 2.1f, radio * 1.4f, radio * 2.1f), cupula);
        int pisos = Mathf.Max(1, Mathf.FloorToInt(alto / 2.2f));
        for (int p = 0; p < pisos; p++)
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + p * 45f) * Mathf.Deg2Rad;
                Primitiva(PrimitiveType.Sphere, "Ventana", b, new Vector3(Mathf.Cos(a) * radio, 1.4f + p * 2.2f, Mathf.Sin(a) * radio),
                          new Vector3(0.45f, 0.6f, 0.45f), ventana);
            }
    }

    // =====================================================================
    //  ETAPA 5 · LA GRAN TRAICIÓN (final)
    // =====================================================================

    const string ScenePath5 = "Assets/Scenes/Etapa5_Traicion.unity";

    [MenuItem("Octavianos/Crear Etapa 5 (La gran traición)")]
    public static void CrearEtapa5()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath5) != null &&
            !EditorUtility.DisplayDialog("Etapa 5",
                "La escena Etapa5_Traicion ya existe. ¿Quieres rehacerla desde cero?", "Sí, rehacer", "Cancelar"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Materiales ----------
        var arenaClara = Mat("ArenaClara",   new Color(0.86f, 0.80f, 0.62f));
        var roca       = Mat("Roca",         new Color(0.35f, 0.37f, 0.40f));
        var rocaCasa   = Mat("RocaCasa",     new Color(0.55f, 0.52f, 0.48f));
        var piedraCorte = Mat("PiedraCortada", new Color(0.70f, 0.66f, 0.58f));
        var oscuro     = Mat("Hueco",        new Color(0.03f, 0.04f, 0.05f));
        var redMat     = Mat("RedHumanos",   new Color(0.75f, 0.72f, 0.6f));
        var orcaNegro  = Mat("OrcaNegro",    new Color(0.04f, 0.04f, 0.06f));
        var orcaBlanco = Mat("OrcaBlanco",   new Color(0.92f, 0.92f, 0.95f));
        Material[] fachadas =
        {
            Mat("FachadaArena",  new Color(0.85f, 0.78f, 0.62f)),
            Mat("FachadaCoral",  new Color(0.95f, 0.62f, 0.55f)),
            Mat("FachadaMar",    new Color(0.55f, 0.75f, 0.85f)),
            Mat("FachadaLila",   new Color(0.72f, 0.62f, 0.88f)),
        };
        var ventana  = MatEmisiva("VentanaOctopolis", new Color(1f, 0.85f, 0.45f), 2f);
        var marca    = MatEmisiva("MarcaBusqueda", new Color(0.6f, 0.95f, 1f), 1.2f);
        var simRojo  = MatEmisiva("SimboloRojo",  new Color(0.9f, 0.15f, 0.15f), 2f);
        var simVerde = MatEmisiva("SimboloVerde", new Color(0.15f, 0.85f, 0.3f), 2f);
        var simAzul  = MatEmisiva("SimboloAzul",  new Color(0.1f, 0.3f, 1f), 2.2f);
        var pielOcta = Mat("PielOctavio", new Color(0.85f, 0.45f, 0.35f));
        var blanco   = Mat("OjoBlanco", Color.white);
        var negro    = Mat("OjoPupila", new Color(0.05f, 0.05f, 0.05f));

        // ---------- Fondo ----------
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "FondoSector9";
        suelo.transform.localScale = new Vector3(18f, 1f, 18f);
        suelo.GetComponent<Renderer>().sharedMaterial = arenaClara;

        Vector3 plaza = new Vector3(0f, 0f, 40f);
        Vector3 centroCiudad = plaza + new Vector3(0f, 0f, 12f);
        Vector3 posRed = new Vector3(-42f, 0f, -22f);
        Vector3 posCueva = new Vector3(48f, 0f, 30f);

        // ---------- Octópolis (ya construida, de noche) ----------
        var ciudad = new GameObject("Octopolis").transform;
        ciudad.position = centroCiudad;
        Random.InitState(9);
        int nEdificio = 0;
        CrearEdificio(ciudad, centroCiudad + new Vector3(0f, 0f, 8f), 3f, 16f, fachadas[0], ventana, rocaCasa, ref nEdificio);
        for (int anillo = 0; anillo < 2; anillo++)
        {
            float radio = 16f + anillo * 10f;
            int cuantos = 8 + anillo * 4;
            for (int k = 0; k < cuantos; k++)
            {
                float ang = (k + 0.5f * anillo) * (360f / cuantos);
                if (ang > 140f && ang < 220f) continue;
                float a = (ang + 90f) * Mathf.Deg2Rad;
                Vector3 p = centroCiudad + new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio);
                float alto = Random.Range(5f, 11f) - anillo * 1.5f;
                CrearEdificio(ciudad, p, Random.Range(1.8f, 2.6f), alto, fachadas[Random.Range(0, fachadas.Length)], ventana, rocaCasa, ref nEdificio);
            }
        }
        var arco = new GameObject("PuertaOctopolis").transform;
        arco.SetParent(ciudad);
        arco.position = plaza + new Vector3(0f, 0f, 7.5f);
        Primitiva(PrimitiveType.Cube, "PilarIzq", arco, new Vector3(-4f, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f), piedraCorte);
        Primitiva(PrimitiveType.Cube, "PilarDer", arco, new Vector3(4f, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f), piedraCorte);
        Primitiva(PrimitiveType.Cube, "Dintel", arco, new Vector3(0f, 7.5f, 0f), new Vector3(9.5f, 1.2f, 1.4f), piedraCorte);
        Primitiva(PrimitiveType.Cylinder, "Circulo", arco, new Vector3(-2f, 7.5f, -0.75f), new Vector3(0.8f, 0.05f, 0.8f), simRojo).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Primitiva(PrimitiveType.Cube, "Cuadrado", arco, new Vector3(0f, 7.5f, -0.75f), new Vector3(0.75f, 0.75f, 0.1f), simVerde);
        var triA = new GameObject("Triangulo").transform;
        triA.SetParent(arco, false);
        triA.localPosition = new Vector3(2f, 7.5f, -0.78f);
        triA.localScale = Vector3.one * 0.7f;
        Primitiva(PrimitiveType.Cube, "Base", triA, new Vector3(0f, -0.45f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul);
        Primitiva(PrimitiveType.Cube, "LadoIzq", triA, new Vector3(-0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, 60f);
        Primitiva(PrimitiveType.Cube, "LadoDer", triA, new Vector3(0.3f, 0.07f, 0f), new Vector3(1.2f, 0.2f, 0.12f), simAzul).transform.localRotation = Quaternion.Euler(0f, 0f, -60f);

        var plazaT = new GameObject("PlazaSector9").transform;
        plazaT.position = plaza;
        for (int k = 0; k < 12; k++)
        {
            float a = k * 30f * Mathf.Deg2Rad;
            Primitiva(PrimitiveType.Cylinder, "Losa_" + k, plazaT, new Vector3(Mathf.Cos(a) * 7f, 0.05f, Mathf.Sin(a) * 7f), new Vector3(1.6f, 0.05f, 1.6f), piedraCorte);
        }
        var puntoPlaza = new GameObject("PuntoPlaza").transform;
        puntoPlaza.position = plaza + Vector3.up * 1.5f;
        var hazPlaza = Primitiva(PrimitiveType.Cylinder, "HazPlaza", null, plaza + new Vector3(0f, 7f, 0f), new Vector3(0.35f, 7f, 0.35f), marca);

        // ---------- La red de los humanos (vacía) ----------
        var red = new GameObject("RedHumanos").transform;
        red.position = posRed;
        for (int k = -3; k <= 3; k++)
        {
            Primitiva(PrimitiveType.Cylinder, "CuerdaV_" + k, red, new Vector3(k * 1.2f, 2.5f, 0f), new Vector3(0.08f, 2.5f, 0.08f), redMat);
            var h = Primitiva(PrimitiveType.Cylinder, "CuerdaH_" + k, red, new Vector3(0f, 2.5f + k * 0.7f, 0f), new Vector3(0.08f, 3.8f, 0.08f), redMat);
            h.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        for (int k = 0; k < 4; k++)
            Primitiva(PrimitiveType.Sphere, "Boya_" + k, red, new Vector3(-3.6f + k * 2.4f, 5.4f, 0f), Vector3.one * 0.6f, Mat("BoyaNaranja", new Color(1f, 0.5f, 0.15f)));
        var puntoRed = new GameObject("PuntoRed").transform;
        puntoRed.position = posRed + new Vector3(0f, 1.5f, 3f);
        var hazRed = Primitiva(PrimitiveType.Cylinder, "HazRed", null, posRed + new Vector3(0f, 8f, 0f), new Vector3(0.35f, 8f, 0.35f), marca);

        // ---------- La cueva del arrecife (hueca por dentro) ----------
        var cuevaT = new GameObject("CuevaArrecife").transform;
        cuevaT.position = posCueva;
        for (int k = 0; k < 9; k++) // un anillo de rocas abierto hacia la ciudad (oeste)
        {
            float ang = 230f + k * (260f / 8f); // deja abierto el lado oeste (hacia la ciudad)
            float a = ang * Mathf.Deg2Rad;
            var r = Primitiva(PrimitiveType.Sphere, "RocaCueva_" + k, cuevaT, new Vector3(Mathf.Cos(a) * 7f, 2f, Mathf.Sin(a) * 7f), new Vector3(5f, 6f, 5f), roca);
            r.AddComponent<SphereCollider>();
        }
        Primitiva(PrimitiveType.Cylinder, "Techo", cuevaT, new Vector3(0f, 5.5f, 0f), new Vector3(16f, 0.8f, 16f), roca);
        Primitiva(PrimitiveType.Cylinder, "Suelo", cuevaT, new Vector3(0f, 0.02f, 0f), new Vector3(13f, 0.02f, 13f), oscuro);
        var puntoCueva = new GameObject("PuntoCueva").transform;
        puntoCueva.position = posCueva + new Vector3(-1.5f, 1.5f, 0f);
        var escondites = new Transform[4];
        for (int k = 0; k < 4; k++)
        {
            escondites[k] = new GameObject("Escondite_" + k).transform;
            float a = (-60f + k * 40f) * Mathf.Deg2Rad; // al fondo de la cueva (lado este)
            escondites[k].position = posCueva + new Vector3(Mathf.Cos(a) * 3.2f, 1f, Mathf.Sin(a) * 3.2f);
        }
        var luzCueva = new GameObject("LuzCueva").AddComponent<Light>();
        luzCueva.transform.SetParent(cuevaT, false);
        luzCueva.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        luzCueva.type = LightType.Point;
        luzCueva.color = new Color(0.5f, 0.8f, 1f);
        luzCueva.range = 9f;
        luzCueva.intensity = 1.5f;

        // ---------- Personajes ----------
        GameObject Npc(string nombre, Vector3 pos, Color color, float escala, Vector3 mirarA)
        {
            var piel = Mat("Piel" + nombre.Replace(" ", ""), color);
            var go = CrearPulpo(nombre, pos, piel, blanco, negro, out var cuerpoNpc, out var pielNpc);
            go.transform.localScale = Vector3.one * escala;
            Vector3 m = mirarA - pos; m.y = 0f;
            if (m.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(m);
            var s = go.AddComponent<OctavioSkinColor>();
            var so2 = new SerializedObject(s);
            so2.FindProperty("readKeyboard").boolValue = false;
            so2.FindProperty("neutral").colorValue = color;
            var arr2 = so2.FindProperty("skinRenderers");
            arr2.arraySize = pielNpc.Count;
            for (int i = 0; i < pielNpc.Count; i++) arr2.GetArrayElementAtIndex(i).objectReferenceValue = pielNpc[i];
            so2.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<NPCIdle>().visual = cuerpoNpc;
            return go;
        }

        Vector3 inicio = plaza + new Vector3(0f, 2f, -6f);
        Vector3 sur = plaza + new Vector3(0f, 0f, -40f);
        var perla = Npc("Perla", inicio + new Vector3(-3.5f, -0.5f, -1.5f), new Color(1f, 0.85f, 0.35f), 1.1f, sur);
        string[] nombresPeques = { "Lapa", "Coralina", "Burbuja", "Perlita" };
        Color[] coloresPeques = { new Color(0.95f, 0.6f, 0.4f), new Color(1f, 0.55f, 0.75f), new Color(0.5f, 0.8f, 1f), new Color(1f, 0.88f, 0.45f) };
        var peques = new Transform[4];
        for (int k = 0; k < 4; k++)
            peques[k] = Npc(nombresPeques[k], inicio + new Vector3(4f + (k % 2) * 1.6f, -1f, -1f - (k / 2) * 1.6f), coloresPeques[k], 0.55f, sur).transform;

        // Orcas
        Vector3[] salida = { new Vector3(-12f, 5f, -55f), new Vector3(0f, 6f, -62f), new Vector3(12f, 5f, -55f) };
        Vector3[] llegada = { new Vector3(-8f, 4f, 18f), new Vector3(0f, 5f, 14f), new Vector3(8f, 4f, 18f) };
        Vector3[] ataque = { new Vector3(-22f, 7f, 44f), new Vector3(0f, 9f, 66f), new Vector3(22f, 7f, 48f) };
        var orcas = new Transform[3];
        var llegadaT = new Transform[3];
        var ataqueT = new Transform[3];
        int[] orden = { 1, 0, 2 }; // la primera de la lista es la Orca Mayor, la del centro
        for (int j = 0; j < 3; j++)
        {
            int k = orden[j];
            orcas[j] = CrearOrca(j == 0 ? "OrcaMayor" : "Orca_" + j, salida[k], j == 0 ? 1.25f : 1f, orcaNegro, orcaBlanco, blanco, negro).transform;
            llegadaT[j] = new GameObject("LlegadaOrca_" + j).transform; llegadaT[j].position = llegada[k];
            ataqueT[j] = new GameObject("AtaqueOrca_" + j).transform; ataqueT[j].position = ataque[k];
        }

        // ---------- Octavio (ya mayor) ----------
        var octavio = CrearPulpo("Octavio", inicio, pielOcta, blanco, negro, out var cuerpo, out var pielO);
        octavio.transform.localScale = Vector3.one * 1.3f;
        octavio.transform.rotation = Quaternion.LookRotation(Vector3.back);
        var rb = octavio.AddComponent<Rigidbody>();
        rb.useGravity = false;
        octavio.AddComponent<SphereCollider>().radius = 0.6f;
        var swim = octavio.AddComponent<OctavioSwimController>();
        SetRef(swim, "visual", cuerpo);
        var skin = octavio.AddComponent<OctavioSkinColor>();
        var so = new SerializedObject(skin);
        var arr = so.FindProperty("skinRenderers");
        arr.arraySize = pielO.Count;
        for (int i = 0; i < pielO.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pielO[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Cámara, luz y ambiente (anochecer) ----------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = inicio + new Vector3(0f, 2f, 6f);
            cam.transform.LookAt(octavio.transform);
            cam.farClipPlane = 300f;
            SetRef(cam.gameObject.AddComponent<ThirdPersonCamera>(), "target", octavio.transform);
        }
        var sol = Object.FindAnyObjectByType<Light>();
        if (sol != null && sol != luzCueva)
        {
            sol.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
            sol.color = new Color(0.55f, 0.65f, 0.9f);
            sol.intensity = 0.45f;
        }
        var amb = new GameObject("Ambiente").AddComponent<UnderwaterAmbience>();
        var sa = new SerializedObject(amb);
        sa.FindProperty("waterColor").colorValue = new Color(0.03f, 0.13f, 0.22f);
        sa.FindProperty("fogDensity").floatValue = 0.022f;
        sa.FindProperty("ambientColor").colorValue = new Color(0.16f, 0.2f, 0.28f);
        sa.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Controlador ----------
        var ctrl = new GameObject("Etapa5").AddComponent<Etapa5Controller>();
        ctrl.perla = perla.transform;
        ctrl.pequenos = peques;
        ctrl.orcas = orcas;
        ctrl.llegadaOrcas = llegadaT;
        ctrl.ataqueOrcas = ataqueT;
        ctrl.red = puntoRed;
        ctrl.marcaRed = hazRed;
        ctrl.plaza = puntoPlaza;
        ctrl.marcaPlaza = hazPlaza;
        ctrl.cueva = puntoCueva;
        ctrl.escondites = escondites;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath5);
        AnadirABuild(ScenePath5);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = octavio;
        Debug.Log("[Octavianos] Etapa 5 creada. Pulsa Play. En el editor, F9 se salta los recorridos.");
    }

    // Una orca hecha con primitivas: cuerpo negro, barriga y manchas blancas, aletas. Mira hacia +Z.
    static GameObject CrearOrca(string nombre, Vector3 pos, float escala, Material negroM, Material blancoM, Material ojoB, Material ojoN)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.position = pos;
        raiz.transform.localScale = Vector3.one * escala;
        var visual = new GameObject("Visual").transform;
        visual.SetParent(raiz.transform, false);
        var cuerpo = Primitiva(PrimitiveType.Capsule, "Cuerpo", visual, Vector3.zero, new Vector3(2.4f, 3.6f, 2.4f), negroM);
        cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var barriga = Primitiva(PrimitiveType.Capsule, "Barriga", visual, new Vector3(0f, -0.6f, 0.6f), new Vector3(1.7f, 2.4f, 1.4f), blancoM);
        barriga.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Primitiva(PrimitiveType.Sphere, "ManchaI", visual, new Vector3(-0.95f, 0.45f, 2.3f), new Vector3(0.5f, 0.45f, 1.1f), blancoM);
        Primitiva(PrimitiveType.Sphere, "ManchaD", visual, new Vector3(0.95f, 0.45f, 2.3f), new Vector3(0.5f, 0.45f, 1.1f), blancoM);
        Primitiva(PrimitiveType.Sphere, "OjoI", visual, new Vector3(-1.05f, 0.25f, 2.7f), Vector3.one * 0.25f, ojoN);
        Primitiva(PrimitiveType.Sphere, "OjoD", visual, new Vector3(1.05f, 0.25f, 2.7f), Vector3.one * 0.25f, ojoN);
        var dorsal = Primitiva(PrimitiveType.Cube, "AletaDorsal", visual, new Vector3(0f, 1.9f, -0.4f), new Vector3(0.25f, 2.2f, 1.2f), negroM);
        dorsal.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        Primitiva(PrimitiveType.Cube, "Cola", visual, new Vector3(0f, 0f, -3.9f), new Vector3(3f, 0.2f, 1f), negroM);
        var aI = Primitiva(PrimitiveType.Cube, "AletaI", visual, new Vector3(-1.5f, -0.6f, 1f), new Vector3(1.6f, 0.15f, 0.8f), negroM);
        aI.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        var aD = Primitiva(PrimitiveType.Cube, "AletaD", visual, new Vector3(1.5f, -0.6f, 1f), new Vector3(1.6f, 0.15f, 0.8f), negroM);
        aD.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        var idle = raiz.AddComponent<NPCIdle>();
        idle.visual = visual;
        idle.bobAmplitude = 0.3f;
        idle.bobSpeed = 0.8f;
        idle.lookDistance = 0f; // las orcas no se giran a mirar a Octavio
        raiz.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        return raiz;
    }

    // =====================================================================
    //  REALIDAD AUMENTADA · escena de prueba para el móvil (cualquier móvil)
    // =====================================================================

    const string ScenePathRA = "Assets/Scenes/RA_Prueba.unity";

    [MenuItem("Octavianos/Crear escena RA de prueba (móvil)")]
    public static void CrearEscenaRA()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        luz.intensity = 1.1f;

        // Sesión de RA (solo se activa si el móvil tiene ARCore)
        var sesionGO = new GameObject("AR Session");
        var sesion = sesionGO.AddComponent<UnityEngine.XR.ARFoundation.ARSession>();
        sesionGO.AddComponent<UnityEngine.XR.ARFoundation.ARInputManager>();

        // XR Origin con la cámara
        var origenGO = new GameObject("XR Origin");
        var origen = origenGO.AddComponent<Unity.XR.CoreUtils.XROrigin>();
        var planos = origenGO.AddComponent<UnityEngine.XR.ARFoundation.ARPlaneManager>();
        var rayos = origenGO.AddComponent<UnityEngine.XR.ARFoundation.ARRaycastManager>();
        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(origenGO.transform, false);
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offset.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 30f;
        camGO.AddComponent<AudioListener>();
        var camaraAR = camGO.AddComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();
        var fondoAR = camGO.AddComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();
        var tpd = camGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        var pos = new UnityEngine.InputSystem.InputAction("Posicion", binding: "<HandheldARInputDevice>/devicePosition");
        var rot = new UnityEngine.InputSystem.InputAction("Rotacion", binding: "<HandheldARInputDevice>/deviceRotation");
        tpd.positionInput = new UnityEngine.InputSystem.InputActionProperty(pos);
        tpd.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rot);
        var simple = camGO.AddComponent<CamaraSimpleRA>();
        origen.Camera = cam;
        origen.CameraFloorOffsetObject = offset;

        // Octavio, de unos 25 cm
        var piel = Mat("PielOctavio", new Color(0.85f, 0.45f, 0.35f));
        var octavio = CrearPulpo("Octavio", new Vector3(0f, -0.35f, 1.2f), piel, Mat("OjoBlanco", Color.white), Mat("OjoPupila", new Color(0.05f, 0.05f, 0.05f)), out var cuerpo, out _);
        octavio.transform.localScale = Vector3.one * 0.2f;
        var idle = octavio.AddComponent<NPCIdle>();
        idle.visual = cuerpo;
        idle.bobAmplitude = 0.08f;
        idle.lookDistance = 0f;

        // Texto de estado
        var canvas = UIKit.CreateCanvas("UI", 10);
        var panel = UIKit.Panel(canvas.transform, "Panel", new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.98f), new Color(0f, 0f, 0f, 0.55f));
        var texto = UIKit.Label(panel.transform, "Estado", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), "", 34, Color.white, TextAnchor.MiddleCenter);

        var mision = new GameObject("MisionRA").AddComponent<MisionRA>();
        mision.sesion = sesion;
        mision.camaraAR = camaraAR;
        mision.fondoAR = fondoAR;
        mision.seguimientoAR = tpd;
        mision.planos = planos;
        mision.rayos = rayos;
        mision.camaraSimple = simple;
        mision.octavio = octavio.transform;
        mision.estado = texto;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePathRA);
        AnadirABuild(ScenePathRA);
        AssetDatabase.SaveAssets();
        Debug.Log("[Octavianos] Escena RA creada. En el editor usa la webcam (modo cámara); en el móvil elige sola el modo.");
    }

    static Material MatEmisiva(string nombre, Color color, float intensidad)
    {
        var m = Mat(nombre, color);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * intensidad);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    static float DistanciaAlSegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        p.y = a.y = b.y = 0f;
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(p, a + ab * t);
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
