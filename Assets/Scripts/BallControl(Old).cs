using UnityEngine;

public class GolfBallController_01 : MonoBehaviour
{
    [Header("Ustawienia uderzenia")]
    public float maxPower = 20f;
    public float rotationSpeed = 100f;
    public float arrowDistance = 1f;

    [Header("Obrót piłki (opcjonalnie)")]
    public bool canRotateBall = true;
    public float ballRotationSpeed = 50f;

    [Header("Referencje")]
    public Transform arrow;
    public GameObject rangeIndicator;   // wskaźnik zasięgu

    [Header("Tłumienie")]
    public float linearDrag = 2f;
    public float angularDrag = 2f;

    [Header("Ładowanie")]
    public float chargeSpeed = 20f;
    public float powerScaleExponent = 1.5f;

    [Header("Proximity")]
    public bool requirePlayerNearby = true;
    private bool playerNearby = false;

    private Rigidbody rb;
    private float currentPower = 0f;
    private bool isAiming = true;
    private float aimAngle = 0f;
    private float stoppedThreshold = 0.01f;   // próg prędkości dla zatrzymania

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("Brak Rigidbody na piłce!");

        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        if (arrow != null)
            arrow.gameObject.SetActive(false);
        else
            Debug.LogWarning("Strzałka nie jest przypisana!");

        if (rangeIndicator != null)
            rangeIndicator.SetActive(true); // startowo widoczny
    }

    void Update()
    {
        if (isAiming) // piłka stoi
        {
            // WSKAŹNIK ZASIĘGU: zawsze widoczny, gdy piłka stoi
            if (rangeIndicator != null && !rangeIndicator.activeSelf)
                rangeIndicator.SetActive(true);

            bool playerIsNear = (!requirePlayerNearby || playerNearby);

            if (playerIsNear)
            {
                // STRZAŁKA: widoczna tylko gdy gracz w zasięgu
                if (arrow != null && !arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(true);

                // Celowanie
                float rotateInput = 0f;
                if (Input.GetKey(KeyCode.Q))
                    rotateInput = -1f;
                if (Input.GetKey(KeyCode.E))
                    rotateInput = 1f;

                aimAngle += rotateInput * rotationSpeed * Time.deltaTime;
                Vector3 direction = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

                // Ustaw strzałkę
                if (arrow != null)
                {
                    arrow.position = transform.position + direction * arrowDistance;
                    arrow.rotation = Quaternion.LookRotation(direction);
                    float t = currentPower / maxPower;
                    float scaledT = Mathf.Pow(t, powerScaleExponent);
                    float scale = 0.2f + scaledT * 0.3f;
                    arrow.localScale = new Vector3(0.2f, 0.2f, scale);
                }

                // Opcjonalny obrót piłki
                if (canRotateBall)
                {
                    float ballRotX = 0f, ballRotZ = 0f;
                    // (zakomentowane klawisze, możesz odkomentować)
                    Vector3 ballRotationDelta = new Vector3(ballRotX, 0, ballRotZ) * ballRotationSpeed * Time.deltaTime;
                    transform.Rotate(ballRotationDelta, Space.Self);
                }

                // Ładowanie mocy
                if (Input.GetMouseButton(0))
                {
                    currentPower += Time.deltaTime * chargeSpeed;
                    currentPower = Mathf.Clamp(currentPower, 0, maxPower);
                }

                // Uderzenie
                if (Input.GetMouseButtonUp(0))
                {
                    HitBall(direction);
                }
            }
            else // gracz poza zasięgiem – strzałka ukryta
            {
                if (arrow != null && arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(false);
            }
        }
        else // piłka w ruchu
        {
            // Ukryj wskaźnik i strzałkę
            if (rangeIndicator != null && rangeIndicator.activeSelf)
                rangeIndicator.SetActive(false);
            if (arrow != null && arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(false);

            // Sprawdź, czy piłka się zatrzymała
            if (rb.linearVelocity.magnitude < stoppedThreshold && rb.angularVelocity.magnitude < stoppedThreshold)
            {
                // Zatrzymana – włącz celowanie
                if (!requirePlayerNearby || playerNearby)
                    EnableAiming();
                else
                    EnableAiming(); // wskaźnik ma być widoczny mimo braku gracza
            }
        }
    }

    void HitBall(Vector3 direction)
    {
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * currentPower, ForceMode.Impulse);

        currentPower = 0f;
        isAiming = false; // piłka w ruchu
    }

    public void EnableAiming()
    {
        // Włącza tryb celowania (piłka stoi)
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        isAiming = true;
        // Wskaźnik zostanie pokazany w Update (bo isAiming == true)
        // Strzałka pokaże się, gdy gracz wejdzie w zasięg
    }

    public void SetPlayerNearby(bool nearby)
    {
        playerNearby = nearby;
        if (requirePlayerNearby && nearby)
        {
            // Jeśli piłka stoi i gracz wszedł w zasięg, włącz celowanie (jeśli nie jest włączone)
            if (rb.linearVelocity.magnitude < stoppedThreshold && rb.angularVelocity.magnitude < stoppedThreshold)
            {
                EnableAiming();
            }
        }
    }
}