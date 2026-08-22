using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TapInfoHandler : MonoBehaviour
{
    public string infoTitle = "NALANDA";
    public string infoDetails = "World's first residential university. Founded 5th c. CE. Destroyed 1193 CE. UNESCO World Heritage Site.";
    public System.Action storyCallback;
    bool triggered;
    AudioSource audioSource;
    Camera cam;

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void Awake() { cam = Camera.main; if (cam == null) cam = FindFirstObjectByType<Camera>(); audioSource = GetComponent<AudioSource>(); if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>(); }

    void Update()
    {
        if (triggered) return;
        bool tapped = false;
        Vector2 pos = Vector2.zero;

        if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            tapped = true; pos = Touch.activeTouches[0].screenPosition;
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapped = true; pos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            tapped = true; pos = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            tapped = true; pos = Input.mousePosition;
        }

        if (!tapped) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = cam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform) || hit.collider.gameObject == gameObject)
            {
                Trigger();
            }
            else
            {
                // also allow any tap on solidModel if this handler is on solid child
                var root = transform.root;
                if (hit.transform.root == root) Trigger();
            }
        }
    }

    void Trigger()
    {
        if (triggered) return;
        triggered = true;
        Debug.Log("[TapInfo] Tapped " + infoTitle);
        // play detail narration
        var clip = Resources.Load<AudioClip>("Audio/04_details");
        if (clip != null) { audioSource.clip = clip; audioSource.Play(); }
        storyCallback?.Invoke();
        // also show via UIManager fallback
        if (UIManager.Instance != null) UIManager.Instance.ForceShowStory();
    }

    // for old OnMouseDown fallback
    void OnMouseDown() { if (!triggered) Trigger(); }
}
