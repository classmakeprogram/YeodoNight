using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원거리 로봇이 발사하는 투사체. 프리팹 요구사항:
///  - Collider (Is Trigger 체크)
///  - Rigidbody (Is Kinematic 해제; 중력은 코드가 끔)
/// 플레이어에 닿으면 데미지, 그 외 무엇에든 닿으면 소멸(적끼리는 무시).
/// Fire()로 쏘면 프리팹별 풀에서 재사용한다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 8f;
    public float lifetime = 5f;

    private Rigidbody rb;
    private PooledObject pooled;

    // 프리팹(에셋)별 풀. 씬 리로드로 파괴된 오브젝트는 SimplePool.Get이 걸러낸다.
    private static readonly Dictionary<GameObject, SimplePool> s_pools = new Dictionary<GameObject, SimplePool>();

    public static void Fire(GameObject prefab, Vector3 origin, Vector3 dir, float speed, float damage)
    {
        if (!s_pools.TryGetValue(prefab, out SimplePool pool))
            s_pools[prefab] = pool = new SimplePool(prefab);

        GameObject go = pool.Get(origin, Quaternion.LookRotation(dir));
        EnemyProjectile proj = go.GetComponent<EnemyProjectile>();
        if (proj == null) { pool.Release(go); return; }

        if (proj.pooled == null) proj.pooled = go.AddComponent<PooledObject>();
        proj.pooled.Arm(pool, proj.lifetime);
        proj.speed = speed;
        proj.damage = damage;
        proj.Launch(dir);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void Launch(Vector3 direction)
    {
        direction = direction.normalized;
        transform.forward = direction;
        rb.linearVelocity = direction * speed;
        if (pooled == null) Destroy(gameObject, lifetime);
    }

    private void Despawn()
    {
        if (pooled != null) pooled.Return();
        else Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;
        if (root.CompareTag("Enemy") || root.CompareTag("HiddenEnemy")) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null) player.TakeDamage(damage);

        Despawn();
    }
}
