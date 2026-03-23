using UnityEngine;

public class PlayerProximity : MonoBehaviour
{
    [Header("Ustawienia")]
    public string playerTag = "Player";
    public float activationDistance = 2f;
    public bool requireProximity = true;

    private GolfBallController ballController;
    private Transform player; // teraz będzie ustawiany z zewnątrz

    void Start()
    {
        ballController = GetComponent<GolfBallController>();
        if (ballController == null)
        {
            Debug.LogError("PlayerProximity wymaga GolfBallController na tym samym obiekcie!");
            enabled = false;
            return;
        }

        // Jeśli player nie został ustawiony przez SetPlayerTransform, spróbuj znaleźć po tagu
        if (player == null)
            FindPlayer();
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        player = playerTransform;
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("Nie znaleziono gracza z tagiem: " + playerTag);
    }

    void Update()
    {
        if (!requireProximity)
        {
            ballController.SetPlayerNearby(true);
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                ballController.SetPlayerNearby(false);
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool isNear = distance <= activationDistance;
        ballController.SetPlayerNearby(isNear);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}