# BUG 记录

> 每次卡住超过 20 分钟，就在这里记一条。
> **这份文档是「这真是你自己做的」最有力的证据**——AI 不记得你昨晚为什么卡了两小时，
> 但你真的调过，就写得出这种细节。
>
> 写法：现象 → 排查顺序 → 原因 → 修复 → 学到什么。
> 不要只写「修好了」，要写「怎么找到的」。

---

## BUG-001 编译不过：`cannot convert from 'Projectile' to 'UnityEngine.GameObject'`

- **现象**：脚本导入后 Unity 一片红，进不了 Play 模式
- **排查**：
  1. 双击 Console 第一条红字，跳到 `Tower.cs` 第 94 行
  2. 那一行是 `PoolManager.Instance.Spawn(Data.projectilePrefab, ...)`
  3. 看 `PoolManager.Spawn` 的签名：第一个参数是 `GameObject`
  4. 看 `TowerData.projectilePrefab` 的类型：是 `Projectile`（组件，不是 GameObject）
- **原因**：组件的引用不能直接当 GameObject 用，要取 `.gameObject`
- **修复**：`var prefabGo = Data.projectilePrefab.gameObject;` 再传 `prefabGo`
- **学到**：Unity 里「组件 ↔ 物体」的转换是高频错误；报错里提到类型不匹配，
  就先去看两边的声明，别急着改逻辑

---

## BUG-002 点「设置」按钮毫无反应，Console 也没有报错

- **现象**：主菜单点设置，什么都没发生；按钮本身是亮的、能点
- **排查**：
  1. 先确认按钮的 `onClick` 有没有绑上 —— 代码里是运行时 `AddListener`，不是 Inspector 绑的
  2. 在 `SettingsPanel.Awake` 顶部加 `Debug.Log` —— **没有打印**
  3. 说明 `Awake` 根本没执行
  4. 回头看 Hierarchy：`SettingsPanel` 这个物体**默认是隐藏的**
- **原因**：Unity 不会对未激活的物体执行 `Awake`/`Start`，所以监听器没绑上
- **修复**：脚本挪到常驻的空物体上，只把子 `Panel` 关掉；
  并在 `Awake` 里加「panel 为空或指向自己就 LogError」的保护
- **学到**：**「没报错但没反应」优先怀疑生命周期没跑到**，不是逻辑写错。
  加一句 `Debug.Log` 定位执行到哪一步，比读十遍代码快

---

## BUG-003 子弹会拐弯去追另一只怪

- **现象**：偶尔看到子弹飞一半突然转向，命中一只刚刷出来的敌人，伤害莫名其妙
- **排查**：
  1. 先怀疑目标没死透 —— 但 `IsAlive` 判断是有的
  2. 打印子弹的 `target.GetInstanceID()`，发现同一个 ID 先死后活
  3. 想通了：对象池 `Despawn` 只是 `SetActive(false)`，**对象没销毁**，
     下一波 `Spawn` 复用的就是同一个实例，`Setup` 又把 `IsAlive` 设回 true
  4. 于是子弹发现「目标又活了」，就继续追
- **原因**：用「引用相等」判断「是不是同一只怪」，在对象池下不成立
- **修复**：给 `Enemy` 加 `SpawnId`，每次 `Setup` 自增；子弹记录发射时的 `SpawnId`，
  不相等就认为目标已消失
- **学到**：**对象池会打破「引用唯一性」这个直觉**。池化对象上任何「记住是谁」的逻辑，
  都要用 ID / 版本号，不能用引用

---

## 模板（复制这段开始写）

```markdown
## BUG-00X 一句话描述现象
- 现象：
- 排查：
  1.
  2.
  3.
- 原因：
- 修复：
- 学到：
```

## BUG-004 精灵在运行时不显示，但物体照常沿路径移动

- 现象：Play 后物体位置在变（每秒日志确认 x 从 -9 涨到 -8.x），
  但 Game 视图里看不到贴图；运行时选中 Enemy，
  发现 Sprite Renderer 的「精灵」字段变成了「无」
- 排查：
  1. 先看编辑状态——场景里精灵是拖好的，有图
  2. 说明不是配置问题，是「运行时被某段代码清掉了」
  3. 全局搜哪里给 sprite 赋值，定位到 Enemy.Setup 里的
     `body.sprite = data.sprite;`
  4. 再看数据来源：DevSpawner 里用 ScriptableObject.CreateInstance
     临时创建 EnemyData，从没设置过 sprite 字段，默认就是 null
- 原因：数据里没配精灵时，初始化代码把场景上已有的精灵覆盖成了空
- 修复：改成 `if (data.sprite != null) body.sprite = data.sprite;`
  没配就保留原图
- 学到：**「编辑时正常、运行时坏」优先怀疑初始化代码把已有值覆盖了**；
  可选项的赋值要判空，不要拿 null 去覆盖已有状态
  