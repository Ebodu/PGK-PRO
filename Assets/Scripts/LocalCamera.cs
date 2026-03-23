using Cinemachine;
using PurrNet;
using UnityEngine;

public class LocalSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cameraPrefab;
    [SerializeField] private GameObject ballPrefab;      // prefab piłki
    [SerializeField] private GameObject arrowPrefab;     // prefab strzałki
	[SerializeField] private GameObject discPrefab; // the flat disc
		
    private bool _spawned;

    private void Start()
{
    if (TryGetComponent<NetworkIdentity>(out var identity))
    {
        Debug.Log($"[LocalSpawner] Start: identity.isOwner = {identity.isOwner}");
        if (identity.isOwner)
        {
            SpawnCamera();
            SpawnBall();
        }
        else
        {
            Debug.Log("[LocalSpawner] Player is not owner, skipping spawn.");
        }
    }
    else
    {
        Debug.LogError("[LocalSpawner] No NetworkIdentity found on this object!");
    }
}

    private void SpawnCamera()
    {
        if (_spawned) return;
        _spawned = true;

        GameObject camObj = Instantiate(cameraPrefab);
        ThirdPersonCam cam = camObj.GetComponentInChildren<ThirdPersonCam>();
        if (cam == null)
        {
            Debug.LogError("ThirdPersonCam not found in camera prefab!", camObj);
            Destroy(camObj);
            return;
        }

        cam.player = transform;
        cam.orientation = transform.Find("Orientation");
        cam.playerObj = transform.Find("PlayerObj");
        cam.rb = GetComponent<Rigidbody>();
        cam.combatLookAt = transform.Find("CombatLookAt") ?? cam.orientation;

        // Ustawienie Cinemachine
        var vcams = camObj.GetComponentsInChildren<CinemachineVirtualCameraBase>();
        foreach (var vcam in vcams)
        {
            if (vcam is CinemachineFreeLook freeLook)
            {
                freeLook.Follow = transform;
                freeLook.LookAt = cam.combatLookAt;
            }
            else if (vcam is CinemachineVirtualCamera virtCam)
            {
                virtCam.Follow = transform;
                virtCam.LookAt = cam.combatLookAt;
            }
        }

        Debug.Log($"Camera spawned for {gameObject.name} – player = {cam.player?.name}");
    }

    private void SpawnBall()
{
    if (ballPrefab == null)
    {
        Debug.LogWarning("Brak przypisanego ballPrefab w LocalCameraSpawner!");
        return;
    }

    Vector3 spawnPos = transform.position + new Vector3(0, 0.5f, 1f);
    GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

    // Skonfiguruj PlayerProximity
    PlayerProximity proximity = ball.GetComponent<PlayerProximity>();
    if (proximity != null)
        proximity.SetPlayerTransform(transform);

    // Skonfiguruj GolfBallController
    GolfBallController ballController = ball.GetComponent<GolfBallController>();
    if (ballController != null)
    {
        // Stwórz dysk jako osobny obiekt (nie dziecko piłki)
        if (discPrefab != null)
        {
            GameObject disc = Instantiate(discPrefab, spawnPos, Quaternion.identity);
            // Dodaj skrypt FollowBall, aby śledził pozycję piłki
            FollowBall follower = disc.GetComponent<FollowBall>();
            if (follower == null)
                follower = disc.AddComponent<FollowBall>();
            follower.ball = ball.transform;
            follower.yOffset = 0.05f; // dysk lekko nad ziemią, jeśli potrzebujesz

            // Przypisz dysk jako rangeIndicator, aby GolfBallController mógł nim sterować (pokazywać/ukrywać)
            ballController.rangeIndicator = disc;
        }

        // Stwórz strzałkę
        if (arrowPrefab != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            ballController.arrow = arrow.transform;
        }
    }

    Debug.Log($"Ball spawned for {gameObject.name}");
}
}