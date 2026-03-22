using Cinemachine;
using PurrNet;
using UnityEngine;

public class LocalCameraSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cameraPrefab;
    private bool _spawned;

    private void Start()
    {
        if (TryGetComponent<NetworkIdentity>(out var identity) && identity.isOwner)
        {
            SpawnCamera();
        }
    }

    private void SpawnCamera()
    {
        if (_spawned) return;
        _spawned = true;

        GameObject camObj = Instantiate(cameraPrefab);
    
        // Find the ThirdPersonCam script (on Main Camera)
        ThirdPersonCam cam = camObj.GetComponentInChildren<ThirdPersonCam>();
        if (cam == null)
        {
            Debug.LogError("ThirdPersonCam not found in camera prefab!", camObj);
            Destroy(camObj);
            return;
        }

        // Assign references for the custom script
        cam.player = transform;
        cam.orientation = transform.Find("Orientation");
        cam.playerObj = transform.Find("PlayerObj");
        cam.rb = GetComponent<Rigidbody>();
        cam.combatLookAt = transform.Find("CombatLookAt") ?? cam.orientation;

        // --- NEW: Set up Cinemachine cameras ---
        // Find all CinemachineVirtualCamera and CinemachineFreeLook components in the prefab
        var vcams = camObj.GetComponentsInChildren<CinemachineVirtualCameraBase>();
        foreach (var vcam in vcams)
        {
            // For FreeLook, assign both Follow and LookAt
            if (vcam is CinemachineFreeLook freeLook)
            {
                freeLook.Follow = transform;           // or cam.orientation
                freeLook.LookAt = cam.combatLookAt;    // or cam.playerObj
            }
            // For regular virtual cameras (like the ones inside the FreeLook rigs)
            else if (vcam is CinemachineVirtualCamera virtCam)
            {
                virtCam.Follow = transform;
                virtCam.LookAt = cam.combatLookAt;
            }
        }

        Debug.Log($"Camera spawned for {gameObject.name} – player = {cam.player?.name}");
    }
}