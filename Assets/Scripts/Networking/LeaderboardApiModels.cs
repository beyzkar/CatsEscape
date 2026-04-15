using System;
using UnityEngine;

/// <summary>
/// JSON contracts for the leaderboard API. Field names must match server JSON keys
/// (JsonUtility is case-sensitive). Adjust these types if your backend uses different names.
/// </summary>
[Serializable]
public class ScoreSubmitRequest
{
    public string playerName;
    public int score;
}

[Serializable]
public class LeaderboardApiEntry
{
    public string playerName;
    public int score;
}

/// <summary>
/// Wrapper for GET responses. Example JSON:
/// { "entries": [ { "playerName": "Cat", "score": 420 }, ... ] }
/// </summary>
[Serializable]
public class LeaderboardGetResponse
{
    public LeaderboardApiEntry[] entries;
}

[Serializable]
public class SubmitScoreResult
{
    public bool success;
    public string errorMessage;
    public long responseCode;
}
