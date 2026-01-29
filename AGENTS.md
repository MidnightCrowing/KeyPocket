# KeyPocket 开发指南

> 给 AI 助手和开发者的项目规范手册

## 🏗️ 架构设计

### MVVM 模式
本项目严格遵循 MVVM（Model-View-ViewModel）架构：

- **Model** (`KeyPocket.Core/Models`): 纯粹的数据结构，不依赖 UI 框架
- **View** (`*.xaml`): 只负责 UI 展示，通过数据绑定与 ViewModel 通信
- **ViewModel** (`ViewModels/`): 业务逻辑和状态管理，使用 `CommunityToolkit.Mvvm`

**核心原则**：
- View 永远不直接访问 Model
- ViewModel 不引用任何 UI 控件（`Button`、`TextBox` 等）
- 使用 `ICommand`、`ObservableProperty` 和消息传递（`WeakReferenceMessenger`）实现解耦

### 分层结构
```
KeyPocket.Core/          # 核心业务逻辑层（无 UI 依赖）
├── Models/              # 数据模型
├── Services/            # 业务服务（加密、存储）
└── Crypto/              # 加密工具（仅使用 Windows DPAPI）

KeyPocket.UI/            # WinUI 3 用户界面层
├── ViewModels/          # 视图模型
├── Pages/               # 页面视图
├── Helpers/             # UI 辅助类
└── Converters/          # 值转换器
```

---

## 🔒 安全约束

### ⚠️ 禁止网络通信
**绝对不要在项目中添加任何形式的网络请求代码**（`HttpClient`、`WebClient`、API 调用等）。

**原因**：
- KeyPocket 管理的是用户的 API 密钥，一旦通过网络发送（即使是"检查更新"或"遥测数据"），都会造成**不可逆的安全风险**
- 用户不会再也不会信任一个"偷偷联网"的密钥管理工具

**不允许例外情况**

### 加密方式
- 只使用 Windows DPAPI（`ProtectedData.Protect/Unprotect`）
- 不要引入自定义加密算法或第三方加密库
- 密钥加密后存储在本地 JSON 文件中

---

## 📝 Git 提交规范

### 提交标题格式
```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Type 类型（必填）
| Type | 说明 | 示例 |
|------|------|------|
| `feat` | 新功能 | `feat(search): add fuzzy search for providers` |
| `fix` | Bug 修复 | `fix(theme): resolve icon crash on theme change` |
| `docs` | 文档更新 | `docs: update README with setup instructions` |
| `style` | 代码格式（不影响逻辑） | `style: format code with Prettier` |
| `refactor` | 重构 | `refactor(icon): extract ProviderIconHelper` |
| `test` | 测试相关 | `test: add unit tests for encryption service` |
| `chore` | 构建/工具链 | `chore: update NuGet packages` |
| `perf` | 性能优化 | `perf(search): reduce query latency by 40%` |
| `ci` | CI/CD 配置 | `ci: add GitHub Actions workflow` |

### Scope（可选但推荐）
指明改动的模块或组件：
- `search` - 搜索功能
- `theme` - 主题系统
- `icon` - 图标管理
- `crypto` - 加密逻辑
- `settings` - 设置页面
- `dock` - 停靠窗口

### 标题要求
- ✅ **必须使用英文**
- ✅ 动词使用现在时（`add` 而非 `added`）
- ✅ 首字母小写
- ✅ 不超过 72 字符
- ✅ 不以句号结尾

### 提交签名验证（强制）
如果你计划将代码提交到 GitHub 仓库：
- ✅ **必须对代码进行签名**，提交时显示 "Verified" 标记
- ❌ 未签名的提交将**无法合并**到主分支
- 💡 配置方法：[GitHub GPG 签名指南](https://docs.github.com/en/authentication/managing-commit-signature-verification)

### Body 和 Footer（可选）
如果需要详细说明，可以添加正文和页脚：
```
fix(provider): prevent duplicate API keys

- Add validation before inserting new keys
- Display error message when duplicate detected
- Update unit tests to cover edge cases

Related PR: https://github.com/user/repo/pull/123
Fixes #456
```

---

## 🛠️ 开发最佳实践

### 辅助类设计
创建 Helper 类时，遵循以下原则：
- **纯函数优先**：避免隐式依赖全局状态（如直接访问 `ThemeHelper.IsDarkTheme()`）
- **线程安全**：确保方法可以在任何线程调用（参数传递而非内部读取 UI 状态）
- **单一职责**：一个 Helper 只做一件事（`ProviderIconHelper` 管图标，`CrashLogHelper` 管日志）

示例：
```csharp
// ✅ 好的设计：参数化，线程安全
public static Uri GetPresetIconUri(string presetName, bool isDark)

