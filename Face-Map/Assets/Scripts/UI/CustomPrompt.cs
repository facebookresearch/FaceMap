using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPrompt : MonoBehaviour
{
    public UnityEngine.UI.Text textfield;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeText(string newText)
    {
        textfield.text = newText;
    }
}
