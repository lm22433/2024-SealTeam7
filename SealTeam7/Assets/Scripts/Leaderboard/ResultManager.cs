using System;
using Firebase.Database;
using UnityEngine;
using Newtonsoft.Json;

namespace Leaderboard
{
    public class ResultManager : MonoBehaviour
    {
        private static ResultManager _instance;

        private DatabaseReference databaseReference;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
            
            databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        }
        
        public static ResultManager GetInstance() => _instance;
        
        public void SaveResult(GameResult gameResult)
        {
            string json = JsonConvert.SerializeObject(gameResult, Formatting.None);
            databaseReference.Child("gameResults").Child(Guid.NewGuid().ToString()).SetRawJsonValueAsync(json);
        }
    }
}