using System.Collections;
using UnityEngine;

public class ReconstructSequence : MonoBehaviour
{
    public GameObject wireframePrefab;
    public GameObject solidPrefab;
    public Material wireframeMat;
    public Material solidMat;
    
    GameObject wireframeModel, solidModel;
    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Run(Vector3 pos, Quaternion rot, System.Action onComplete)
    {
        StartCoroutine(DoSequence(pos, rot, onComplete));
    }

    IEnumerator DoSequence(Vector3 pos, Quaternion rot, System.Action onComplete)
    {
        Debug.Log("[Reconstruct] START pos " + pos + " rot " + rot.eulerAngles);

        // keep upright facing (ruin already faces camera, reuse that)
        // no extra rotation needed - already correct from ARPlacement

        // PHASE 1 wireframe - scale+fade in, hold 1s
        GameObject wirePrefab = wireframePrefab != null ? wireframePrefab : solidPrefab;
        wireframeModel = Instantiate(wirePrefab, pos, rot);
        // fix scale factor like old project
        var solidBounds = solidPrefab != null ? solidPrefab.GetComponentInChildren<Renderer>() : null;
        var wireRend = wireframeModel.GetComponentInChildren<Renderer>();
        float scaleFactor = 1f;
        // estimate using instantiated bounds after frame
        yield return null;
        if (wireRend != null)
        {
            float h = wireRend.bounds.size.y;
            if (h > 0.01f) scaleFactor = 0.6f / h;
            Debug.Log("[Reconstruct] wire scaleFactor " + scaleFactor + " h " + h);
        }

        if (wireframeMat != null) ApplyMaterial(wireframeModel, wireframeMat);
        else Debug.LogWarning("[Reconstruct] wireframeMat is NULL - using original");

        wireframeModel.transform.localScale = Vector3.zero;
        SetAlpha(wireframeModel, 0f);

        float t = 0f; float d1 = 0.6f;
        while (t < 1f)
        {
            t += Time.deltaTime / d1;
            float e = Mathf.Clamp01(t);
            e = e * e * (3f - 2f * e);
            wireframeModel.transform.localScale = Vector3.one * scaleFactor * e;
            SetAlpha(wireframeModel, e);
            yield return null;
        }
        wireframeModel.transform.localScale = Vector3.one * scaleFactor;
        SetAlpha(wireframeModel, 1f);
        Debug.Log("[Reconstruct] Wireframe visible " + wireframeModel.transform.localScale);

        yield return new WaitForSeconds(0.9f); // hold visible

        // PHASE 2 solid fading in at same pos/rot, same scale
        solidModel = Instantiate(solidPrefab, pos, rot);
        solidModel.transform.localScale = Vector3.one * scaleFactor;
        // keep textured materials - only make transparent for fade
        SetAlpha(solidModel, 0f);
        // ensure collider for tap
        if (solidModel.GetComponentInChildren<Collider>() == null)
        {
            var r = solidModel.GetComponentInChildren<Renderer>();
            if (r != null) r.gameObject.AddComponent<BoxCollider>();
        }
        // attach TapInfoHandler to solid for tap->story
        var tap = solidModel.GetComponentInChildren<TapInfoHandler>();
        if (tap == null)
        {
            var r = solidModel.GetComponentInChildren<Renderer>();
            if (r != null) r.gameObject.AddComponent<TapInfoHandler>();
        }
        solidModel.GetComponentInChildren<TapInfoHandler>().infoTitle = "NALANDA";
        solidModel.GetComponentInChildren<TapInfoHandler>().infoDetails = "World's oldest residential university (5th c. CE). Destroyed 1193 CE. UNESCO World Heritage.";
        solidModel.GetComponentInChildren<TapInfoHandler>().storyCallback = onComplete;

        PlayClip(2); // 03_reconstruct

        float t2 = 0f; float d2 = 1.0f;
        while (t2 < 1f)
        {
            t2 += Time.deltaTime / d2;
            float e = Mathf.Clamp01(t2);
            e = e * e * (3f - 2f * e);
            SetAlpha(solidModel, e);
            SetAlpha(wireframeModel, 1f - e);
            yield return null;
        }
        SetAlpha(solidModel, 1f);
        Destroy(wireframeModel);
        Debug.Log("[Reconstruct] Solid faded in - tap solid for story");

        PlayClip(3); // 04_details - background narration for solid
        // Wait before auto-show story: hint already, tap will trigger, auto after 6s
        // Don't invoke onComplete yet - let tap handle it, but auto after 6s
        float wait = 0f;
        while (wait < 6f)
        {
            // if story already shown via tap, exit
            if (solidModel == null) yield break;
            // check if callback already invoked (UIManager will set)
            // we leave auto fallback to UIManager's delayed - here just wait
            wait += Time.deltaTime;
            yield return null;
        }
        // auto trigger if not yet tapped
        if (onComplete != null) onComplete.Invoke();
    }

    void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.materials = mats;
        }
    }

    void SetAlpha(GameObject go, float a)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = a;
                    m.SetColor("_BaseColor", c);
                }
                else if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = a;
                    m.color = c;
                }
                if (a < 0.99f)
                {
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    m.renderQueue = 3000;
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
                }
                else
                {
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    m.SetInt("_ZWrite", 1);
                    m.DisableKeyword("_ALPHABLEND_ON");
                    m.renderQueue = 2000;
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0);
                }
            }
        }
    }

    void PlayClip(int idx)
    {
        string[] names = { "01_intro", "02_ruin", "03_reconstruct", "04_details", "05_closing" };
        var clip = Resources.Load<AudioClip>("Audio/" + names[idx]);
        if (clip == null) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public GameObject GetSolid() { return solidModel; }
}
