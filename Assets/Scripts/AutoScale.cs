
using UnityEngine;

/// <summary>
/// Automatically scales rehabilitation objects based on their type.
/// </summary>
[DisallowMultipleComponent]
public class RehabObjectScaler : MonoBehaviour
{
    [Header("Default Scale Factors (meters)")]
    public float utensilScale = 0.2f; // ~20cm
    public float bowlScale = 0.15f;   // ~15cm
    public float plateScale = 0.25f;  // ~25cm

    [ContextMenu("Rescale Now")]
    public void Rescale()
    {
        string n = name.ToLower();
        if (n.Contains("spoon") || n.Contains("fork"))
        {
            transform.localScale = Vector3.one * utensilScale;
        }
        else if (n.Contains("bowl"))
        {
            transform.localScale = Vector3.one * bowlScale;
        }
        else if (n.Contains("plate"))
        {
            transform.localScale = Vector3.one * plateScale;
        }
    }

    void Start()
    {
        Rescale();
    }
}
