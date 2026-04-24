using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Utils.Coroutine
{
    public class CoroutineHelper : MonoBehaviour
    {
        public void RunCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
}
