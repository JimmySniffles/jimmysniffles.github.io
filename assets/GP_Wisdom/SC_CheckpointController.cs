using UnityEngine;

public class SC_CheckpointController : MonoBehaviour
{
    #region Variables
    [Header("Checkpoint")]
    [Tooltip("Gets and sets if object is inspirited (false = physical world, true = spirit world).")][SerializeField] private bool inSpirited;
    [Tooltip("List of checkpoint sprites.")][SerializeField] private Sprite[] checkpointSprites;
    [Tooltip("Gets and sets current checkpoint.")] private bool setCheckpoint;

    [Header("Components")]
    [Tooltip("Gets SpriteRenderer component to change sprite.")] private SpriteRenderer spriteRenderer;
    [Tooltip("Gets GameController script to get and set respawn.")] private SC_GameController gameController;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update() => UpdateCheckpoint();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioController = GetComponent<SC_AudioController>();
        gameController = GameObject.FindWithTag("GameController").GetComponent<SC_GameController>();
        if (gameController.RespawnPoint == transform.position) { setCheckpoint = true; }
        SetCheckpointSprite();
    }
    /// <summary> Set previous checkpoint to off. </summary>
    private void UpdateCheckpoint()
    {
        if (setCheckpoint && gameController.RespawnPoint != transform.position)
        {
            setCheckpoint = false;
            SetCheckpointSprite();
        }
    }
    /// <summary> Set checkpoint sprite based on set checkpoint and if inspirited. </summary>
    private void SetCheckpointSprite()
    {
        if (!inSpirited)
        {
            if (!setCheckpoint)
            {
                spriteRenderer.sprite = checkpointSprites[0];
            }
            else
            {
                spriteRenderer.sprite = checkpointSprites[1];
            }
        }
        else
        {
            if (!setCheckpoint)
            {
                spriteRenderer.sprite = checkpointSprites[2];
            }
            else
            {
                spriteRenderer.sprite = checkpointSprites[3];
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If player, set checkpoint respawn position and sprite if not set.
        if (collision.CompareTag("Player") && !setCheckpoint) 
        {
            gameController.RespawnPoint = transform.position;
            audioController.PlayAudio("play", "Enabled", 0, true);
            setCheckpoint = true;
            SetCheckpointSprite();
        }
    }
    #endregion
}