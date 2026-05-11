using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("References")]
    private PlayerMovementAdvancedV2 pm;   // POPRAWKA: prawidłowa nazwa klasy
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;

    [Header("Grappling")]
    public float maxGrappleDistance = 25f;
    public float grappleDelayTime = 0.1f;
    public float overshootYAxis = 2f;

    private Vector3 grapplePoint;

    [Header("Cooldown")]
    public float grapplingCd = 1f;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    private bool grappling;

    private void Start()
    {
        pm = GetComponent<PlayerMovementAdvancedV2>();   // POPRAWKA: prawidłowa nazwa klasy

        if (cam == null)
            TryFindCamera();
    }

    private void Update()
    {
        if (cam == null)
            TryFindCamera();

        if (Input.GetKeyDown(grappleKey))
            StartGrapple();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    // POPRAWKA: metoda wyszukuje kamery ThirdPersonCam dla trybu trzecioosobowego
    private void TryFindCamera()
    {
        // Najpierw próbuje znaleźć ThirdPersonCam przypisaną do tego gracza
        ThirdPersonCam[] allCams = FindObjectsOfType<ThirdPersonCam>();
        foreach (ThirdPersonCam tpc in allCams)
        {
            if (tpc.player == transform)
            {
                Camera camComponent = tpc.GetComponentInChildren<Camera>();
                if (camComponent != null)
                {
                    cam = camComponent.transform;
                    return;
                }
                cam = tpc.transform;
                return;
            }
        }

        // Fallback: użyj głównej kamery w scenie
        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (grappling && cam != null && gunTip != null && lr != null)
            lr.SetPosition(0, gunTip.position);
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;
        if (cam == null) return;

        grappling = true;

        if (pm != null)
            pm.freeze = true;

        RaycastHit hit;
        // POPRAWKA: raycast wychodzi z kamery (tryb trzecioosobowy)
        // Środek ekranu → przesunięcie na świat 3D przez kamerę
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        if (lr != null)
        {
            lr.enabled = true;
            lr.SetPosition(1, grapplePoint);
        }
    }

    private void ExecuteGrapple()
    {
        if (pm == null) return;

        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {
        if (pm != null)
            pm.freeze = false;

        grappling = false;
        grapplingCdTimer = grapplingCd;

        if (lr != null)
            lr.enabled = false;
    }

    public bool IsGrappling() => grappling;

    public Vector3 GetGrapplePoint() => grapplePoint;
}