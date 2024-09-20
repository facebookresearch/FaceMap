using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateTrialNumberInformation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateInfo(Vector2Int vector)
    {
        UnityEngine.UI.Text text = GetComponent<UnityEngine.UI.Text>();
        //text.text = "Trial " + vector.x.ToString() + " out of " + vector.y.ToString();
        var parts = text.text.Split(' ');
        parts[1] = vector.x.ToString();
        parts[4] = vector.y.ToString();
        text.text = string.Join(" ", parts);
    }
}
