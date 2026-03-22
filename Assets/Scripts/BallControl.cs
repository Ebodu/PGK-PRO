using UnityEngine;

public class GolfBallController : MonoBehaviour
{
    [Header("Ustawienia uderzenia")]
    public float maxPower = 20f;               // maksymalna siła uderzenia
    public float rotationSpeed = 100f;         // szybkość obrotu celownika
    public float arrowDistance = 1f;           // odległość strzałki od piłki

    [Header("Referencje")]
    public Transform arrow;                    // obiekt strzałki (nie dziecko piłki)
    public GameObject rangeIndicator;          // obiekt wskaźnika zasięgu (cylinder, sprite itp.)

    [Header("Fizyka")]
    public float linearDrag = 2f;              // normalny opór liniowy
    public float angularDrag = 2f;             // normalny opór obrotowy
    public float brakeDrag = 5f;               // opór przy hamowaniu (gdy prędkość niska)
    public float brakeThreshold = 0.5f;        // prędkość, poniżej której włączamy hamulec
    public float stopThreshold = 0.05f;        // prędkość uznawana za całkowity bezruch

    [Header("Ładowanie")]
    public float chargeSpeed = 20f;            // szybkość narastania mocy
    public float powerScaleExponent = 1.5f;    // nieliniowość skali strzałki

    [Header("Proximity")]
    public bool requirePlayerNearby = true;    // czy wymagać bliskości gracza

    private Rigidbody rb;
    private float currentPower = 0f;
    private float aimAngle = 0f;
    private bool playerNearby = false;          // stan aktualizacji z PlayerProximity
    private bool isMoving = false;              // czy piłka jest w ruchu
    private bool isBraking = false;             // czy aktualnie hamujemy (dla efektu)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("GolfBallController wymaga komponentu Rigidbody!");
            return;
        }

        // ustawienia początkowe
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        // ukryj strzałkę na starcie (pojawi się dopiero gdy będzie można celować)
        if (arrow != null) arrow.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(true); // piłka stoi – wskaźnik widoczny
    }

    void Update()
    {
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
                    rb.linearDamping = brakeDrag;   // włącz hamulec
                    isBraking = true;
                }
            }
            else
            {
                if (isBraking)
                {
                    rb.linearDamping = linearDrag;  // przywróć normalny opór
                    isBraking = false;
                }
            }
        }

        // 3. Gdy piłka się zatrzymała (przesunięcie z ruchu w stan spoczynku)
        if (wasMoving && !isMoving)
        {
            // przywróć normalne tłumienie (na wypadek gdyby hamulec był aktywny)
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            isBraking = false;

            // Wskaźnik zasięgu pojawi się automatycznie w następnej klatce (bo isMoving == false)
            // Strzałka pojawi się, gdy gracz będzie w zasięgu
        }

        // 4. Zachowanie w zależności od stanu
        if (isMoving)
        {
            // Piłka się porusza – ukryj wszystko
            if (rangeIndicator != null && rangeIndicator.activeSelf)
                rangeIndicator.SetActive(false);
            if (arrow != null && arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(false);
        }
        else
        {
            // Piłka stoi – wskaźnik zasięgu zawsze widoczny
            if (rangeIndicator != null && !rangeIndicator.activeSelf)
                rangeIndicator.SetActive(true);

            // Czy można celować? (stoi + (brak wymogu bliskości lub gracz blisko))
            bool canAim = !requirePlayerNearby || playerNearby;

            if (canAim)
            {
                // Pokaż strzałkę
                if (arrow != null && !arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(true);

                // Obsługa celowania i uderzenia
                HandleAiming();
            }
            else
            {
                // Gracz daleko – ukryj strzałkę
                if (arrow != null && arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(false);
            }
        }
    }

    void HandleAiming()
    {
        // Obrót celownika klawiszami Q/E
        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotateInput = 1f;

        aimAngle += rotateInput * rotationSpeed * Time.deltaTime;
        Vector3 direction = Quaternion.Euler(0, aimAngle, 0) * Vector3.forward;

        // Ustawienie strzałki
        if (arrow != null)
        {
            arrow.position = transform.position + direction * arrowDistance;
            arrow.rotation = Quaternion.LookRotation(direction);

            // Skalowanie strzałki w zależności od mocy
            float t = currentPower / maxPower;
            float scaledT = Mathf.Pow(t, powerScaleExponent);
            float scale = 0.2f + scaledT * 0.3f;   // od 0.2 do 0.5
            arrow.localScale = new Vector3(0.2f, 0.2f, scale);
        }

        // Ładowanie mocy (lewy przycisk myszy)
        if (Input.GetMouseButton(0))
        {
            currentPower += Time.deltaTime * chargeSpeed;
            currentPower = Mathf.Clamp(currentPower, 0, maxPower);
        }

        // Uderzenie (puszczenie lewego przycisku)
        if (Input.GetMouseButtonUp(0) && currentPower > 0)
        {
            HitBall(direction);
        }
    }

    void HitBall(Vector3 direction)
    {
        // Przygotuj piłkę do uderzenia
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * currentPower, ForceMode.Impulse);

        currentPower = 0f;

        // Piłka zaczyna się poruszać
        isMoving = true;

        // Ukryj strzałkę i wskaźnik (Update zrobi to w następnej klatce)
        if (arrow != null) arrow.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }

    // Metoda wywoływana z zewnątrz (np. z PlayerProximity) – informuje o bliskości gracza
    public void SetPlayerNearby(bool nearby)
    {
        playerNearby = nearby;
        // Jeśli piłka stoi i gracz wszedł w zasięg, strzałka pojawi się w następnej klatce (bo canAim się zmieni)
        // Nie wymagamy dodatkowych akcji.
    }
}