using UnityEngine;

/// <summary>
/// 원거리 로봇이 발사하는 투사체. 프리팹 요구사항:
///  - Collider (Is Trigger 체크)
///  - Rigidbody (Is Kinematic 해제; 중력은 코드가 끔)
/// 플레이어에 닿으면 데미지, 그 외 무엇에든 닿으면 소멸(적끼리는 무시).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 8f;
    public float lifetime = 5f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void Launch(Vector3 direction)
    {
        direction = direction.normalized;
        transform.forward = direction;
        rb.velocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;
        if (root.CompareTag("Enemy") || root.CompareTag("HiddenEnemy")) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null) player.TakeDamage(damage);

        Destroy(gameObject);
    }
}
