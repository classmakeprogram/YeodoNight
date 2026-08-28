using UnityEngine;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Mission Progress")]
    public int killCount = 0;              
    public bool killedHiddenEnemy = false;  
    public int headshotKillCount = 0;      

    [Header("Stage Settings")]
    public int currentStage = 1;           
    public float currentEnemyHpModifier = 0f; 
    public Text missionStatusText;         

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateMissionUI();
    }

    public void OnEnemyKilled(bool isHeadshot, bool isHidden)
    {
        killCount++;
        if (isHidden) killedHiddenEnemy = true;
        if (isHeadshot) headshotKillCount++;

        UpdateMissionUI();     
        CheckMissionComplete(); 
    }

    private void CheckMissionComplete()
    {
        if (killCount >= 5 && killedHiddenEnemy && headshotKillCount >= 3)
        {
            NextStage(); 
        }
    }

    private void NextStage()
    {
        currentStage++;
        ResetMissionProgress();

        if (currentStage >= 3)
        {
            currentEnemyHpModifier = (currentStage - 2) * 20f;
        }

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ResetSpawnerForNextStage();
        }

        Debug.Log($"[STAGE {currentStage} 시작] 적 체력 보너스: +{currentEnemyHpModifier}");
    }

    private void ResetMissionProgress()
    {
        killCount = 0;
        killedHiddenEnemy = false;
        headshotKillCount = 0;
        UpdateMissionUI();
    }

    public void UpdateMissionUI()
    {
        if (missionStatusText != null)
        {
            missionStatusText.text = $"STAGE: {currentStage}\n" +
                                     $"Kills: {killCount} / 5\n" +
                                     $"Headshots: {headshotKillCount} / 3\n" +
                                     $"Hidden Target: {(killedHiddenEnemy ? "COMPLETE" : "SEARCHING")}";
        }
    }
}