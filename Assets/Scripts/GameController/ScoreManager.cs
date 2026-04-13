using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public event Action<int> OnScoreChanged;

    public int Score { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResetScore();
    }

    /// <summary>
    /// Añade o resta puntos al marcador del jugador.
    /// </summary>
    public void AddPoints(int amount)
    {
        //sumar o restar
        Score = Mathf.Max(0, Score + amount);

        //notifica cambios
        OnScoreChanged?.Invoke(Score);
    }

    public void ResetScore()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }

    public int GetScore() => Score;
}
