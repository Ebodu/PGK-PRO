using UnityEngine;

public class PlayerProximity : MonoBehaviour
{
    [Header("Ustawienia")]
    public string playerTag = "Player";
    public float activationDistance = 2f;
    public bool requireProximity = true;

    private GolfBallController ballController;
    private Transform player;

    void Start()
    {
        ballController = GetComponent<GolfBallController>();
        if (ballController == null)
        {
            Debug.LogError("PlayerProximity wymaga GolfBallController na tym samym obiekcie!");
            enabled = false; // wy³¹cz skrypt, bo nie ma sensu dzia³aæ
            return;
        }

        FindPlayer();
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
        // Jeœli nie wymagamy bliskoœci, zawsze zezwalaj
        if (!requireProximity)
        {
            ballController.SetPlayerNearby(true);
            return;
        }

        // Jeœli gracza nie ma, próbuj znaleŸæ (mo¿e zosta³ stworzony póŸniej)
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                // Nadal brak gracza – nie blokuj, ale i nie zezwalaj? Mo¿esz ustawiæ false.
                ballController.SetPlayerNearby(false);
                return;
            }
        }

        // SprawdŸ odleg³oœæ
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