using UnityEngine;

public class SC_AnimationController : MonoBehaviour
{
    #region Variables
    [Tooltip("Sets types of animations.")] public enum Animation { Idle, Run, Jump, Fall, Slide, Climb, Attack1, Attack2, AirAttack, Death, Interact, Push, Dodge, Sit };
    [Tooltip("Gets and sets current animation.")] public Animation CurrentAnimation { get; private set; }
    [Tooltip("Gets Animator component.")] public Animator Animator { get; private set; }
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    #endregion

    #region Class Functions
    /// <summary> Get components. </summary>
    private void Initialization()
    {
        Animator = GetComponent<Animator>();
    }
    /// <summary> Set animation direction on x axis. </summary>
    public void SetDirection(Vector2 facingDirection)
    {
        if (!Mathf.Approximately(facingDirection.x, 0f)) { transform.rotation = facingDirection.x < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity; }
    }
    /// <summary> Set animation without interrupting itself. </summary>
    public void SetAnimation(Animation newAnimation)
    {
        if (CurrentAnimation == newAnimation) { return; }
        Animator.Play(newAnimation.ToString());
        CurrentAnimation = newAnimation;
    }
    /// <summary> Get current animation index frame (starts at 0). Formula link: https://answers.unity.com/questions/149717/check-the-current-frame-of-an-animation.html </summary>
    public int GetAnimationFrame(int animationFrames)
    {
        int animationIndex = ((int)(Animator.GetCurrentAnimatorStateInfo(0).normalizedTime * (animationFrames))) % animationFrames;
        return animationIndex;
    }
    #endregion
}