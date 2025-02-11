using System.Collections;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int frameRate = 60;

        void Start() {
            StartCoroutine(SetFramerate());
        }
        private IEnumerator SetFramerate() {
            yield return new WaitForSeconds(1);
            Application.targetFrameRate = frameRate;
        }
    }
}
