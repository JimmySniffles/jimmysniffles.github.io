using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SC_AIController : MonoBehaviour
{
    #region Variables
    [Header("AI")]
    [Tooltip("Gets and sets current state.")][SerializeField] private State currentState = State.idle;
    [Tooltip("Sets types of states.")] private enum State { idle, patrol, chase, retreat, attack, death };
    [Tooltip("Sets movement speed.")][SerializeField] private float moveSpeed;
    [Tooltip("Sets Rigidbody2D gravity scale.")][SerializeField] private float gravityModifier;
    [Tooltip("Sets attack 1 damage.")][SerializeField] private float attack1Damage;
    [Tooltip("Sets attack 2 damage.")][SerializeField] private float attack2Damage;
    [Tooltip("Sets attack radius range.")][SerializeField] private float attackRange;
    [Tooltip("Sets target distance to attack.")][SerializeField] private float attackDistance;
    [Tooltip("Sets target hear distance to chase or retreat.")][SerializeField] private float hearDistance;
    [Tooltip("Sets target sight distance to chase or retreat.")][SerializeField] private float sightDistance;
    [Tooltip("Sets patrol distance for automatic patrol.")][SerializeField] private float patrolDistance;
    [Tooltip("Sets camera view distance for VFX.")][SerializeField] private float cameraViewDistance;

    [Header("Behaviours")]
    [Tooltip("Gets and sets patrol type.")][SerializeField] private PatrolType patrolType = PatrolType.none;
    [Tooltip("Sets patrol types.")] private enum PatrolType { none, automatic, manual };
    [Tooltip("Sets patrol points for manual patrol.")][SerializeField] private Transform[] patrolPoints;
    [Tooltip("Gets current patrol point.")] private int currentPatrolPoint;
    [Tooltip("Gets chase behaviour.")][SerializeField] private bool chase;
    [Tooltip("Gets retreat behaviour.")][SerializeField] private bool retreat;
    [Tooltip("Gets attack behaviour.")][SerializeField] private bool attack;
    [Tooltip("Gets multiple attack behaviour.")][SerializeField] private bool multiAttack;
    [Tooltip("Gets death behaviour.")][SerializeField] private bool death;

    [Header("Animation SFX")]
    [Tooltip("Gets and sets movement type.")][SerializeField] private MovementType movementType = MovementType.none;
    [Tooltip("Sets movement types.")] private enum MovementType { none, incremental, continuous };
    [Tooltip("Sets incremental movement hit frames for SFX.")][SerializeField] private int[] incrementalHitFrame;
    [Tooltip("Sets incremental movement total frames for SFX.")][SerializeField] private int incrementalFrames;
    [Tooltip("Sets attack 1 hit frame for hit register and SFX.")][SerializeField] private int attack1HitFrame;
    [Tooltip("Sets attack 1 total frames for hit register and SFX.")][SerializeField] private int attack1Frames;
    [Tooltip("Sets attack 1 name to play SFX.")][SerializeField] private string attack1Name;
    [Tooltip("Sets attack 2 hit frame for hit register and SFX.")][SerializeField] private int attack2HitFrame;
    [Tooltip("Sets attack 2 total frames for hit register and SFX.")][SerializeField] private int attack2Frames;
    [Tooltip("Sets attack 2 name to play SFX.")][SerializeField] private string attack2Name;

    [Header("Components")]
    [Tooltip("Gets BoxCollider2D component.")] private BoxCollider2D boxCollider;
    [Tooltip("Gets Rigidbody2D component.")] private Rigidbody2D rigidBody;
    [Tooltip("Gets Transform attack point component.")] private Transform attackPoint;
    [Tooltip("Gets HealthController script component.")] private SC_HealthController healthController;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets AnimationController script component.")] private SC_AnimationController animationController;
    [Tooltip("Gets CameraController script component.")] private SC_CameraController cameraController;
    [Tooltip("Sets target to chase or retreat.")] private Transform target;
    [Tooltip("Gets PlayerController target controller component.")] private SC_PlayerController targetController;

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
    [Tooltip("Gets running patrol cry Coroutine to prevent multiple.")] private Coroutine coroutinePatrol;
    [Tooltip("Gets attack once to prevent multiple hits.")] private bool attackOnce;
    [Tooltip("Gets random attack to set once repeatedly before entering attack state.")] private int randomAttack;
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
        attackPoint = transform.GetChild(0);
        healthController = GetComponent<SC_HealthController>();
        audioController = GetComponent<SC_AudioController>();
        animationController = GetComponent<SC_AnimationController>();
        cameraController = GameObject.Find("PR_CameraHolder").GetComponent<SC_CameraController>();
        target = GameObject.FindWithTag("Player").GetComponent<Transform>();
        targetController = target.GetComponent<SC_PlayerController>();
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
            float wallDistanceCheck = boxCollider.bounds.size.x / 2 + 0.1f, edgeBuffer = 0.2f;
            Vector2 edgePosition = facingDirection.x > 0 ? new(boxCollider.bounds.center.x + boxCollider.size.x / 2 + edgeBuffer, boxCollider.bounds.center.y - boxCollider.size.y / 2) : 
                                                           new (boxCollider.bounds.center.x - boxCollider.size.x / 2 - edgeBuffer, boxCollider.bounds.center.y - boxCollider.size.y / 2);
            Vector2 edgePositionTarget = new(edgePosition.x, edgePosition.y - 0.5f);
            notOnEdge = Physics2D.Linecast(edgePosition, edgePositionTarget, groundMask);
            onGround = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, groundMask) || Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, platformMask);
            onWall = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, facingDirection, wallDistanceCheck, groundMask) || Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, facingDirection, wallDistanceCheck, platformMask);

            // Set rotation direction.
            animationController.SetDirection(facingDirection);

            // Set animations.
            if (currentState != State.attack)
            {
                if (Mathf.Round(rigidBody.velocity.x) == 0f && onGround) 
                { 
                    animationController.SetAnimation(SC_AnimationController.Animation.Idle); 
                }
                if (Mathf.Round(rigidBody.velocity.x) != 0f && onGround) 
                { 
                    animationController.SetAnimation(SC_AnimationController.Animation.Run);
                    if (movementType == MovementType.incremental)
                    {
                        int currentFrame = animationController.GetAnimationFrame(incrementalFrames);
                        if (currentFrame == incrementalHitFrame[0] || currentFrame == incrementalHitFrame[1])
                        {
                            audioController.PlayAudio("play", "Step", true, 0);
                        }
                    }

                }
            }
            if (rigidBody.velocity.y < 0f && !onGround) { animationController.SetAnimation(SC_AnimationController.Animation.Fall); }
            if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Fall.ToString()) && onGround && targetDistance <= cameraViewDistance * cameraViewDistance)
            {
                audioController.PlayAudio("play", "Land", true, 0);
                StartCoroutine(cameraController.CameraShake(0.07f, 0.03f));
            }

            Debug.DrawRay(boxCollider.bounds.center, facingDirection * wallDistanceCheck, Color.green); // Debug direction
            Debug.DrawLine(edgePosition, edgePositionTarget, Color.red); // Debug not on edge detection
        }
    }
    /// <summary> Set target detection if chase or retreat behaviour is enabled. </summary>
    private void GetTargetDetection()
    {
        if (currentState != State.death && currentState != State.attack)
        {
            if (chase || retreat)
            {
                Vector3 sightDirection = facingDirection * sightDistance;
                RaycastHit2D sightTarget = Physics2D.Linecast(boxCollider.bounds.center, boxCollider.bounds.center + sightDirection, targetMask);
                targetDistance = (boxCollider.bounds.center - target.position).sqrMagnitude;
                if (sightTarget.collider != null || targetDistance <= hearDistance * hearDistance)
                {
                    audioController.PlayAudio("play", "Attack", true, 0);
                    if (coroutinePatrol != null) { StopCoroutine(nameof(CoolDown)); coroutinePatrol = null; }
                    if (chase && targetController.CurrentState != SC_PlayerController.State.death) { currentState = State.chase; }
                    if (retreat) { currentState = State.retreat; }
                }
                Debug.DrawLine(boxCollider.bounds.center, boxCollider.bounds.center + sightDirection, Color.blue); // Debug detection
            }
        }
    }
    /// <summary> Idle behaviour: Stay in position with idle animation for period of time. </summary>
    private void Idle()
    {
        if (patrolType != PatrolType.none)
        {
            coroutine ??= StartCoroutine(CoolDown("idle", Random.Range(1f, 3f)));
        }   
    }
    /// <summary> Patrol behaviour: Automatic patrol (move between distance) or manual patrol (move to patrol points). </summary>
    private void Patrol()
    {
        coroutinePatrol ??= StartCoroutine(CoolDown("patrol", Random.Range(8f, 15f)));
        // Automatic patrol: If enabled move towards max distance, wall or not on ground, then switch direction with random wait time.
        if (patrolType == PatrolType.automatic)
        {
            Vector3 patrolTargetPosition = facingDirection.x > 0f ? new(patrolAnchorPosition.x + patrolDistance, patrolAnchorPosition.y) :
                                                                   new(patrolAnchorPosition.x - patrolDistance, patrolAnchorPosition.y);
            float targetPatrolDistance = (boxCollider.bounds.center - patrolTargetPosition).sqrMagnitude, minimalDistance = 1f;
            if (targetPatrolDistance > minimalDistance && !onWall && notOnEdge)
            {
                rigidBody.velocity = new(facingDirection.x * moveSpeed, rigidBody.velocity.y);
            }
            else coroutine ??= StartCoroutine(CoolDown("autoPatrol", Random.Range(4f, 6f)));
        }

        // Manual patrol: If enabled move towards patrol point, then move to the next with random wait time.
        if (patrolType == PatrolType.manual)
        {
            float targetPatrolDistance = (boxCollider.bounds.center - patrolPoints[currentPatrolPoint].position).sqrMagnitude, minimalDistance = 1f;
            if (targetPatrolDistance > minimalDistance)
            {
                facingDirection = (patrolPoints[currentPatrolPoint].position - boxCollider.bounds.center).normalized;
                rigidBody.velocity = new(facingDirection.x * moveSpeed, rigidBody.velocity.y);
            }
            else coroutine ??= StartCoroutine(CoolDown("manualPatrol", Random.Range(4f, 6f)));
        }
    }
    /// <summary> Chase behaviour: Move towards target when detected. </summary>
    private void Chase()
    {
        // Chase target.
        facingDirection = (target.position - boxCollider.bounds.center).normalized;
        rigidBody.velocity = new(facingDirection.x * moveSpeed, rigidBody.velocity.y);

        // Change state depending on distance to target.
        if (targetDistance > sightDistance * sightDistance || targetController.CurrentState == SC_PlayerController.State.death) 
        {
            facingDirection = facingDirection.x > 0f ? new(-1f, 0f) : new(1f, 0f);
            patrolAnchorPosition = transform.position;
            if (patrolType == PatrolType.manual) { patrolType = PatrolType.automatic; }
            currentState = State.patrol;
        }
        if (attack && targetDistance <= attackDistance * attackDistance && targetController.CurrentState != SC_PlayerController.State.death)
        {
            if (multiAttack) { randomAttack = Random.Range(1, 3); }
            currentState = State.attack; 
        }
    }
    /// <summary> Retreat behaviour: Move away from target when detected. </summary>
    private void Retreat()
    {
        // Retreat from target.
        if (!onWall && notOnEdge) 
        { 
            facingDirection = -(target.position - boxCollider.bounds.center).normalized; 
        } 
        else coroutine ??= StartCoroutine(CoolDown("retreat", 2f));
        rigidBody.velocity = new Vector2(facingDirection.x * moveSpeed, rigidBody.velocity.y);

        // Change state depending on distance to target.
        if (targetDistance > sightDistance * sightDistance)
        {
            StopCoroutine(nameof(CoolDown));
            coroutine = null;
            rigidBody.velocity = new(0f, rigidBody.velocity.y);
            facingDirection = facingDirection.x > 0 ? new(1f, 0f) : new(-1f, 0f);
            patrolAnchorPosition = transform.position;
            if (patrolType == PatrolType.manual) { patrolType = PatrolType.automatic; }
            currentState = State.patrol;
        }
        if (chase && targetDistance <= sightDistance * sightDistance)
        {
            audioController.PlayAudio("play", "Attack", true, 0);
            currentState = State.chase;
        }
    }
    /// <summary> Attack behaviour: Attack target within distance. </summary>
    private void Attack()
    {
        int currentFrame;
        float attackDamage;
        rigidBody.velocity = new(0f, rigidBody.velocity.y);
        if (multiAttack)
        {
            if (randomAttack == 1)
            {
                animationController.SetAnimation(SC_AnimationController.Animation.Attack1);
                currentFrame = animationController.GetAnimationFrame(attack1Frames);
                attackDamage = attack1Damage;
            }
            else
            {
                animationController.SetAnimation(SC_AnimationController.Animation.Attack2);
                currentFrame = animationController.GetAnimationFrame(attack2Frames);
                attackDamage = attack2Damage;
            }
        }
        else
        {
            animationController.SetAnimation(SC_AnimationController.Animation.Attack1);
            currentFrame = animationController.GetAnimationFrame(attack1Frames);
            attackDamage = attack1Damage;
        }

        if (!attackOnce) 
        {
            if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Attack1.ToString()) && currentFrame == attack1HitFrame ||
                animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Attack2.ToString()) && currentFrame == attack2HitFrame)
            {
                Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, targetMask);
                foreach (Collider2D target in hitTargets)
                {
                    StartCoroutine(target.GetComponent<SC_HealthController>().SetHealth("-", attackDamage, 0f));
                    target.GetComponent<SC_FlashController>().SetFlash("Damage");
                    audioController.PlayAudio("play", "Attack", true, 0);
                    if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Attack1.ToString()))
                    {
                        audioController.PlayAudio("play", attack1Name, true, 0);
                    }
                    else
                    {
                        audioController.PlayAudio("play", attack2Name, true, 0);
                    }               
                    StartCoroutine(cameraController.CameraShake(0.2f, 0.04f));
                }
                attackOnce = true;
            }       
        }

        if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Attack1.ToString()) || 
            animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Attack2.ToString()))
        {
            if (animationController.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f)
            {
                attackOnce = false;
                if (chase) { currentState = State.chase; }
                if (retreat) { currentState = State.retreat; }
            }
        }
    }
    /// <summary> Death behaviour: When health reached 0, stop movement, play death animation, destroy game object. </summary>
    private void Death()
    {
        rigidBody.velocity = new(0f, rigidBody.velocity.y);
        audioController.PlayAudio("play", "Attack", true, 0);
        animationController.SetAnimation(SC_AnimationController.Animation.Death);
        if (!onGround)
        {
            animationController.Animator.speed = 0f;
        }
        else
        {
            animationController.Animator.speed = 1f;
            boxCollider.enabled = false;
            rigidBody.Sleep();
            if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Death.ToString()) && animationController.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f)
            {
                Destroy(gameObject, Random.Range(10f, 15f));
            }
        }
    }
    /// <summary> Hit behaviour: Retreat if in idle or patrol state. </summary>
    public void Hit()
    {
        audioController.PlayAudio("play", "Attack", true, 0);
        if (currentState == State.idle || currentState == State.patrol)
        {
            currentState = State.retreat;
        }
    }
    /// <summary> Cool down set by type and duration ("idle", "autoPatrol", "manualPatrol", "retreat", "death"). </summary>
    private IEnumerator CoolDown(string type, float duration)
    {
        switch (type)
        {
            case "idle":
                yield return new WaitForSeconds(duration);
                currentState = State.patrol;
                coroutine = null;
                break;
            case "patrol":
                yield return new WaitForSeconds(duration);
                audioController.PlayAudio("play", "Patrol", true, 0);
                coroutinePatrol = null;
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
                facingDirection = (target.position - boxCollider.bounds.center).normalized;
                yield return new WaitForSeconds(duration);
                coroutine = null;
                break;
            default:
                Debug.LogWarning($"Cooldown type: {type} not found!");
                break;
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
    #endregion
}