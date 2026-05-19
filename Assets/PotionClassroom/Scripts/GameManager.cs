using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

namespace PotionClassroom
{
    public class GameManager : MonoBehaviour
    {
        [Header("Audio")]
        public AudioClip ambientMusic;
        [Range(0f, 1f)] public float ambientVolume = 0.3f;

        [Header("Partie")]
        [Tooltip("Duree totale de la partie en secondes.")]
        public float gameDuration = 120f;

        [Header("References")]
        public OrderManager orderManager;
        [Tooltip("Rig XR a deplacer pour le tutoriel. Laisse vide pour le detecter automatiquement.")]
        public Transform playerRig;

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

        [Header("Tutoriel")]
        public bool showTutorialOnStart = true;
        [Tooltip("Si actif, le tutoriel ne reapparait pas apres un restart de scene.")]
        public bool showTutorialOnlyOnce = true;
        [Tooltip("Place le joueur devant le parchemin des commandes au lancement.")]
        public bool placePlayerInFrontOfOrders = true;
        [Tooltip("Hauteur minimale des yeux du joueur pendant le tutoriel.")]
        public float tutorialMinimumEyeHeight = 1.85f;
        [Tooltip("Offset monde applique depuis Torah_S pour placer la camera du joueur au tutoriel.")]
        public Vector3 tutorialCameraOffsetFromOrders = new Vector3(0f, 0.15f, -1.65f);
        [Tooltip("Offset monde du point que le joueur regarde sur Torah_S.")]
        public Vector3 tutorialLookAtOffset = Vector3.zero;
        [Tooltip("Duree pendant laquelle le placement est reapplique apres les updates XR.")]
        public float tutorialPlacementLockDuration = 1f;
        [Tooltip("Affiche l'ecran tutoriel directement sur le parchemin plutot que devant les yeux.")]
        public bool pinTutorialScreenToOrders = true;
        [Tooltip("Distance de l'ecran tutoriel devant les yeux du joueur.")]
        public float tutorialScreenDistance = 1.6f;
        [Tooltip("Hauteur de l'ecran tutoriel devant les yeux du joueur.")]
        public float tutorialScreenVerticalOffset = 0.02f;
        [Tooltip("Taille du titre du tutoriel.")]
        public float tutorialTitleFontSize = 62f;
        [Tooltip("Taille du texte du tutoriel.")]
        public float tutorialBodyFontSize = 34f;
        [Tooltip("Espacement vertical des lignes du texte du tutoriel.")]
        public float tutorialBodyLineSpacing = -10f;
        [Tooltip("Zone du titre dans le canvas tutoriel, en proportions 0-1.")]
        public Vector4 tutorialTitleArea = new Vector4(0.02f, 0.80f, 0.95f, 0.98f);
        [Tooltip("Zone du texte dans le canvas tutoriel, en proportions 0-1.")]
        public Vector4 tutorialBodyArea = new Vector4(0.02f, 0.24f, 0.95f, 0.78f);
        [Tooltip("Zone du bouton Suivant/Start dans le canvas tutoriel, en proportions 0-1.")]
        public Vector4 tutorialStartButtonArea = new Vector4(0.34f, 0.02f, 0.58f, 0.18f);
        [TextArea(2, 4)]
        public string tutorialIntroText = "Bienvenue dans l'atelier de potions.";
        [TextArea(4, 8)]
        public string tutorialRulesText =
            "Lis la commande sur le parchemin, prépare les potions demandées, dépose-les dans le coffre, puis valide avec le bouton.";
        [TextArea(2, 4)]
        public string tutorialScoreText =
            "Chaque commande validée ajoute 1 point. Fais le meilleur score avant la fin du chrono.";
        public string tutorialNextButtonText = "Suivant";
        public string tutorialStartButtonText = "Start";

        // ------------------------------------------------------------------
        private static bool _tutorialAlreadyShown = false;
        private static bool _skipTutorialOnNextLoad = false;

        private float _timeRemaining;
        private int   _score = 0;
        private bool  _gameRunning = false;

        private GameObject       _hudCanvas;
        private TextMeshProUGUI  _timerText;
        private TextMeshProUGUI  _scoreText;
        private GameObject       _gameOverCanvas;
        private TextMeshProUGUI  _gameOverScoreText;
        private GameObject       _tutorialCanvas;
        private TextMeshProUGUI  _tutorialTitleText;
        private TextMeshProUGUI  _tutorialBodyText;
        private TextMeshProUGUI  _tutorialButtonLabelText;
        private Button           _tutorialButton;
        private int              _tutorialPageIndex = 0;
        private float            _tutorialPlacementUntilTime = -1f;
        private bool             _lockPlayerPlacement = false;

        // ------------------------------------------------------------------
        private void Awake()
        {
            if (orderManager != null)
                orderManager.PrepareForTutorial();
        }

