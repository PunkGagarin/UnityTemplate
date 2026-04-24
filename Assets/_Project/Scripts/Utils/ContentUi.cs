using UnityEngine;

namespace _Project.Scripts.Utils
{
    public class ContentUi : MonoBehaviour
    {
        [SerializeField] protected GameObject content;

        public virtual void Show()
        {
            content.SetActive(true);
        }
        
        public virtual void Hide()
        {
            content.SetActive(false);
        }
    }
}