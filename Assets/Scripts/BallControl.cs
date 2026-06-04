using UnityEngine;

public class GolfBallController : MonoBehaviour
{
    [Header("Ustawienia uderzenia")]
    public float maxPower = 20f;
    public float rotationSpeed = 100f;
    public float arrowDistance = 1f;

    [Header("Referencje")]
    public Transform arrow;
    public GameObject rangeIndicator;

    [Header("Fizyka")]
    public float linearDrag = 2f;
    public float angularDrag = 2f;
    public float brakeDrag = 5f;
    public float brakeThreshold = 0.5f;
    public float stopThreshold = 0.05f;

    [Header("Ładowanie")]
    public float chargeSpeed = 20f;
    public float powerScaleExponent = 1.5f;

    [Header("Proximity")]
    public bool requirePlayerNearby = true;

    [HideInInspector] public ulong ownerId;

    private Rigidbody rb;
    private float currentPower = 0f;
    private float aimAngle = 0f;
    private bool playerNearby = false;
    private bool isMoving = false;
    private bool isBraking = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    // NOWE: czy piłka może być uderzana (ale Update działa zawsze)
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("GolfBallController wymaga komponentu Rigidbody!");
            return;
        }

        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        if (arrow != null) arrow.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(true);
        
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // 0. ZAWSZE sprawdzaj klawisz R (nawet gdy canMove == false)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Klawisz R wciśnięty – resetuję piłkę");
            ResetBall();
            return;
        }

        // Jeśli piłka nie może się poruszać (w dołku) – nie obsługujemy dalej
        if (!canMove) return;

        // 1. Określenie stanu ruchu
        bool wasMoving = isMoving;
        isMoving = (rb.linearVelocity.magnitude > stopThreshold || rb.angularVelocity.magnitude > stopThreshold);

        // 2. Hamowanie przy niskiej prędkości
        if (isMoving)
        {
            float speed = rb.linearVelocity.magnitude;
            if (speed < brakeThreshold && speed > 0.01f)
            {
                if (!isBraking)
                {
                    rb.linearDamping = brakeDrag;
                    isBraking = true;
                }
            }
            else
            {
                if (isBraking)
                {
                    rb.linearDamping = linearDrag;
                    isBraking = false;
                }
            }
        }

        // 3. Gdy piłka się zatrzymała
        if (wasMoving && !isMoving)
        {
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            isBraking = false;
        }

        // 4. Zachowanie w zależności od stanu
        if (isMoving)
        {
            if (rangeIndicator != null && rangeIndicator.activeSelf)
                rangeIndicator.SetActive(false);
            if (arrow != null && arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(false);
        }
        else
        {
            if (rangeIndicator != null && !rangeIndicator.activeSelf)
                rangeIndicator.SetActive(true);

            bool canAim = !requirePlayerNearby || playerNearby;

            if (canAim)
            {
                if (arrow != null && !arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(true);
                HandleAiming();
            }
            else
            {
                if (arrow != null && arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(false);
            }
        }
    }

    void HandleAiming()
    {
        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotateInput = 1f;

        aimAngle += rotateInput * rotationSpeed * Time.deltaTime;
        Vector3 direction = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

        if (arrow != null)
        {
            arrow.position = transform.position + direction * arrowDistance;
            arrow.rotation = Quaternion.LookRotation(direction);

            float t = currentPower / maxPower;
            float scaledT = Mathf.Pow(t, powerScaleExponent);
            float scale = 0.2f + scaledT * 0.3f;
            arrow.localScale = new Vector3(0.2f, 0.2f, scale);
        }

        if (Input.GetMouseButton(0))
        {
            currentPower += Time.deltaTime * chargeSpeed;
            currentPower = Mathf.Clamp(currentPower, 0, maxPower);
        }

        if (Input.GetMouseButtonUp(0) && currentPower > 0)
        {
            HitBall(direction);
        }
    }

    void HitBall(Vector3 direction)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * currentPower, ForceMode.Impulse);

        currentPower = 0f;
        isMoving = true;

        if (arrow != null) arrow.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }

    public void SetPlayerNearby(bool nearby)
    {
        playerNearby = nearby;
    }

    // NOWE metody do blokowania/odblokowywania ruchu (używane przez HoleTrigger)
    public void DisableMovement()
    {
        if (rb.isKinematic) return; // bezpiecznik – jeśli już kinematic, nie rób nic

        canMove = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (arrow != null) arrow.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }

    public void EnableMovement()
    {
        canMove = true;
        rb.isKinematic = false;
        // UI pojawi się automatycznie w Update, gdy piłka stanie
    }

    public void ResetBall()
    {
        canMove = true;
        rb.isKinematic = false;   // ważne: odblokuj kinematic
    
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    
        transform.position = startPosition;
        transform.rotation = startRotation;
    
        // reszta stanów...
        isMoving = false;
        isBraking = false;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        currentPower = 0f;
        aimAngle = 0f;
        playerNearby = false;
    
        if (rangeIndicator != null) rangeIndicator.SetActive(true);
        if (arrow != null) arrow.gameObject.SetActive(false);
    
        Debug.Log("Piłka zresetowana");
    }
}