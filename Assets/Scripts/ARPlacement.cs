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
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        Debug.Log("[AR] Awake - RaycastManager: " + (raycastManager != null) + " PlaneManager: " + (planeManager != null));
    }

    void Update()
    {
        if (placed) return;
        if (raycastManager == null) return;

        Vector2 touchPos;
        bool began = false;

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
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        hits.Clear();
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            if (ruinPrefab == null) { Debug.LogError("[AR] ruinPrefab NULL"); return; }
            // last working + minimal upright fix: face camera after place like old project
            Quaternion uprightRot = Quaternion.Euler(0, hitPose.rotation.eulerAngles.y, 0);
            spawnedRuin = Instantiate(ruinPrefab, hitPose.position, uprightRot);
            // face camera + snap to ground (proven)
            var toCam = cam.transform.position - hitPose.position;
            toCam.y = 0;
            if (toCam.sqrMagnitude > 0.001f) spawnedRuin.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            var rend = spawnedRuin.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                float offset = hitPose.position.y - rend.bounds.min.y;
                spawnedRuin.transform.position += Vector3.up * offset;
                float h = rend.bounds.size.y;
                if (h > 0.01f) { float s = 0.6f / h; spawnedRuin.transform.localScale = Vector3.one * s; }
            }
            if (spawnedRuin.GetComponentInChildren<Collider>() == null)
            {
                var r = spawnedRuin.GetComponentInChildren<Renderer>();
                if (r != null) r.gameObject.AddComponent<BoxCollider>();
            }
            if (planeManager != null) foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);
            placed = true;
            Debug.Log("[AR] Ruin placed at " + spawnedRuin.transform.position);
            UIManager.Instance.ShowReconstructButton();
            UIManager.Instance.HideHint();
            PlayClip(1);
        }
        else
        {
            Debug.Log("[AR] Raycast missed - tap on plane");
        }
    }

    void PlayClip(int idx)
    {
        string[] names = { "01_intro", "02_ruin", "03_reconstruct", "04_details", "05_closing" };
        var clip = Resources.Load<AudioClip>("Audio/" + names[idx]);
        if (clip == null) return;
        var src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.Play();
    }

    public GameObject GetRuin() { return spawnedRuin; }
    public Vector3 GetRuinPosition() { return spawnedRuin != null ? spawnedRuin.transform.position : Vector3.zero; }
    public Quaternion GetRuinRotation() { return spawnedRuin != null ? spawnedRuin.transform.rotation : Quaternion.identity; }
    public void DestroyRuin() { if (spawnedRuin != null) Destroy(spawnedRuin); spawnedRuin = null; }
}
