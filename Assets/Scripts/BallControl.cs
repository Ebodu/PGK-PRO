using UnityEngine;
using PurrNet;
using System.Collections;

public class GolfBallController : NetworkBehaviour
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

    // Stan sieciowy: czy piłka jest w dołku
    private readonly SyncVar<bool> isInHole = new(false);
    
    private Rigidbody rb;
    private float currentPower = 0f;
    private float aimAngle = 0f;
    private bool playerNearby = false;
    private bool isMoving = false;
    private bool isBraking = false;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private bool canMove => !isInHole.value;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("Brak Rigidbody!", this);
        
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // Resetowanie piłki (tylko dla właściciela)
        if (Input.GetKeyDown(KeyCode.R) && isOwner)
        {
            RequestResetBall();
            return;
        }

        if (!canMove)
        {
            if (arrow != null && arrow.gameObject.activeSelf) 
                arrow.gameObject.SetActive(false);
            if (rangeIndicator != null && rangeIndicator.activeSelf) 
                rangeIndicator.SetActive(false);
            return;
        }

        bool wasMoving = isMoving;
        isMoving = (rb.linearVelocity.magnitude > stopThreshold || 
                    rb.angularVelocity.magnitude > stopThreshold);

        // Hamowanie przy niskiej prędkości
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

        if (wasMoving && !isMoving)
        {
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            isBraking = false;
        }

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
            if (canAim && arrow != null && !arrow.gameObject.activeSelf)
            {
                arrow.gameObject.SetActive(true);
            }
            else if (!canAim && arrow != null && arrow.gameObject.activeSelf)
            {
                arrow.gameObject.SetActive(false);
            }
            
            if (canAim) HandleAiming();
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
            RequestHitBall(direction, currentPower);
            currentPower = 0f;
        }
    }

    [ServerRpc]
    private void RequestHitBall(Vector3 direction, float power)
    {
        // Tylko serwer wykonuje fizykę
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * power, ForceMode.Impulse);
        isMoving = true;
        
        // Synchronizacja efektu dźwiękowego
        RpcPlayHitSound();
    }

    [ServerRpc]
    private void RequestResetBall()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        isInHole.value = false;
        isMoving = false;
        isBraking = false;
        
        // Synchronizacja dla wszystkich graczy
        RpcResetBallEffects();
    }

    [ObserversRpc]
    private void RpcPlayHitSound()
    {
        // Tutaj możesz dodać odtwarzanie dźwięku uderzenia
        Debug.Log("Odtwarzam dźwięk uderzenia");
    }

    [ObserversRpc]
    private void RpcResetBallEffects()
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(true);
        if (arrow != null) arrow.gameObject.SetActive(false);
        Debug.Log("Piłka zresetowana");
    }

    [Server]
    public void ServerOnEnterHole()
    {
        isInHole.value = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        // Synchronizacja dla wszystkich graczy
        RpcOnHoleEntered();
    }

    [ObserversRpc]
    private void RpcOnHoleEntered()
    {
        if (arrow != null) arrow.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }

    public void SetPlayerNearby(bool nearby)
    {
        playerNearby = nearby;
    }
}