using UnityEngine;
using System.Collections;
using System.Threading;

public class Main : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        UnityEngine.Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;

        var timer = new Timer(_ =>
        {
            UnityEngine.Debug.Log("go exception");
            throw new System.Exception("sinuyo");
        }, null, 5000, Timeout.Infinite);
    }

    private void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        UnityEngine.Debug.Log("Threaded message" + condition + stackTrace + type);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
