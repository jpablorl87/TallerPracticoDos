using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    //This object can be decorated?
    public bool canBeDecorated = false;

    //Allowed decotarions tags "BallDecoration", "LightDecoration"
    public string[] allowedDecorationTags;

    public bool CanDecorate(string decorationTag)
    {
        if (!canBeDecorated) return false;
        foreach (string tag in allowedDecorationTags)
        {
            if (tag == decorationTag) return true;
        }
        return false;
    }
}
