using System.Collections;
using UnityEngine;

[System.Serializable] public class Flash
{
    [Tooltip("Sets flash name.")] public string name;
    [Tooltip("Sets flash color.")] public Color color;
    [Tooltip("Sets flash duration (seconds).")][Range(0f, 3f)] public float duration;
}
public class SC_FlashController : MonoBehaviour
{
    #region Variables
    [Tooltip("Gets and sets flash types.")][SerializeField] private Flash[] flashTypes;
    [Tooltip("Gets selected flash color.")] private Color flashColor;
    [Tooltip("Gets selected flash duration (seconds).")] private float flashDuration;

    [Header("Components")]
    [Tooltip("Gets SpriteRenderer component.")] private SpriteRenderer spriteRenderer;
    [Tooltip("Gets default Material component.")] private Material defaultMaterial;
    [Tooltip("Gets flash Material component.")] private Material flashMaterial;
    [Tooltip("Gets running Coroutine to prevent multiple.")] private Coroutine flashCoroutine;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMaterial = spriteRenderer.material;
        flashMaterial = Resources.Load("MA_Flash", typeof(Material)) as Material;
        flashMaterial = new Material(flashMaterial);
    }
    /// <summary> Sets sprite material to name identifier color and duration. </summary>
    public void SetFlash(string name)
    {
        if (flashCoroutine != null) { StopCoroutine(flashCoroutine); }
        foreach(Flash flash in flashTypes)
        {
            if (name == flash.name)
            {
                flashColor = flash.color;
                flashDuration = flash.duration;
            }
        }
        flashCoroutine = StartCoroutine(FlashCoroutine(flashColor, flashDuration));
    }
    /// <summary> Sets sprite material to name identifier color and duration. </summary>
    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        spriteRenderer.material = flashMaterial;
        flashMaterial.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.material = defaultMaterial;
        flashCoroutine = null;
    }
    #endregion
}