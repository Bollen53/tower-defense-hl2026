using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景淡入淡出 + 异步加载。
/// 不需要手动摆到场景里：游戏启动时它会自己建一个常驻（DontDestroyOnLoad）的黑幕 Canvas。
/// 用法：SceneFader.Instance.LoadScene("Level_01");
///
/// 如果你非要在场景里手动放一个，也能用：Awake 会自己补建 UI，重复的实例会被销毁。
/// </summary>
public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;

    public static SceneFader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SceneFader>();
                if (instance == null) Bootstrap();
            }
            return instance;
        }
    }

    private CanvasGroup group;
    private bool busy;

    /// <summary>游戏开始前自动创建，保证第一个场景也有淡入效果。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        var existing = FindFirstObjectByType<SceneFader>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        var go = new GameObject("SceneFader");
        DontDestroyOnLoad(go);
        go.AddComponent<SceneFader>();   // Awake 里建 UI、设 instance
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (group == null) BuildUI();
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760; // 盖住所有 UI

        var img = new GameObject("Black", typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(canvasGo.transform, false);
        img.color = Color.black;
        // 必须是 true：转场期间用它挡住鼠标，防止黑屏时还能点地图建塔
        img.raycastTarget = true;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
    }

    private IEnumerator Start()
    {
        if (group == null) BuildUI();
        if (group == null) yield break;

        // 进场景时从黑到透明
        yield return FadeTo(0f, 0.4f);
        group.blocksRaycasts = false;
    }

    public void LoadScene(string sceneName)
    {
        if (busy) return;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneFader] 场景名是空的。");
            return;
        }

        busy = true;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        if (group == null) BuildUI();

        if (group == null)
        {
            Debug.LogError("[SceneFader] 黑幕 Canvas 创建失败，已跳过转场。");
            busy = false;
            yield break;
        }

        group.blocksRaycasts = true;
        yield return FadeTo(1f, 0.3f);

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            // 场景没加进 Build Settings 时返回 null，Editor 只会打一条 Error
            Debug.LogError($"[SceneFader] 场景 '{sceneName}' 加载失败：" +
                           "检查 File > Build Settings 里是否已加入，且名字完全一致。");
            yield return FadeTo(0f, 0.3f);
            group.blocksRaycasts = false;
            busy = false;
            yield break;
        }

        while (!op.isDone) yield return null;

        // SceneFader 是 DontDestroyOnLoad 的，跨场景只有这一个实例，
        // 所以「淡入」必须在加载完成后由这里负责，而不是新场景的 Start。
        yield return FadeTo(0f, 0.4f);

        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        if (group == null) yield break;

        float start = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // 用不受暂停影响的时间
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        group.alpha = target;
    }
}
