using FishNet.Example;
using FishNet.Object;
using UnityEngine;

public class DisableFishNetHudOnGameStart : NetworkBehaviour
{
    // Wird vom Server aufgerufen, sobald das Spiel startet
    [ObserversRpc]
    public void DisableHudObserversRpc()
    {
        // FindObjectsByType findet ALLE in der Szene (auch wenn inaktiv je nach Unity-Version/Settings)
        var huds = FindObjectsByType<NetworkHudCanvases>(FindObjectsSortMode.None);

        foreach (var hud in huds)
        {
            if (hud == null)
                continue;

            // 1) Script deaktivieren (damit es nicht wieder irgendwas re-enabled)
            hud.enabled = false;

            // 2) GameObject deaktivieren (damit Canvas/Buttons wirklich weg sind)
            hud.gameObject.SetActive(false);
        }
    }
}
