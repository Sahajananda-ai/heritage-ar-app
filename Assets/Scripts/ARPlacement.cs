using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARPlacement : MonoBehaviour
{
    public GameObject ruinPrefab;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    GameObject spawnedRuin;
    bool placed;
    Camera cam;
    List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        cam = Camera.main;
        Debug.Log("[AR] Awake - RaycastManager: " + (raycastManager != null) + " PlaneManager: " + (planeManager != null));
    }

    void Update()
    {
        if (placed) return;
        if (raycastManager == null) return;
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;
        
        hits.Clear();
        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            spawnedRuin = Instantiate(ruinPrefab, hitPose.position, hitPose.rotation);
            
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
            
            placed = true;
            Debug.Log("[AR] Ruin placed at " + hitPose.position);
            UIManager.Instance.ShowReconstructButton();
        }
    }

    public GameObject GetRuin() { return spawnedRuin; }
    public Vector3 GetRuinPosition() { return spawnedRuin != null ? spawnedRuin.transform.position : Vector3.zero; }
    public Quaternion GetRuinRotation() { return spawnedRuin != null ? spawnedRuin.transform.rotation : Quaternion.identity; }
    public void DestroyRuin() { if (spawnedRuin != null) Destroy(spawnedRuin); }
}
