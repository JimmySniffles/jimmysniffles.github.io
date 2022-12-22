using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class SC_PlatformController : MonoBehaviour
{
    #region Variables
    [Header("One Way Platform")]
    [Tooltip("Gives one way platform behaviour.")][SerializeField] private bool oneWay;

    [Header("Moving Platform")]
    [Tooltip("Gives moving platform behaviour.")][SerializeField] private bool moving;
    [Tooltip("Gets and sets moving platform type.")][SerializeField] private Type movingPlatformType = Type.autoMovingHorizontal;
    [Tooltip("Sets the selectable types of moving platforms.")] private enum Type { autoMovingHorizontal, autoMovingVertical, manualMoving };
    [Tooltip("Sets movement speed.")][SerializeField] private float moveSpeed;
    [Tooltip("Sets movement distance for automatic moving platforms.")][SerializeField] private float moveDistance;
    [Tooltip("Sets move start direction for automatic moving platforms.")][Range(-1, 1)][SerializeField] private int moveStartDirection;
    [Tooltip("Sets move points for manual moving platforms.")][SerializeField] private Transform[] movePoints;
    [Tooltip("Gets current move point.")] private int currentMovePoint;

    [Header("Jump Pad Platform")]
    [Tooltip("Gives jump pad platform behaviour.")][SerializeField] private bool jumpPad;
    [Tooltip("Sets jump force.")][SerializeField] private float jumpForce;

    [Header("Falling Platform")]
    [Tooltip("Gives falling platform behaviour when touched.")][SerializeField] private bool falling;
    [Tooltip("Sets Rigidbody2D gravity scale.")][SerializeField] private float gravityModifier;
    [Tooltip("Sets fall timer (seconds).")][SerializeField] private float fallTimer;
    [Tooltip("Sets fall movement vector.")] private Vector2 fallMovement;

    [Header("Disintegrate Platform")]
    [Tooltip("Gives disintegrate (collider disabled) platform behaviour when touched.")][SerializeField] private bool disintegrate;
    [Tooltip("Sets disintegrate timer (seconds).")][SerializeField] private float disintegrateTimer;
    [Tooltip("Gets current disintegrate timer.")] private float currentDisintegrateTimer;

    [Header("Components")]
    [Tooltip("Gets Rigidbody2D component.")] private Rigidbody2D rigidBody;
    [Tooltip("Gets BoxCollider2D component.")] private BoxCollider2D boxCollider;
    [Tooltip("Gets PlatformEffector2D component for one-way platforms.")] private PlatformEffector2D platformEffector;
    [Tooltip("Gets SpriteRenderer component.")] private SpriteRenderer spriteRenderer;

    [Header("Checks")]
    [Tooltip("Gets ground tiles layer mask.")] private LayerMask groundMask;
    [Tooltip("Gets if on ground.")] private bool onGround;
    [Tooltip("Gets if on wall.")] private bool onWall;
    [Tooltip("Gets if object on disintegrate platform.")] private bool onDistintegratePlatform;
    [Tooltip("Gets and sets movement direction.")] private Vector2 moveDirection;
    [Tooltip("Sets move anchor position.")] private Vector3 moveAnchorPosition;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update() => PlatformMovement();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        platformEffector = GetComponent<PlatformEffector2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveAnchorPosition = transform.position;
        if (moveStartDirection == 0) { moveStartDirection = 1; }
        if (!oneWay) { platformEffector.enabled = false; boxCollider.usedByEffector = false; }
        if (movingPlatformType == Type.autoMovingHorizontal) { moveDirection = new Vector2(moveStartDirection, 0f); }
        if (movingPlatformType == Type.autoMovingVertical) { moveDirection = new Vector2(0f, moveStartDirection); }
        if (disintegrate) { currentDisintegrateTimer = disintegrateTimer; }
        groundMask = LayerMask.GetMask("GroundTiles");
    }
    /// <summary> Defines collision, auto and manual moving platforms, resets disintegrated platform. </summary>
    private void PlatformMovement()
    {
        // Collision and reset disintegrate if timer is reset and box collider is disabled.
        onGround = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, groundMask);
        onWall = Physics2D.BoxCast(transform.position, boxCollider.bounds.size, 0f, moveDirection, 0.5f, groundMask);     
        if (currentDisintegrateTimer < disintegrateTimer && !onDistintegratePlatform) { currentDisintegrateTimer += Time.deltaTime; }
        if (currentDisintegrateTimer >= disintegrateTimer && !boxCollider.enabled)
        {
            boxCollider.enabled = true;
            spriteRenderer.color = new Color32(255, 255, 0, 255);
        }

        if (moving)
        {
            // Automatic moving platform: If enabled move towards max distance and switch direction.
            if (movingPlatformType == Type.autoMovingHorizontal || movingPlatformType == Type.autoMovingVertical)
            {
                Vector3 moveDirectionDistance = moveDirection * moveDistance, moveTargetPosition = moveAnchorPosition + moveDirectionDistance;
                float moveTargetDistance = (transform.position - moveTargetPosition).sqrMagnitude;
                if (moveTargetDistance > 1f && !onWall && !onGround)
                {
                    transform.position = Vector2.MoveTowards(transform.position, moveTargetPosition, moveSpeed * Time.deltaTime);
                }
                else if (!onGround)
                {
                    moveAnchorPosition = transform.position;
                    moveDirection = -moveDirection;
                }
            }
            // Manual moving platform: If enabled move towards move point and move to the next.
            if (movingPlatformType == Type.manualMoving)
            {
                float moveTargetDistance = (transform.position - movePoints[currentMovePoint].position).sqrMagnitude;
                if (moveTargetDistance > 1f && !onGround)
                {
                    transform.position = Vector2.MoveTowards(transform.position, movePoints[currentMovePoint].position, moveSpeed * Time.deltaTime);
                }
                else if (!onGround)
                {
                    if (currentMovePoint + 1 < movePoints.Length) { currentMovePoint++; } else { currentMovePoint = 0; }
                }
            }
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Moving platform becomes parent of the player so the player sticks to moving platform if player y position is above platform y position + half of the players scale.
            if (moving)
            {
                if (collision.transform.position.y > (transform.position.y + collision.transform.lossyScale.x / 2))
                {
                    collision.transform.SetParent(transform);
                }
            }
            // Jump pad platform boosts the players rigidbody velocity.
            if (jumpPad)
            {
                collision.gameObject.GetComponent<Rigidbody2D>().AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
            // Falling platform kills the player if falling and not on the ground.
            if (falling)
            {
                if (fallTimer <= 0 && !onGround)
                {
                    collision.gameObject.GetComponent<SC_HealthController>().SetHealth("--", 0f, 0f);
                }
            }
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Moving platform removes player as child to allow player to move freely if player is moving.
            if (moving)
            {
                float playerVelocity = collision.gameObject.GetComponent<Rigidbody2D>().velocity.x;
                if (playerVelocity > 0.25f || playerVelocity < -0.25f)
                {
                    collision.transform.SetParent(null);
                }
                else if (collision.transform.position.y > (transform.position.y + collision.transform.lossyScale.x / 2))
                {
                    collision.transform.SetParent(transform);
                }
                else
                {
                    collision.transform.SetParent(null);
                }
            }
            // Falling platform falls when touched by player after period of time.
            if (falling)
            {
                if (fallTimer > 0) { fallTimer -= Time.deltaTime; }
                if (fallTimer <= 0 && !onGround)
                {
                    fallMovement.y += Physics2D.gravity.y * gravityModifier * Time.deltaTime;
                    rigidBody.MovePosition(rigidBody.position + fallMovement * Time.deltaTime);
                }
            }
            // Disintegrate platform disintegrates (disabling collider and changing sprite) when touched by player after period of time.
            if (disintegrate)
            {
                onDistintegratePlatform = true;
                if (currentDisintegrateTimer > 0) { currentDisintegrateTimer -= Time.deltaTime; }
                if (currentDisintegrateTimer <= 0)
                {
                    boxCollider.enabled = false;
                    spriteRenderer.color = new Color32(0, 0, 0, 50);
                }
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (onDistintegratePlatform) { onDistintegratePlatform = false; }
            if (moving)
            {
                collision.transform.SetParent(null);
            }
        }
    }
    #endregion
}