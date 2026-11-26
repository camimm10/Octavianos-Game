using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Solo en el editor: al pulsar Play, pone la vista Game a la escala mínima
/// para que se vea la imagen entera (con el escalado de Windows al 150 %
/// Unity la amplía a 1.5x y corta los textos de la interfaz).
/// Se puede desactivar en el menú Octavianos → Ajustar vista Game al dar Play.
/// </summary>
[InitializeOnLoad]
static class GameViewAutoScale
{
    const string PrefKey = "Octavianos.GameViewAutoScale";
    const string MenuPath = "Octavianos/Ajustar vista Game al dar Play";

    static int framesLeft;

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    static GameViewAutoScale()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(MenuPath)]
    static void Toggle() => Enabled = !Enabled;

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !Enabled) return;
        // Esperamos unos frames a que Unity termine de maximizar la ventana
        framesLeft = 10;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (--framesLeft > 0) return;
        EditorApplication.update -= Tick;
        FitAllGameViews();
    }

    static void FitAllGameViews()
    {
        try
        {
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo snapZoom = gameViewType.GetMethod("SnapZoom", flags, null, new[] { typeof(float) }, null);
            PropertyInfo minScale = gameViewType.GetProperty("minScale", flags);
            if (snapZoom == null || minScale == null) return;

            foreach (var view in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                float min = (float)minScale.GetValue(view);
                snapZoom.Invoke(view, new object[] { min });
                ((EditorWindow)view).Repaint();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Octavianos] No se pudo ajustar la vista Game automáticamente: " + e.Message);
        }
    }
}
