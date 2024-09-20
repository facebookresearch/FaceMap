using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedbackScreenLogic : MonoBehaviour
{
    public UnityEngine.UI.InputField inputField;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ContinueButtonClick()
    {
        //string content = feedbackScreenText.text;
        string content = inputField.text;
        if (content.Length == 0)
            content = " ";
        System.IO.File.WriteAllText(SessionData.GetFeedbackFilepath(), content);
        StaticMethods.NextScene();
    }
}
