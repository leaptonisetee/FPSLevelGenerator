using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
 //   public Text fpsText;
    private int frameCount = 0;
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float updateInterval = 1.0f;
    private float timePassed = 0.0f;

    void Start()
    {
        
    }
        void Update()
        {
            frameCount++;
            deltaTime += Time.unscaledDeltaTime;
            timePassed += Time.unscaledDeltaTime;

            if (timePassed > updateInterval)
            {
                fps = frameCount / deltaTime;
                UnityEngine.Debug.Log(string.Format("FPS: {0:F2}", fps));
                frameCount = 0;
                deltaTime = 0.0f;
                timePassed = 0.0f;
            }
        }
    
}