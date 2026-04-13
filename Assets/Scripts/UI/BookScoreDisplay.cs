using UnityEngine;
using TMPro;
using System.Collections;

public class BookScoreDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private float _updateSpeed = 0.05f;

    private int _displayedScore = 0;
    private int _targetScore = 0;
    private Coroutine _scoreRoutine;

    private void Start()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += UpdateTargetScore;

        _displayedScore = 0;
        _targetScore = 0;
        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateTargetScore;
    }

    private void UpdateTargetScore(int newScore)
    {
        _targetScore = newScore;

        //reinicia la animación si ya estaba en marcha
        if (_scoreRoutine != null)
            StopCoroutine(_scoreRoutine);

        _scoreRoutine = StartCoroutine(AnimateScore());
    }

    private IEnumerator AnimateScore()
    {
        while (_displayedScore != _targetScore)
        {
            if (_displayedScore < _targetScore)
                _displayedScore++;
            else if (_displayedScore > _targetScore)
                _displayedScore--;

            UpdateVisual();
            yield return new WaitForSeconds(_updateSpeed);
        }

        _scoreRoutine = null;
    }

    private void UpdateVisual()
    {
        if (_scoreText != null)
            _scoreText.text = $"{_displayedScore}";
    }
}
