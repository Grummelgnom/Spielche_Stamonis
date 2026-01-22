using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HighscoreClient : MonoBehaviour
{
    public static HighscoreClient Instance { get; private set; }

    [Header("API Base URL")]
    [SerializeField] private string baseUrl = "http://localhost/bullethell_api";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Serializable]
    private class SubmitRequest { public string player_name; public int score; }

    [Serializable]
    private class SubmitResponse { public bool ok; public int id; public string error; }

    [Serializable]
    public class HighscoreEntry { public int id; public string player_name; public int score; public string created_at; }

    [Serializable]
    private class GetResponse { public bool ok; public HighscoreEntry[] highscores; public string error; }

    public void SubmitScore(string playerName, int score)
    {
        StartCoroutine(SubmitScoreCoroutine(playerName, score));
    }

    public void FetchHighscores(Action<HighscoreEntry[]> onResult)
    {
        StartCoroutine(GetHighscoresCoroutine(onResult));
    }

    private IEnumerator SubmitScoreCoroutine(string playerName, int score)
    {
        var url = $"{baseUrl}/submit_score.php";

        var reqObj = new SubmitRequest { player_name = playerName, score = score };
        string json = JsonUtility.ToJson(reqObj);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Submit score failed: {req.error}");
            yield break;
        }

        var res = JsonUtility.FromJson<SubmitResponse>(req.downloadHandler.text);
        if (res != null && res.ok)
        {
            Debug.Log($"Score submitted! Name: {playerName}, Score: {score}, ID: {res.id}");
        }
        else
        {
            Debug.LogError($"Submit score error: {(res != null ? res.error : "Invalid response")}");
        }
    }

    private IEnumerator GetHighscoresCoroutine(Action<HighscoreEntry[]> onResult)
    {
        var url = $"{baseUrl}/get_highscores.php";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Get highscores failed: {req.error}");
            onResult?.Invoke(Array.Empty<HighscoreEntry>());
            yield break;
        }

        var res = JsonUtility.FromJson<GetResponse>(req.downloadHandler.text);
        if (res != null && res.ok)
        {
            Debug.Log($"Fetched {res.highscores.Length} highscores");
            onResult?.Invoke(res.highscores);
        }
        else
        {
            Debug.LogError($"Get highscores error: {(res != null ? res.error : "Invalid response")}");
            onResult?.Invoke(Array.Empty<HighscoreEntry>());
        }
    }
}
