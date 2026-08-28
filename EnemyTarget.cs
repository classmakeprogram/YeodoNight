using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float hp = 80f;             
    public bool isHiddenEnemy = false;  
    private bool isDead = false;

    private void Start()
    {
        if (MissionManager.Instance != null && MissionManager.Instance.currentStage >= 3)
        {
            hp += MissionManager.Instance.currentEnemyHpModifier;
        }
    }

    public void TakeDamage(float damage, bool isHeadshot, bool isHidden)
    {
        if (isDead) return;

        hp -= damage;
        if (hp <= 0) Die(isHeadshot, isHidden);
    }

    private void Die(bool isHeadshot, bool isHidden)
    {
        isDead = true;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnEnemyKilled(isHeadshot, isHidden || isHiddenEnemy);
        }
        Destroy(gameObject); 
    }
}