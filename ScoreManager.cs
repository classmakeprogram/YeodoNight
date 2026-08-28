using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 점수 / 콤보 / 웨이브 / 생존시간 집계 + 로컬 랭킹(PlayerPrefs 저장).
/// 미션 모드에서도 점수는 계속 쌓인다(랭킹 표시용).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("점수")]
    public int killPoints = 100;
    public int headshotBonus = 150;
    public int hiddenBonus = 300;
    [Tooltip("이 시간(초) 안에 다음 처치를 하면 콤보 유지, 넘기면 콤보 초기화.")]
    public float comboWindow = 3f;
    public int maxComboMultiplier = 8;

    [Header("랭킹")]
    public int leaderboardSize = 5;

    public int Score { get; private set; }
    public int Kills { get; private set; }
    public int Headshots { get; private set; }
    public int Combo { get; private set; }
    public int Wave { get; private set; }
    public float RunTime { get; private set; }

    private float lastKillTime;
    private bool running;

    private const string PrefsKey = "yeodo_leaderboard_v1";

    [Serializable]
    public struct Entry
    {
        public string name;
        public int score;
    }

    [Serializable]
    private class Board
    {
        public List<Entry> entries = new List<Entry>();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Update()
    {
        if (!running) return;
        RunTime += Time.deltaTime;
        if (Combo > 0 && Time.time - lastKillTime > comboWindow) Combo = 0;
    }

    public void ResetRun()
    {
        Score = 0;
        Kills = 0;
        Headshots = 0;
        Combo = 0;
        Wave = 1;
        RunTime = 0f;
        running = true;
    }

    public void FinalizeRun()
    {
        running = false;
    }

    public void SetWave(int wave)
    {
        Wave = wave;
    }

    public void AddKill(bool headshot, bool hidden)
    {
        if (!running) return;

        Combo = Mathf.Min(Combo + 1, maxComboMultiplier);
        lastKillTime = Time.time;

        int mult = Mathf.Max(1, Combo);
        int gained = (killPoints
                      + (headshot ? headshotBonus : 0)
                      + (hidden ? hiddenBonus : 0)) * mult;
        Score += gained;

        Kills++;
        if (headshot) Headshots++;
    }

    public void AddScore(int amount)
    {
        if (running) Score += Mathf.Max(0, amount);
    }

    // ---------- 로컬 랭킹 ----------

    public List<Entry> GetLeaderboard()
    {
        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(json)) return new List<Entry>();

        Board b = JsonUtility.FromJson<Board>(json);
        return (b != null && b.entries != null) ? b.entries : new List<Entry>();
    }

    /// <summary>점수가 랭킹 안에 드는지.</summary>
    public bool Qualifies(int score)
    {
        List<Entry> list = GetLeaderboard();
        return list.Count < leaderboardSize || score > list[list.Count - 1].score;
    }

    /// <summary>이름+점수를 저장하고 정렬/컷. 등재 순위(0-based) 반환, 미등재면 -1.</summary>
    public int SubmitScore(string name, int score)
    {
        string safeName = string.IsNullOrWhiteSpace(name) ? "AAA" : name.Trim().ToUpper();

        List<Entry> list = GetLeaderboard();
        list.Add(new Entry { name = safeName, score = score });
        list.Sort((a, c) => c.score.CompareTo(a.score));
        if (list.Count > leaderboardSize)
            list.RemoveRange(leaderboardSize, list.Count - leaderboardSize);

        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(new Board { entries = list }));
        PlayerPrefs.Save();

        return list.FindIndex(e => e.name == safeName && e.score == score);
    }

    public void ClearLeaderboard()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
    }
}
