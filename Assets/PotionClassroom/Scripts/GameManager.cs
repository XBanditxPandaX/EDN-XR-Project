using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionClassroom
{
    public class GameManager : MonoBehaviour
    {
        [Header("Partie")]
        [Tooltip("Duree totale de la partie en secondes.")]
        public float gameDuration = 120f;

        [Header("References")]
        public OrderManager orderManager;
        [Tooltip("Transform du parchemin Torah_S — le timer apparait au-dessus.")]
        public Transform torah;
        [Tooltip("Hauteur du timer au-dessus du parchemin.")]
        public float timerHeightAboveTorah = 0.3f;

        // ------------------------------------------------------------------
        private float _timeRemaining;
        private int   _score = 0;
        private bool  _gameRunning = false;

        private GameObject       _hudCanvas;
        private TextMeshProUGUI  _timerText;
        private TextMeshProUGUI  _scoreText;

        // ------------------------------------------------------------------
        private void Start()
        {
            BuildHUD();
            StartGame();
        }

        private void Update()
        {
            if (!_gameRunning) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                EndGame();
            }

            RefreshHUD();
        }

        // ------------------------------------------------------------------
        public void StartGame()
        {
            _timeRemaining = gameDuration;
            _score         = 0;
            _gameRunning   = true;
            RefreshHUD();
        }

        // Appele par OrderManager quand une commande est validee
        public void OnOrderValidated()
        {
            _score++;
            RefreshHUD();
        }

        // ------------------------------------------------------------------
        private void EndGame()
        {
            _gameRunning = false;

            _timerText.text  = "00:00";
            _timerText.color = Color.red;
            _scoreText.text  = $"Termine !\nScore : {_score}";

            if (orderManager != null)
                orderManager.gameObject.SetActive(false);

            Debug.Log($"[GameManager] Partie terminee ! Score final : {_score}");
        }

        private void RefreshHUD()
        {
            int m = Mathf.FloorToInt(_timeRemaining / 60f);
            int s = Mathf.FloorToInt(_timeRemaining % 60f);
            _timerText.text  = $"{m:00}:{s:00}";
            _timerText.color = _timeRemaining < 30f ? Color.red : Color.white;
            _scoreText.text  = $"Score : {_score}";
        }

        private void PositionHUD()
        {
            // Inutilise : le HUD est positionne une fois dans BuildHUD
        }

        // ------------------------------------------------------------------
        private void BuildHUD()
        {
            _hudCanvas = new GameObject("GameHUD");

            // Attache au parchemin si disponible, sinon a la scene
            if (torah != null)
            {
                _hudCanvas.transform.SetParent(torah);
                _hudCanvas.transform.localPosition = Vector3.up * timerHeightAboveTorah;
                _hudCanvas.transform.localRotation = Quaternion.identity;
                _hudCanvas.transform.localScale    = Vector3.one;
            }

            Canvas canvas = _hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _hudCanvas.AddComponent<CanvasScaler>();

            RectTransform rt = _hudCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500f, 160f);
            // Copie la scale du parchemin pour correspondre a sa taille
            _hudCanvas.transform.localScale = torah != null
                ? Vector3.one * 0.002f
                : Vector3.one * 0.002f;

            // --- Timer (haut) ---
            GameObject timerGo = new GameObject("Timer");
            timerGo.transform.SetParent(_hudCanvas.transform, false);
            _timerText = timerGo.AddComponent<TextMeshProUGUI>();
            _timerText.fontSize  = 80;
            _timerText.color     = Color.white;
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.text      = "02:00";
            RectTransform trt = timerGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            // --- Score (bas) ---
            GameObject scoreGo = new GameObject("Score");
            scoreGo.transform.SetParent(_hudCanvas.transform, false);
            _scoreText = scoreGo.AddComponent<TextMeshProUGUI>();
            _scoreText.fontSize  = 60;
            _scoreText.color     = Color.yellow;
            _scoreText.alignment = TextAlignmentOptions.Center;
            _scoreText.text      = "Score : 0";
            RectTransform srt = scoreGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.5f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
        }
    }
}
