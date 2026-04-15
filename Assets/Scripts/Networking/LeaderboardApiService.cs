using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// POST score + GET leaderboard using UnityWebRequest. Attach to a GameObject (e.g. same as LeaderboardManager).
/// Configure base URL and paths to match your backend.
/// </summary>
public class LeaderboardApiService : MonoBehaviour
{
    [Header("Endpoints")]
    [Tooltip("e.g. https://your-api.com — no trailing slash")]
    public string baseUrl = "https://example.com";

    [Tooltip("POST path, e.g. /scores or /api/v1/scores")]
    public string submitScorePath = "/scores";

    [Tooltip("GET path returning JSON with an \"entries\" array")]
    public string leaderboardPath = "/leaderboard";

    [Header("Request")]
    [Tooltip("Optional query appended to GET, e.g. ?limit=10&game=cats")]
    public string leaderboardQuery = "?limit=10";

    [Tooltip("Seconds before request is aborted (0 = Unity default)")]
    public int requestTimeoutSeconds = 15;

    [Header("Debug")]
    public bool logRequests;

    private static string CombineUrl(string root, string path, string query)
    {
        if (string.IsNullOrEmpty(root))
            return path + (query ?? "");

        root = root.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            path = "";
        else if (!path.StartsWith("/"))
            path = "/" + path;

        string q = query ?? "";
        if (!string.IsNullOrEmpty(q) && !q.StartsWith("?") && !q.StartsWith("&"))
            q = "?" + q.TrimStart('?');

        return root + path + q;
    }

    /// <summary>
    /// POST JSON body { playerName, score }. Invokes onComplete on the main thread when finished.
    /// </summary>
    public IEnumerator SubmitScore(string playerName, int score, Action<SubmitScoreResult> onComplete)
    {
        var result = new SubmitScoreResult();
        if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("example.com"))
        {
            result.success = false;
            result.errorMessage = "LeaderboardApiService: set a valid baseUrl in the Inspector.";
            onComplete?.Invoke(result);
            yield break;
        }

        string url = CombineUrl(baseUrl, submitScorePath, null);
        var payload = new ScoreSubmitRequest
        {
            playerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName,
            score = score
        };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (requestTimeoutSeconds > 0)
                req.timeout = requestTimeoutSeconds;

            if (logRequests)
                Debug.Log($"[LeaderboardApi] POST {url}\n{json}");

            yield return req.SendWebRequest();

            result.responseCode = req.responseCode;
            if (req.result != UnityWebRequest.Result.Success)
            {
                result.success = false;
                result.errorMessage = $"POST failed: {req.error} (HTTP {req.responseCode})";
                if (!string.IsNullOrEmpty(req.downloadHandler?.text))
                    result.errorMessage += " Body: " + req.downloadHandler.text;
            }
            else if (req.responseCode >= 400)
            {
                result.success = false;
                result.errorMessage = $"POST HTTP {req.responseCode}: {req.downloadHandler?.text}";
            }
            else
            {
                result.success = true;
            }
        }

        onComplete?.Invoke(result);
    }

    /// <summary>
    /// GET leaderboard JSON. Parses LeaderboardGetResponse with an "entries" array.
    /// </summary>
    public IEnumerator FetchLeaderboard(Action<bool, string, LeaderboardApiEntry[]> onComplete)
    {
        if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("example.com"))
        {
            onComplete?.Invoke(false, "LeaderboardApiService: set a valid baseUrl in the Inspector.", null);
            yield break;
        }

        string url = CombineUrl(baseUrl, leaderboardPath, leaderboardQuery);

        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            if (requestTimeoutSeconds > 0)
                req.timeout = requestTimeoutSeconds;

            if (logRequests)
                Debug.Log($"[LeaderboardApi] GET {url}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, $"GET failed: {req.error} (HTTP {req.responseCode})", null);
                yield break;
            }

            if (req.responseCode >= 400)
            {
                onComplete?.Invoke(false, $"GET HTTP {req.responseCode}: {req.downloadHandler?.text}", null);
                yield break;
            }

            string text = req.downloadHandler?.text;
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke(false, "GET returned empty body.", null);
                yield break;
            }

            try
            {
                var parsed = JsonUtility.FromJson<LeaderboardGetResponse>(text);
                if (parsed == null || parsed.entries == null)
                {
                    onComplete?.Invoke(false, "JSON parse ok but \"entries\" is missing or null. Body: " + text, null);
                    yield break;
                }

                onComplete?.Invoke(true, null, parsed.entries);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(false, "JSON parse error: " + ex.Message + "\nBody: " + text, null);
            }
        }
    }
}
