using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Un personaje que habla con Octavio cuando se acerca.
/// Si autoStart está activo, la conversación empieza sola al llegar a talkDistance;
/// si no, aparece "[E] Hablar".
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    public float talkDistance = 3.5f;
    public bool autoStart = true;
    public bool onlyOnce = true;

    [Header("Objetivos que se muestran arriba")]
    public string objectiveBefore = "Acércate al pulpo que te está mirando";
    public string objectiveAfter = "";

    public DialogueLine[] lines;

    [Header("Qué pasa al terminar")]
    [Tooltip("Si se asigna, este guía empieza a nadar al terminar la conversación")]
    public NPCGuide guideAfter;
    public UnityEvent onFinished;

    OctavioSwimController player;
    OctavioSkinColor playerSkin;
    OctavioSkinColor mySkin;
    bool talking;
    bool done;

    void Start()
    {
        player = FindAnyObjectByType<OctavioSwimController>();
        if (player != null) playerSkin = player.GetComponent<OctavioSkinColor>();
        mySkin = GetComponent<OctavioSkinColor>();
        DialogueUI.Get().SetObjective(objectiveBefore);
    }

    void Update()
    {
        if (player == null || talking || (done && onlyOnce)) return;

        var ui = DialogueUI.Get();
        bool near = Vector3.Distance(player.transform.position, transform.position) <= talkDistance;

        if (near && autoStart) { StartTalk(); return; }

        ui.SetPrompt(near ? "[E] Hablar" : "");
        if (near && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            StartTalk();
    }

    public void StartTalk()
    {
        talking = true;
        if (player != null) player.CanMove = false;
        DialogueUI.Get().SetObjective(""); // durante la conversación no se muestra el objetivo

        // Mirarse el uno al otro
        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

        DialogueUI.Get().Play(lines, OnLine, OnEnd);
    }

    void OnLine(DialogueLine line)
    {
        var skin = line.isPlayer ? playerSkin : mySkin;
        if (skin != null && line.skinColor.a > 0f) skin.SetColor(line.skinColor);
    }

    void OnEnd()
    {
        talking = false;
        done = true;
        if (player != null) player.CanMove = true;
        if (playerSkin != null) playerSkin.ResetColor();
        if (mySkin != null) mySkin.ResetColor();

        DialogueUI.Get().SetObjective(objectiveAfter);
        if (guideAfter != null) guideAfter.Begin();
        onFinished?.Invoke();
    }
}
