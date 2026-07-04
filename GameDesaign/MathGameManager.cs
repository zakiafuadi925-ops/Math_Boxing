private string currentInput = "";

public void OnNumberKeyPressed(string number)
{
    if(currentInput.Length < 2) // Karena batas maksimal hasil adalah 20 (2 digit)
    {
        currentInput += number;
        UpdateInputDisplayUI(); // Tampilkan angka yang sedang diketik anak di layar
    }
}

public void OnEnterPressed()
{
    int finalAnswer = int.Parse(currentInput);
    if(finalAnswer == correctAnswer)
    {
        SubmitCorrectAnswerToServer(); // Jawaban benar, kirim ke Supabase
    }
    else
    {
        TriggerWrongAnswerPenalty(); // Salah, kunci tombol 1 detik
    }
    currentInput = ""; // Reset input
}