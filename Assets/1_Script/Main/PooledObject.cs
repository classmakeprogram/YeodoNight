using UnityEngine;

/// <summary>
/// 풀에서 꺼낸 오브젝트를 수명이 끝나면 자동으로 풀에 반환한다. SimplePool과 짝으로 사용한다.
/// </summary>
public class PooledObject : MonoBehaviour
{
    private SimplePool pool;
    private float releaseTime = float.PositiveInfinity;

    /// <summary>반환 예정 시각을 예약한다. lifetime &lt;= 0이면 자동 반환하지 않는다.</summary>
    public void Arm(SimplePool owner, float lifetime)
    {
        pool = owner;
        releaseTime = lifetime > 0f ? Time.time + lifetime : float.PositiveInfinity;
    }

    private void Update()
    {
        if (pool != null && Time.time >= releaseTime)
            Return();
    }

    /// <summary>즉시 풀에 반환한다.</summary>
    public void Return()
    {
        SimplePool p = pool;
        pool = null;
        releaseTime = float.PositiveInfinity;
        if (p != null) p.Release(gameObject);
    }
}
