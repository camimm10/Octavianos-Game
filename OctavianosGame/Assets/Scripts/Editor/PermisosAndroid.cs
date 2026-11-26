using System.IO;
using UnityEditor.Android;

/// <summary>
/// Al generar el proyecto de Android, añade al manifiesto los permisos que Unity no pone solo:
///  - ACTIVITY_RECOGNITION: para el contador de pasos (sin él, Android nunca da los pasos).
///  - Ubicación: para medir la distancia al caminar y comprobar las misiones de recoger basura.
/// La app los pide al usuario cuando hacen falta (y funciona igual si los rechaza).
/// </summary>
public class PermisosAndroid : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 10;

    static readonly string[] Permisos =
    {
        "android.permission.ACTIVITY_RECOGNITION",
        "android.permission.ACCESS_FINE_LOCATION",
        "android.permission.ACCESS_COARSE_LOCATION",
    };

    public void OnPostGenerateGradleAndroidProject(string ruta)
    {
        string manifiesto = Path.Combine(ruta, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifiesto)) return;
        string s = File.ReadAllText(manifiesto);
        string extra = "";
        foreach (var p in Permisos)
            if (!s.Contains("\"" + p + "\"")) extra += $"  <uses-permission android:name=\"{p}\" />\n";
        if (!s.Contains("android.hardware.sensor.stepcounter"))
            extra += "  <uses-feature android:name=\"android.hardware.sensor.stepcounter\" android:required=\"false\" />\n";
        if (!s.Contains("android.hardware.location.gps"))
            extra += "  <uses-feature android:name=\"android.hardware.location.gps\" android:required=\"false\" />\n";
        if (extra.Length == 0) return;
        int i = s.IndexOf("<application", System.StringComparison.Ordinal);
        if (i < 0) return;
        File.WriteAllText(manifiesto, s.Insert(i, extra + "  "));
    }
}
