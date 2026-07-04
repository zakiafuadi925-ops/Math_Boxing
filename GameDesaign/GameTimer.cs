using System.Collections;
using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private int timeRemaining = 60;
    private bool isGameActive = false;

    public void StartCountdown()
    {
        isGameActive = true;
        StartCoroutine(TimerCoroutine());
    }

    IEnumerator TimerCoroutine()
    {
        while (timeRemaining > 0 && isGameActive)
        {
            timerText.text = timeRemaining.ToString() + "s";
            yield return new WaitForSeconds(1.0f);
            timeRemaining--;
            
            // Opsional: Kirim sinkronisasi waktu ke Supabase setiap 5-10 detik jika diperlukan
        }

        EndMatch();
    }

    private void EndMatch()
    {
        isGameActive = false;
        timerText.text = "TIME UP!";
        // Panggil fungsi SupabaseManager untuk mengunci skor akhir dan menampilkan pemenang
    }
}