using System;
using UnityEngine;

/// <summary>
/// JSON contracts for the leaderboard API. Field names must match server JSON keys.
/// Updated to match MongoDB backend schema.
/// </summary>
[Serializable]
public class ScoreSubmitRequest
{
    public string uid;
    public string displayName;
    public string authType;
    public int levelNumber;
    public int score;
    public int xpEarned;
    public float timeSeconds;
}

[Serializable]
public class LeaderboardApiEntry
{
    public string uid;
    public string displayName;
    public int levelNumber;
    public int score;
    public int xpEarned;
    public float timeSeconds;
}

/// <summary>
/// Wrapper for GET responses.
/// </summary>
[Serializable]
public class LeaderboardGetResponse
{
    public bool success;
    public LeaderboardApiEntry[] entries;
}

[Serializable]
public class SubmitScoreResult
{
    public bool success;
    public string errorMessage;
    public long responseCode;
}