        private void Start()
        {
            if (ambientMusic != null)
            {
                AudioSource amb = gameObject.AddComponent<AudioSource>();
                amb.clip         = ambientMusic;
                amb.loop         = true;
                amb.playOnAwake  = false;
                amb.spatialBlend = 0f;
                amb.volume       = ambientVolume;
                amb.Play();
            }

            BuildHUD();
            BuildGameOverScreen();
            BuildTutorialScreen();

            bool shouldShowTutorial = ShouldShowTutorial();
            _skipTutorialOnNextLoad = false;

            if (shouldShowTutorial)
            {
                if (_hudCanvas != null)
                    _hudCanvas.SetActive(false);

                ShowTutorialScreen();
            }
            else
            {
                BeginPlayerPlacementLock();
                StartGame();
            }
        }

        private void Update()
        {
            if (_hudCanvas != null && _hudCanvas.activeSelf)
                PositionHUD();

            if (_tutorialCanvas != null && _tutorialCanvas.activeSelf && !pinTutorialScreenToOrders)
                PositionTutorialScreen();

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

        private void LateUpdate()
        {
            if (!_lockPlayerPlacement) return;

            if (Time.time > _tutorialPlacementUntilTime)
            {
                _lockPlayerPlacement = false;
                return;
            }

            PlacePlayerForTutorial();
        }

        // ------------------------------------------------------------------
        public void StartGame()
        {
            _timeRemaining = gameDuration;
            _score         = 0;
            _gameRunning   = true;

            if (_tutorialCanvas != null)
                _tutorialCanvas.SetActive(false);
            if (_hudCanvas != null)
                _hudCanvas.SetActive(true);
            if (_gameOverCanvas != null)
                _gameOverCanvas.SetActive(false);

            orderManager?.chest?.ClearChest();
            orderManager?.BeginOrders();
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
            _tutorialAlreadyShown = true;
            _skipTutorialOnNextLoad = true;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);
        }

        public void AdvanceTutorial()
        {
            if (_tutorialPageIndex < 2)
            {
                _tutorialPageIndex++;
                RefreshTutorialPage();
                return;
            }

            StartGame();
        }

        // ------------------------------------------------------------------
        private void EndGame()
        {
            _gameRunning = false;

            _timerText.text  = "00:00";
            _timerText.color = Color.red;
            _scoreText.text  = $"Termine !\nScore : {_score}";

            if (_hudCanvas != null)
                _hudCanvas.SetActive(false);

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

        private void PositionTutorialScreen()
        {
            Camera cam = Camera.main;
            if (cam == null || _tutorialCanvas == null) return;

            if (pinTutorialScreenToOrders && orderManager != null && orderManager.displayParent != null)
            {
                Vector3 targetCenter = GetTutorialTargetCenter();
                Vector3 toScreen = targetCenter - cam.transform.position;
                if (toScreen.sqrMagnitude < 0.001f)
                    toScreen = cam.transform.forward;

                _tutorialCanvas.transform.position = targetCenter - toScreen.normalized * 0.03f;
                _tutorialCanvas.transform.rotation = Quaternion.LookRotation(toScreen.normalized, Vector3.up);
                return;
            }

            _tutorialCanvas.transform.position = cam.transform.position
                + cam.transform.forward * tutorialScreenDistance
                + cam.transform.up * tutorialScreenVerticalOffset;
            _tutorialCanvas.transform.rotation = cam.transform.rotation;
        }

        private bool ShouldShowTutorial()
        {
            return showTutorialOnStart && !_skipTutorialOnNextLoad && (!showTutorialOnlyOnce || !_tutorialAlreadyShown);
        }

        private void BeginPlayerPlacementLock()
        {
            PlacePlayerForTutorial();
            _tutorialPlacementUntilTime = Time.time + Mathf.Max(0.1f, tutorialPlacementLockDuration);
            _lockPlayerPlacement = true;
        }

        private void PlacePlayerForTutorial()
        {
            if (!placePlayerInFrontOfOrders || orderManager == null || orderManager.displayParent == null)
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Transform rig = playerRig != null ? playerRig : FindPlayerRig(cam.transform);
            if (rig == null) return;

            Vector3 targetCenter = GetTutorialTargetCenter();
            Vector3 desiredCameraPosition = targetCenter + tutorialCameraOffsetFromOrders;
            desiredCameraPosition.y = Mathf.Max(desiredCameraPosition.y, tutorialMinimumEyeHeight);

            Vector3 desiredForward = targetCenter - desiredCameraPosition;
            desiredForward.y = 0f;

            Vector3 currentForward = cam.transform.forward;
            currentForward.y = 0f;

            if (desiredForward.sqrMagnitude > 0.001f && currentForward.sqrMagnitude > 0.001f)
            {
                float yaw = Vector3.SignedAngle(currentForward.normalized, desiredForward.normalized, Vector3.up);
                rig.RotateAround(cam.transform.position, Vector3.up, yaw);
            }

            rig.position += desiredCameraPosition - cam.transform.position;
        }

        private Vector3 GetTutorialTargetCenter()
        {
            return orderManager.displayParent.position + tutorialLookAtOffset;
        }

        private Transform FindPlayerRig(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                if (current.name.Contains("XR Origin"))
                    return current;
                current = current.parent;
            }

            return start.root;
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

        private void BuildTutorialScreen()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 0;

            _tutorialCanvas = new GameObject("TutorialScreen");
            _tutorialCanvas.layer = uiLayer;

            Canvas canvas = _tutorialCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _tutorialCanvas.AddComponent<CanvasScaler>();
            _tutorialCanvas.AddComponent<TrackedDeviceGraphicRaycaster>();

            RectTransform rt = _tutorialCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(620f, 380f);
            _tutorialCanvas.transform.localScale = Vector3.one * 0.002f;

            GameObject panelGo = new GameObject("Panel");
            panelGo.layer = uiLayer;
            panelGo.transform.SetParent(_tutorialCanvas.transform, false);

            Image panel = panelGo.AddComponent<Image>();
            panel.color = Color.clear;
            panel.raycastTarget = false;

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            Color inkColor = new Color(0.01f, 0.006f, 0.002f, 1f);

            _tutorialTitleText = CreateText(panelGo.transform, "Title", "TUTORIEL", tutorialTitleFontSize, inkColor,
                TextAlignmentOptions.Center, AreaMin(tutorialTitleArea), AreaMax(tutorialTitleArea), uiLayer);
            _tutorialTitleText.fontStyle = FontStyles.Bold;

            _tutorialBodyText = CreateText(panelGo.transform, "Instructions", tutorialIntroText, tutorialBodyFontSize, inkColor,
                TextAlignmentOptions.MidlineLeft, AreaMin(tutorialBodyArea), AreaMax(tutorialBodyArea), uiLayer);
            _tutorialBodyText.fontStyle = FontStyles.Bold;
            _tutorialBodyText.lineSpacing = tutorialBodyLineSpacing;

            GameObject buttonGo = new GameObject("StartButton");
            buttonGo.layer = uiLayer;
            buttonGo.transform.SetParent(panelGo.transform, false);

            Image buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.36f, 0.88f, 0.22f, 1f);

