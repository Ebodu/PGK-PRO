using UnityEngine;

public class GolfBallController : MonoBehaviour
{
    [Header("Ustawienia uderzenia")]
    public float maxPower = 20f;
    public float rotationSpeed = 100f;
    public float arrowDistance = 1.5f;

    [Header("Obrót piłki (opcjonalnie)")]
    public bool canRotateBall = true;
    public float ballRotationSpeed = 50f;

    [Header("Referencje")]
    public Transform arrow;  // STRZAŁKA NIE MOŻE BYĆ DZIECKIEM PIŁKI!

    [Header("Tłumienie")]
    [Tooltip("Opór powietrza/tarcie - im wyższe, tym szybciej piłka zwalnia")]
    public float linearDrag = 0.5f;  // Możesz to ustawić też w Rigidbody, ale tu dla pewności

    private Rigidbody rb;
    private float currentPower = 0f;
    private bool isAiming = true;
    private float aimAngle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("Brak Rigidbody na piłce!");

        // Ustaw drag w rigidbody (jeśli nie chcesz polegać na ustawieniach inspektora)
        rb.linearDamping = linearDrag;  // linearDamping to to samo co Drag w inspektorze
        rb.angularDamping = 0.5f;       // tłumienie obrotowe

        if (arrow != null)
            arrow.gameObject.SetActive(true);
        else
            Debug.LogWarning("Strzałka nie jest przypisana!");
    }

    void Update()
    {
        if (isAiming)
        {
            // Obrót kierunku celowania
            float rotateInput = Input.GetAxis("Horizontal");

            aimAngle += rotateInput * rotationSpeed * Time.deltaTime;

            Vector3 direction = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

            // Ustaw strzałkę (zakładając, że NIE jest dzieckiem piłki)
            if (arrow != null)
            {
                arrow.position = transform.position + direction * arrowDistance;
                arrow.rotation = Quaternion.LookRotation(direction);
                // Opcjonalnie: skaluj strzałkę w zależności od mocy
                float scale = 1f + (currentPower / maxPower) * 0.5f; // 1x do 1.5x
                arrow.localScale = new Vector3(1, 1, scale);
            }

            // Ręczny obrót piłki
            if (canRotateBall)
            {
                float ballRotX = 0f, ballRotZ = 0f;
                if (Input.GetKey(KeyCode.Q)) ballRotZ = -1f;
                if (Input.GetKey(KeyCode.E)) ballRotZ = 1f;
                if (Input.GetKey(KeyCode.R)) ballRotX = -1f;
                if (Input.GetKey(KeyCode.F)) ballRotX = 1f;

                Vector3 ballRotationDelta = new Vector3(ballRotX, 0, ballRotZ) * ballRotationSpeed * Time.deltaTime;
                transform.Rotate(ballRotationDelta, Space.Self);
            }

            // Ładowanie siły
            if (Input.GetKey(KeyCode.Space))
            {
                currentPower += Time.deltaTime * 10f;
                currentPower = Mathf.Clamp(currentPower, 0, maxPower);
            }

            // Uderzenie
            if (Input.GetKeyUp(KeyCode.Space))
            {
                HitBall(direction);
            }
        }
        else
        {
            // Gdy nie celujemy, sprawdzamy czy piłka się zatrzymała (można w Update, ale to oszczędza raycasty)
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

    // Opcjonalnie: jeśli chcesz użyć kolizji do wykrywania zatrzymania, zostaw to, ale Update już to robi
    // void OnCollisionStay(Collision collision)
    // {
    //     if (!isAiming && rb.velocity.magnitude < 0.1f)
    //     {
    //         EnableAiming();
    //     }
    // }
}