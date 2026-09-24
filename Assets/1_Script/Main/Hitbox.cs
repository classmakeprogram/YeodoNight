using UnityEngine;

/// <summary>
/// 적 모델의 부위별 콜라이더(머리, 몸통, 팔다리)에 부착한다.
/// 피격 시 부위 배수를 곱해 루트의 EnemyTarget으로 데미지를 전달한다.
/// 모델러는 이 컴포넌트를 콜라이더에 붙이고 isHead / damageMultiplier 만 설정하면 된다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Tooltip("머리 부위면 체크. 헤드샷 판정 및 미션 3에 사용된다.")]
    public bool isHead = false;

    [Tooltip("기본 데미지에 곱해지는 배수. 권장값 - 머리 3.2, 몸통 1.0, 팔다리 0.7")]
    public float damageMultiplier = 1f;

    private EnemyTarget target;

    private void Awake()
    {
        target = GetComponentInParent<EnemyTarget>();
        if (target == null)
            Debug.LogWarning($"[Hitbox] 부모 계층에 EnemyTarget이 없습니다: {name}", this);
    }

    /// <summary>PlayerController가 레이캐스트 피격 시 호출.</summary>
    public void Receive(float baseDamage)
    {
        if (target == null) return;
        target.TakeDamage(baseDamage * damageMultiplier, isHead);
    }
}
