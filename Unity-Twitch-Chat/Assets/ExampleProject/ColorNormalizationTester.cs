using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorNormalizationTester : MonoBehaviour
{
    private Text[] children;

    private void Start()
    {
        children = GetComponentsInChildren<Text>();

        foreach (Text text in children)
        {
            text.color = Lexone.UnityTwitchChat.ChatColors.NormalizeColor(text.color);
        }
    }
}
