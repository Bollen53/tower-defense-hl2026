# 02 C# 速成（面向 Python / C 背景）

> 目标：**3 天内能看懂并修改本套塔防代码**，不是学完 C#。
> 你有 C 基础，所以「类型、指针、编译」这些概念不用重学；C# 的坑主要在**面向对象 + 引用类型 + Unity 的特殊规则**。
> 建议：边读边在 Unity 里开个空场景试，不要干读。

---

## 1. 三分钟对照表

| 你在 Python / C 里写的 | C# 里写成 | 备注 |
|---|---|---|
| `def foo(a, b=1):` | `void Foo(int a, int b = 1) { }` | 必须写参数类型和返回类型 |
| `self.x = 1` | `this.x = 1;` 或直接 `x = 1;` | `this` 可省略（同 C++） |
| `x = 3` | `int x = 3;` / `var x = 3;` | `var` 是「自动推断类型」，不是动态类型 |
| `print(x)` | `Debug.Log(x);` | Unity 里看 Console 窗口 |
| `f"{a} 个"` | `$"{a} 个"` | 字符串插值，最常用 |
| `if x is None:` | `if (x == null)` | ⚠️ Unity 对象有「假 null」，见第 5 节 |
| `[1,2,3]` | `new List<int> { 1, 2, 3 }` | 数组 `int[] a = new int[3]` 长度固定 |
| `{"k": 1}` | `new Dictionary<string,int>()` | 需要 `using System.Collections.Generic;` |
| `for i in range(3):` | `for (int i = 0; i < 3; i++)` | 也有 `foreach (var e in list)` |
| `class A(B):` | `class A : B` | 单继承，可多接口 |
| `@decorator` | `[Attribute]` | 方括号写在成员上面 |
| `lambda x: x*2` | `x => x * 2` | 委托 / 事件用 |
| `try/except` | `try/catch` | 游戏里少用，异常很慢 |
| `//` 整除 | `/` 对 int 就是整除 | 想得小数至少一边是 float |

---

## 2. 你真正要新学的 6 个概念

### ① 类、字段、属性

```csharp
public class Tower
{
    private int level = 1;              // 字段：类内部用
    public int Level { get; private set; } = 1;   // 属性：外面能读，只有自己能写

    public int UpgradeCost => 60;       // 表达式属性（只读，等价于 get { return 60; }）
}
```

塔防代码里到处是 `public int Gold { get; private set; }`：
**「别人只能看，改必须走我的方法」**——这就是「封装」，答辩可以提一句。

### ② 事件（观察者模式）

这是本套代码用得最多、也是你最陌生的东西：

```csharp
// 发布者（GameManager）
public event Action<int> GoldChanged;      // 「金币变了」这个事件，携带一个 int
GoldChanged?.Invoke(Gold);                 // 触发（? 表示没人订阅时不报错）

// 订阅者（HUD）
GameManager.Instance.GoldChanged += OnGoldChanged;   // 订阅
GameManager.Instance.GoldChanged -= OnGoldChanged;   // 退订（重要！）
```

为什么要这样？如果不这样，HUD 就得每帧 `if (gold != lastGold)` 去轮询，
而且游戏逻辑和 UI 会互相引用成一团。用事件，**GameManager 根本不知道 HUD 存在**。

> 记忆法：`+=` 是「我也要听」，`-=` 是「我不听了」。对象销毁前一定要 `-=`，否则会持有已销毁对象的引用。

### ③ 接口

```csharp
public interface IDamageable
{
    void TakeDamage(float amount, DamageType type);
}

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public void TakeDamage(float amount, DamageType type) { /* 实现 */ }
}
```

塔的代码里只写「目标是 `IDamageable`」，不需要知道它是敌人还是别的什么。
这就是赛题进阶里「减少模块间不必要的直接引用」。

### ④ 协程（Coroutine）—— Unity 特有，最重要

C 里你写不了「等 1 秒再继续」，Unity 用协程：

```csharp
private IEnumerator SpawnWave()
{
    yield return new WaitForSeconds(1f);   // 等 1 秒，期间游戏继续跑
    SpawnEnemy();
    yield return null;                     // 等下一帧
}
```

启动：`StartCoroutine(SpawnWave());`
**关键理解**：`yield return` 不是「暂停整个游戏」，是「这个方法先让出控制权，下帧/过几秒再回来继续」。

本套代码的波次调度、淡入淡出、冲刺加速、飘字，全是协程。

> 坑：`WaitForSeconds` 会受 `Time.timeScale` 影响（暂停时不动，这正是我们要的）；
> 而 `WaitForSecondsRealtime` 不受影响（飘字提示用它）。

### ⑤ ScriptableObject（数据资产）

```csharp
[CreateAssetMenu(fileName = "Enemy_", menuName = "TD/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public float maxHealth = 100f;
    public float moveSpeed = 1.5f;
}
```

写完这个脚本，在 Project 窗口右键 → `Create > TD > Enemy Data`，就得到一个**资产文件**。
改数值不用改代码、不用重新编译，策划（你自己）在 Inspector 里拖着调就行。

这是赛题「数据与逻辑分离」的标准答案。

### ⑥ `[SerializeField]` 与 Inspector

```csharp
[SerializeField] private TMP_Text goldText;   // private，但会显示在 Inspector 里让你拖
public int startGold = 160;                   // public 也会显示
[HideInInspector] public int temp;            // public 但不显示
```

**这是 Unity 新手第一大坑**：`private` 字段如果不加 `[SerializeField]`，Inspector 里根本看不到，
你拖不了引用，运行就空指针。本套代码里所有需要在编辑器里拖的字段都加了。

