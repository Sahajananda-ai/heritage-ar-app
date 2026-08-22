using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARPlacement : MonoBehaviour
{
    public GameObject ruinPrefab;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    GameObject spawnedRuin;
    bool placed;
    Camera cam;
    List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Awake()
    {
        cam = Camera.main;
        Debug.Log("[AR] Awake - RaycastManager: " + (raycastManager != null) + " PlaneManager: " + (planeManager != null));
    }

    void Update()
    {
        if (placed) return;
        if (raycastManager == null) return;

        Vector2 touchPos;
        bool began = false;

        // New Input System (EnhancedTouch)
        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchPos = t.screenPosition;
                began = true;
            }
            else return;
        }
        else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            began = true;
        }
        else
        {
            // Fallback: old Input (if Both mode) + mouse for editor
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
            {
                touchPos = Input.GetTouch(0).position;
                began = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                touchPos = Input.mousePosition;
                began = true;
            }
            else return;
        }

        if (!began) return;

        // Don't place if tapping UI
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        hits.Clear();
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            if (ruinPrefab == null) { Debug.LogError("[AR] ruinPrefab is NULL! Assign in Inspector"); return; }
            spawnedRuin = Instantiate(ruinPrefab, hitPose.position, hitPose.rotation);
            
            if (planeManager != null)
                foreach (var plane in planeManager.trackables)
                    plane.gameObject.SetActive(false);
            
            placed = true;
            Debug.Log("[AR] Ruin placed at " + hitPose.position);
            UIManager.Instance.ShowReconstructButton();
            UIManager.Instance.HideHint();
        }
        else
        {
            Debug.Log("[AR] Raycast missed - tap on detected plane");
        }
    }

    public GameObject GetRuin() { return spawnedRuin; }
    public Vector3 GetRuinPosition() { return spawnedRuin != null ? spawnedRuin.transform.position : Vector3.zero; }
    public Quaternion GetRuinRotation() { return spawnedRuin != null ? spawnedRuin.transform.rotation : Quaternion.identity; }
    public void DestroyRuin() { if (spawnedRuin != null) Destroy(spawnedRuin); }
}
