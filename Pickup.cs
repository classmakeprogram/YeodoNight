using UnityEngine;

/// <summary>
/// 바닥에 놓는 회복 / 탄약 아이템. Collider(Is Trigger)만 있으면 된다.
/// respawnSeconds > 0 이고 visual이 지정되면 획득 후 재생성한다(무한 웨이브용).
/// </summary>
[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum Kind { Health, Ammo }

    public Kind kind = Kind.Health;
    [Tooltip("Health면 회복량, Ammo면 예비탄 증가량.")]
    public float amount = 25f;
    [Tooltip("0이면 획득 시 제거. >0이면 이 초 뒤 재생성(visual 지정 필요).")]
    public float respawnSeconds = 0f;
    [Tooltip("재생성 시 숨겼다 다시 켤 메시 오브젝트.")]
    public GameObject visual;

    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
    }

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        bool consumed = kind == Kind.Health
            ? player.AddHealth(amount)
            : player.AddAmmo(Mathf.RoundToInt(amount));

        if (!consumed) return; // 이미 가득 → 아이템 유지

        if (respawnSeconds > 0f && visual != null)
        {
            col.enabled = false;
            visual.SetActive(false);
            Invoke(nameof(Respawn), respawnSeconds);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Respawn()
    {
        col.enabled = true;
        visual.SetActive(true);
    }
}
