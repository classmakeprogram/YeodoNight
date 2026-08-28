using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 로봇 적 AI. 감지 범위 안에 플레이어가 들어오면 추격하고, 근접하면 공격한다.
/// 거리별로 앉기 / 걷기 / 뛰기 상태를 애니메이터에 전달한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotEnemyAI : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 3.5f;
    public float detectRange = 25f;
    public float stopRange = 2f;

    [Header("공격")]
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

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
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // ponytail: 시야 판정 없음(벽 너머도 인지). 엄폐물 있는 레벨이면 Physics.Linecast로 obstacleMask 체크 추가.
        if (dist <= detectRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            isCrouching = dist <= 4f;
            isRunning = dist > 12f;
            agent.speed = isCrouching ? moveSpeed * 0.5f : (isRunning ? moveSpeed * 2f : moveSpeed);

            if (dist <= attackRange && Time.time >= nextAttackTime)
                Attack();
        }
        else
        {
            agent.isStopped = true;
            isCrouching = false;
            isRunning = false;
        }

        UpdateAnimator();
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        if (anim != null) anim.SetTrigger("attack");
        if (playerCtrl != null) playerCtrl.TakeDamage(attackDamage);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
    }
}
