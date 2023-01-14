using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelButton : MonoBehaviour
{
    public string levelName;
    float stayTimer = 5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "grab")
        {
            stayTimer -= Time.fixedDeltaTime;
            if(stayTimer<0)
                SceneManager.LoadSceneAsync(levelName);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "grab")
        {
            stayTimer = 5f;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
