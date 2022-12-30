using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable] public class SaveData
{
    [Tooltip("Gets player level progress.")] public string levelProgress;
    [Tooltip("Gets player level progress respawn position.")] public float[] positionProgress;
    // Constructor (class set-up functions) to get data from another script.
    public SaveData(SC_GameController gameController)
    {
        levelProgress = gameController.CurrentLevel;
        positionProgress = new float[3];
        positionProgress[0] = gameController.RespawnPoint.x;
        positionProgress[1] = gameController.RespawnPoint.y;
        positionProgress[2] = gameController.RespawnPoint.z;
    }
}
public class SC_GameController : MonoBehaviour
{
    #region Variables
    [Header("Scene Manager")]
    [Tooltip("Sets next scene to transition.")][SerializeField] private string nextSceneName;
    [Tooltip("Sets scene transition speed. Higher = slower transition.")][Range(1f, 3f)] public float sceneTransitionSpeed;
    [Tooltip("Gets and sets current scene transition in.")] public transitionType sceneTransitionIn = transitionType.wipeIn;
    [Tooltip("Gets and sets current scene transition out.")] public transitionType sceneTransitionOut = transitionType.wipeOut;
    [Tooltip("List types of scene transitions.")] public enum transitionType { wipeIn, wipeOut, fadeIn, fadeOut, circleIn, circleOut };
    [Tooltip("Gets list of audio category names to set starting scene audio.")][SerializeField] private string[] sceneAudioCategory;
    [Tooltip("Sets audio fade speed for scene transition. Lower = slower transition.")][Range(0.3f, 1f)][SerializeField] private float sceneAudioFadeSpeed;

    [Header("Saving")]
    [Tooltip("Gets and sets custom file saving path.")] private static string filePath;
    [Tooltip("Gets SaveData variables to get and set saving data.")] public SaveData GetSaveData { get; private set; }
    [Tooltip("Gets and sets respawn point for the player.")] public Vector3 RespawnPoint { get; set; }
    [Tooltip("Gets and sets current level name.")] public string CurrentLevel { get; private set; }

