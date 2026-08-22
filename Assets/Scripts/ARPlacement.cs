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
    AudioSource audioSource;

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Awake()
    {
        cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialize = false;
        if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (planeManager == null) planeManager = FindFirstObjectByType<ARPlaneManager>();
        Debug.Log("[AR] Awake - RaycastManager: " + (raycastManager != null) + " PlaneManager: " + (planeManager != null) + " ruinPrefab: "+ (ruinPrefab!=null));
    }

    void Update()
    {
        if (placed) return;
        if (raycastManager == null) return;

        Vector2 touchPos;
        bool began = false;
        int fingerId = -1;

        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchPos = t.screenPosition;
                fingerId = (int)t.touchId;
                began = true;
            }
            else return;
        }
        else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            began = true;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            touchPos = Input.GetTouch(0).position;
            fingerId = Input.GetTouch(0).fingerId;
            began = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            touchPos = Input.mousePosition;
            began = true;
        }
        else return;

        if (!began) return;

        var es = UnityEngine.EventSystems.EventSystem.current;
        bool overUI = false;
        if (es != null) overUI = fingerId >= 0 ? es.IsPointerOverGameObject(fingerId) : es.IsPointerOverGameObject();
        if (overUI) return;

        hits.Clear();
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            if (ruinPrefab == null) { Debug.LogError("[AR] ruinPrefab NULL"); return; }
            PlaceRuin(hitPose.position, hitPose.rotation);
        }
        else
        {
            // fallback 1.5m in front of camera
            Vector3 pos = cam.transform.position + cam.transform.forward * 1.5f;
            pos.y = 0;
            PlaceRuin(pos, Quaternion.identity);
        }
    }

    void PlaceRuin(Vector3 position, Quaternion rotation)
    {
        placed = true;
        if (planeManager != null) foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);

        spawnedRuin = Instantiate(ruinPrefab, position, rotation);
        // face camera (proven from old project)
        var toCam = cam.transform.position - position;
        toCam.y = 0;
        if (toCam.sqrMagnitude > 0.001f) spawnedRuin.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);

        // snap to ground
        var rend = spawnedRuin.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float offset = position.y - rend.bounds.min.y;
            spawnedRuin.transform.position += Vector3.up * offset;
        }
        NormalizeScale(spawnedRuin);

        // add collider for tap (if missing)
        if (spawnedRuin.GetComponentInChildren<Collider>() == null)
        {
            var r = spawnedRuin.GetComponentInChildren<Renderer>();
            if (r != null) r.gameObject.AddComponent<BoxCollider>();
        }

        Debug.Log("[AR] Ruin placed at " + spawnedRuin.transform.position + " rot " + spawnedRuin.transform.rotation.eulerAngles);
        PlayClip(1); // 02_ruin
        UIManager.Instance.ShowReconstructButton();
        UIManager.Instance.HideHint();
    }

    void NormalizeScale(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;
        float targetHeight = 0.6f;
        float h = r.bounds.size.y;
        if (h > 0.001f)
        {
            float s = targetHeight / h;
            go.transform.localScale = Vector3.one * s;
            Debug.Log("[AR] NormalizeScale " + s);
        }
    }

    void PlayClip(int idx)
    {
        string[] names = { "01_intro", "02_ruin", "03_reconstruct", "04_details", "05_closing" };
        if (idx < 0 || idx >= names.Length) return;
        var clip = Resources.Load<AudioClip>("Audio/" + names[idx]);
        if (clip == null) { Debug.Log("[AR] clip not found " + names[idx]); return; }
        audioSource.clip = clip;
        audioSource.Play();
    }

    public GameObject GetRuin() { return spawnedRuin; }
    public Vector3 GetRuinPosition() { return spawnedRuin != null ? spawnedRuin.transform.position : Vector3.zero; }
    public Quaternion GetRuinRotation() { return spawnedRuin != null ? spawnedRuin.transform.rotation : Quaternion.identity; }
    public void DestroyRuin() { if (spawnedRuin != null) Destroy(spawnedRuin); spawnedRuin = null; }
}
