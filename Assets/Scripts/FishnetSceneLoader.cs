using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FishNetSceneLoader : NetworkBehaviour
{
    [Header("Szene zum Laden")]
    [Scene]  // Drag & Drop aus Hierarchy oder Name eingeben
    public string sceneName = "GameScene";

    // Button ruft diese Client-zu-Server Funktion auf
    [ClientRpc]  // Oder ServerRpc(RequireOwnership = false) falls nur Host
    public void RequestLoadScene(NetworkConnection sender = null)
    {
        if (!IsServer)
            return;

        SceneLoadData sld = new SceneLoadData(sceneName)
        {
            ReplaceScenes = ReplaceOption.All,  // Alle Szenen ersetzen
            Options = new LoadSceneOptions()
            {
                Global = true  // Für alle Clients laden
            }
        };

        SceneManager.LoadGlobalScenes(sld);
    }

    // Alternative: Nur für bestimmte Clients
    public void LoadSceneFor(NetworkConnection conn)
    {
        if (!IsServer)
            return;

        SceneLoadData sld = new SceneLoadData(sceneName);
        SceneManager.LoadConnectionScenes(conn, sld);
    }
}
