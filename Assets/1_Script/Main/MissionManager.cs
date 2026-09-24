using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지별 미션 3종을 순서대로 진행한다.
///   미션 1 : 적 5명 처치
///   미션 2 : 숨겨진 적 처치
///   미션 3 : 헤드샷으로 3명 처치  -> 스테이지 클리어, 다음 스테이지
/// 스테이지 3부터 적 체력에 보너스가 붙는다.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    public enum Mission { KillFive, KillHidden, HeadshotThree }

    [Header("목표 수치")]
    public int killsRequired = 5;
    public int headshotsRequired = 3;

    [Header("스테이지")]
    public int currentStage = 1;
    public int maxStage = 5;
    [Tooltip("스테이지 3부터 (stage-2) * 이 값 만큼 적 체력이 증가한다.")]
    public float hpModifierPerStage = 20f;
    public float currentEnemyHpModifier = 0f;

    [Header("UI")]
    public Text missionStatusText;

    public Mission CurrentMission { get; private set; } = Mission.KillFive;
    public int KillCount { get; private set; }
    public int HeadshotKillCount { get; private set; }
    public bool KilledHiddenEnemy { get; private set; }
    public bool GameCleared { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        UpdateMissionUI();
    }

    public void OnEnemyKilled(bool isHeadshot, bool isHidden)
    {
        if (GameCleared) return;

        // 카운터는 미션 구분 없이 스테이지 전체에서 누적한다.
        // (미션별로 게이팅하면 숨은 적을 미션1에서 잡았을 때 이후 미션을 못 깨는 소프트락이 생긴다)
        KillCount++;
        if (isHeadshot) HeadshotKillCount++;
        if (isHidden) KilledHiddenEnemy = true;

        AdvanceMission();

        // 적이 전멸했는데 미션 미완료면 보충 스폰(진행 정지 방지).
        if (!GameCleared && EnemySpawner.Instance != null)
            EnemySpawner.Instance.EnsureEnemiesAlive();

        UpdateMissionUI();
    }

    /// <summary>현재 미션 조건이 충족됐으면 다음 미션으로. 이미 충족된 미션은 건너뛴다.</summary>
    private void AdvanceMission()
    {
        while (!GameCleared)
        {
            switch (CurrentMission)
            {
                case Mission.KillFive:
                    if (KillCount < killsRequired) return;
                    CurrentMission = Mission.KillHidden;
                    Debug.Log("[미션 완료] 다음 목표: 숨은 적 처치");
                    continue;

                case Mission.KillHidden:
                    if (!KilledHiddenEnemy) return;
                    CurrentMission = Mission.HeadshotThree;
                    Debug.Log("[미션 완료] 다음 목표: 헤드샷 3명");
                    continue;

                case Mission.HeadshotThree:
                    if (HeadshotKillCount < headshotsRequired) return;
                    NextStage();
                    return;
            }
            return;
        }
    }

    private void NextStage()
    {
        if (currentStage >= maxStage)
        {
            GameClear();
            return;
        }

        currentStage++;
        CurrentMission = Mission.KillFive;
        KillCount = 0;
        HeadshotKillCount = 0;
        KilledHiddenEnemy = false;
        currentEnemyHpModifier = Mathf.Max(0, currentStage - 2) * hpModifierPerStage;

        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.SpawnStageEnemies(currentStage);

        Debug.Log($"[STAGE {currentStage}] 적 체력 보너스 +{currentEnemyHpModifier}");
        UpdateMissionUI();
    }

    private void GameClear()
    {
        GameCleared = true;
        Debug.Log("[게임 클리어] 모든 스테이지 완료");
        if (missionStatusText != null) missionStatusText.text = "MISSION COMPLETE";
        if (GameManager.Instance != null) GameManager.Instance.OnGameCleared();
    }

    public void UpdateMissionUI()
    {
        if (missionStatusText == null) return;

        string objective;
        switch (CurrentMission)
        {
            case Mission.KillFive:
                objective = $"적 처치 {KillCount} / {killsRequired}";
                break;
            case Mission.KillHidden:
                objective = KilledHiddenEnemy ? "숨은 적 처치 완료" : "숨은 적을 찾아 처치하라";
                break;
            case Mission.HeadshotThree:
                objective = $"헤드샷 처치 {HeadshotKillCount} / {headshotsRequired}";
                break;
            default:
                objective = "-";
                break;
        }

        missionStatusText.text = $"STAGE {currentStage} / {maxStage}\n{objective}";
    }
}
