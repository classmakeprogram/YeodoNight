using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 로봇 적 AI. 감지 범위 안에 플레이어가 들어오면 반응한다.
///  - Melee : 붙어서 근접 공격
///  - Ranged: 선호 사거리를 유지하며 투사체 발사(너무 가까우면 후퇴)
/// 거리/상태를 애니메이터에 전달한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotEnemyAI : MonoBehaviour
{
    public enum AttackStyle { Melee, Ranged }

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
    }

    private void Update()
    {
        if (isDead || player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 시야(LOS) 판정: 벽 등에 가려지면 인지하지 않는다. obstacleMask로 차단 레이어를 지정한다.
        if (dist > detectRange || !HasLineOfSight())
        {
            agent.isStopped = true;
            isCrouching = false;
            isRunning = false;
        }
        else if (attackStyle == AttackStyle.Melee)
        {
            TickMelee(dist);
        }
        else
        {
            TickRanged(dist);
        }

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
            nextAttackTime = Time.time + attackCooldown;
            if (anim != null) anim.SetTrigger("attack");
            if (playerCtrl != null) playerCtrl.TakeDamage(attackDamage);
        }
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

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        EnemyProjectile proj = go.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.speed = projectileSpeed;
            proj.damage = projectileDamage;
            proj.Launch(dir);
        }
    }

    private void FaceTarget()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
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
