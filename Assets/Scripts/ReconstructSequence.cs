using System.Collections;
using UnityEngine;

public class ReconstructSequence : MonoBehaviour
{
    public GameObject wireframePrefab;
    public GameObject solidPrefab;
    public Material wireframeMat;
    public Material solidMat;
    
    GameObject wireframeModel, solidModel;

    public void Run(Vector3 pos, Quaternion rot, System.Action onComplete)
    {
        StartCoroutine(DoSequence(pos, rot, onComplete));
    }

    IEnumerator DoSequence(Vector3 pos, Quaternion rot, System.Action onComplete)
    {
        Debug.Log("[Reconstruct] Phase 1: Wireframe appearing");

        wireframeModel = Instantiate(wireframePrefab, pos, rot);
        if (wireframeMat != null) ApplyMaterial(wireframeModel, wireframeMat);
        wireframeModel.transform.localScale = Vector3.zero;
        SetAlpha(wireframeModel, 0f);
        // fade in + scale fast
        float t = 0f;
        float d1 = 0.5f;
        while (t < 1f)
        {
            t += Time.deltaTime / d1;
            float e = t * t * (3f - 2f * t);
            wireframeModel.transform.localScale = Vector3.one * e;
            SetAlpha(wireframeModel, e);
            yield return null;
        }
        wireframeModel.transform.localScale = Vector3.one;
        SetAlpha(wireframeModel, 1f);

        yield return new WaitForSeconds(0.25f);

        Debug.Log("[Reconstruct] Phase 2: Solid fading in (wireframe fading out) - fast");
        solidModel = Instantiate(solidPrefab, pos, rot);
        // KEEP original textured materials - just make them transparent for fade
        MakeTransparent(solidModel);
        SetAlpha(solidModel, 0f);

        float t2 = 0f;
        float d2 = 0.9f;
        while (t2 < 1f)
        {
            t2 += Time.deltaTime / d2;
            float e = t2 * t2 * (3f - 2f * t2);
            SetAlpha(solidModel, e);
            SetAlpha(wireframeModel, 1f - e);
            yield return null;
        }
        SetAlpha(solidModel, 1f);
        Destroy(wireframeModel);

        Debug.Log("[Reconstruct] DONE");
        onComplete?.Invoke();
    }

    void ApplyMaterial(GameObject go, Material mat)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.materials = mats;
        }
    }

    void MakeTransparent(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                // Enable transparency for URP Lit
                if (m.HasProperty("_Surface"))
                    m.SetFloat("_Surface", 1f); // 1 = Transparent
                if (m.HasProperty("_Blend"))
                    m.SetFloat("_Blend", 0f); // 0 = Alpha
                if (m.HasProperty("_SrcBlend"))
                    m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (m.HasProperty("_DstBlend"))
                    m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_ZWrite"))
                    m.SetFloat("_ZWrite", 0f);
                m.renderQueue = 3000;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
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
                // Also handle _BaseColor alpha for URP
                if (m.HasProperty("_Alpha"))
                    m.SetFloat("_Alpha", a);
            }
        }
    }
}