// ❌ 不推荐：内部读取全局状态，只能在 UI 线程调用
public static Uri GetPresetIconUri(string presetName)
{
    var isDark = ThemeHelper.IsDarkTheme(); // 隐式依赖
    // ...
}
```

### 图标资源约定
- 预设图标放在 `Assets/ProviderIcons/`
- 命名格式：`{name}-{theme}.png`（如 `openai-dark.png`、`openai-light.png`）
- 如果只有单一版本，使用 `{name}.png`（如 `default.png`）
- 通过 `ProviderIconHelper` 统一访问，不要直接拼接路径

### ViewModel 生命周期
- 避免在 ViewModel 持有 UI 资源（`BitmapImage`、`ImageSource` 等）的长期引用
- 如需更新图标，重新创建对象而非修改旧对象（避免 `ObjectDisposedException`）
- 手动实现属性时跳过相等性检查（如 `CustomIconSource`）

---

## 📦 依赖管理

### 允许的依赖
- `CommunityToolkit.Mvvm` - MVVM 框架
- `Microsoft.WindowsAppSDK` - WinUI 3 运行时
- `Newtonsoft.Json` / `System.Text.Json` - JSON 序列化

### 禁止的依赖
- ❌ 任何 HTTP 客户端库（`RestSharp`、`Flurl` 等）
- ❌ 遥测/分析 SDK（`Application Insights`、`Google Analytics` 等）
- ❌ 第三方加密库（`BouncyCastle`、`libsodium` 等）

---

## 🎨 UI/UX 指南

### 设计原则
- **减少点击次数**：常用操作放在最显眼的位置
- **即时反馈**：操作后立刻显示结果（不要让用户猜"成功了吗？"）
- **撤销机制**：危险操作（删除）提供确认或撤销

### ⚠️ 避免使用模态对话框
对话框会**卡住用户的全部注意力**，让他们无法进行其他操作。请优先使用以下替代方案：

**✅ 推荐做法**：
- **内联编辑**：直接在原位置显示编辑控件
  - 例如：点击"添加 API Key"按钮后，在按钮位置展开输入框
  - 例如：点击 Provider 名称后，直接在列表项内显示编辑状态

- **浮出控件（Flyout）**：轻量级弹出面板，不阻塞背景操作
  - 例如：选择图标时，使用 `MenuFlyout` 或 `ContentDialog` 的非模态版本
  - 例如：删除确认用 `TeachingTip` 而非 `ContentDialog`

- **侧边栏/抽屉**：适合复杂表单或设置
  - 例如：Provider 详细设置页面可以从右侧滑入

**❌ 只在极端情况使用对话框**：
- 致命错误（应用即将崩溃）
- 不可逆的危险操作（删除所有数据）
- 必须用户立即响应的系统级提示

**示例对比**：
| 场景 | ❌ 对话框 | ✅ 更好的方式 |
|------|-----------|---------------|
| 添加新 Provider | 弹出对话框填表单 | 直接在列表末尾展开编辑卡片 |
| 修改 API Key | 对话框输入新值 | 点击后原地变为可编辑状态 |
| 选择图标 | 模态对话框选择器 | Flyout 浮出图标网格 |
| 删除提示 | "确定删除吗？" 对话框 | TeachingTip + 撤销按钮 |

### 主题适配
- 所有自定义图标必须提供 `-light` 和 `-dark` 两个版本
- 使用 `{ThemeResource}` 而非硬编码颜色
- 测试时在两种主题下都检查一遍

### 无障碍支持
- 为图标按钮添加 `AutomationProperties.Name`
- 确保键盘导航流畅（Tab 顺序合理）
- 对比度符合 WCAG AA 标准

---

## 🐛 调试与日志

### Crash Log
- 所有未捕获异常自动写入 `crash.log`
- 通过搜索框输入 `crash` 快速打开日志文件
- 不要在日志中记录敏感信息（API 密钥、用户数据）

### 调试技巧
- 使用 Visual Studio 的 XAML Hot Reload 加速 UI 调试
- 在 ViewModel 的 Command 中设置断点而非 XAML 事件
- 善用 `OutputDebugString` 或 `Debug.WriteLine` 而非 `MessageBox`

---

**记住**：用户选择 KeyPocket 是因为它不联网、不偷数据、不耍花招。保持简单，保持透明，保持可信。
