using System.Collections;
using UnityEngine;

public class SC_HealthController : MonoBehaviour
{
    #region Variables   
    [Header("Health")]
    [Tooltip("Sets max health.")][Range(0, 100)][SerializeField] private float health = 100f;
    [Tooltip("Gets and sets current health.")] public float CurrentHealth { get; private set; }
    [field: Tooltip("Gets and sets invincibility.")][field: SerializeField] public bool Invincible { get; set; }
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    #endregion

    #region Class Functions
    /// <summary> Set variables. </summary>
    private void Initialization()
    {
        CurrentHealth = health;
    }
    /// <summary> Set health by type, amount and duration ("+" add health), ("-" sub health), ("+=" add health overtime, secs), ("-=" sub health overtime, secs), ("++" add full health), ("--" sub full health). </summary>
    public IEnumerator SetHealth(string type, float amount, float duration)
    {
        switch (type)
        {
            case "+": // Add health.
                CurrentHealth += amount;
                if (CurrentHealth >= health) { CurrentHealth = health; }
                break;
            case "-": // Subtract health.
                if (!Invincible)
                {
                    CurrentHealth -= amount;
                    if (CurrentHealth <= 0) { CurrentHealth = 0; }
                }
                break;
            case "+=": // Add health overtime.
                while (duration > 0)
                {
                    if (CurrentHealth < health)
                    {
                        CurrentHealth += amount;
                    }
                    else
                    {
                        CurrentHealth = health;
                    }                
                    yield return new WaitForSeconds(1f);
                    duration -= 1f;
                }
                break;
            case "-=": // Subtract health overtime.
                if (!Invincible)
                {
                    while (duration > 0)
                    {
                        if (CurrentHealth > 0)
                        {
                            CurrentHealth -= amount;
                        }
                        else
                        {
                            CurrentHealth = 0;
                            break;
                        }
                        yield return new WaitForSeconds(1f);
                        duration -= 1f;
                    }
                }
                break;
            case "++": // Add full health.
                CurrentHealth = health;
                break;
            case "--": // Subtract full health.
                CurrentHealth = 0;
                break;
            default:
                Debug.LogWarning($"Set health type: {type} not found!");
                break;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Eliminate full health if object enters death bounds.
        if (collision.CompareTag("DeathBounds"))
        {
            StartCoroutine(SetHealth("--", 0f, 0f));
        }
    }
    #endregion
}