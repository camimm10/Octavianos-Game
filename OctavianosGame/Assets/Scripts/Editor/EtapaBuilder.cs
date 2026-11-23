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

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath3);
        AnadirABuild(ScenePath3);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = octavio;
        Debug.Log("[Octavianos] Etapa 3 creada. Pulsa Play. En el editor, F9 se salta los recorridos.");
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
