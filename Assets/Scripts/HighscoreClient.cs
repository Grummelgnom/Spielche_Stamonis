using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HighscoreClient : MonoBehaviour
{
    // Singleton für globalen Zugriff
    public static HighscoreClient Instance { get; private set; }

    [Header("API Base URL")]
    [SerializeField] private string baseUrl = "http://localhost/bullethell_api";  // Backend-API Basis-URL

    private void Awake()
    {
        // Singleton-Setup
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // JSON-Daten für Score-Submit
    [Serializable]
    private class SubmitRequest
    {
        public string player_name;
        public int score;
    }

    // Response bei Score-Submit
    [Serializable]
    private class SubmitResponse
    {
        public bool ok;
        public int id;
        public string error;
    }

    // Einzelner Highscore-Eintrag
    [Serializable]
    public class HighscoreEntry
    {
        public int id;
        public string player_name;
        public int score;
        public string created_at;
    }

    // Response bei Highscore-Request
    [Serializable]
    private class GetResponse
    {
        public bool ok;
        public HighscoreEntry[] highscores;
        public string error;
    }

    // Score an Server senden
    public void SubmitScore(string playerName, int score)
    {
        StartCoroutine(SubmitScoreCoroutine(playerName, score));
    }

    // Highscores vom Server holen
    public void FetchHighscores(Action<HighscoreEntry[]> onResult)
    {
        StartCoroutine(GetHighscoresCoroutine(onResult));
    }

    // Coroutine: Score-Submit (POST /submit_score.php)
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
            yield break;

        var res = JsonUtility.FromJson<SubmitResponse>(req.downloadHandler.text);
        if (res != null && res.ok)
        {
            // Score erfolgreich gespeichert
        }
    }

    // Coroutine: Highscores abrufen (GET /get_highscores.php)
    private IEnumerator GetHighscoresCoroutine(Action<HighscoreEntry[]> onResult)
    {
        var url = $"{baseUrl}/get_highscores.php";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onResult?.Invoke(Array.Empty<HighscoreEntry>());
            yield break;
        }

        var res = JsonUtility.FromJson<GetResponse>(req.downloadHandler.text);
        if (res != null && res.ok)
        {
            onResult?.Invoke(res.highscores);
        }
        else
        {
            onResult?.Invoke(Array.Empty<HighscoreEntry>());
        }
    }
}
