using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SC_ProjectileController : MonoBehaviour
{
    #region Variables
    [Header("Projectile")]
    [Tooltip("Gets and sets current state.")] public State CurrentState = State.thrown;
    [Tooltip("Gets and sets the states the class can be in.")] public enum State { thrown, returning };
    [field: Tooltip("Gets and sets projectile speed.")][field: SerializeField] public float ProjectileSpeed { get; set; }
    [field: Tooltip("Gets and sets projectile maximum distance from instantiation.")][field: SerializeField] public float ProjectileDistance { get; set; }
    [field: Tooltip("Gets and sets projectile damage.")][field: SerializeField] public float ProjectileDamage { get; set; }
    [Tooltip("Gets and sets projectile rotation speed.")][SerializeField] private float projectileRotation;
    [field: Tooltip("Gets and sets projectile instantiator game object.")][field: SerializeField] public GameObject Instantiator { get; set; }
    [Tooltip("Gets and sets projectile direction.")] public Vector3 ProjectileDirection { get; set; }
    [Tooltip("Gets and sets if instantiator is able to teleport.")] public bool TeleportReady { get; private set; }

    [Header("Behaviours")]
    [Tooltip("Gets bouncing behaviour.")][SerializeField] private bool bouncing;
    [Tooltip("Gets returning behaviour.")][SerializeField] private bool returning;
    [Tooltip("Gets teleportation behaviour.")][SerializeField] private bool teleporting;
    [Tooltip("Sets max bouncing amount.")][SerializeField] private int bouncingAmount;
    [Tooltip("Sets delay to teleport.")][SerializeField] private float teleportDelay;
    [Tooltip("Gets and sets current bouncing amount.")] private int currentBouncingAmount;
    [Tooltip("Gets and sets current teleporting delay.")] private float currentTeleportDelay;
    [Tooltip("Gets current projectile distance from instantiator.")] private float currentProjectileDistance;
    [Tooltip("Gets previous current projectile distance from instantiator to check if projectile is stuck.")] private float projectileLastDistance;
    [Tooltip("Gets previous frame rigidbody velocity.")] private Vector2 projectileLastSpeed;
    [Tooltip("Gets time passed when projectile distance has not changed.")] private float projectileTimePassed;

    [Header("Components")]
    [Tooltip("Gets Rigidbody2D component.")] private Rigidbody2D rigidBody;
    [Tooltip("Gets SpriteRenderer component.")] private SpriteRenderer spriteRenderer;
    [Tooltip("Gets AudioController script component.")] private SC_AudioController audioController;
    [Tooltip("Gets CameraController script component for camera shake.")] private SC_CameraController cameraController;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Start() => Launch();
    void Update()
    {
        switch (CurrentState)
        {
            case State.thrown: Thrown(); break;
            case State.returning: Returning(); break;
        }
    }
    void FixedUpdate() => Rotate();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        Physics2D.IgnoreLayerCollision(8, 9, true); // Ignore player.
        Physics2D.IgnoreLayerCollision(9, 10, true); // Ignore AI.
        Physics2D.IgnoreLayerCollision(0, 9, true); // Ignore default boundary.
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioController = GetComponent<SC_AudioController>();
        cameraController = GameObject.Find("PR_CameraHolder").GetComponent<SC_CameraController>();
        currentTeleportDelay = teleportDelay;
    }
    /// <summary> Launch projectile at set direction and speed. Vector3 is used to protect the z. </summary>
    private void Launch()
    {
        rigidBody.velocity = new Vector3(ProjectileDirection.x, ProjectileDirection.y, 0f).normalized * ProjectileSpeed;
        audioController.PlayAudio("play", "Throw", true, 0);
        audioController.PlayAudio("play", "In Air", true, 0);
    }
    /// <summary> Rotate projectile with consistent set rate in FixedUpdate. </summary>
    private void Rotate()
    {
        rigidBody.rotation += projectileRotation;
        projectileLastSpeed = rigidBody.velocity;
        projectileLastDistance = currentProjectileDistance;
    }
    /// <summary> Decrease teleport delay and check if projectile is outside distance or bounce range to return. </summary>
    private void Thrown()
    {
        currentProjectileDistance = (transform.position - Instantiator.transform.position).sqrMagnitude;
        // Projectile instantiator is able to teleport to projectile position when timer reaches 0.
        if (teleporting)
        {
            if (currentTeleportDelay > 0f)
            {
                currentTeleportDelay -= Time.deltaTime;
            }
            else if (Physics2D.GetIgnoreLayerCollision(8, 9))
            {
                Physics2D.IgnoreLayerCollision(8, 9, false);
            }
            if (!TeleportReady && currentTeleportDelay <= 0f && currentBouncingAmount <= bouncingAmount - 1 && currentProjectileDistance <= ProjectileDistance * ProjectileDistance - 5f)
            {
                audioController.PlayAudio("play", "Teleport Up", false, 0);
                spriteRenderer.color = new Color(0f, 239f, 239f);
                TeleportReady = true;
            }
            if (TeleportReady && currentTeleportDelay <= 0f && currentBouncingAmount > bouncingAmount - 1 || currentProjectileDistance > ProjectileDistance * ProjectileDistance - 5f)
            {
                audioController.PlayAudio("play", "Teleport Down", false, 0);
                spriteRenderer.color = new Color(255f, 255f, 255f);
                TeleportReady = false;
            }
        }
        
        // Projectile returns to instantiator based on bouncing or distance amount.
        if (returning)
        {
            if (currentBouncingAmount >= bouncingAmount || currentProjectileDistance >= ProjectileDistance * ProjectileDistance)
            {
                Physics2D.IgnoreLayerCollision(8, 9, false);
                rigidBody.Sleep();
                CurrentState = State.returning;
            }
        }
    }
    /// <summary> Ignore collisions and return to instantiator position. </summary>
    private void Returning()
    { 
        transform.position = Vector2.MoveTowards(transform.position, Instantiator.transform.position, ProjectileSpeed * Time.deltaTime);

        // Destroy projectile if stuck for period of time.
        if (currentProjectileDistance == projectileLastDistance)
        {
            projectileTimePassed += Time.deltaTime;
            if (projectileTimePassed > 3f)
            {
                InstantiatorCollision();
            }
        }
    }
    /// <summary> Set variables, play effects and destory projectile upon collision with instantiator. </summary>
    private void InstantiatorCollision()
    {
        Instantiator.GetComponent<SC_PlayerController>().HasWeapon = true;
        Instantiator.GetComponent<SC_FlashController>().SetFlash("Interact");
        audioController.PlayAudio("play", "Catch", true, 0);
        audioController.PlayAudio("stop", "In Air", true, 0);
        StartCoroutine(cameraController.CameraShake(0.08f, 0.01f));
        Destroy(gameObject, 0.05f);
    }
    /// <summary> Bounce projectile. </summary>
    private void Bounce(Collision2D collision)
    {
        Vector2 projectileDirection;
        //ProjectileSpeed = projectileLastSpeed.magnitude;
        projectileDirection = Vector2.Reflect(projectileLastSpeed.normalized, collision.GetContact(0).normal);
        rigidBody.velocity = projectileDirection * ProjectileSpeed; // Mathf.Max(ProjectileSpeed, ProjectileSpeed); // Can set minimum speed if projectile speed is too low.
        currentBouncingAmount++;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Projectile collides with the ground and bounces.
        if (bouncing && CurrentState == State.thrown && collision.collider.gameObject.layer == LayerMask.NameToLayer("GroundTiles"))
        {
            audioController.PlayAudio("play", "Hit Ground", true, 0);
            StartCoroutine(cameraController.CameraShake(0.09f, 0.01f));
            Bounce(collision);
        }
        // Projectile collides with instantiator.
        if (collision.collider.gameObject == Instantiator && !Physics2D.GetIgnoreLayerCollision(8, 9))
        {
            InstantiatorCollision();
        }
        // Projectile collides with interactables.
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Interactables"))
        {
            collision.collider.gameObject.GetComponent<SC_FlashController>().SetFlash("Interact");
            audioController.PlayAudio("play", "Hit Tucker", true, 0);
            StartCoroutine(cameraController.CameraShake(0.05f, 0.01f));
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Projectile collides with boundary.
        if (collision.gameObject.layer == LayerMask.NameToLayer("Default") && teleporting)
        {
            audioController.PlayAudio("play", "Teleport Down", false, 0);
            spriteRenderer.color = new Color(255f, 255f, 255f);
            TeleportReady = false;
            teleporting = false;
        }
        // Projectile collides with AI.
        if (collision.gameObject.layer == LayerMask.NameToLayer("AI"))
        {
            StartCoroutine(collision.gameObject.GetComponent<SC_HealthController>().SetHealth("-", ProjectileDamage, 0f));
            collision.gameObject.GetComponent<SC_FlashController>().SetFlash("Damage");
            collision.gameObject.GetComponent<SC_AIController>().Hit();
            audioController.PlayAudio("play", "Hit AI", true, 0);
            StartCoroutine(cameraController.CameraShake(0.05f, 0.01f));
        }
    }
    #endregion
}