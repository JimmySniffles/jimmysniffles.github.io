using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SC_HealthController))]

public class SC_AIController : MonoBehaviour
{
    #region Variables
    [Header("AI")]
    [Tooltip("Gets and sets current state.")][SerializeField] private State currentState = State.idle;
    [Tooltip("Sets types of states.")] private enum State { idle, patrol, chase, retreat, attack, death };
    [Tooltip("Sets movement speed.")][SerializeField] private float moveSpeed;
    [Tooltip("Sets Rigidbody2D gravity scale.")][SerializeField] private float gravityModifier;
    [Tooltip("Sets attack damage.")][SerializeField] private float attackDamage;
    [Tooltip("Sets target sight distance to chase or retreat.")][SerializeField] private float sightDistance;
    [Tooltip("Sets target hear distance to chase or retreat.")][SerializeField] private float hearDistance;
    [Tooltip("Sets target distance to attack.")][SerializeField] private float attackDistance;
    [Tooltip("Sets patrol distance for automatic patrol.")][SerializeField] private float patrolDistance;
    [Tooltip("Sets target to chase or retreat.")][SerializeField] private Transform target;

    [Header("Behaviours")]
    [Tooltip("Gets and sets patrol type.")][SerializeField] private PatrolType patrolType = PatrolType.none;
    [Tooltip("Sets patrol types.")] private enum PatrolType { none, automatic, manual };
    [Tooltip("Sets patrol points for manual patrol.")][SerializeField] private Transform[] patrolPoints;
    [Tooltip("Gets current patrol point.")] private int currentPatrolPoint;
    [Tooltip("Gets chase behaviour.")][SerializeField] private bool chase;
    [Tooltip("Gets retreat behaviour.")][SerializeField] private bool retreat;
    [Tooltip("Gets attack behaviour.")][SerializeField] private bool attack;
    [Tooltip("Gets death behaviour.")][SerializeField] private bool death;
    [Tooltip("Sets types of animations.")] private enum Animation { Idle, Run, Fall, Attack1, Attack2, Death };
    [Tooltip("Gets and sets current animation.")] private Animation currentAnimation = Animation.Idle;

    [Header("Components")]
    [Tooltip("Gets BoxCollider2D component.")] private BoxCollider2D boxCollider;
    [Tooltip("Gets Rigidbody2D component.")] private Rigidbody2D rigidBody;
    [Tooltip("Gets Animator component.")] private Animator animator;
    [Tooltip("Gets HealthController script component.")] private SC_HealthController healthController;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;

