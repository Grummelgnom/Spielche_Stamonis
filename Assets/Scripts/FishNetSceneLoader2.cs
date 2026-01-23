using FishNet;
using FishNet.Object;
using UnityEngine;

public class FishNetSceneLoader2 : NetworkBehaviour
{ 
    /// <summary>
    /// Diese Methode wird vom Button aufgerufen.
    /// </summary>
    public void LoadSceneRequest(string sceneName)
    {
        // Client fordert den Server auf
        if (IsClientInitialized)
        {
            RequestSceneChangeServerRpc(sceneName);
        }
    }

    /// <summary>
    /// L‰uft ausschlieﬂlich auf dem Server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestSceneChangeServerRpc(string sceneName)
    {
        // Sicherheitscheck
        if (!IsServerInitialized)
            return;

        // FishNet-konformer Scenewechsel
        InstanceFinder.SceneManager.LoadScene(sceneName);
    }
}

