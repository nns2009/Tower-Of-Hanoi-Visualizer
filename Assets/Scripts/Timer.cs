using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI Label;
    public string Prefix = "", Suffix = " s";

    void Update()
    {
        Label.text = Prefix + Time.time.ToString("0.0").Replace(',', '.') + Suffix;
    }
}
