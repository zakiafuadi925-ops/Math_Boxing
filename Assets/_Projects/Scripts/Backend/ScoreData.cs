using System;

namespace MathBoxing.Backend
{
    [Serializable]
    public class MatchScorePayload
    {
        // Kita siapkan dua kolom, nanti kita pilih salah satu yang diisi sesuai status player
        public int p1_score;
        public int p2_score;
    }
}