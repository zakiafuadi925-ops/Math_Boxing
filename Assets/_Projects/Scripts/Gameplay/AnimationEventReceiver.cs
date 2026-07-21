using UnityEngine;

namespace MathBoxing.Visuals
{
    public class AnimationEventReceiver : MonoBehaviour
    {
        // Fungsi dummy untuk menyerap/menampung pemicu event dari klip animasi
        public void OnAnimationEventTriggered()
        {
            // Dibiarkan kosong agar Unity tidak melempar error
        }

        public void Hit() { }
        public void Attack() { }
    }
}