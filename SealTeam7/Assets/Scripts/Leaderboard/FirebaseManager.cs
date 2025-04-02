using System;
using CandyCoded.env;
using Firebase;
using Firebase.AppCheck;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace Leaderboard
{
    public class FirebaseManager : MonoBehaviour
    {
        private static FirebaseManager _instance;

        private DatabaseReference _databaseReference;
        
        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            if (!env.TryParseEnvironmentVariable("FIREBASE_APP_CHECK_DEBUG_TOKEN", out string firebaseAppCheckDebugToken))
            {
                Debug.LogError("Could not find environment variable FIREBASE_APP_CHECK_DEBUG_TOKEN.");
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    DebugAppCheckProviderFactory.Instance.SetDebugToken(firebaseAppCheckDebugToken);
                    FirebaseAppCheck.SetAppCheckProviderFactory(DebugAppCheckProviderFactory.Instance);
                    Debug.Log("Setup FirebaseManager successfully.");
                }
                else
                {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + task.Exception);
                }
            });
        }

        private void Start() => _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        public static FirebaseManager GetInstance() => _instance;

        public void SaveGameResult(GameResult gameResult)
        {
            string json = JsonUtility.ToJson(gameResult);
            Debug.Log("Saving game result: " + json);
            _databaseReference.Child("gameResults").Child(Guid.NewGuid().ToString()).SetRawJsonValueAsync(json).ContinueWithOnMainThread(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        Debug.Log("Successfully saved game result.");
                    }
                    else
                    {
                        Debug.LogError($"Failed to save game result: {task.Exception}");
                    }
                });
        }
    } 
}