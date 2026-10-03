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
    private RobotEnemyAI ai;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        ai = GetComponent<RobotEnemyAI>();
    }

    private void Start()
    {
        Hp = baseHp;
        // 미션 체력 보정은 Mission 모드에서만. Waves 모드는 스포너가 hpPerWave로 보정하므로
        // MissionManager가 씬에 남아 있어도 이중 적용되지 않도록 막는다.
        bool wavesMode = EnemySpawner.Instance != null
            && EnemySpawner.Instance.mode == EnemySpawner.SpawnMode.Waves;
        if (!wavesMode && MissionManager.Instance != null)
            Hp += MissionManager.Instance.currentEnemyHpModifier;
    }

    public void TakeDamage(float damage, bool isHeadshot)
    {
        if (isDead) return;

        Hp -= damage;
        if (anim != null) anim.SetTrigger("hit");
        if (Hp > 0f && ai != null) ai.OnHit();

        if (HUD.Instance != null)
            HUD.Instance.ReportHit(transform.position + Vector3.up * 1.6f, damage, isHeadshot, Hp <= 0f);

        if (Hp <= 0f) Die(isHeadshot);
    }

    /// <summary>자폭형이 스스로 터질 때. 플레이어 처치가 아니므로 점수·미션 카운트 없음.</summary>
    public void SelfDestruct()
    {
        if (isDead) return;
        Die(false, false);
    }

    private void Die(bool isHeadshot, bool byPlayer = true)
    {
        isDead = true;

        if (ai != null) ai.OnDeath();

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (anim != null) anim.SetTrigger("die");

        // 스포너가 생존 수를 먼저 갱신해야 MissionManager의 보충 스폰 판정이 정확하다.
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.NotifyEnemyKilled();

        if (!byPlayer)
        {
            if (EnemySpawner.Instance != null) EnemySpawner.Instance.EnsureEnemiesAlive();
            Destroy(gameObject);
            return;
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnEnemyKilled(isHeadshot, isHiddenEnemy);
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddKill(isHeadshot, isHiddenEnemy);

        Destroy(gameObject, deathDelay);
    }
}
