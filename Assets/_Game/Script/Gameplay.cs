using System;
using System.Globalization;
using UnityEngine;

public class Gameplay : MonoBehaviour {
    [SerializeField] float countTimeLimit;
    [SerializeField] string str_timeLimit;
    DateTime timeLimit;
    DateTime now;

    void OnApplicationPause(bool pause) {
        if (!pause)
            now = DateTime.Now;
    }

    void Start() {
        InitTimeLimit();
    }

    void Update() {
        now = now.AddSeconds(Time.unscaledDeltaTime);
        if (Input.GetKeyDown(KeyCode.Alpha0))
            InitTimeLimit();
        if (Input.GetKeyDown(KeyCode.Alpha0)) {
            TimeSpan deltaTime = timeLimit - now;
            Debug.Log(string.Format("{ 0:D2}:{ 1:D2}:{ 2:D2}", (int)deltaTime.TotalHours, deltaTime.Minutes, deltaTime.Seconds));
        }
    }

    void InitTimeLimit() {
        Debug.Log("Init time limit");
        if (string.IsNullOrEmpty(str_timeLimit)) {
            timeLimit = now.AddMinutes(countTimeLimit);
            str_timeLimit = timeLimit.ToString("O");
        } else
            timeLimit = DateTime.ParseExact(str_timeLimit, "O", CultureInfo.InvariantCulture);
    }
}