---

## 3. Unity 的固定套路（背下来就行）

```csharp
using UnityEngine;

public class MyThing : MonoBehaviour
{
    [SerializeField] private float speed = 1f;   // 1. 需要拖的引用/要调的数值

    private void Awake()   { }   // 2. 最先执行，做初始化（拿组件、设单例）
    private void Start()   { }   // 3. 第一帧前，访问别的对象最安全
    private void Update()  { }   // 4. 每帧，处理输入、移动、计时
    private void OnDestroy() { } // 5. 销毁时退订事件
}
```

- `MonoBehaviour` 是「能挂到游戏物体上的脚本」的基类。**一个 .cs 文件名必须和 public class 名一致**。
- `transform.position` 是物体位置，`Time.deltaTime` 是上一帧耗时。
- **所有和帧率无关的移动都要乘 `Time.deltaTime`**：
  `transform.position += dir * speed * Time.deltaTime;`
  忘了乘，游戏在 120fps 下会比 60fps 快一倍。

---

## 4. 本节只讲塔防会用到的 API

```csharp
// 向量
Vector2 a = new Vector2(1, 2);
Vector2.MoveTowards(pos, target, maxDistanceDelta);   // 朝目标移动，不超速（敌人移动就用它）
Vector2.Distance(a, b);
(a - b).normalized;          // 方向
(a - b).sqrMagnitude;        // 距离平方，比 Distance 快，只比较大小就用它

// 数学
Mathf.Clamp01(x);  Mathf.Max(a, b);  Mathf.CeilToInt(x);  Mathf.Lerp(a, b, t);
Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;                  // 算朝向角度

// 时间
Time.deltaTime;  Time.timeScale = 0f;                 // 暂停

// 对象
Instantiate(prefab, pos, rot);
Destroy(gameObject);
GetComponent<Tower>();
GetComponentInChildren<SpriteRenderer>();

// 输入（旧版 Input Manager）
Input.GetMouseButtonDown(0);                          // 左键按下那一帧
Input.GetKeyDown(KeyCode.Escape);

// 调试
Debug.Log("...");
Debug.LogWarning("...");
Debug.LogError("...");
```

---

## 5. 五个必踩的坑（提前知道能省一周）

### 坑 1：Unity 的「假 null」

`Destroy(obj)` 之后，C# 引用其实还在，但 Unity 把它标记成「已销毁」。

```csharp
if (obj != null)         // ✅ Unity 重载了 !=，会正确判断「已销毁」
if (obj is null)         // ❌ 这是纯 C# 判断，已销毁的对象也会返回 false（认为不为空）
if (obj?.Something)      // ⚠️ 空引用判断用的是纯 C# 规则，可能抛 MissingReferenceException
```

**结论：Unity 对象判空一律用 `if (x != null)` / `if (x == null)`。**

### 坑 2：Awake 顺序不可控

A 的 `Awake` 里可能拿不到 B 在 `Awake` 里设的单例。
本套代码的做法：**`Awake` 里只设自己，`Start` 里才去访问别人**。
所以 `HUD` 的订阅写在 `Start` 里，不写在 `OnEnable`。

### 坑 3：`Time.timeScale = 0` 之后什么都停了

暂停后 `Update` 还在跑，但 `Time.deltaTime == 0`、`WaitForSeconds` 永远等不到。
所以：
- 暂停菜单的动画、飘字，要用 `Time.unscaledDeltaTime` / `WaitForSecondsRealtime`
- 退出场景前一定要 `Time.timeScale = 1f`，否则编辑器里下次 Play 还是 0 倍速

### 坑 4：`==` 比的是引用，不是内容

```csharp
string a = "ab"; string b = "a" + "b";
a == b        // ✅ string 重载了 ==，比内容，true
someList == otherList   // ❌ 比引用
```

### 坑 5：值类型默认值是 0，引用类型是 null

```csharp
int x;            // 0（不会报错，容易掩盖逻辑错误）
Tower t;          // null，用了就 NullReferenceException
```

`NullReferenceException` 是 Unity 里 90% 的报错。看到它，**先看 Console 里的行号，再看那个字段有没有拖引用**。

---

## 6. 三天练习（每个 20~40 分钟，直接在本套代码上改）

**Day 1**
1. 把 `EnemyData.maxHealth` 从 100 改成 300，在 Inspector 里看敌人在游戏里变肉。
2. 在 `Tower.Fire` 里加一行 `Debug.Log($"开火，伤害 {Stats.damage}");`，观察 Console 输出频率。
3. 把 `GameManager.startGold` 改成 999，验证建造栏按钮不再变灰。

**Day 2**
4. 给 `Enemy.ApplySlow` 加一行 `Debug.Log($"{name} 被减速到 {multiplier}");`。
5. 在 `WaveManager.SpawnEnemy` 里打印当前波次，确认 5 波都出怪了。
6. 把 `PauseMenu.Update` 里的 `KeyCode.Escape` 改成 `KeyCode.P`，验证暂停键变了。

**Day 3**
7. 给 `HUD` 加一个「击杀数」显示：`GameManager` 加一个 `KillCount` 属性和 `KillCountChanged` 事件，
   `Enemy.HandleDied` 里 `AddKill()`，HUD 订阅刷新。
   **这道题做出来，说明你已经会「事件驱动」了，答辩能讲。**
8. 新建一个 `EnemyData`（快、血少、奖励高），塞进第 1 波，观察手感变化。

> 做完这 8 个，你就已经能读懂本套代码 80% 的内容了。
> 剩下的 20%（对象池、协程调度、事件解耦）在改代码的过程中自然就懂了。
