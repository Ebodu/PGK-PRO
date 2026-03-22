using UnityEngine;

public class GolfBallController : MonoBehaviour
{
    [Header("Ustawienia uderzenia")]
    public float maxPower = 20f;
    public float rotationSpeed = 100f;     // szybkość obracania celownika
    public float arrowDistance = 1.5f;

    [Header("Obrót piłki (opcjonalnie)")]
    public bool canRotateBall = true;
    public float ballRotationSpeed = 50f;

    [Header("Referencje")]
    public Transform arrow;  // STRZAŁKA NIE MOŻE BYĆ DZIECKIEM PIŁKI!

    [Header("Tłumienie")]
    public float linearDrag = 0.5f;

    private Rigidbody rb;
    private float currentPower = 0f;
    private bool isAiming = true;
    private float aimAngle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("Brak Rigidbody na piłce!");

        rb.linearDamping = linearDrag;
        rb.angularDamping = 0.5f;

        if (arrow != null)
            arrow.gameObject.SetActive(true);
        else
            Debug.LogWarning("Strzałka nie jest przypisana!");
    }

    void Update()
    {
        if (isAiming)
        {
            // Celowanie za pomocą strzałek lewo/prawo
            float rotateInput = 0f;
            if (Input.GetKey(KeyCode.E))
                rotateInput = -1f;
            if (Input.GetKey(KeyCode.Q))
                rotateInput = 1f;

            aimAngle += rotateInput * rotationSpeed * Time.deltaTime;
            Vector3 direction = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

            // Ustawianie strzałki wizualnej
            if (arrow != null)
            {
                arrow.position = transform.position + direction * arrowDistance;
                arrow.rotation = Quaternion.LookRotation(direction);
                // Skala w zależności od mocy
                float scale = 0.3f + (currentPower / maxPower) * 0.5f;
                arrow.localScale = new Vector3(0.3f, 0.3f, scale);
            }

            // Obrót piłki (Q/E/R/F) – niezależnie od sterowania graczem
            if (canRotateBall)
            {
                float ballRotX = 0f, ballRotZ = 0f;
                //if (Input.GetKey(KeyCode.Q)) ballRotZ = -1f;
                //if (Input.GetKey(KeyCode.E)) ballRotZ = 1f;
                //if (Input.GetKey(KeyCode.R)) ballRotX = -1f;
                //if (Input.GetKey(KeyCode.F)) ballRotX = 1f;

                Vector3 ballRotationDelta = new Vector3(ballRotX, 0, ballRotZ) * ballRotationSpeed * Time.deltaTime;
                transform.Rotate(ballRotationDelta, Space.Self);
            }

            // Ładowanie mocy – lewy przycisk myszy
            if (Input.GetMouseButton(0))
            {
                currentPower += Time.deltaTime * 10f;
                currentPower = Mathf.Clamp(currentPower, 0, maxPower);
            }

            // Uderzenie – puszczenie lewego przycisku myszy
            if (Input.GetMouseButtonUp(0))
            {
                HitBall(direction);
            }
        }
        else
        {
            // Czekanie aż piłka się zatrzyma
            if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
            {
                EnableAiming();
            }
        }
    }

    void HitBall(Vector3 direction)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * currentPower, ForceMode.Impulse);

        currentPower = 0f;
        isAiming = false;
        if (arrow != null)
            arrow.gameObject.SetActive(false);
    }

    public void EnableAiming()
    {
        isAiming = true;
        if (arrow != null)
            arrow.gameObject.SetActive(true);
    }
}