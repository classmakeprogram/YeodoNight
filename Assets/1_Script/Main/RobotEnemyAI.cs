using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 로봇 적 AI. 감지 범위 안에 플레이어가 들어오면 반응한다.
///  - Melee : 붙어서 근접 공격
///  - Ranged: 선호 사거리를 유지하며 투사체 발사(너무 가까우면 후퇴)
///  - Explode: 붙어서 자폭(범위 데미지, 처치 점수 없음)
/// HiddenEnemy 태그면 플레이어가 가까이 오거나 맞힐 때까지 모습을 감추고 대기한다.
/// 거리/상태를 애니메이터에 전달한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotEnemyAI : MonoBehaviour
{
    public enum AttackStyle { Melee, Ranged, Explode }

    [Header("이동")]
    public float moveSpeed = 3.5f;
    public float detectRange = 25f;
    public float stopRange = 2f;

    [Header("시야")]
    [Tooltip("시야를 막는 레이어(레벨 지오메트리 등). 기본은 전 레이어.")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("시야 기준점. 비우면 몸통 위쪽(1.5m)을 사용한다.")]
    public Transform eye;

    [Header("공격")]
    public AttackStyle attackStyle = AttackStyle.Melee;
    public float attackDamage = 10f;      // 근접 데미지
    public float attackRange = 2.5f;      // 근접 사거리
    public float attackCooldown = 1.5f;
    [Tooltip("자폭형 폭발 반경. 데미지는 attackDamage.")]
    public float explodeRadius = 3f;

    [Header("피격")]
    public float staggerTime = 0.3f;
    public float knockback = 0.5f;

    [Header("숨은 적")]
    [Tooltip("이 거리 안에서 시야가 트이면 정체를 드러낸다.")]
    public float revealRange = 6f;

    [Header("원거리 (attackStyle = Ranged)")]
    public GameObject projectilePrefab;   // EnemyProjectile 컴포넌트를 가진 프리팹
    public Transform muzzle;              // 발사 위치. 없으면 몸통 위쪽
    public float preferredRange = 12f;
    public float projectileSpeed = 20f;
    public float projectileDamage = 8f;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private PlayerController playerCtrl;
    private float nextAttackTime;
    private bool isCrouching;
    private bool isRunning;
    private bool isDead;
    private float staggerUntil;
    private bool concealed;
    private Renderer[] renderers;

    private void Awake()
    {
        if (!CompareTag("Enemy") && !CompareTag("HiddenEnemy"))
            gameObject.tag = "Enemy";

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = stopRange;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCtrl = p.GetComponent<PlayerController>();
        }

        // 태그는 스포너가 Instantiate 직후 바꾸므로 Awake가 아니라 Start에서 확인한다.
        if (CompareTag("HiddenEnemy"))
        {
            renderers = GetComponentsInChildren<Renderer>();
            SetConcealed(true);
        }
    }

    private void SetConcealed(bool value)
    {
        concealed = value;
        foreach (Renderer r in renderers) r.enabled = !value;
    }

    private void Update()
    {
        if (isDead || player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (concealed)
        {
            agent.isStopped = true;
            if (dist <= revealRange && HasLineOfSight()) SetConcealed(false);
            return;
        }

        if (Time.time < staggerUntil)
        {
            agent.isStopped = true;
            UpdateAnimator();
            return;
        }

        // 시야(LOS) 판정: 벽 등에 가려지면 인지하지 않는다. obstacleMask로 차단 레이어를 지정한다.
        if (dist > detectRange || !HasLineOfSight())
        {
            agent.isStopped = true;
            isCrouching = false;
            isRunning = false;
        }
        else if (attackStyle == AttackStyle.Ranged)
        {
            TickRanged(dist);
        }
        else
        {
            TickMelee(dist);
        }

        if (isDead) return; // 자폭한 경우
        UpdateAnimator();
    }

    private void TickMelee(float dist)
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);

        isCrouching = dist <= 4f;
        isRunning = dist > 12f;
        agent.speed = isCrouching ? moveSpeed * 0.5f : (isRunning ? moveSpeed * 2f : moveSpeed);

        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            if (attackStyle == AttackStyle.Explode) { Explode(); return; }
            nextAttackTime = Time.time + attackCooldown;
            if (anim != null) anim.SetTrigger("attack");
            if (playerCtrl != null) playerCtrl.TakeDamage(attackDamage);
        }
    }

    private void Explode()
    {
        if (playerCtrl != null && Vector3.Distance(transform.position, player.position) <= explodeRadius)
            playerCtrl.TakeDamage(attackDamage);
        EnemyTarget t = GetComponent<EnemyTarget>();
        if (t != null) t.SelfDestruct();
        else Destroy(gameObject);
    }

    private void TickRanged(float dist)
    {
        float near = preferredRange * 0.6f;
        float far = preferredRange * 1.2f;

        if (dist < near)
        {
            Vector3 away = (transform.position - player.position).normalized;
            agent.isStopped = false;
            agent.speed = moveSpeed * 1.5f;
            agent.SetDestination(transform.position + away * 4f);
            isRunning = true;
            isCrouching = false;
        }
        else if (dist > far)
        {
            agent.isStopped = false;
            agent.speed = dist > preferredRange * 2f ? moveSpeed * 2f : moveSpeed;
            agent.SetDestination(player.position);
            isRunning = agent.speed > moveSpeed;
            isCrouching = false;
        }
        else
        {
            agent.isStopped = true;
            isRunning = false;
            isCrouching = false;
            FaceTarget();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                if (anim != null) anim.SetTrigger("attack");
                RangedAttack();
            }
        }
    }

    private void RangedAttack()
    {
        if (projectilePrefab == null) return;

        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.5f;
        Vector3 dir = ((player.position + Vector3.up) - origin).normalized;

        EnemyProjectile.Fire(projectilePrefab, origin, dir, projectileSpeed, projectileDamage);
    }

    private void FaceTarget()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    /// <summary>EnemyTarget이 피격 시 호출. 잠깐 경직 + 플레이어 반대 방향으로 밀림. 숨은 적은 정체가 드러난다.</summary>
    public void OnHit()
    {
        if (isDead) return;
        if (concealed) SetConcealed(false);
        staggerUntil = Time.time + staggerTime;
        if (player != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            agent.Move(away.normalized * knockback);
        }
    }

    /// <summary>EnemyTarget이 사망 시 호출. 이동/공격 정지.</summary>
    public void OnDeath()
    {
        isDead = true;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        agent.enabled = false;
        enabled = false;
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetBool("isCrouching", isCrouching);
        anim.SetBool("isRunning", isRunning);
        anim.SetBool("isMoving", agent.velocity.sqrMagnitude > 0.01f);
        anim.SetFloat("moveSpeed", agent.velocity.magnitude);
    }

    private static readonly RaycastHit[] s_losHits = new RaycastHit[8];

    /// <summary>플레이어까지 시야가 트여 있는지(벽·장애물 차단) 판정한다.</summary>
    private bool HasLineOfSight()
    {
        Vector3 origin = eye != null ? eye.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;

        int n = Physics.RaycastNonAlloc(origin, dir / dist, s_losHits, dist, obstacleMask, QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        Transform nearestT = null;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = s_losHits[i];
            if (h.collider.transform.root == transform.root) continue; // 자기 몸은 제외
            if (h.distance < nearest) { nearest = h.distance; nearestT = h.collider.transform; }
        }

        // 아무것도 안 맞았거나 가장 가까운 게 플레이어면 시야 확보. 벽이 먼저 맞으면 차단.
        return nearestT == null || nearestT.root == player.root;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackStyle == AttackStyle.Ranged ? preferredRange : attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
    }
}
