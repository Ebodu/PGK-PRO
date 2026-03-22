using UnityEngine;

public class GolfBallController : MonoBehaviour
{
    [Header("Ustawienia uderzenia")]
    public float maxPower = 20f;
    public float rotationSpeed = 100f;     // szybkość obracania celownika
    public float arrowDistance = 1f;

    [Header("Obrót piłki (opcjonalnie)")]
    public bool canRotateBall = true;
    public float ballRotationSpeed = 50f;

    [Header("Referencje")]
    public Transform arrow;  // STRZAŁKA NIE MOŻE BYĆ DZIECKIEM PIŁKI!

    [Header("Tłumienie")]
    public float linearDrag = 2f;
    public float angularDrag = 2f;

    [Header("Ładowanie")]
    public float chargeSpeed = 20f;          // szybkość ładowania (wartość/s)
    public float powerScaleExponent = 1.5f;  // nieliniowość skali (1 = liniowa, >1 = szybszy wzrost na początku)

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
        rb.angularDamping = angularDrag;

        if (arrow != null)
            arrow.gameObject.SetActive(true);
        else
            Debug.LogWarning("Strzałka nie jest przypisana!");
    }

    void Update()
    {
        if (isAiming == true)
        {
            // Celowanie za pomocą strzałek lewo/prawo
            float rotateInput = 0f;
            if (Input.GetKey(KeyCode.Q))
                rotateInput = -1f;
            if (Input.GetKey(KeyCode.E))
                rotateInput = 1f;

            aimAngle += rotateInput * rotationSpeed * Time.deltaTime;
            Vector3 direction = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

            // Ustawianie strzałki wizualnej
            if (arrow != null)
            {
                arrow.position = transform.position + direction * arrowDistance;
                arrow.rotation = Quaternion.LookRotation(direction);

                float t = currentPower / maxPower;              // 0..1
                float scaledT = Mathf.Pow(t, powerScaleExponent); // nieliniowe
                float scale = 0.2f + scaledT * 0.3f;            // od 0.2 do 0.5
                arrow.localScale = new Vector3(0.2f, 0.2f, scale);
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
                currentPower += Time.deltaTime * chargeSpeed;
                currentPower = Mathf.Clamp(currentPower, 0, maxPower);
            }

            // Uderzenie – puszczenie lewego przycisku myszy
            if (Input.GetMouseButtonUp(0))
            {
                HitBall(direction);
            }
            if (rb.linearVelocity.magnitude > 0.05f && rb.angularVelocity.magnitude > 0.05f)
            {
                isAiming = false;
                if (arrow != null)
                    arrow.gameObject.SetActive(false);
            }
        }
        else
        {
            if (rb.linearVelocity.magnitude < 0.5f)
            {
                rb.linearDamping = 3.5f;   // duży opór, by szybciej zatrzymać
            }
            else
            {
                rb.linearDamping = linearDrag; // normalny opór
            }
            // Czekanie aż piłka się zatrzyma
            if (rb.linearVelocity.magnitude < 0.05f && rb.angularVelocity.magnitude < 0.05f)
            {
                EnableAiming();
            }
        }
    }

    void HitBall(Vector3 direction)
    {
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag; // lub ta sama zmienna, jeśli zrobisz publiczną

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * currentPower, ForceMode.Impulse);

        currentPower = 0f;
    }

    public void EnableAiming()
    {
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        isAiming = true;
        if (arrow != null)
            arrow.gameObject.SetActive(true);
    }
}