    [Header("Checks")]
    [Tooltip("Gets ground tiles layer mask.")] private LayerMask groundMask;
    [Tooltip("Gets platform layer mask.")] private LayerMask platformMask;
    [Tooltip("Gets target layer mask.")] private LayerMask targetMask;
    [Tooltip("Gets if on ground.")] private bool onGround;
    [Tooltip("Gets if on wall.")] private bool onWall;
    [Tooltip("Gets if not on edge.")] private bool notOnEdge;
    [Tooltip("Gets and sets facing movement direction.")] private Vector2 facingDirection;
    [Tooltip("Sets auto patrol anchor position.")] private Vector3 patrolAnchorPosition;
    [Tooltip("Gets distance from target.")] private float targetDistance;
    [Tooltip("Gets running Coroutine to prevent multiple.")] private Coroutine coroutine;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update()
    {
        GetCollisionDirection();
        GetTargetDetection();
    }
    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.idle: Idle(); break;
            case State.patrol: Patrol(); break;
            case State.chase: Chase(); break;
            case State.retreat: Retreat(); break;
            case State.attack: Attack(); break;
            case State.death: Death(); break;
        }
    }
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        healthController = GetComponent<SC_HealthController>();
        audioController = GetComponent<SC_AudioController>();
        rigidBody.gravityScale = gravityModifier;
        facingDirection = new(Random.Range(-1, 1), 0f);
        if (facingDirection.x == 0f) { facingDirection.x = 1f; }
        patrolAnchorPosition = transform.position;
        groundMask = LayerMask.GetMask("GroundTiles");
        platformMask = LayerMask.GetMask("Platforms");
        targetMask = LayerMask.GetMask("Player");
    }
    /// <summary> Get collision and rotation direction. </summary>
    private void GetCollisionDirection()
    {
        if (currentState != State.death)
        {
            if (healthController.CurrentHealth <= 0 && death) { currentState = State.death; }

            // Get collision.
            onGround = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, groundMask) || Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, platformMask);
            onWall = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, facingDirection, 0.6f, groundMask) || Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, facingDirection, 0.6f, platformMask);
            float edgeBuffer = 0.2f;
            Vector2 edgePosition = facingDirection.x > 0 ? new(transform.position.x + boxCollider.size.x / 2 + edgeBuffer, transform.position.y - boxCollider.size.y / 2) : 
                                                           new (transform.position.x - boxCollider.size.x / 2 - edgeBuffer, transform.position.y - boxCollider.size.y / 2);
            Vector2 edgePositionTarget = new(edgePosition.x, edgePosition.y - 0.5f);
            notOnEdge = Physics2D.Linecast(edgePosition, edgePositionTarget, groundMask);

            // Set rotation direction.
            if (!Mathf.Approximately(facingDirection.x, 0f)) { transform.rotation = facingDirection.x < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity; }

            // Set animations.
            if (rigidBody.velocity.x == 0f && onGround) { SetAnimation(Animation.Idle); }
            if (rigidBody.velocity.x != 0f && onGround) { SetAnimation(Animation.Run); }
            if (rigidBody.velocity.y < 0f && !onGround) { SetAnimation(Animation.Fall); }

            Debug.DrawLine(edgePosition, edgePositionTarget, Color.red); // Debug not on edge detection
            Debug.DrawRay(transform.position, facingDirection * 0.6f, Color.green); // Debug direction
        }
    }
    /// <summary> Set target detection if chase or retreat behaviour is enabled. </summary>
    private void GetTargetDetection()
    {
        if (currentState != State.death)
        {
            if (chase || retreat)
            {
                Vector3 sightDirection = facingDirection * sightDistance;
                RaycastHit2D sightTarget = Physics2D.Linecast(transform.position, transform.position + sightDirection, targetMask);
                targetDistance = (transform.position - target.position).sqrMagnitude;
                if (sightTarget.collider != null || targetDistance <= hearDistance * hearDistance)
                {
                    if (chase) { currentState = State.chase; }
                    if (retreat) { currentState = State.retreat; }
                }
                Debug.DrawLine(transform.position, transform.position + sightDirection, Color.blue); // Debug detection
            }
        }
    }
    /// <summary> Set animation without interrupting itself. </summary>
    private void SetAnimation(Animation newAnimation)
    {
        if (currentAnimation == newAnimation) { return; }
        animator.Play(newAnimation.ToString());
        currentAnimation = newAnimation;
    }
    /// <summary> Idle behaviour: Stay in position with idle animation for period of time. </summary>
    private void Idle()
    {
        if (patrolType != PatrolType.none && coroutine == null)
        {
            coroutine = StartCoroutine(CoolDown("idle", Random.Range(1f, 3f)));
        }   
    }
    /// <summary> Patrol behaviour: Automatic patrol (move between distance) or manual patrol (move to patrol points). </summary>
    private void Patrol()
    {
        // Automatic patrol: If enabled move towards max distance, wall or not on ground, then switch direction with random wait time.
        if (patrolType == PatrolType.automatic)
        {
            Vector3 patrolTargetPosition = facingDirection.x > 0 ? new(patrolAnchorPosition.x + patrolDistance, patrolAnchorPosition.y) :
                                                                   new(patrolAnchorPosition.x - patrolDistance, patrolAnchorPosition.y);
            float targetPatrolDistance = (transform.position - patrolTargetPosition).sqrMagnitude, minimalDistance = patrolDistance * patrolDistance + 1f;
            if (targetPatrolDistance < minimalDistance && !onWall && notOnEdge)
            {
                rigidBody.velocity = new(facingDirection.x * moveSpeed, rigidBody.velocity.y);
            }
            else if (coroutine == null)
            {
                coroutine = StartCoroutine(CoolDown("autoPatrol", Random.Range(4f, 6f)));
            }
        }

        // Manual patrol: If enabled move towards patrol point, then move to the next with random wait time.
        if (patrolType == PatrolType.manual)
        {
            float targetPatrolDistance = (transform.position - patrolPoints[currentPatrolPoint].position).sqrMagnitude, minimalDistance = 1f;
            if (targetPatrolDistance > minimalDistance)
            {
                facingDirection = (patrolPoints[currentPatrolPoint].position - transform.position).normalized;
                rigidBody.velocity = new(facingDirection.x * moveSpeed, rigidBody.velocity.y);
            }
            else if (coroutine == null)
            {
                coroutine = StartCoroutine(CoolDown("manualPatrol", Random.Range(4f, 6f)));
            }
        }
    }
    /// <summary> Chase behaviour: Move towards target when detected. </summary>
    private void Chase()
    {
        // Chase target.
        facingDirection = (target.position - transform.position).normalized;
        rigidBody.velocity = new(facingDirection.x * moveSpeed, rigidBody.velocity.y);

        // Change state depending on distance to target.
        if (targetDistance > sightDistance) 
        {
            facingDirection = facingDirection.x > 0 ? new(1f, 0f) : new(-1f, 0f);
            patrolAnchorPosition = transform.position;  
            currentState = State.patrol;
        }
        if (attack && targetDistance <= attackDistance) { currentState = State.attack; }
    }
    /// <summary> Retreat behaviour: Move away from target when detected. </summary>
    private void Retreat()
    {
        // Retreat from target.
        if (!onWall && notOnEdge) 
        { 
            facingDirection = -(target.position - transform.position).normalized; 
        } 
        else if (coroutine == null)
        { 
            coroutine = StartCoroutine(CoolDown("retreat", 2f));
        }
        rigidBody.velocity = new Vector2(facingDirection.x * moveSpeed, rigidBody.velocity.y);

        // Change state depending on distance to target.
        if (targetDistance > sightDistance)
        {
            StopCoroutine(nameof(CoolDown));
            coroutine = null;
            rigidBody.velocity = new(0f, rigidBody.velocity.y);
            facingDirection = facingDirection.x > 0 ? new(1f, 0f) : new(-1f, 0f);
            patrolAnchorPosition = transform.position;
            currentState = State.patrol;
        }
    }
    /// <summary> Attack behaviour: Attack target within distance. </summary>
    private void Attack()
    {

    }
    /// <summary> Death behaviour: When health reached 0, stop movement, play death animation, destroy game object. </summary>
    private void Death()
    {
        rigidBody.velocity = new(0f, rigidBody.velocity.y);
        SetAnimation(Animation.Death);
        if (!onGround)
        {
            animator.speed = 0f;
        }
        else
        {
            animator.speed = 1f;
            boxCollider.enabled = false;
            rigidBody.Sleep();
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(Animation.Death.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f)
            {
                Destroy(gameObject, Random.Range(10f, 15f));
            }
        }
    }
    /// <summary> Cool down set by type and duration ("idle", "autoPatrol", "manualPatrol", "retreat", "death"). </summary>
    private IEnumerator CoolDown(string type, float duration)
    {
        switch (type)
        {
            case "idle":
                yield return new WaitForSeconds(duration);
                coroutine = null;
                currentState = State.patrol;
                break;
            case "autoPatrol":
                rigidBody.velocity = new(0f, rigidBody.velocity.y);
                yield return new WaitForSeconds(duration);
                facingDirection = -facingDirection;
                patrolAnchorPosition = transform.position;
                coroutine = null;
                break;
            case "manualPatrol":
                rigidBody.velocity = new(0f, rigidBody.velocity.y);
                yield return new WaitForSeconds(duration);
                if (currentPatrolPoint + 1 < patrolPoints.Length) { currentPatrolPoint++; } else { currentPatrolPoint = 0; }
                coroutine = null;
                break;
            case "retreat":
                facingDirection = (target.position - transform.position).normalized;
                yield return new WaitForSeconds(duration);
                coroutine = null;
                break;
            default:
                Debug.LogWarning($"Cooldown type: {type} not found!");
                break;
        }
    }
    #endregion
}