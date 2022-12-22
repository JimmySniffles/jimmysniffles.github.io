using UnityEngine;

public class SC_CircleController : MonoBehaviour
{
    #region Variables
    [Header("Camera Shader")]
    [Tooltip("Gets shader circle scene transition.")][SerializeField] private Shader shaderCircle;
    [Tooltip("Sets shader circle target scene transition.")] public Vector2 ShaderCircleTarget { get; set; }
    [Tooltip("Gets material for circle scene transition.")] private Material materialCircle;
    [Tooltip("Sets shader cricle radius scene transition.")] public float RadiusCircle { get; set; }
    [Tooltip("Sets radius size to fill entire screen.")] public float RadiusCircleSize { get; set; }
    [Tooltip("Sets horizontal aspect ratio.")] private readonly float horizontal = 16f;
    [Tooltip("Sets vertical aspect ratio.")] private readonly float vertical = 9f;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        materialCircle = new Material(shaderCircle);
        RadiusCircleSize = 1.5f;
        RadiusCircle = RadiusCircleSize;
        UpdateShader();
    }
    /// <summary> Update circle shader for circle scene transition. </summary>
    public void UpdateShader()
    {
        float radiusSpeedCircleCut = Mathf.Max(horizontal, vertical);
        materialCircle.SetFloat("_Horizontal", horizontal);
        materialCircle.SetFloat("_Vertical", vertical);
        materialCircle.SetFloat("_RadiusSpeed", radiusSpeedCircleCut);
        materialCircle.SetFloat("_Radius", RadiusCircle);
        materialCircle.SetVector("_Offset", ShaderCircleTarget);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination) => Graphics.Blit(source, destination, materialCircle);
    #endregion
}