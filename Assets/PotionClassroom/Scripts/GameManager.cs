using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
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

        [Header("HUD")]
        [Tooltip("Distance du HUD devant les yeux du joueur.")]
        public float hudDistance = 2f;
        [Tooltip("Hauteur du HUD (positif = haut de l'ecran).")]
        public float hudVerticalOffset = 0.85f;

        [Header("Game Over")]
        [Tooltip("Distance de l'ecran Game Over devant les yeux du joueur.")]
        public float gameOverDistance = 2f;
        [Tooltip("Hauteur de l'ecran Game Over devant les yeux du joueur.")]
        public float gameOverVerticalOffset = 0.05f;

        // ------------------------------------------------------------------
        private float _timeRemaining;
        private int   _score = 0;
        private bool  _gameRunning = false;

        private GameObject       _hudCanvas;
        private TextMeshProUGUI  _timerText;
        private TextMeshProUGUI  _scoreText;
        private GameObject       _gameOverCanvas;
        private TextMeshProUGUI  _gameOverScoreText;

        // ------------------------------------------------------------------
        private void Start()
        {
            BuildHUD();
            BuildGameOverScreen();
            StartGame();
        }

        private void Update()
        {
            PositionHUD();
            if (_gameOverCanvas != null && _gameOverCanvas.activeSelf)
                PositionGameOverScreen();

            if (!_gameRunning) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                EndGame();
                return;
            }

            RefreshHUD();
        }

        // ------------------------------------------------------------------
        public void StartGame()
        {
            _timeRemaining = gameDuration;
            _score         = 0;
            _gameRunning   = true;
            if (_gameOverCanvas != null)
                _gameOverCanvas.SetActive(false);
            RefreshHUD();
        }

        // Appele par OrderManager quand une commande est validee
        public void OnOrderValidated()
        {
            if (!_gameRunning) return;

            _score++;
            RefreshHUD();
        }

        public void RestartGame()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);
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

            ShowGameOverScreen();

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
            Camera cam = Camera.main;
            if (cam == null || _hudCanvas == null) return;

            _hudCanvas.transform.position = cam.transform.position
                + cam.transform.forward * hudDistance
                + cam.transform.up * hudVerticalOffset;
            _hudCanvas.transform.rotation = cam.transform.rotation;
        }

        private void PositionGameOverScreen()
        {
            Camera cam = Camera.main;
            if (cam == null || _gameOverCanvas == null) return;

            _gameOverCanvas.transform.position = cam.transform.position
                + cam.transform.forward * gameOverDistance
                + cam.transform.up * gameOverVerticalOffset;
            _gameOverCanvas.transform.rotation = cam.transform.rotation;
        }

        // ------------------------------------------------------------------
        private void BuildHUD()
        {
            _hudCanvas = new GameObject("GameHUD");

            Canvas canvas = _hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _hudCanvas.AddComponent<CanvasScaler>();

            RectTransform rt = _hudCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500f, 160f);
            _hudCanvas.transform.localScale = Vector3.one * 0.002f;

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

        private void BuildGameOverScreen()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 0;

            _gameOverCanvas = new GameObject("GameOverScreen");
            _gameOverCanvas.layer = uiLayer;

            Canvas canvas = _gameOverCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _gameOverCanvas.AddComponent<CanvasScaler>();
            _gameOverCanvas.AddComponent<TrackedDeviceGraphicRaycaster>();

            RectTransform rt = _gameOverCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, 260f);
            _gameOverCanvas.transform.localScale = Vector3.one * 0.002f;

            GameObject panelGo = new GameObject("Panel");
            panelGo.layer = uiLayer;
            panelGo.transform.SetParent(_gameOverCanvas.transform, false);
            Image panel = panelGo.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.82f);
            panel.raycastTarget = false;

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            CreateText(panelGo.transform, "Title", "GAME OVER", 48, Color.red, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.93f), uiLayer);

            _gameOverScoreText = CreateText(panelGo.transform, "FinalScore", "Score final : 0", 30, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.62f), uiLayer);

            GameObject buttonGo = new GameObject("RestartButton");
            buttonGo.layer = uiLayer;
            buttonGo.transform.SetParent(panelGo.transform, false);

            Image buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.98f, 0.78f, 0.18f, 1f);

            Button restartButton = buttonGo.AddComponent<Button>();
            restartButton.targetGraphic = buttonImage;
            restartButton.colors = new ColorBlock
            {
                normalColor = new Color(0.98f, 0.78f, 0.18f, 1f),
                highlightedColor = new Color(1f, 0.94f, 0.45f, 1f),
                pressedColor = new Color(0.9f, 0.55f, 0.08f, 1f),
                selectedColor = new Color(1f, 0.86f, 0.28f, 1f),
                disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            restartButton.onClick.AddListener(RestartGame);

            RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.26f, 0.12f);
            buttonRt.anchorMax = new Vector2(0.74f, 0.34f);
            buttonRt.offsetMin = Vector2.zero;
            buttonRt.offsetMax = Vector2.zero;

            CreateText(buttonGo.transform, "Label", "Relancer", 28, Color.black, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, uiLayer);

            _gameOverCanvas.SetActive(false);
        }

        private TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int layer)
        {
            GameObject go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return tmp;
        }

        private void ShowGameOverScreen()
        {
            if (_gameOverCanvas == null) return;

            if (_gameOverScoreText != null)
                _gameOverScoreText.text = $"Score final : {_score}";

            _gameOverCanvas.SetActive(true);
            PositionGameOverScreen();
        }
    }
}
