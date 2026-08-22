using UnityEngine;

public class TapInfoHandler : MonoBehaviour
{
    public string infoTitle = "Structure";
    public string infoDetails = "Details about this part.";

    void OnMouseDown()
    {
        Debug.Log("Tapped: " + infoTitle + " - " + infoDetails);
    }
}
