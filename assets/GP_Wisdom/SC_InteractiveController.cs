using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SC_InteractiveController : MonoBehaviour
{
    #region Variables
    [Header("Interactive")]
    [Tooltip("Sets interact function event.")][SerializeField] private UnityEvent interactFunction;
    [Tooltip("Sets interact input and UI distance.")][Range(1f, 2f)][SerializeField] private float interactDistance;
    [Tooltip("Sets display UI distance.")][Range(5f, 15f)][SerializeField] private float displayDistance;
    [Tooltip("Sets display UI movement speed.")][Range(1f, 2f)][SerializeField] private float displaySpeed;
    [Tooltip("Gets interactive inner scale.")] private Vector2 interactiveInnerScale;
    [Tooltip("Gets interactive outer scale.")] private Vector2 interactiveOuterScale;
    [Tooltip("Gets and sets interact event to execute once.")] public bool CallOnce { get; set; }

    [Header("Components")]
    [Tooltip("Gets player GameObject for transform position.")] private GameObject player;
    [Tooltip("Gets player layer mask.")] private LayerMask playerMask;
    [Tooltip("Gets World Canvas RectTransform component.")] private RectTransform displayCanvasTransform;
    [Tooltip("Gets interactive inner RectTransform component.")] private RectTransform interactiveInnerTransform;
    [Tooltip("Gets interactive outer RectTransform component.")] private RectTransform interactiveOuterTransform;
    [Tooltip("Gets interactive icon RectTransform component.")] private RectTransform interactiveIconTransform;
    [Tooltip("Gets interactive icon Image component.")] private Image interactiveIconImage;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    void Update() => GetDistanceInput();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        player = GameObject.FindWithTag("Player");
        playerMask = LayerMask.GetMask("Player");
        displayCanvasTransform = transform.GetChild(0).gameObject.GetComponent<RectTransform>();
        interactiveInnerTransform = displayCanvasTransform.gameObject.transform.GetChild(0).GetComponent<RectTransform>();
        interactiveOuterTransform = displayCanvasTransform.gameObject.transform.GetChild(1).GetComponent<RectTransform>();
        interactiveIconTransform = displayCanvasTransform.gameObject.transform.GetChild(2).GetComponent<RectTransform>();
        interactiveIconImage = displayCanvasTransform.gameObject.transform.GetChild(2).GetComponent<Image>();
        interactiveInnerScale = interactiveInnerTransform.localScale;
        interactiveOuterScale = interactiveOuterTransform.localScale;
        interactiveInnerTransform.sizeDelta = new(0f, 0f);
        interactiveInnerTransform.gameObject.SetActive(false);
        interactiveOuterTransform.gameObject.SetActive(false);
        interactiveIconTransform.gameObject.SetActive(false);
        displayCanvasTransform.gameObject.SetActive(false);
    }
    /// <summary> Get distance and input from player to display UI and execute interact function. </summary>
    private void GetDistanceInput()
    {
        float playerDistance = (transform.position - player.transform.position).sqrMagnitude;
        Vector3 playerDirection = (player.transform.position - transform.position).normalized * displayDistance;
        RaycastHit2D hitTarget = Physics2D.Linecast(transform.position, transform.position + playerDirection, playerMask);

        // Maintain UI above interactable by resetting canvas z rotation to the opposite game object rotation.
        if (transform.rotation.z != 0f && displayCanvasTransform.rotation.z != -transform.rotation.z)
        {
            displayCanvasTransform.rotation = Quaternion.Euler(0, 0, -transform.rotation.z);
        }

        if (hitTarget.collider != null && playerDistance > interactDistance * interactDistance)
        {
            if (CallOnce) { CallOnce = false; }
            displayCanvasTransform.gameObject.SetActive(true);
            interactiveInnerTransform.gameObject.SetActive(true);
            
            if (!Mathf.Approximately(interactiveInnerTransform.sizeDelta.x, 1f))
            {
                LeanTween.size(interactiveInnerTransform, new(1f, 1f), 0.8f).setEaseOutBack();
            }
            if (interactiveInnerTransform.sizeDelta.x > 0.95f)
            {
                interactiveOuterTransform.gameObject.SetActive(true);
                interactiveIconTransform.gameObject.SetActive(true);
                if (!Mathf.Approximately(interactiveOuterTransform.localScale.x, interactiveOuterScale.x))
                {
                    interactiveOuterTransform.localScale = Vector2.Lerp(new(interactiveOuterTransform.localScale.x, interactiveOuterTransform.localScale.y), new(interactiveOuterScale.x, interactiveOuterScale.y), Time.time * displaySpeed);
                }
                if (interactiveIconTransform.gameObject.activeSelf)
                {
                    interactiveIconImage.color = Color.Lerp(new(255f, 255f, 255f, 1f), new(255f, 255f, 255f, 0f), Mathf.PingPong(Time.time * 1.5f, 1f));
                }
            }
        }
        if (hitTarget.collider != null && playerDistance <= interactDistance * interactDistance)
        {
            interactiveOuterTransform.localScale = Vector2.Lerp(new(interactiveOuterScale.x, interactiveOuterScale.y), new(interactiveOuterScale.x + 0.1f, interactiveOuterScale.y + 0.1f), Mathf.PingPong(Time.time * displaySpeed, 1f));
            if (interactiveIconTransform.gameObject.activeSelf)
            {
                interactiveIconImage.color = Color.Lerp(new(255f, 255f, 255f, 1f), new(255f, 255f, 255f, 0f), Mathf.PingPong(Time.time * 1.5f, 1f));
            }
            if (Input.GetButtonDown("Fire1") && !CallOnce)
            {
                interactFunction.Invoke();
                CallOnce = true;
            }
        }
        if (hitTarget.collider == null && displayCanvasTransform.gameObject.activeSelf)
        {
            if (CallOnce) { CallOnce = false; }
            interactiveIconTransform.gameObject.SetActive(false);
            if (!Mathf.Approximately(interactiveOuterTransform.localScale.x, interactiveInnerScale.x))
            {
                interactiveOuterTransform.localScale = Vector2.Lerp(new(interactiveOuterTransform.localScale.x, interactiveOuterTransform.localScale.y), new(interactiveInnerScale.x, interactiveInnerScale.y), Time.time * displaySpeed);
            }
            else
            {
                interactiveOuterTransform.gameObject.SetActive(false);
            }
            if (interactiveInnerTransform.sizeDelta.x > 0.1f)
            {
                LeanTween.size(interactiveInnerTransform, new(0f, 0f), 0.3f);
            }
            else
            {
                LeanTween.cancel(interactiveInnerTransform);
                interactiveInnerTransform.sizeDelta = new(0f, 0f);
                interactiveInnerTransform.gameObject.SetActive(false);
                displayCanvasTransform.gameObject.SetActive(false);
            }
        }
    }
    #endregion
}