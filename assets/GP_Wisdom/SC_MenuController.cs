using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_MenuController : MonoBehaviour
{
    #region Variables
    [field: Header("Pause Menu")]
    [Tooltip("Gets and sets if game is paused.")] public bool GamePaused { get; set; }
    [Tooltip("Sets game to pause for mobile.")] public bool PauseMobile { get; private set; }
    [Tooltip("Sets game pause delay for player action release input.")] public float PauseDelay { get; private set; }

    [Header("Components")]
    [Tooltip("Gets pause menu GameObject.")] private GameObject pauseMenuUI;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets GameController script component for setting scene transitions.")] private SC_GameController gameController;
    [Tooltip("Gets mobile input UI GameObject to toggle UI when game paused.")] private GameObject mobileInputUI;
    #endregion

    #region Call Functions
    void Start() => Initialization();
    void Update() => PauseMenuControl();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        // Set in the start so the audio menu items are initialized in awake.
        audioController = GetComponent<SC_AudioController>();
        pauseMenuUI = GameObject.Find("PauseMenu");
        if (pauseMenuUI != null && pauseMenuUI.activeSelf) { pauseMenuUI.SetActive(false); }
        if (GameObject.FindWithTag("MobileUI") != null) { mobileInputUI = GameObject.FindWithTag("MobileUI"); }
        gameController = GameObject.FindWithTag("GameController").GetComponent<SC_GameController>();
    }
    /// <summary> Pause menu control. </summary>
    private void PauseMenuControl()
    {
        if (pauseMenuUI != null)
        {
            if (PauseDelay > 0 && !GamePaused && !PauseMobile) { PauseDelay -= Time.deltaTime; }

            bool pause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            if (pause)
            {
                if (GamePaused)
                {
                    pauseMenuUI.SetActive(false);
                    Time.timeScale = 1f;
                    GamePaused = false;
                }
                else
                {
                    pauseMenuUI.SetActive(true);
                    Time.timeScale = 0f;
                    GamePaused = true;
                    PauseDelay = 0.1f;
                }
            }
            if (PauseMobile)
            {
                pauseMenuUI.SetActive(true);
                if (mobileInputUI != null) { mobileInputUI.SetActive(false); }
                Time.timeScale = 0f;
                GamePaused = true;
                PauseMobile = false;
                PauseDelay = 0.1f;
            }
        }
    }
    /// <summary> Menu item control set by type ("begin", "return", "credits", "exit", "pauseReturn", "restart", "pauseExit"). </summary>
    public void MenuControl(string menuItem)
    {
        switch (menuItem)
        {
            case "begin":
                audioController.PlayAudio("play", "Select", 0, false);
                StartCoroutine(gameController.SceneTransition("wipeIn", "SE_Testlevel", gameController.sceneTransitionSpeed));
                break;
            case "return":
                audioController.PlayAudio("play", "Select", 0, false);
                if (gameController.GetSaveData.levelProgress != null)
                {
                    StartCoroutine(gameController.SceneTransition("wipeIn", gameController.GetSaveData.levelProgress, gameController.sceneTransitionSpeed));
                }
                else
                {
                    StartCoroutine(gameController.SceneTransition("wipeIn", "SE_Testlevel", gameController.sceneTransitionSpeed));
                }
                break;
            case "settings":
                audioController.PlayAudio("play", "Navigate", 0, false);
                break;
            case "credits":
                audioController.PlayAudio("play", "Select", 0, false);
                StartCoroutine(gameController.SceneTransition("wipeIn", "SE_Testlevel", gameController.sceneTransitionSpeed));
                break;
            case "exit":
                audioController.PlayAudio("play", "Select", 0, false);
                StartCoroutine(gameController.SceneTransition("exit", "", gameController.sceneTransitionSpeed));
                break;
            case "pauseReturn":
                audioController.PlayAudio("play", "Select", 0, false);
                pauseMenuUI.SetActive(false);
                if (mobileInputUI != null) { mobileInputUI.SetActive(true); }
                Time.timeScale = 1f;
                GamePaused = false;
                break;
            case "pauseRestart":
                audioController.PlayAudio("play", "Select", 0, false);
                StartCoroutine(gameController.SceneTransition(gameController.sceneTransitionIn.ToString(), SceneManager.GetActiveScene().name, gameController.sceneTransitionSpeed));
                break;
            case "pauseExit":
                audioController.PlayAudio("play", "Select", 0, false);
                StartCoroutine(gameController.SceneTransition(gameController.sceneTransitionIn.ToString(), "SE_MainMenu", gameController.sceneTransitionSpeed));
                break;
            default:
                Debug.LogWarning($"Menu item: {menuItem} not found!");
                break;
        }
    }
    /// <summary> Set full screen on or off. </summary>
    public void SetFullScreen(bool isFullScreen) => Screen.fullScreen = isFullScreen;
    #endregion
}