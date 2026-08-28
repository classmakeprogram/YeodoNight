using UnityEngine;

/// <summary>
/// 적 체력 / 사망 처리. 적 프리팹 루트에 부착한다.
/// 부위 판정은 자식 콜라이더의 Hitbox가 담당하고, 이 스크립트로 데미지를 모은다.
/// </summary>
public class EnemyTarget : MonoBehaviour
{
    [Header("스탯")]
    [Tooltip("스테이지 1~2 / 웨이브 1 기준 체력. 스테이지·웨이브에 따라 스포너가 보정한다.")]
    public float baseHp = 80f;
    public bool isHiddenEnemy = false;

    [Header("사망")]
    [Tooltip("사망 애니메이션 재생 시간(초). 0이면 즉시 제거.")]
    public float deathDelay = 0f;

    public float Hp { get; private set; }
    private bool isDead;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        Hp = baseHp;
        if (MissionManager.Instance != null)
            Hp += MissionManager.Instance.currentEnemyHpModifier;
    }

    public void TakeDamage(float damage, bool isHeadshot)
    {
        if (isDead) return;

        Hp -= damage;
        if (anim != null) anim.SetTrigger("hit");

        if (HUD.Instance != null)
            HUD.Instance.ReportHit(transform.position + Vector3.up * 1.6f, damage, isHeadshot, Hp <= 0f);

        if (Hp <= 0f) Die(isHeadshot);
    }

    private void Die(bool isHeadshot)
    {
        isDead = true;

        RobotEnemyAI ai = GetComponent<RobotEnemyAI>();
        if (ai != null) ai.OnDeath();

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (anim != null) anim.SetTrigger("die");

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnEnemyKilled(isHeadshot, isHiddenEnemy);
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddKill(isHeadshot, isHiddenEnemy);
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.NotifyEnemyKilled();

        Destroy(gameObject, deathDelay);
    }
}
