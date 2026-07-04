public interface IMathQuestion
{
    string GetQuestionText();
    int GetCorrectAnswer();
}

// Contoh Implementasi Kelas Soal Aljabar
public class AlgebraQuestion : IMathQuestion
{
    private int x;
    private int multiplier;
    private int result;

    public AlgebraQuestion()
    {
        // Generate angka acak yang menghasilkan bilangan bulat
        x = Random.Range(1, 10); // Jawaban disembunyikan
        multiplier = Random.Range(2, 5);
        result = x * multiplier;
    }

    public string GetQuestionText() => $"Berapa nilai x?  {multiplier}x = {result}";
    public int GetCorrectAnswer() => x;
}