using FishNet.Object;
using UnityEngine;

public class OwnBallController : NetworkBehaviour
{
    private Rigidbody rb;
    private float roundTime;

    private Vector3 lastVelocity;
    private Vector3 goalVelocity;

    private void Start()
    {
        if (!IsServerInitialized) Destroy(this);

        rb = GetComponentInChildren<Rigidbody>();
        goalVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f), 0);
    }

    private void FixedUpdate()
    {
        roundTime += Time.fixedDeltaTime;
        lastVelocity = rb.linearVelocity;
        rb.linearVelocity = goalVelocity.normalized * Mathf.Max(roundTime / 10f, 4f);
    }

    private void OnCollisionEnter(Collision col)
    {
        ContactPoint cp = col.contacts[0];
        goalVelocity = Vector3.Reflect(lastVelocity, cp.normal);
        Debug.Log("LV" + lastVelocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "LeftGoal":
                OwnNetworkGameManager.Instance.ScorePoint(1);
                break;
            case "RightGoal":
                OwnNetworkGameManager.Instance.ScorePoint(0);
                break;
            default: return;
        }
        Despawn(DespawnType.Destroy);
        Destroy(gameObject);
    }
}
