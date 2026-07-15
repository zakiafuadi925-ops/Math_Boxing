using UnityEngine;
using UnityEngine.UI;

public class BoxerActionController : MonoBehaviour
{
    [Header("Component System")]
    [SerializeField] private Animator boxerAnimator;

    [Header("Numpad Buttons")]
    [SerializeField] private Button[] numpadButtons; // Array untuk menampung tombol 1-9

    private void Start()
    {
        // Validasi sirkuit komponen
        if (boxerAnimator == null)
        {
            boxerAnimator = GetComponent<Animator>();
        }

        // Daftarkan listener untuk setiap tombol numpad secara otomatis via indeks
        for (int i = 0; i < numpadButtons.Length; i++)
        {
            int buttonIndex = i + 1; // Angka 1 sampai 9
            if (numpadButtons[i] != null)
            {
                numpadButtons[i].onClick.AddListener(() => OnNumpadPressed(buttonIndex));
            }
        }
    }

    /// <summary>
    /// Jalur interaksi saat tombol numpad ditekan
    /// </summary>
    public void OnNumpadPressed(int number)
    {
        Debug.Log($"[Sinyal Input] Tombol {number} ditekan. Mengirim data ke Animator.");

        // Pemetaan logika angka numpad ke actionType Animator
        switch (number)
        {
            case 1: // Contoh: Tombol 1 memicu Jab
                TriggerAnimationAction(1);
                break;
            case 2: // Contoh: Tombol 2 memicu Cross
                TriggerAnimationAction(2);
                break;
            case 3: // Contoh: Tombol 3 memicu Uppercut
                TriggerAnimationAction(3);
                break;
            case 4: // Contoh: Tombol 4 memicu Hook
                TriggerAnimationAction(4);
                break;
            case 5: // Contoh: Tombol 5 memicu Dodge
                TriggerAnimationAction(5);
                break;
            case 6: // Contoh: Tombol 6 memicu isHit
                TriggerAnimationAction(6);
                break;
            default:
                // Jika tombol lain belum dipetakan, paksa kembali bersiap ke Idle
                TriggerAnimationAction(0);
                break;
        }
    }

    private void TriggerAnimationAction(int actionCode)
    {
        if (boxerAnimator != null)
        {
            // Set parameter integer sesuai dengan sirkuit kabel transisi kita
            boxerAnimator.SetInteger("actionType", actionCode);
            
            // Panggil fungsi reset otomatis agar setelah animasi selesai, 
            // state bisa mendeteksi kembalinya nilai ke Idle (0) jika diperlukan
            Invoke(nameof(ResetToIdle), 0.5f); 
        }
    }

    private void ResetToIdle()
    {
        if (boxerAnimator != null)
        {
            boxerAnimator.SetInteger("actionType", 0);
        }
    }
}