            _tutorialButton = buttonGo.AddComponent<Button>();
            _tutorialButton.targetGraphic = buttonImage;
            _tutorialButton.colors = new ColorBlock
            {
                normalColor = new Color(0.36f, 0.88f, 0.22f, 1f),
                highlightedColor = new Color(0.58f, 1f, 0.4f, 1f),
                pressedColor = new Color(0.18f, 0.62f, 0.1f, 1f),
                selectedColor = new Color(0.45f, 0.95f, 0.28f, 1f),
                disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            _tutorialButton.onClick.AddListener(AdvanceTutorial);

            RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
            buttonRt.anchorMin = AreaMin(tutorialStartButtonArea);
            buttonRt.anchorMax = AreaMax(tutorialStartButtonArea);
            buttonRt.offsetMin = Vector2.zero;
            buttonRt.offsetMax = Vector2.zero;

            _tutorialButtonLabelText = CreateText(buttonGo.transform, "Label", tutorialNextButtonText, 28, Color.black, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, uiLayer);

            _tutorialCanvas.SetActive(false);
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
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return tmp;
        }

        private Vector2 AreaMin(Vector4 area) => new Vector2(area.x, area.y);

        private Vector2 AreaMax(Vector4 area) => new Vector2(area.z, area.w);

        private void RefreshTutorialPage()
        {
            if (_tutorialTitleText != null)
                _tutorialTitleText.gameObject.SetActive(_tutorialPageIndex == 0);

            if (_tutorialBodyText != null)
            {
                _tutorialBodyText.text = _tutorialPageIndex switch
                {
                    0 => tutorialIntroText,
                    1 => tutorialRulesText,
                    _ => tutorialScoreText
                };
            }

            if (_tutorialButtonLabelText != null)
                _tutorialButtonLabelText.text = _tutorialPageIndex < 2 ? tutorialNextButtonText : tutorialStartButtonText;
        }

        private void ShowGameOverScreen()
        {
            if (_gameOverCanvas == null) return;

            if (_gameOverScoreText != null)
                _gameOverScoreText.text = $"Score final : {_score}";

            _gameOverCanvas.SetActive(true);
            PositionGameOverScreen();
        }

        private void ShowTutorialScreen()
        {
            if (_tutorialCanvas == null) return;

            _tutorialAlreadyShown = true;
            _tutorialPageIndex = 0;
            RefreshTutorialPage();
            BeginPlayerPlacementLock();
            _tutorialCanvas.SetActive(true);
            PositionTutorialScreen();
        }
    }
}
