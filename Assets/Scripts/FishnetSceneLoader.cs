using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FishNetSceneLoader : NetworkBehaviour
{
    public static FishNetSceneLoader Instance { get; private set; }

    [Header("Szene zum Laden")]
    [Scene]
    public string sceneName = "GameScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);   ← auskommentiert lassen, 
            // außer du willst den Loader wirklich über Szenenwechsel behalten
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Client ruft diese Funktion auf → Server lädt für alle!
    [ServerRpc(RequireOwnership = false)]
    private void RequestLoadSceneRpc()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("sceneName ist leer – kann keine Szene laden!");
            return;
        }

        SceneLoadData sld = new SceneLoadData(sceneName)
        {
            ReplaceScenes = ReplaceOption.All,
            Options = new LoadSceneOptions()
            {
                Global = true
            }
        };

        base.SceneManager.LoadGlobalScenes(sld);
        Debug.Log($"Lade Szene global: {sceneName}");
    }

    // Öffentliche Funktion für Button OnClick (Client-seitig)
    public void LoadSceneButton()
    {
        if (!IsClient)
        {
            Debug.LogWarning("LoadSceneButton wurde nicht von einem Client aufgerufen");
            return;
        }

        RequestLoadSceneRpc();
    }

    // Optional: Hilfs-Debug im Update entfernen (verursacht Spam)
    // void Update()
    // {
    //     Debug.Log("Update läuft");
    // }
}

internal class LoadSceneOptions : LoadOptions
{
    public bool Global { get; set; }
}