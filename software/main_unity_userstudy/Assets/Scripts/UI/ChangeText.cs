using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeText : MonoBehaviour
{
    UnityEngine.UI.Text text;

    Gradient gradient;

    public Color startColor;
    public Color endColor;

    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<UnityEngine.UI.Text>();
        PopulateGradients();
    }

    private void PopulateGradients()
    {
        gradient = new Gradient();

        colorKey = new GradientColorKey[3];
        colorKey[0].color = startColor;
        colorKey[0].time = 0.0f;
        colorKey[1].color = Color.yellow;
        colorKey[1].time = 0.5f;
        colorKey[2].color = endColor;
        colorKey[2].time = 1.0f;

        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SliderToText(float sliderValue)
    {
        text.text = ((int)sliderValue).ToString();
    }

    public void SliderToTextColor(float sliderValue)
    {
        float maxValue = 10;

        float fraction = sliderValue / maxValue;
        Color output = gradient.Evaluate(fraction);
        text.color = output;
    }


}
