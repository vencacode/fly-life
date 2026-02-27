using UnityEngine;
using TMPro;

/// <summary>
/// Visual marker for a poo instance on the city map.
/// Floats gently and shows emoji + type label.
/// </summary>
public class PooMarker : MonoBehaviour
{
    public TextMeshPro Label;
    public SpriteRenderer Icon;

    [Header("Float animation")]
    public float FloatAmplitude = 0.15f;
    public float FloatSpeed     = 1.2f;

    Vector3 _basePos;

    void Start()
    {
        _basePos = transform.position;
        if (Label) Label.text = "Poo";
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * FloatSpeed + _basePos.x) * FloatAmplitude;
        transform.position = _basePos + new Vector3(0, y, 0);
    }

    public void SetType(string pooName)
    {
        if (Label) Label.text = $"Poo\n<size=60%>{pooName}</size>";
    }
}
