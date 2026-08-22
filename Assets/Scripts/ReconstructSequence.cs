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
        ApplyMaterial(wireframeModel, wireframeMat);
        wireframeModel.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 2f;
            float e = t * t * (3f - 2f * t);
            wireframeModel.transform.localScale = Vector3.one * e;
            yield return null;
        }
        wireframeModel.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.5f);

        Debug.Log("[Reconstruct] Phase 2: Solid rising from below");
        solidModel = Instantiate(solidPrefab, pos + Vector3.down * 2f, rot);
        ApplyMaterial(solidModel, solidMat);

        float t2 = 0f;
        Vector3 startPos = solidModel.transform.position;
        Vector3 endPos = pos;
        while (t2 < 1f)
        {
            t2 += Time.deltaTime / 2f;
            float e = t2 * t2 * (3f - 2f * t2);
            solidModel.transform.position = Vector3.Lerp(startPos, endPos, e);
            yield return null;
        }
        solidModel.transform.position = endPos;

        Debug.Log("[Reconstruct] Phase 3: Wireframe fading out");
        float t3 = 0f;
        while (t3 < 1f)
        {
            t3 += Time.deltaTime / 1f;
            SetAlpha(wireframeModel, 1f - t3);
            yield return null;
        }
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
            }
        }
    }
}
