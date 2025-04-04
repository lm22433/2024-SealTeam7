using System;
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

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    DebugAppCheckProviderFactory.Instance.SetDebugToken("683EA485-3D35-4085-95D9-64B804096824");
                    FirebaseAppCheck.SetAppCheckProviderFactory(DebugAppCheckProviderFactory.Instance);
                    Debug.Log("Setup FirebaseManager successfully.");
                }
                else
                {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + task.Exception);
                }
            });
        }

        private void Start()
        {
            _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

            var test =  new GameResult(
                "test",
                "test",
                1,
                1,
                1,
                1,
                1,
                null
            );
            SaveGameResult(test);
        }

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