using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 屏幕中下方的飘字提示（「金币不足！」「这里不能建造」）。
/// 场景里建一个空物体挂它，下面挂一个 TMP 文本，把引用拖进来。
/// 代码里随便什么地方都能调：ToastMessage.Show("金币不足！");
/// </summary>
public class ToastMessage : MonoBehaviour
{
    public static ToastMessage Instance { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float showTime = 1.6f;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (group == null) Debug.LogWarning("[Toast] group（CanvasGroup）未指定，提示不会淡出。", this);
        else group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string message)
    {
        if (Instance != null) Instance.ShowInternal(message);
        else Debug.Log("[Toast] " + message);
    }

    private void ShowInternal(string message)
    {
        if (label != null) label.text = message;

        if (group == null) return;

        // 物体被关掉时 StartCoroutine 会抛异常，而 Show 是全工程到处在调的
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[Toast] ToastMessage 当前不可用（物体被关闭或组件被禁用），提示被丢弃：" + message);
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(showTime);

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - t / 0.3f;
            yield return null;
        }

        group.alpha = 0f;
        routine = null;
    }
}
