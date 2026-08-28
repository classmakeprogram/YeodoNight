using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD: 히트마커, 데미지 숫자, 점수/콤보/웨이브 텍스트, 저체력 화면 효과.
/// 참조는 인스펙터에서 연결. 비워 두면 해당 기능만 조용히 꺼진다.
/// Canvas는 Screen Space - Overlay 기준(데미지 숫자 좌표 계산).
/// </summary>
public class HUD : MonoBehaviour
{
    public static HUD Instance;

    [Header("참조")]
    public PlayerController player;
    public Camera cam;

    [Header("전투 피드백")]
    public Image hitMarker;                     // 평소 비활성 상태로 둘 것
    public float hitMarkerTime = 0.12f;
    public Text damageNumberPrefab;             // 프리팹(비활성 상태의 Text)
    public RectTransform damageNumberParent;    // Canvas 하위 빈 RectTransform
    public float damageNumberDuration = 0.6f;
    public float damageNumberRise = 40f;

    [Header("텍스트")]
    public Text scoreText;
    public Text comboText;
    public Text waveText;

    [Header("저체력 효과")]
    public Image lowHpImage;                    // 화면 덮는 붉은 이미지(alpha 0에서 시작)
    [Range(0f, 1f)] public float lowHpThreshold = 0.4f;
    [Range(0f, 1f)] public float lowHpMaxAlpha = 0.6f;

    private float hitMarkerHideTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerController>();
        }
        if (cam == null) cam = Camera.main;
        if (hitMarker != null) hitMarker.enabled = false;
    }

    private void Update()
    {
        if (hitMarker != null && hitMarker.enabled && Time.unscaledTime >= hitMarkerHideTime)
            hitMarker.enabled = false;

        ScoreManager sm = ScoreManager.Instance;
        if (sm != null)
        {
            if (scoreText != null) scoreText.text = $"SCORE {sm.Score:N0}";
            if (comboText != null) comboText.text = sm.Combo >= 2 ? $"x{sm.Combo}" : "";
            if (waveText != null) waveText.text = $"WAVE {sm.Wave}";
        }

        if (lowHpImage != null && player != null)
        {
            float ratio = player.maxHp > 0f ? player.Hp / player.maxHp : 1f;
            float t = ratio >= lowHpThreshold ? 0f : 1f - ratio / lowHpThreshold;
            Color c = lowHpImage.color;
            c.a = t * lowHpMaxAlpha;
            lowHpImage.color = c;
        }
    }

    /// <summary>EnemyTarget이 피격/처치 시 호출.</summary>
    public void ReportHit(Vector3 worldPos, float damage, bool headshot, bool killed)
    {
        ShowHitMarker(killed);
        SpawnDamageNumber(worldPos, damage, headshot);
    }

    public void ShowHitMarker(bool killed)
    {
        if (hitMarker == null) return;
        hitMarker.enabled = true;
        hitMarker.color = killed ? Color.red : Color.white;
        hitMarkerHideTime = Time.unscaledTime + hitMarkerTime;
    }

    private void SpawnDamageNumber(Vector3 worldPos, float damage, bool headshot)
    {
        if (damageNumberPrefab == null || damageNumberParent == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) return; // 카메라 뒤

        Text t = Instantiate(damageNumberPrefab, damageNumberParent);
        t.gameObject.SetActive(true);
        t.transform.position = screenPos;
        t.text = Mathf.RoundToInt(damage).ToString();
        t.color = headshot ? new Color(1f, 0.3f, 0.2f) : Color.white;
        t.fontSize = damageNumberPrefab.fontSize + (headshot ? 6 : 0);

        StartCoroutine(FloatAndFade(t, screenPos));
    }

    private IEnumerator FloatAndFade(Text t, Vector3 start)
    {
        float elapsed = 0f;
        Color baseColor = t.color;
        while (elapsed < damageNumberDuration && t != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = elapsed / damageNumberDuration;
            t.transform.position = start + Vector3.up * (damageNumberRise * k);
            Color c = baseColor;
            c.a = 1f - k;
            t.color = c;
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }
}
