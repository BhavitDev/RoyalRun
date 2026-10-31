using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ScoreRecord
{
    public string name;
    public int score;
    public long submittedUtcTicks;
}

[Serializable]
internal sealed class ScoreRecordCollection
{
    public List<ScoreRecord> records = new List<ScoreRecord>();
}

public static class LocalLeaderboard
{
    private const string StorageKey = "RoyalRunner.LocalLeaderboard.v1";
    private const int MaxRecords = 1000;

    public static List<ScoreRecord> Load()
    {
        var result = new List<ScoreRecord>();
        if (!PlayerPrefs.HasKey(StorageKey)) return result;
        try
        {
            var data = JsonUtility.FromJson<ScoreRecordCollection>(PlayerPrefs.GetString(StorageKey));
            if (data?.records == null) return result;
            foreach (var row in data.records)
                if (row != null && !string.IsNullOrWhiteSpace(row.name) && row.score >= 0)
                    result.Add(row);
            Sort(result);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Saved leaderboard data was invalid and has been ignored: " + exception.Message);
        }
        return result;
    }

    public static int Submit(string name, int score, out List<ScoreRecord> sorted)
    {
        sorted = Load();
        var record = new ScoreRecord { name = name, score = Mathf.Max(0, score), submittedUtcTicks = DateTime.UtcNow.Ticks };
        sorted.Add(record);
        Sort(sorted);
        int rank = sorted.IndexOf(record) + 1;
        if (sorted.Count > MaxRecords) sorted.RemoveRange(MaxRecords, sorted.Count - MaxRecords);
        PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(new ScoreRecordCollection { records = sorted }));
        PlayerPrefs.Save();
        return rank;
    }

    private static void Sort(List<ScoreRecord> rows)
    {
        rows.Sort((a, b) =>
        {
            int scoreOrder = b.score.CompareTo(a.score);
            return scoreOrder != 0 ? scoreOrder : a.submittedUtcTicks.CompareTo(b.submittedUtcTicks);
        });
    }
}
