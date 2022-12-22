using UnityEngine;

[System.Serializable] public class BushTucker
{
    [Tooltip("Sets bush tucker name.")][HideInInspector] public string bushTuckerName;
    [Tooltip("Sets bush tucker type.")][HideInInspector] public bool bushTuckerEnabled;
    [Tooltip("Sets SpriteRenderer sprite.")] public Sprite sprite;
    [Tooltip("List of player stats that can be changed.")] public enum Stats { health, movement, jump, climb, weaponThrow, invincibility };
    [Tooltip("List of selectable types to change player stats.")] public Stats changeType = Stats.health;
    [Tooltip("Sets stat change amount to increase (true) or decrease (false).")] public bool changeIncrementType;
    [Tooltip("Sets stat change amount.")] public float changeAmount;
    [Tooltip("Sets stat change duration (seconds).")] public float changeDuration;
}
public class SC_PowerupController : MonoBehaviour
{
    #region Variables
    [Header("Bush Tucker")]
    [Tooltip("List of selectable types of bush tucker.")][SerializeField] private Type selectedBushTucker = Type.fingerLime;
    [Tooltip("List types of bush tucker.")] private enum Type { fingerLime, lemonMyrtle, midyimBerry, quandong, lemonAspen, muntries, cycad, deadlyNightshade, mistletoe, fingerCherry };
    [Tooltip("List of bush tucker stats.")][SerializeField] private BushTucker[] bushTuckerStats;
    [Tooltip("Gets enabled bush tucker stats change type.")] private string currentChangeType;
    [Tooltip("Gets enabled bush tucker stats change increment type.")] private bool currentChangeIncrementType;
    [Tooltip("Gets enabled bush tucker stats change amount.")] private float currentChangeAmount;
    [Tooltip("Gets enabled bush tucker stats change duration.")] private float currentChangeDuration;

    [Header("Components")]
    [Tooltip("Gets SpriteRenderer component.")] private SpriteRenderer spriteRenderer;
    [Tooltip("Gets Rigidbody2D component.")] private Rigidbody2D rigidBody;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets PlayerController script component to change stats.")] private SC_PlayerController playerController;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        audioController = GetComponent<SC_AudioController>();
        playerController = GameObject.FindWithTag("Player").GetComponent<SC_PlayerController>();
        Physics2D.IgnoreLayerCollision(8, 11, true); // Ignore Player.
        Physics2D.IgnoreLayerCollision(10, 11, true); // Ignore AI.
        bushTuckerStats[(int)selectedBushTucker].bushTuckerEnabled = true;
        // Iterates through enum array and sets the sprite renderer to the enabled bush tucker sprite.
        for (int i = 0; i < System.Enum.GetValues(typeof(Type)).Length; i++)
        {
            if (bushTuckerStats[(int)selectedBushTucker].bushTuckerEnabled)
            {
                spriteRenderer.sprite = bushTuckerStats[(int)selectedBushTucker].sprite;
            }
        }
    }
    /// <summary> Gets stats change variables, calls SetChangeStats function in PlayerController script and destroy. </summary>
    public void SetPlayerStats()
    {
        // Iterates through enum array and sets the stats change variables to the enabled bush tucker variables.
        for (int i = 0; i < System.Enum.GetValues(typeof(Type)).Length; i++)
        {
            if (bushTuckerStats[(int)selectedBushTucker].bushTuckerEnabled)
            {
                currentChangeType = bushTuckerStats[(int)selectedBushTucker].changeType.ToString();
                currentChangeIncrementType = bushTuckerStats[(int)selectedBushTucker].changeIncrementType;
                currentChangeAmount = bushTuckerStats[(int)selectedBushTucker].changeAmount;
                currentChangeDuration = bushTuckerStats[(int)selectedBushTucker].changeDuration;
            }
        }
        audioController.PlayAudio("play", "Pick", 0, true);
        audioController.PlayAudio("play", "Consume", 0, true);
        playerController.SetStatsChange(currentChangeType, currentChangeIncrementType, currentChangeAmount, currentChangeDuration);
        Destroy(gameObject, 0.4f);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Enable gravity.
        if (collision.collider.CompareTag("Boomerang")) { rigidBody.WakeUp(); }
    }
    #endregion
}