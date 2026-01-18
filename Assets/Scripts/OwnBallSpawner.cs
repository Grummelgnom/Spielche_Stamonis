using FishNet.Object;
using System.Collections;
using UnityEngine;

public class OwnBallSpawner : NetworkBehaviour
{
    public static OwnBallSpawner Instance;
    [SerializeField] private GameObject ballPrefab;

    void Start()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Server]
    public IEnumerator SpawnBall(float delay)
    {
        yield return new WaitForSeconds(delay);
        if(OwnNetworkGameManager.Instance.CurrentState == GameState.Playing)
        {
            GameObject ballInstance = Instantiate(ballPrefab);
            Spawn(ballInstance);
        }
    }
}
