using System.Collections;
using UnityEngine;

[System.Serializable] public class Parallax
{
    [Tooltip("Sets layer name.")] public string layerName;
    [Tooltip("Gets layer game object transform.")] public Transform layerTransform;
    [Tooltip("Sets X layer offset.")] public float layerXOffset;
    [Tooltip("Sets Y layer offset.")] public float layerYOffset;
    [Tooltip("Sets X layer movement speed. 1 = adjacent with camera, highest depth.")][Range(0.1f, 1f)] public float layerXMovement;
    [Tooltip("Sets Y layer movement speed. 1 = adjacent with camera, highest depth.")][Range(0.1f, 1f)] public float layerYMovement;
}
public class SC_CameraController : MonoBehaviour
{
    #region Variables
    [Header("Camera Manager")]
    [Tooltip("Gets camera target position to follow.")][SerializeField] private GameObject cameraTarget;
    [Tooltip("Sets x offset from camera target.")][Range(0f, 5f)][SerializeField] private float cameraXOffset;
    [Tooltip("Sets y offset from camera target.")][Range(0f, 5f)][SerializeField] private float cameraYOffset;
    [Tooltip("Sets camera movement speed. Higher value = slower movement.")][Range(0.01f, 0.1f)][SerializeField] private float cameraSpeed;
    [Tooltip("Sets camera shake modifier.")][Range(0f, 2f)][SerializeField] private float cameraShakeModifier = 1f;
    [Tooltip("List of parallax layers to modify.")][SerializeField] private Parallax[] parallaxLayers;

    [Header("Components")]
    [Tooltip("Gets camera holder GameObject for camera shake.")] private GameObject cameraHolder;
    [Tooltip("Gets Camera component from main camera.")] private Camera cameraMain;
    [Tooltip("Gets Camera component start FOV (orthographic size).")] public float CameraStartFOV { get; private set; }
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update() => CameraParallax();
    void LateUpdate() => CameraMovement();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        cameraHolder = gameObject;
        cameraMain = transform.GetChild(0).GetComponent<Camera>();
        CameraStartFOV = cameraMain.orthographicSize;
        if (cameraTarget == null) { cameraTarget = GameObject.FindWithTag("Player"); }
    }
    /// <summary> Move layer game objects with camera position to achieve parrallax. </summary>
    private void CameraParallax()
    {
        // Iterates through parallax array and sets each layer transform to camera transform with set layer movement speed.
        for (int i = 0; i < parallaxLayers.GetLength(0); i++)
        {
            if (parallaxLayers[i].layerTransform != null)
            {
                parallaxLayers[i].layerTransform.transform.position = new Vector2((cameraMain.transform.position.x + parallaxLayers[i].layerXOffset) * parallaxLayers[i].layerXMovement, 
                                                                                  (cameraMain.transform.position.y + parallaxLayers[i].layerYOffset) * parallaxLayers[i].layerYMovement);
            }         
        }      
    }
    /// <summary> Camera follows target with slow move in and slow move out (smooth damp). </summary>
    private void CameraMovement()
    {
        if (cameraTarget != null)
        {
            float cameraTargetXOffset = (cameraTarget.transform.localRotation.eulerAngles.y > 0) ? -Mathf.Abs(cameraXOffset) : Mathf.Abs(cameraXOffset);
            Vector2 cameraTargetPosition = new(cameraTarget.transform.position.x + cameraTargetXOffset, cameraTarget.transform.position.y + cameraYOffset), velocity = Vector2.zero;
            cameraMain.transform.position = Vector2.SmoothDamp(cameraMain.transform.position, cameraTargetPosition, ref velocity, cameraSpeed);
        }
    }
    /// <summary> Camera zoom with set zoom FOV (orthographic size) and zoom speed. </summary>
    public IEnumerator CameraZoom(float cameraZoomFOV, float cameraZoomSpeed)
    {    
        while (cameraMain.orthographicSize < cameraZoomFOV - 0.1f || cameraMain.orthographicSize > cameraZoomFOV + 0.1f)
        {
            cameraMain.orthographicSize = Mathf.Lerp(cameraMain.orthographicSize, cameraZoomFOV, cameraZoomSpeed * Time.deltaTime);
            yield return null;
        }
        cameraMain.orthographicSize = cameraZoomFOV;
    }
    /// <summary> Camera holder shakes with set magnitude (strength) and duration. </summary>
    public IEnumerator CameraShake(float magnitude, float duration)
    {
        Vector2 cameraStartPosition = cameraHolder.transform.localPosition;
        while (duration > 0)
        {
            cameraHolder.transform.localPosition = new Vector2(Random.Range(-1f, 1f) * magnitude * cameraShakeModifier, Random.Range(-1f, 1f) * magnitude * cameraShakeModifier);
            duration -= Time.deltaTime;
            yield return null;
        }
        cameraHolder.transform.localPosition = cameraStartPosition;
    }
    #endregion
}