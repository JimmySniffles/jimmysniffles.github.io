using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SC_PlayerController : MonoBehaviour
{
    #region Variables
    [Tooltip("Sets types of states.")] public enum State { free, jump, wallJump, climb, dodge, attack, teleport, death };
    [Tooltip("Gets and sets current state.")][field: SerializeField] public State CurrentState { get; private set; }

    [Header("Movement")]
    [Tooltip("Sets movement speed.")][SerializeField] private float moveSpeed;
    [Tooltip("Sets dodge speed.")][SerializeField] private float dodgeSpeed;
    [Tooltip("Sets jump height.")][SerializeField] private float jumpHeight;
    [Tooltip("Sets variable jump cut. Higher = smoother cut.")][SerializeField] private float jumpCut;
    [Tooltip("Sets hang time to jump after moving off the ground (seconds).")][SerializeField] private float jumpHang;
    [Tooltip("Sets jump input buffer to jump before landing on the ground (seconds).")][SerializeField] private float jumpBuffer;
    [Tooltip("Sets wall jump speed horizontally.")][SerializeField] private float wallJumpSpeed;
    [Tooltip("Sets wall jump height vertically.")][SerializeField] private float wallJumpHeight;
    [Tooltip("Sets wall jump movement control delay.")][SerializeField] private float wallJumpDelay;
    [Tooltip("Sets wall sliding speed.")][SerializeField] private float wallSlideSpeed;
    [Tooltip("Sets wall climb speed.")][SerializeField] private float wallClimbSpeed;
    [Tooltip("Sets wall climb timer (seconds).")][SerializeField] private float wallClimbStamina;
    [Tooltip("Sets Rigidbody2D gravity scale.")][SerializeField] private float gravityModifier;
    [Tooltip("Sets Rigidbody2D gravity scale while falling.")][SerializeField] private float gravityFallModifier;
    [Tooltip("Sets Rigidbody2D maximum y velocity (gravity).")][SerializeField] private float gravityMaxVelocity;

    [field: Header("Combat")]
    [field: Tooltip("Gets if weapon is held.")][field: SerializeField] public bool HasWeapon { get; set; }
    [Tooltip("Sets weapon speed.")][SerializeField] private float weaponSpeed;
    [Tooltip("Sets weapon distance.")][SerializeField] private float weaponDistance;
    [Tooltip("Sets weapon damage.")][SerializeField] private float weaponDamage;
    [Tooltip("Sets weapon air throw amount.")][SerializeField] private int weaponAirThrow;
    [Tooltip("Sets weapon air throw time scale.")][Range(0f, 1f)][SerializeField] private float weaponAirTimeScale;
    [Tooltip("Sets weapon air throw timer (seconds).")][SerializeField] private float weaponAirTime;
    [Tooltip("Sets weapon air throw gravity.")][SerializeField] private float weaponAirGravity;
    [Tooltip("Sets weapon air throw gravity timer (seconds).")][SerializeField] private float weaponAirGravityTime;
    [Tooltip("Sets weapon teleport gravity timer (seconds).")][SerializeField] private float weaponTeleportGravityTime;
    [Tooltip("Sets teleport scale speed.")][SerializeField] private float teleportScaleSpeed;
    [Tooltip("Sets teleport hold time (seconds).")][SerializeField] private float teleportTime;
    [Tooltip("Gets weapon prefab game object.")] private UnityEngine.Object weaponPrefab;
    [Tooltip("Gets spawned weapon prefab game object.")] private GameObject spawnedWeapon;

    [Header("Components")]
    [Tooltip("Gets BoxCollider2D component.")] private BoxCollider2D boxCollider;
    [Tooltip("Gets Rigidbody2D component.")] private Rigidbody2D rigidBody;
    [Tooltip("Gets SpriteRenderer component.")] private SpriteRenderer spriteRenderer;
    [Tooltip("Gets HealthController script component.")] private SC_HealthController healthController;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets AnimationController script component.")] private SC_AnimationController animationController;
    [Tooltip("Gets MenuController script component to get game pause.")] private SC_MenuController menuController;
    [Tooltip("Gets GameController script component to get respawn.")] private SC_GameController gameController;
    [Tooltip("Gets CameraController script component.")] private SC_CameraController cameraController;
    [Tooltip("Gets main camera game object.")] private Camera cameraMain;
    [Tooltip("Gets teleport ParticleSystem.")] private ParticleSystem particlesTeleport;

    [Header("Checks")]
    [Tooltip("Gets ground tiles layer mask.")] private LayerMask groundMask;
    [Tooltip("Gets platform layer mask.")] private LayerMask platformMask;
    [Tooltip("Gets interactables layer mask.")] private LayerMask interactMask;
    [Tooltip("Gets if on ground.")] private bool onGround;
    [Tooltip("Gets if on wall.")] private bool onWall;
    [Tooltip("Gets running teleport Coroutine to prevent multiple.")] private Coroutine teleportCoroutine;
    [Tooltip("Gets if near an interactable to not attack.")] private bool nearInteractable;
    [Tooltip("Gets facing movement direction.")] private Vector2 facingDirection;
    [Tooltip("Gets cursor direction to set projectile direction.")] private Vector2 cursorDirection;
    [Tooltip("Gets if input is disabled.")] public bool InputDisabled { get; set; }
    [Tooltip("Gets delay time the player can move after wall jumping.")] private bool canMove = true;
    [Tooltip("Gets if jump pressed to prevent jump animation cancelling.")] private bool jumpPressed;
    [Tooltip("Gets if wall jump pressed to prevent jump animation cancelling.")] private bool wallJumpPressed;
    [Tooltip("Gets current jump hang time amount.")] private float currentJumpHang;
    [Tooltip("Gets current jump buffer amount.")] private float currentJumpBuffer;
    [Tooltip("Gets current wall climb timer.")] private float currentWallClimbStamina;
    [Tooltip("Gets current weapon air throw amount.")] private int currentWeaponAirThrow;
    [Tooltip("Gets current weapon air throw time length.")] private float currentWeaponAirTime;
    [Tooltip("Gets current weapon air throw gravity time length.")] private float currentWeaponAirGravityTime;

    [Header("Inputs")]
    [Tooltip("Gets horizontal movement input.")] private float moveHorizontal;
    [Tooltip("Gets jump press input.")] private bool jump;
    [Tooltip("Gets jump release input.")] private bool jumpRelease;
    [Tooltip("Gets action release input.")] private bool actionRelease;
    [Tooltip("Gets action hold input.")] private bool actionHold;
    [Tooltip("Gets dodge press input.")] private bool dodge;
    [Tooltip("Gets if UI mobile input is active.")] private bool mobileInput = false;
    [Tooltip("Gets mobile horizontal movement input.")] public float mobileMove { get; set; }
    [Tooltip("Gets mobile jump press input.")] public bool mobileJump { get; set; }
    [Tooltip("Gets mobile jump release input.")] public bool mobileJumpRelease { get; set; }
    [Tooltip("Gets mobile action release input.")] public bool mobileActionRelease { get; set; }
    [Tooltip("Gets mobile action hold input.")] public bool mobileActionHold { get; set; }
    [Tooltip("Gets mobile dodge press input.")] public bool mobileDodge { get; set; }
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update()
    {
        GetInputs();
        GetCollisionDirection();
        SetGravity();
        SetGameTime();
        switch (CurrentState)
        {
            case State.free: Free(); break;
            case State.jump: Jump(); break;
            case State.wallJump: WallJump(); break;
            case State.climb: Climb(); break;
            case State.dodge: Dodge(); break;
            case State.attack: Attack(); break;
            case State.teleport: Teleport(); break;
            case State.death: Death(); break;
        }
    }
    void FixedUpdate() => Move();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthController = GetComponent<SC_HealthController>();
        audioController = GetComponent<SC_AudioController>();
        animationController = GetComponent<SC_AnimationController>();
        weaponPrefab = Resources.Load("PR_Boomerang");
        particlesTeleport = transform.GetChild(0).GetComponent<ParticleSystem>();
        cameraMain = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        cameraController = GameObject.Find("PR_CameraHolder").GetComponent<SC_CameraController>();
        gameController = GameObject.FindWithTag("GameController").GetComponent<SC_GameController>();
        menuController = GameObject.Find("PR_UI").GetComponent<SC_MenuController>();   
        rigidBody.gravityScale = gravityModifier;
        groundMask = LayerMask.GetMask("GroundTiles");
        platformMask = LayerMask.GetMask("Platforms");
        interactMask = LayerMask.GetMask("Interactables");
        if (GameObject.FindWithTag("MobileUI") != null) { mobileInput = true; }
        Physics2D.IgnoreLayerCollision(8, 10, true); // Ignore AI.
        HasWeapon = true;
    }
    /// <summary> Get input if not paused, dead or input disabled. </summary>
    private void GetInputs()
    {
        if (healthController.CurrentHealth <= 0) { CurrentState = State.death; }

        if (!menuController.GamePaused && !InputDisabled && CurrentState != State.death)
        {
            moveHorizontal = mobileInput ? mobileMove : Input.GetAxisRaw("Horizontal");
            jump = mobileInput ? mobileJump : Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.UpArrow);
            jumpRelease = mobileInput ? mobileJumpRelease : Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.UpArrow);
            actionRelease = mobileInput ? mobileActionRelease : Input.GetButtonUp("Fire1");
            actionHold = mobileInput ? mobileActionHold : Input.GetButton("Fire1");
            dodge = mobileInput ? mobileDodge : Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

            // Jump input buffer.
            if (jump)
            {
                currentJumpBuffer = jumpBuffer;
            }
            else if (currentJumpBuffer > 0f)
            {
                currentJumpBuffer -= Time.deltaTime;
            }
        }
        if (mobileInput)
        {
            mobileJump = false;
            mobileJumpRelease = false;
            mobileActionRelease = false;
        }
    }
    /// <summary> Get collision and set rotation direction. </summary>
    private void GetCollisionDirection()
    {
        float interactRadius = 2f;
        facingDirection = new(moveHorizontal, 0f);
        if (!onWall) { animationController.SetDirection(facingDirection); }

        onGround = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, groundMask) || Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, 0.2f, platformMask);
        onWall = Physics2D.Linecast(boxCollider.bounds.center, new(boxCollider.bounds.center.x + 0.6f * facingDirection.x, boxCollider.bounds.center.y), groundMask) || 
                 Physics2D.Linecast(boxCollider.bounds.center, new(boxCollider.bounds.center.x + 0.6f * facingDirection.x, boxCollider.bounds.center.y), platformMask);
        nearInteractable = Physics2D.OverlapCircle(transform.position, interactRadius, interactMask) ? true : false;

        if (onGround)
        {
            currentJumpHang = jumpHang;
            currentWallClimbStamina = wallClimbStamina;
            currentWeaponAirTime = weaponAirTime;
        }
        else if (currentJumpHang > 0)
        {
            currentJumpHang -= Time.deltaTime;  
        }
        if (onGround || onWall) { currentWeaponAirThrow = weaponAirThrow; }
        if (jumpPressed && !onGround) { jumpPressed = false; }
        if (wallJumpPressed && !onWall) { wallJumpPressed = false; }
        if (onWall && !onGround && CurrentState == State.free) 
        { 
            spriteRenderer.flipX = true; 
        } 
        else if (canMove && Mathf.Round(moveHorizontal) != 0f && !onWall || CurrentState == State.climb) 
        { 
            spriteRenderer.flipX = false; 
        }
        if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Fall.ToString()) && onGround)
        {
            audioController.PlayAudio("play", "Land", true, 0);
            StartCoroutine(cameraController.CameraShake(0.07f, 0.03f));
        }
    }
    /// <summary> Set gravity (if throwing weapon or falling) and max gravity. </summary>
    private void SetGravity()
    {
        if (currentWeaponAirGravityTime > 0f)
        {
            rigidBody.gravityScale = weaponAirGravity;
            currentWeaponAirGravityTime -= Time.deltaTime;
        }
        else
        {
            rigidBody.gravityScale = rigidBody.velocity.y < 0 ? gravityFallModifier : gravityModifier;
        }
        if (rigidBody.velocity.y < gravityMaxVelocity) { rigidBody.velocity = Vector2.ClampMagnitude(rigidBody.velocity, gravityMaxVelocity); }
    }
    /// <summary> Set game time (slow game if action hold is inputed in the air). </summary>
    private void SetGameTime()
    {
        if (actionHold && !menuController.GamePaused && !onWall && !onGround && HasWeapon && currentWeaponAirThrow > 0 && currentWeaponAirTime > 0)
        {
            Time.timeScale = weaponAirTimeScale;
            currentWeaponAirTime -= Time.deltaTime;
        }
        else if (!menuController.GamePaused && Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
    }
    /// <summary> Set movement with physics (using FixedUpdate) and animation. </summary>
    private void Move()
    {
        if (CurrentState == State.free)
        {
            if (canMove) { rigidBody.velocity = new(moveHorizontal * moveSpeed, rigidBody.velocity.y); }
            if (Mathf.Round(moveHorizontal) == 0f && onGround && !jumpPressed) // Idle.
            {
                animationController.SetAnimation(SC_AnimationController.Animation.Idle);
            }
            if (Mathf.Round(moveHorizontal) != 0f && onGround && !jumpPressed) // Run.
            {
                animationController.SetAnimation(SC_AnimationController.Animation.Run);
                int currentFrame = animationController.GetAnimationFrame(16);
                if (currentFrame == 4 || currentFrame == 12)
                {
                    audioController.PlayAudio("play", "Step", true, 0);
                }
            }
        }
    }
    /// <summary> Transition to Jump, WallJump, Climb, Dodge, Attack, Teleport, Interact. Jump used in Update to avoid input delay in FixedUpdate. </summary>
    private void Free()
    {
        float bufferY = -2f;
        if (rigidBody.velocity.y < 0f && !onGround && !onWall) // Fall.
        { 
            animationController.SetAnimation(SC_AnimationController.Animation.Fall); 
        } 
        if (onWall && !onGround && !wallJumpPressed) // Slide.
        {
            rigidBody.velocity = new(rigidBody.velocity.x, Mathf.Clamp(rigidBody.velocity.y, -wallSlideSpeed, float.MaxValue));
            animationController.SetAnimation(SC_AnimationController.Animation.Slide);
            audioController.PlayAudio("play", "Slide", true, 0);
        }
        else if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Slide.ToString()) && !onGround)
        {
            animationController.SetAnimation(SC_AnimationController.Animation.Jump);     
        }
        else
        {
            audioController.PlayAudio("stop", "Slide", true, 0);
        }
        if (currentJumpBuffer > 0 && currentJumpHang > 0) { CurrentState = State.jump; } // Jump.
        if (jump && onWall && !onGround) { CurrentState = State.wallJump; } // Wall jump.
        if (jumpRelease && rigidBody.velocity.y > 0) { rigidBody.velocity = new(rigidBody.velocity.x, rigidBody.velocity.y / jumpCut); } // Variable jump.
        if (actionHold && onWall && currentWallClimbStamina > 0f) { CurrentState = State.climb; } // Climb.
        if (dodge && moveHorizontal != 0f && onGround && !onWall) { CurrentState = State.dodge; } // Dodge.
        if (actionRelease && menuController.PauseDelay <= 0f && !onWall && HasWeapon && currentWeaponAirThrow > 0 && !nearInteractable) // Attack.
        {
            cursorDirection = mobileInput && moveHorizontal != 0f && Input.touchCount >= 2 ? cameraMain.ScreenToWorldPoint(Input.GetTouch(1).rawPosition) - transform.position :
                              cameraMain.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            if (!onGround || onGround && cursorDirection.y > bufferY) // Prevent spawning projectile into the ground.
            {
                CurrentState = State.attack;
            }      
        }
        if (actionRelease && menuController.PauseDelay <= 0f && !onWall && spawnedWeapon != null) { CurrentState = State.teleport; } // Teleport.

        // Return camera zoom to start FOV if spawned weapon equals null.
        if (spawnedWeapon == null && !Mathf.Approximately(cameraMain.orthographicSize, cameraController.CameraStartFOV))
        {
            StartCoroutine(cameraController.CameraZoom(cameraController.CameraStartFOV, 6f));
        }  
    }
    /// <summary> Set jump. </summary>
    private void Jump()
    {
        rigidBody.velocity = new(rigidBody.velocity.x, jumpHeight);
        animationController.SetAnimation(SC_AnimationController.Animation.Jump);
        audioController.PlayAudio("play", "Move", true, 0);
        jumpPressed = true;
        currentJumpBuffer = 0;
        currentJumpHang = 0;
        CurrentState = State.free;
    }
    /// <summary> Set wall jump. </summary>
    private void WallJump()
    {
        rigidBody.velocity = new(wallJumpSpeed * -moveHorizontal, wallJumpHeight);
        animationController.SetAnimation(SC_AnimationController.Animation.Jump);
        audioController.PlayAudio("play", "Move", true, 0);
        StartCoroutine(StatsChange("moveDelay", false, 0f, wallJumpDelay));
        wallJumpPressed = true;
        CurrentState = State.free;
    }
    /// <summary> Set climb (climb wall for set time period). </summary>
    private void Climb()
    {
        rigidBody.velocity = new(rigidBody.velocity.x, wallClimbSpeed);
        animationController.SetAnimation(SC_AnimationController.Animation.Climb);
        currentWallClimbStamina -= Time.deltaTime;
        if (!actionHold || !onWall || currentWallClimbStamina <= 0f) { CurrentState = State.free; }
    }
    /// <summary> Set interact (check if near interactable). </summary>
    private void Interact()
    {

    }
    /// <summary> Set attack (throw projectile with reduced gravity). </summary>
    private void Attack()
    {
        bool runThrow = false;
        if (moveHorizontal == 0f && onGround)
        {
            animationController.SetAnimation(SC_AnimationController.Animation.Attack1);
        }
        if (moveHorizontal != 0f && onGround)
        {
            runThrow = true;
        }
        if (!onGround)
        {
            animationController.SetAnimation(SC_AnimationController.Animation.AirAttack);
        }
        int currentFrame = animationController.GetAnimationFrame(6);
        if ((currentFrame == 4 || runThrow) && spawnedWeapon == null)
        {
            StartCoroutine(cameraController.CameraZoom(cameraController.CameraStartFOV + 1f, 6f));
            spawnedWeapon = (GameObject)Instantiate(weaponPrefab, transform.position, transform.rotation);
            var projectileController = spawnedWeapon.GetComponent<SC_ProjectileController>();
            projectileController.Instantiator = gameObject;
            projectileController.ProjectileSpeed = weaponSpeed;
            projectileController.ProjectileDistance = weaponDistance;
            projectileController.ProjectileDamage = weaponDamage;
            projectileController.ProjectileDirection = cursorDirection;
            HasWeapon = false;
            currentWeaponAirThrow--;
            currentWeaponAirGravityTime = weaponAirGravityTime;
        }
        if (runThrow || animationController.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f || (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.AirAttack.ToString()) && onGround))
        {
            CurrentState = State.free;
        }
    }
    /// <summary> Set teleport. </summary>
    private void Teleport()
    {
        if (teleportCoroutine == null && spawnedWeapon != null)
        {
            var projectileController = spawnedWeapon.GetComponent<SC_ProjectileController>();
            if (projectileController.CurrentState == SC_ProjectileController.State.thrown && projectileController.TeleportReady)
            {
                teleportCoroutine = StartCoroutine(ITeleport());
            }
            else
            {
                CurrentState = State.free;
            }
        }  
    }
    /// <summary> Set teleport (play teleport particle system and change sprite alpha). </summary>
    private IEnumerator ITeleport()
    {
        float timer = 0f;
        Vector3 teleportOriginalScale = new(3f, 3f, 1f), teleportScale = new(0f, 3f, 1f);

        // Teleport Out
        canMove = false;
        currentWeaponAirGravityTime = weaponTeleportGravityTime;
        audioController.PlayAudio("play", "Teleport Out", false, 0);
        particlesTeleport.Play();

        while (transform.localScale.x != 0f)
        {
            timer += teleportScaleSpeed * Time.deltaTime;
            transform.localScale = Vector3.Lerp(teleportOriginalScale, teleportScale, timer);
            yield return null;
        }
        yield return new WaitForSeconds(0.001f);
        if (spawnedWeapon != null)
        {
            transform.position = spawnedWeapon.transform.position;
            Destroy(spawnedWeapon);
        }

        yield return new WaitForSeconds(teleportTime);

        // Teleport In
        particlesTeleport.Play();
        audioController.PlayAudio("play", "Teleport In", false, 0);
        while (transform.localScale.x != teleportOriginalScale.x)
        {
            timer += teleportScaleSpeed * Time.deltaTime;
            transform.localScale = Vector3.Lerp(teleportScale, teleportOriginalScale, timer);
            yield return null;
        }
        canMove = true;
        HasWeapon = true;
        teleportCoroutine = null;
        CurrentState = State.free;
    }
    /// <summary> Set dodge (combat roll animation with invincibility). </summary>
    private void Dodge()
    {
        float direction = transform.rotation.y == 0f ? 1f : -1f;
        rigidBody.velocity = new(direction * dodgeSpeed, rigidBody.velocity.y);
        healthController.Invincible = true;
        animationController.SetAnimation(SC_AnimationController.Animation.Dodge);
        int currentFrame = animationController.GetAnimationFrame(8);
        if (currentFrame == 1)
        {
            audioController.PlayAudio("play", "Move", true, 0);
        }
        if (currentFrame == 5)
        {
            audioController.PlayAudio("play", "Land", true, 0);
            StartCoroutine(cameraController.CameraShake(0.1f, 0.01f));
        }
        if ((animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Dodge.ToString()) && animationController.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f) || !onGround)
        {
            healthController.Invincible = false;
            CurrentState = State.free;
        }
    }
    /// <summary> Set death (stop movement, play death animation, move to respawn, reset health). </summary>
    private void Death()
    {
        rigidBody.velocity = new(0f, rigidBody.velocity.y);
        currentJumpBuffer = 0f;
        if (spawnedWeapon != null)
        {
            Destroy(spawnedWeapon);
            HasWeapon = true;
        }
        animationController.SetAnimation(SC_AnimationController.Animation.Death);
        if (animationController.Animator.GetCurrentAnimatorStateInfo(0).IsName(SC_AnimationController.Animation.Death.ToString()) && animationController.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f)
        {
            transform.position = gameController.RespawnPoint;
            StartCoroutine(healthController.SetHealth("++", 0f, 0f));
            CurrentState = State.free;
        }
    }
    /// <summary> Sets StatsChange by type, amount and duration to avoid coroutine cancelling from called destroyed classes ("moveDelay", "health", "movement", "jump", "climb", "thrown", "invincibility"). </summary>
    public void SetStatsChange(string type, bool increment, float amount, float duration) => StartCoroutine(StatsChange(type, increment, amount, duration));
    /// <summary> Stats change cool down set by type, increment (true = increase, false = decrease), amount and duration. </summary>
    private IEnumerator StatsChange(string type, bool increment, float amount, float duration)
    {
        switch (type)
        {
            case "moveDelay":
                canMove = false;
                yield return new WaitForSeconds(duration);
                canMove = true;
                break;
            case "health":
                if (increment)
                {
                    if (duration > 0f)
                    {
                        StartCoroutine(healthController.SetHealth("+=", amount, duration));
                    }
                    else
                    {
                        StartCoroutine(healthController.SetHealth("+", amount, duration));
                    }                   
                }
                else
                {
                    if (duration > 0f)
                    {
                        StartCoroutine(healthController.SetHealth("-=", amount, duration));
                    }
                    else
                    {
                        StartCoroutine(healthController.SetHealth("-", amount, duration));
                    }
                }
                break;
            case "movement":
                moveSpeed = (increment) ? moveSpeed += amount : moveSpeed -= amount;
                yield return new WaitForSeconds(duration);
                moveSpeed = (increment) ? moveSpeed -= amount : moveSpeed += amount;
                break;
            case "jump":
                if (increment)
                {
                    jumpHeight += amount; wallJumpHeight += amount; wallJumpSpeed += amount;
                }
                else
                {
                    jumpHeight -= amount; wallJumpHeight -= amount; wallJumpSpeed -= amount;
                }
                yield return new WaitForSeconds(duration);
                if (increment)
                {
                    jumpHeight -= amount; wallJumpHeight -= amount; wallJumpSpeed -= amount;
                }
                else
                {
                    jumpHeight += amount; wallJumpHeight += amount; wallJumpSpeed += amount;
                }
                break;
            case "climb":
                if (increment)
                {
                    wallClimbSpeed += amount; wallClimbStamina += amount;
                }
                else
                {
                    wallClimbSpeed -= amount; wallClimbStamina -= amount;
                }
                yield return new WaitForSeconds(duration);
                if (increment)
                {
                    wallClimbSpeed -= amount; wallClimbStamina -= amount;
                }
                else
                {
                    wallClimbSpeed += amount; wallClimbStamina += amount;
                }
                break;
            case "weaponThrow":
                if (increment)
                {
                    weaponSpeed += amount; weaponDistance += amount; weaponDamage += amount;
                }
                else
                {
                    weaponSpeed -= amount; weaponDistance -= amount; weaponDamage -= amount;
                }
                yield return new WaitForSeconds(duration);
                if (increment)
                {
                    weaponSpeed -= amount; weaponDistance -= amount; weaponDamage -= amount;
                }
                else
                {
                    weaponSpeed += amount; weaponDistance += amount; weaponDamage += amount;
                }
                break;
            case "invincibility":
                healthController.Invincible = true;
                yield return new WaitForSeconds(duration);
                healthController.Invincible = false;
                break;
            default:
                Debug.LogWarning($"Change stats type: {type} not found!");
                break;
        }
    }
    #endregion
}