    [Header("Components")]
    [Tooltip("Gets scene transition game object.")] private GameObject sceneTransition;
    [Tooltip("Gets scene transition rect transform.")] private RectTransform sceneTransitionTransform;
    [Tooltip("Gets scene transition canvas group.")] private CanvasGroup sceneTransitionAlpha;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets MenuController script variables to get and set game pause.")] private SC_MenuController menuController;
    [Tooltip("Gets GameController script variables to get and set saving data.")] private SC_GameController gameController;
    [Tooltip("Gets CircleController script component for circle scene transition.")] private SC_CircleController circleController;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Start() => SetSceneAudio();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        filePath = Application.persistentDataPath + "/preserve.sav";
        audioController = GetComponent<SC_AudioController>();
        menuController = GameObject.Find("PR_UI").GetComponent<SC_MenuController>();
        sceneTransition = GameObject.Find("SceneTransition");
        if (SceneManager.GetActiveScene().name != "SE_MainMenu")
        {
            CurrentLevel = SceneManager.GetActiveScene().name;
        }
        if (sceneTransition != null)
        {
            sceneTransitionTransform = sceneTransition.GetComponent<RectTransform>();
            sceneTransitionAlpha = sceneTransition.GetComponent<CanvasGroup>();
        }     
    }

    // Scene Transition
    /// <summary> Set scene and audio transition when scene is loaded. </summary>
    private void SetSceneAudio()
    {
        if (sceneTransition != null && audioController != null)
        {
            StartCoroutine(SceneTransition(sceneTransitionOut.ToString(), "", sceneTransitionSpeed));
            for (int i = 0; i < sceneAudioCategory.Length; i++)
            {
                StartCoroutine(audioController.FadeAudio("fadeIn", sceneAudioCategory[i], true, 0, sceneAudioFadeSpeed));
            }  
        }
    }
    /// <summary> Scene transition set by type, specified scene and speed ("wipeIn", "wipeOut", "fadeIn", "fadeOut", "circleIn", "circleOut", "exit" = exit app). </summary>
    public IEnumerator SceneTransition(string type, string sceneName, float transitionSpeed)
    {
        float outsideCameraLeft = -2150f, outsideCameraRight = 2150f, time, radiusStart, radiusEnd;
        Vector2 transitionTargetPosition; //, circleTarget;
        switch (type)
        {
            case "wipeIn":
                sceneTransitionTransform.anchoredPosition = new(outsideCameraLeft, 0f);
                transitionTargetPosition = new(0f, 0f);
                LeanTween.move(sceneTransitionTransform, transitionTargetPosition, transitionSpeed).setIgnoreTimeScale(true);

                yield return new WaitForSecondsRealtime(transitionSpeed + 0.5f);
                if (menuController != null && menuController.GamePaused) { Time.timeScale = 1f; menuController.GamePaused = false; }
                SceneManager.LoadScene(sceneName);
                break;
            case "wipeOut":
                sceneTransitionTransform.anchoredPosition = new(0f, 0f);
                transitionTargetPosition = new(outsideCameraRight, 0f);
                LeanTween.move(sceneTransitionTransform, transitionTargetPosition, transitionSpeed).setEase(LeanTweenType.easeOutQuint).setIgnoreTimeScale(true);
                
                yield return new WaitForSecondsRealtime(transitionSpeed + 0.5f);
                sceneTransitionTransform.anchoredPosition = new(outsideCameraLeft, 0f);
                break;
            case "fadeIn":
                LeanTween.alphaCanvas(sceneTransitionAlpha, 0f, 0f);
                sceneTransitionTransform.anchoredPosition = new(0f, 0f);
                LeanTween.alphaCanvas(sceneTransitionAlpha, 1f, transitionSpeed).setIgnoreTimeScale(true);

                yield return new WaitForSecondsRealtime(transitionSpeed + 0.5f);
                if (menuController != null && menuController.GamePaused) { Time.timeScale = 1f; menuController.GamePaused = false; }
                SceneManager.LoadScene(sceneName);
                break;
            case "fadeOut":
                sceneTransitionTransform.anchoredPosition = new(0f, 0f);
                LeanTween.alphaCanvas(sceneTransitionAlpha, 0f, transitionSpeed).setIgnoreTimeScale(true);

                yield return new WaitForSecondsRealtime(transitionSpeed + 0.5f);
                sceneTransitionTransform.anchoredPosition = new(outsideCameraLeft, 0f);
                LeanTween.alphaCanvas(sceneTransitionAlpha, 1f, 0f);
                break;        
            case "circleIn":
                radiusStart = circleController.RadiusCircleSize; 
                radiusEnd = 0f;
                time = 0f;
                //circleTarget = new Vector2(Screen.width / 2f, Screen.height / 2f);
                //circleController.shaderCircleTarget = circleTarget;
                circleController.RadiusCircle = radiusStart;
                circleController.UpdateShader();              
                while (time < 1f)
                {
                    circleController.RadiusCircle = Mathf.Lerp(radiusStart, radiusEnd, time);
                    time += Time.deltaTime / transitionSpeed;
                    circleController.UpdateShader();
                    yield return null;
                }
                circleController.RadiusCircle = radiusEnd;
                circleController.UpdateShader();
                if (menuController != null && menuController.GamePaused) { Time.timeScale = 1f; menuController.GamePaused = false; }
                SceneManager.LoadScene(sceneName);
                break;
            case "circleOut":
                radiusStart = 0f;
                radiusEnd = circleController.RadiusCircleSize;
                time = 0f;
                //circleTarget = new Vector2(Screen.width / 2f, Screen.height / 2f);
                //circleController.shaderCircleTarget = circleTarget;
                circleController.RadiusCircle = radiusStart;
                circleController.UpdateShader();
                while (time < 1f)
                {
                    circleController.RadiusCircle = Mathf.Lerp(radiusStart, radiusEnd, time);
                    time += Time.deltaTime / transitionSpeed;
                    circleController.UpdateShader();
                    yield return null;
                }
                circleController.RadiusCircle = radiusEnd;
                circleController.UpdateShader();
                break;
            case "exit":
                sceneTransitionTransform.anchoredPosition = new(outsideCameraLeft, 0f);
                transitionTargetPosition = new(0f, 0f);
                LeanTween.move(sceneTransitionTransform, transitionTargetPosition, transitionSpeed);

                yield return new WaitForSeconds(transitionSpeed + 0.5f);
                Application.Quit();
                break;
            default:
                Debug.LogWarning($"Transition type: {type} not found!");
                break;
        }
    }

    // Saving
    /// <summary> Write data into converted binary file (encryption). </summary>
    public static void WriteData(SC_GameController gameController)
    {
        // Create binary formatter and filestream to stream data contained in the saved binary file to write.
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        FileStream fileStream = new(filePath, FileMode.Create);
        // Create sava data by executing SavaData constructor code and write data into binary file and close file stream.
        SaveData data = new(gameController);
        binaryFormatter.Serialize(fileStream, data);
        fileStream.Close();
    }
    /// <summary> Open binary file data into readable data. </summary>
    public static SaveData ReadData()
    {
        if (File.Exists(filePath))
        {
            // Create binary formatter and filestream to stream data contained in the saved binary file to read.
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = new(filePath, FileMode.Open);
            // Read data in binary file to convert into readable format casted as SaveData to return and close file stream.
            SaveData data = binaryFormatter.Deserialize(fileStream) as SaveData;
            fileStream.Close();
            return data;
        }
        else
        {
            Debug.LogWarning($"Save file in {filePath} file path not found!");
            return null;
        }
    }
    /// <summary> Save data trigger. </summary>
    public void SaveData()
    {
        WriteData(gameController);
    }
    /// <summary> Load data trigger. </summary>
    public void LoadData()
    {
        // Read data and set game controller variables to saved data variables.
        SaveData data = ReadData();
        gameController.CurrentLevel = data.levelProgress;
        Vector3 position;
        position.x = data.positionProgress[0];
        position.y = data.positionProgress[1];
        position.z = data.positionProgress[2];
        gameController.RespawnPoint = position;
    }
    #endregion
}