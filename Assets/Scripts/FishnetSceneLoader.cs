using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FishNetSceneLoader : NetworkBehaviour
{
    [Header("Szene zum Laden")]
    [Scene]
    public string sceneName = "GameScene";

    // Client ruft diese Funktion auf → Server lädt für alle!
    [ServerRpc(RequireOwnership = false)]  // Jeder Client darf aufrufen
    private void RequestLoadSceneRpc()
    {
        SceneLoadData sld = new SceneLoadData(sceneName)
        {
            ReplaceScenes = ReplaceOption.All,
            Options = new LoadSceneOptions()
            {
                Global = true
            }
        };

        base.SceneManager.LoadGlobalScenes(sld);
    }

    // ÖFFENTLICHE Funktion für Button OnClick (Client-seitig)
    public void LoadSceneButton()
    {
        RequestLoadSceneRpc();  // Sendet RPC zum Server
    }

    void Update()
    {
        Debug.Log("Update läuft");
    }
}

internal class LoadSceneOptions : LoadOptions
{
    public bool Global { get; set; }
}