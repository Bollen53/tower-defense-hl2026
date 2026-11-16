# 10 Git 安装与首次提交（Windows）

> 症状：`git : 无法将"git"项识别为 cmdlet...`
> 原因：机器上没装 Git，或者装了但没进 PATH。
> 下面的步骤按顺序做，5 分钟搞定。

---

## 一、安装 Git（二选一）

### 方式 A：一条命令（推荐，Win10 21H2 / Win11 都有 winget）

**新开一个 PowerShell 窗口**（不要用刚才那个），执行：

```powershell
winget install --id Git.Git -e --source winget
```

看到 `已成功安装` 就行。中途可能弹 UAC，点是。

> 如果报「无法找到源」或卡住：
> ```powershell
> winget source update
> winget install --id Git.Git -e
> ```

### 方式 B：官网下载（winget 不可用时）

1. 打开 <https://git-scm.com/download/win>
2. 下载 `64-bit Git for Windows Setup`
3. 一路 `Next`，**所有选项保持默认**（默认值就是对的，别改）
4. 最后 `Install` → `Finish`

---

## 二、⚠️ 一定要「关掉再重开」终端

装完 Git 后，**刚才那个 PowerShell 窗口里仍然没有 git**——因为 PATH 是窗口启动时读的。

```
关闭所有 PowerShell 窗口 → 重新打开 → 执行：
```

```powershell
git --version
```

看到 `git version 2.x.x.windows.x` 才算成功。

> 如果重开还是没有：重启一次电脑（最省事），或者手动加 PATH：
> `C:\Program Files\Git\cmd` 加到「系统属性 → 环境变量 → Path」。

---

## 三、配置身份（第一次用必须做）

不配的话，第一次 `commit` 会报 `Please tell me who you are`。

```powershell
git config --global user.name "你的名字或昵称"
git config --global user.email "你的GitHub邮箱"
git config --global init.defaultBranch main
git config --global core.autocrlf true
```

| 配置 | 作用 |
|---|---|
| `user.name` / `user.email` | 写进每条 commit 的作者信息，**评委会看这个** |
| `init.defaultBranch main` | 新仓库默认分支叫 `main`（和 GitHub 一致，省得后面改名） |
| `core.autocrlf true` | Windows 换行符自动转换，避免整个仓库标红 |

检查：

```powershell
git config --global --list
```

---

## 四、初始化并提交（你刚才卡住的那步）

```powershell
cd D:\Unity\TowerDefense
git init
```

把 `.gitignore` 放进去（**这一步别漏**，否则 `Library/` 几万个文件会被提交）：

```powershell
Copy-Item "C:\Users\lbw13\Documents\deepseek-harness\default-workspace\TowerDefenseKit\gitignore_unity.txt" .gitignore
```

放文档、提交：

```powershell
New-Item -ItemType Directory -Force Docs, Tools | Out-Null
Copy-Item "C:\Users\lbw13\Documents\deepseek-harness\default-workspace\TowerDefenseKit\Docs\*.md" Docs\
Copy-Item "C:\Users\lbw13\Documents\deepseek-harness\default-workspace\TowerDefenseKit\Tools\*.py" Tools\

git add .
git status                  # 看一眼：应该只有 .gitignore / Docs / Tools，没有 Library
git commit -m "chore: 初始化项目结构与开发计划文档"
```

---

## 五、推到 GitHub

1. 网页上新建仓库 `tower-defense-hl2026`，**Public**，**不要**勾 README
2. 回到 PowerShell：

```powershell
git remote add origin https://github.com/<你的用户名>/tower-defense-hl2026.git
git push -u origin main
```

第一次 push 会弹一个**浏览器登录窗口**（Git Credential Manager，装 Git 时自带）。
点 `Sign in with your browser` → 登录 GitHub → 授权 → 回来就推上去了。

> 不需要手动搞 Personal Access Token。如果没弹窗、而是命令行要密码：
> 去 GitHub → Settings → Developer settings → Personal access tokens → 生成一个勾 `repo` 的 token，
> 用户名填 GitHub 用户名，密码处粘贴 token。

---

## 六、报错对照表

| 报错 | 原因 | 解决 |
|---|---|---|
| `无法将"git"项识别为…` | 没装 / PATH 没刷新 | 装完**重开终端**；还不行重启电脑 |
| `Please tell me who you are` | 没配 user.name/email | 回到第三节 |
| `fatal: not a git repository` | 不在工程目录 | `cd D:\Unity\TowerDefense` 再执行 |
| `remote origin already exists` | 重复添加远端 | `git remote set-url origin <新地址>` |
| `failed to push some refs` / `rejected` | 远端已有内容 | `git pull --rebase origin main` 再 push |
| `Authentication failed` | 账号密码方式已废弃 | 用浏览器登录，或改用 token |
| 提交后发现 `Library/` 进去了 | 忘了 .gitignore 就 add 了 | `git rm -r --cached Library` → 再 commit |
| 中文文件名显示成乱码 | Git 默认转义 | `git config --global core.quotepath false` |

---

## 七、验证你真的成功了

```powershell
git log --oneline
git remote -v
```

应该看到：
- 一条 commit：`chore: 初始化项目结构与开发计划文档`
- 两行 remote（fetch / push 指向你的仓库）

然后刷新 GitHub 页面，能看到 `Docs/` 里的文档——**这就是你的第一条开发记录**。

---

## 八、不想用命令行？

装 **GitHub Desktop**：<https://desktop.github.com/>
图形界面点几下就能 commit / push，适合完全不想碰命令的情况。

但建议还是把上面这套命令走一遍——**答辩时如果被问「你怎么用 Git 的」，能说清 `add / commit / push` 比说「我点了个按钮」有说服力**。
