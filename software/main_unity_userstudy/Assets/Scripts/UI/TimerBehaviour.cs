using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerBehaviour : MonoBehaviour
{
    string lateColor = "DB2320";
    string normalColor = "20DB69";
    string earlyColor = "DBC521";

    public float trialSoftCap = 0;
    public float trialMinTimeSpent = 0;
    public float blankScreenTime = 0;

    private float currentTime = 0;

    UnityEngine.UI.Text timerText;

    // Start is called before the first frame update
    void Start()
    {
        timerText = transform.GetChild(0).GetComponent<UnityEngine.UI.Text>();
        UpdateTimer(2);
    }

    public void UpdateTimer(float time)
    {
        if (timerText == null)
            return;

            currentTime = trialSoftCap - time;
        timerText.text = Mathf.CeilToInt(currentTime).ToString();

        if(time < trialMinTimeSpent)
            timerText.color = hexToColor(earlyColor);
        else if(time > trialSoftCap)
            timerText.color = hexToColor(lateColor);
        else
            timerText.color = hexToColor(normalColor);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static Color hexToColor(string hex)
    {
        hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
        hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
        byte a = 255;//assume fully visible unless specified in hex
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        //Only use alpha if the string has enough characters
        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color32(r, g, b, a);
    }
}
