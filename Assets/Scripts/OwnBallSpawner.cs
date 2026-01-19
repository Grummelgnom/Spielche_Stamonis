using FishNet.Object;
using System.Collections;
using UnityEngine;

public class OwnBallSpawner : NetworkBehaviour
{
    public static OwnBallSpawner Instance;

    [SerializeField] private GameObject ballPrefab;

    [Header("Rain Settings")]
    [SerializeField] private float spawnInterval = 0.1f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField] private float spawnY = 6f;

    void Start()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (IsServer)
        {
            StartCoroutine(SpawnBallRain());
        }
    }

    [Server]
    private IEnumerator SpawnBallRain()
    {
        while (true)
        {
            if (OwnNetworkGameManager.Instance.CurrentState == GameState.Playing)
            {
                SpawnBall();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    [Server]
    public void SpawnBall()
    {
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0f);

        GameObject ballInstance = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
        Spawn(ballInstance);
    }
}
