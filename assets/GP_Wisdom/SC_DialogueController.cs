using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable] public class Dialogue
{
    [Tooltip("Lists selectable characters.")] public enum Characters { Unknown, Budburra, Babun, Hope, Avang, Gira, Maroochy, Coolum, Ninderry, Wakan };
    [Header("Start Dialogue")]
    [Tooltip("Sets start dialogue character names.")] public Characters[] startNames;
    [Tooltip("Sets start dialogue character face moods for each sentence.")] public Sprite[] startFaces;
    [Tooltip("Sets start dialogue sentences into the queue.")][TextArea(3, 10)] public string[] startSentences;

    [Header("End Dialogue")]
    [Tooltip("Sets end dialogue character names.")] public Characters[] endNames;
    [Tooltip("Sets end dialogue character face moods for each sentence.")] public Sprite[] endFaces;
    [Tooltip("Sets end dialogue sentences into the queue.")][TextArea(3, 10)] public string[] endSentences;
}
public class SC_DialogueController : MonoBehaviour
{
    #region Variables
    [Tooltip("Sets dialogue type sentences speed (letter per second) Lower = faster.")][Range(0.01f, 0.06f)][SerializeField] private float dialogueTypeSpeed;
    [Tooltip("Sets dialogue move speed.")][Range(0.5f, 2f)][SerializeField] private float dialogueMoveSpeed;
    [Tooltip("Gets and sets dialogue.")][SerializeField] private Dialogue dialogue;
    [Tooltip("Gets and sets first in first out dialogue sentences.")] private Queue<string> dialogueSentences;
    [Tooltip("Gets and sets if dialogue has been spoken to initiate end dialogue. False = not spoken, true = spoken.")] private bool dialogueSpoken;
    [Tooltip("Gets and sets current dialogue line to display corresponding name and face.")] private int currentDialogueIndex;
    [Tooltip("Gets if dialogue sentence (coroutine) has completed typing.")] private bool dialogueSentenceTyped;
    [Tooltip("Gets current dialogue sentence to type to instantly display if necessary.")] private string dialogueSentenceTyping;
    [Tooltip("Gets if dialogue is in progress to ignore dialogue input.")] private bool dialogueInProgress;
    [Tooltip("Sets dialogue start delay to prevent instant dialogue skip on first sentence.")] private float dialogueStartDelay;
    [Tooltip("Gets dialogue start y position.")] private float dialogueStartYPosition;

    [Header("Components")]
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets InteractiveController script component to set call once.")] private SC_InteractiveController interactiveController;
    [Tooltip("Gets PlayerController script component to disable input.")] private SC_PlayerController playerController;
    [Tooltip("Gets dialogue RectTransform component.")] private RectTransform dialogueTransform;
    [Tooltip("Gets dialogue finish icon GameObject component.")] private GameObject dialogueFinishIcon;
    [Tooltip("Gets dialogue finish icon Image component.")] private Image dialogueFinishIconImage;
    [Tooltip("Gets dialogue face Image component.")] private Image dialogueFace;
    [Tooltip("Gets dialogue name TextMeshPro UI component.")] private TextMeshProUGUI dialogueNameText;
    [Tooltip("Gets dialogue sentences TextMeshPro UI component.")] private TextMeshProUGUI dialogueSentenceText;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update() => GetDialogueInput();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        dialogueSentences = new Queue<string>();
        audioController = GetComponent<SC_AudioController>();
        interactiveController = GetComponent<SC_InteractiveController>();

