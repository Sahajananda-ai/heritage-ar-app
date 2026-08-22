using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    ARPlacement placement;
    ReconstructSequence sequence;
    GameObject homeCanvas, reconstructCanvas, storyCanvas;

    GameObject hintCanvas;

    void Awake()
    {
        Instance = this;
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
        else
        {
            // Fix existing EventSystem that has wrong module
            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null
                && es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() != null)
            {
                Destroy(es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>());
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
    }

    public void HideHint() { if (hintCanvas != null) Destroy(hintCanvas); }

    void Start()
    {
        placement = FindFirstObjectByType<ARPlacement>();
        sequence = FindFirstObjectByType<ReconstructSequence>();
        ShowHome();
    }

    void ShowHome()
    {
        homeCanvas = CreateCanvas("HomeCanvas");
        AddOverlay(homeCanvas.transform, new Color(0.05f, 0.05f, 0.1f, 0.95f));
        AddText(homeCanvas.transform, "BHARAT AR", 48, Color.white, new Vector2(0, 100));
        AddText(homeCanvas.transform, "Tap floor to place stupa", 24, Color.grey, new Vector2(0, 40));
        AddButton(homeCanvas.transform, "Begin", new Color(0.85f, 0.55f, 0.15f), new Vector2(0, -40), () =>
        {
            Destroy(homeCanvas);
            // Show hint after Begin
            hintCanvas = CreateCanvas("HintCanvas");
            AddText(hintCanvas.transform, "Move phone to scan floor...", 22, Color.white, new Vector2(0, 60));
            AddText(hintCanvas.transform, "Tap on plane to place ruin", 18, Color.yellow, new Vector2(0, 20));
        });
    }

    public void ShowReconstructButton()
    {
        reconstructCanvas = CreateCanvas("ReconCanvas");
        AddButton(reconstructCanvas.transform, "Reconstruct", new Color(0.85f, 0.55f, 0.15f), new Vector2(0, -60), () =>
        {
            Destroy(reconstructCanvas);
            sequence.Run(placement.GetRuinPosition(), placement.GetRuinRotation(), ShowStory);
            placement.DestroyRuin();
        });
    }

    void ShowStory()
    {
        storyCanvas = CreateCanvas("StoryCanvas");
        AddOverlay(storyCanvas.transform, new Color(0.05f, 0.05f, 0.1f, 0.9f));
        AddText(storyCanvas.transform, "NALANDA", 48, new Color(0.85f, 0.7f, 0.4f), new Vector2(0, 120));
        AddText(storyCanvas.transform, "World's first residential university.\nFounded 5th century CE.\nDestroyed 1193 CE.\nUNESCO World Heritage Site.", 22, Color.white, new Vector2(0, 0));
        AddButton(storyCanvas.transform, "Restart", new Color(0.2f, 0.6f, 0.4f), new Vector2(0, -140), () =>
        {
            Destroy(storyCanvas);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        });
    }

    GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    void AddOverlay(Transform parent, Color col)
    {
        var go = new GameObject("Overlay");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    Text AddText(Transform parent, string content, int size, Color color, Vector2 pos)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 120);
        return t;
    }

    Button AddButton(Transform parent, string label, Color col, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(onClick);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220, 55);

        var txt = new GameObject("Label");
        txt.transform.SetParent(go.transform, false);
        var t = txt.AddComponent<Text>();
        t.text = label;
        t.color = Color.white;
        t.fontSize = 26;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 55);

        return btn;
    }
}
