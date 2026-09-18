using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotEnemyAI : MonoBehaviour
{
    [Header("Enemy AI Settings")]
    public float patrolSpeed = 3.5f;     
    public float detectRange = 25f;      
    public float stopRange = 2f;         
    
    private NavMeshAgent agent;
    private Animator anim; // 이제 씬에 애니메이터가 없어도 터지지 않습니다.
    private Transform playerTransform;

    private bool isCrouching = false;
    private bool isRunning = false;

    private void Awake()
    {
        gameObject.tag = "Enemy";
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>(); // 컴포넌트가 없으면 null 처리됨

        agent.speed = patrolSpeed;
        agent.stoppingDistance = stopRange;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= detectRange)
        {
            // 에이전트가 켜져 있고, 실제 구워진 맵(NavMesh) 위에 정상적으로 서 있을 때만 명령을 내립니다.
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position); 
            }
            else
            {
                return;
            }

            if (distance <= 4f) 
            {
                isCrouching = true;
                isRunning = false;
                agent.speed = patrolSpeed * 0.5f; 
            }
            else if (distance > 12f) 
            {
                isCrouching = false;
                isRunning = true;
                agent.speed = patrolSpeed * 2f;   
            }
            else 
            {
                isCrouching = false;
                isRunning = false;
                agent.speed = patrolSpeed;
            }
        }
        else 
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            isCrouching = false;
            isRunning = false;
        }

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        // [안전 보강] 애니메이터가 아직 없는 임시 깡통 상태라면 아래 대입을 완전히 건너뛰어 에러를 방지합니다.
        if (anim == null) return;

        anim.SetBool("isCrouching", isCrouching);
        anim.SetBool("isRunning", isRunning);
        anim.SetBool("isMoving", agent.velocity.magnitude > 0.1f);
        anim.SetFloat("moveSpeed", agent.velocity.magnitude);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
    }
}