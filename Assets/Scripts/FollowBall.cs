using UnityEngine;

public class FollowBall : MonoBehaviour
{
    public Transform ball;  // przeci¹gnij tu obiekt pi³ki
    public float yOffset = 0.05f; // wysokoœæ nad ziemi¹

    void Update()
    {
        if (ball != null)
        {
            transform.position = new Vector3(ball.position.x, ball.position.y + yOffset, ball.position.z);
        }
    }
}