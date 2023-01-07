using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorNormalizationTester : MonoBehaviour
{
    [Header("Grayscale thresholds")]
    public float low = 0.1f;
    public float high = 0.9f;

    private Text[] children;

    private void Start()
    {
        children = GetComponentsInChildren<Text>();

        foreach (Text text in children)
        {
            text.color = NormalizeColor(text.color);
        }
    }

    private Color NormalizeColor(Color color)
    {
        Debug.Log(color.grayscale);

        if (color.grayscale == 0)
        {
            return new Color(low, low, low, color.a);
        }

        if (color.grayscale < low)
        {
            return new Color(color.r * 2f, color.g * 2f, color.b * 2f, color.a);
        }

        if (color.grayscale > high)
        {
            return new Color(color.r / 2f, color.g / 2f, color.b / 2f, color.a);
        }

        return color;
    }
}
