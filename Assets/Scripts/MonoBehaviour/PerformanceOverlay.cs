using System.Text;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace 命名空间名
{
    /// <summary>
    /// 运行时性能 HUD：显示 FPS 与 ECS 实体总数。
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class PerformanceOverlay : MonoBehaviour
    {
        const float FpsRefreshInterval = 0.25f;
        const float EntityRefreshInterval = 0.5f;

        /// <summary>设为 false 可关闭整个性能 HUD（不创建 UI、不更新）。</summary>
        public static bool Enabled = false;

        /// <summary>调整此值即可缩放整个 HUD 文本（字号、区域、边距、阴影）。</summary>
        const int TextSize = 50;
        const float BaseTextSize = 18f;

        Text performanceText;
        readonly StringBuilder stringBuilder = new StringBuilder(64);

        float fpsTimer;
        float fpsAccumulator;
        int fpsFrameCount;
        float displayedFps;

        float entityTimer;
        int displayedEntityCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (!Enabled)
                return;

            if (FindObjectOfType<PerformanceOverlay>() != null)
                return;

            new GameObject(nameof(PerformanceOverlay)).AddComponent<PerformanceOverlay>();
        }

        void Awake()
        {
            if (!Enabled)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            CreateUI();
        }

        void Update()
        {
            if (!Enabled)
                return;

            UpdateFps();
            UpdateEntityCount();
            RefreshLabel();
        }

        void UpdateFps()
        {
            float deltaTime = Time.unscaledDeltaTime;
            fpsAccumulator += deltaTime;
            fpsFrameCount++;
            fpsTimer += deltaTime;

            if (fpsTimer < FpsRefreshInterval)
                return;

            displayedFps = fpsFrameCount / fpsAccumulator;
            fpsTimer = 0f;
            fpsAccumulator = 0f;
            fpsFrameCount = 0;
        }

        void UpdateEntityCount()
        {
            entityTimer += Time.unscaledDeltaTime;
            if (entityTimer < EntityRefreshInterval)
                return;

            entityTimer = 0f;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                displayedEntityCount = 0;
                return;
            }

            displayedEntityCount = world.EntityManager.UniversalQuery.CalculateEntityCount();
        }

        void RefreshLabel()
        {
            stringBuilder.Clear();
            stringBuilder.AppendLine($"FPS: {displayedFps:0.0}");
            stringBuilder.AppendLine($"Entities: {displayedEntityCount}");
            performanceText.text = stringBuilder.ToString();
        }

        void CreateUI()
        {
            var canvasGo = new GameObject("PerformanceOverlayCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("PerformanceText");
            textGo.transform.SetParent(canvasGo.transform, false);

            performanceText = textGo.AddComponent<Text>();
            performanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            performanceText.color = new Color(0.9f, 0.95f, 1f, 1f);
            performanceText.alignment = TextAnchor.UpperLeft;
            performanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
            performanceText.verticalOverflow = VerticalWrapMode.Overflow;
            performanceText.raycastTarget = false;

            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);

            var rectTransform = performanceText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);

            float textScale = TextSize / BaseTextSize;
            performanceText.fontSize = TextSize;
            shadow.effectDistance = new Vector2(1f, -1f) * textScale;
            rectTransform.anchoredPosition = new Vector2(12f, -12f) * textScale;
            rectTransform.sizeDelta = new Vector2(240f, 60f) * textScale;
        }
    }
}