        if (dialogueTransform == null)
        {
            playerController = GameObject.FindWithTag("Player").GetComponent<SC_PlayerController>();
            dialogueTransform = GameObject.Find("Dialogue").GetComponent<RectTransform>();
            dialogueFinishIcon = GameObject.Find("DialogueFinishIcon");
            dialogueFinishIconImage = dialogueFinishIcon.GetComponent<Image>();
            dialogueFace = GameObject.Find("DialogueFace").GetComponent<Image>();
            dialogueNameText = GameObject.Find("DialogueName").GetComponent<TextMeshProUGUI>();
            dialogueSentenceText = GameObject.Find("DialogueSentence").GetComponent<TextMeshProUGUI>();
            dialogueStartYPosition = dialogueTransform.anchoredPosition.y;
            dialogueFinishIcon.SetActive(false);
        }
    }
    /// <summary> Get input to display next dialogue line. </summary>
    private void GetDialogueInput()
    {
        if (dialogueInProgress)
        {
            if (dialogueStartDelay > 0f) { dialogueStartDelay -= Time.deltaTime; }
            if (dialogueStartDelay <= 0f && Input.GetButtonDown("Fire1") && dialogueSentenceTyped)
            {
                currentDialogueIndex++;
                audioController.PlayAudio("play", "Select", false, 0);
                DialogueNextLine();
            }
            else if (dialogueStartDelay <= 0f && Input.GetButtonDown("Fire1") && !dialogueSentenceTyped)
            {
                StopAllCoroutines();
                dialogueSentenceText.text = dialogueSentenceTyping;
                audioController.PlayAudio("play", "Navigate", false, 0);
                dialogueFinishIcon.SetActive(true);
                dialogueSentenceTyped = true;
            }
            if (dialogueFinishIcon.activeSelf)
            {
                dialogueFinishIconImage.color = Color.Lerp(new(255f, 255f, 255f, 1f), new(255f, 255f, 255f, 0f), Mathf.PingPong(Time.time * 1.5f, 1f));
            }
        }
        else if (dialogueTransform.anchoredPosition.y <= dialogueStartYPosition && interactiveController.CallOnce)
        {
            interactiveController.CallOnce = false;
        }
    }
    /// <summary> Call dialogue start from interactive unity event. </summary>
    public void SetDialogueStart() => DialogueStart(dialogue);
    /// <summary> Dialogue start by clearing previous sentences in the queue and loading next dialogue array sentences. </summary>
    private void DialogueStart(Dialogue dialogue)
    {
        dialogueInProgress = true;
        currentDialogueIndex = 0;
        dialogueStartDelay = 0.05f;
        playerController.InputDisabled = true;
        dialogueSentences.Clear();
        LeanTween.moveY(dialogueTransform, 0f, dialogueMoveSpeed).setEaseOutBack();
        audioController.PlayAudio("play", "Dialogue Start", false, 0);

        if (!dialogueSpoken)
        {
            dialogueNameText.text = dialogue.startNames[currentDialogueIndex].ToString();
            dialogueFace.sprite = dialogue.startFaces[currentDialogueIndex];
            foreach (string sentence in dialogue.startSentences) { dialogueSentences.Enqueue(sentence); }
        }
        else
        {
            dialogueNameText.text = dialogue.endNames[currentDialogueIndex].ToString();
            dialogueFace.sprite = dialogue.endFaces[currentDialogueIndex];
            foreach (string sentence in dialogue.endSentences) { dialogueSentences.Enqueue(sentence); }
        }

        DialogueNextLine();
    }
    /// <summary> Display next dialogue line. </summary>
    public void DialogueNextLine()
    {
        if (dialogueSentences.Count == 0)
        {
            DialogueEnd();
            return;
        }

        if (!dialogueSpoken)
        {
            dialogueNameText.text = dialogue.startNames[currentDialogueIndex].ToString();
            dialogueFace.sprite = dialogue.startFaces[currentDialogueIndex];
        }
        else
        {
            dialogueNameText.text = dialogue.endNames[currentDialogueIndex].ToString();
            dialogueFace.sprite = dialogue.endFaces[currentDialogueIndex];
        }

        dialogueSentenceTyping = dialogueSentences.Dequeue();
        StartCoroutine(DialogueTypeSentence(dialogueSentenceTyping));
    }
    /// <summary> Dialogue end. </summary>
    private void DialogueEnd()
    {
        dialogueInProgress = false;
        dialogueSentenceTyped = false;
        dialogueSpoken = true;
        playerController.InputDisabled = false;
        dialogueFinishIcon.SetActive(false);
        LeanTween.moveY(dialogueTransform, dialogueStartYPosition, dialogueMoveSpeed).setEaseInOutBack();
        audioController.PlayAudio("play", "Dialogue End", false, 0);
    }
    /// <summary> Dialogue letters (characters) display iteratively. </summary>
    private IEnumerator DialogueTypeSentence(string sentence)
    {
        dialogueFinishIcon.SetActive(false);
        dialogueSentenceTyped = false;
        dialogueSentenceText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueSentenceText.text += letter;
            audioController.PlayAudio("play", "Typing", false, 0);
            yield return new WaitForSeconds(dialogueTypeSpeed);
        }
        dialogueFinishIcon.SetActive(true);
        dialogueSentenceTyped = true;
    }
    #endregion
}