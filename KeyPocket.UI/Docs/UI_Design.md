# KeyPocket WinUI 3 界面设计规范

本文档定义了 KeyPocket 桌面应用的界面架构与设计语言，旨在通过"克制"的设计理念，为用户提供一个优雅且高效的模型与密钥管理工具。

---

## 1. 核心设计理念

* **克制 (Restraint)**：拒绝信息堆砌。首页只保留最关键的状态与操作，复杂的配置项应隐藏在二级页面。
* **直观 (Intuition)**：通过图标、颜色（如状态指示灯、标签配色）直观传达信息，减少文字理解成本。
* **现代 (Modern)**：基于 WinUI 3 与 Mica 材质，提供符合 Windows 11 审美的高级感界面。
* **响应式 (Responsive)**：采用瀑布流布局和网格视图,确保在不同窗口尺寸下都有良好的显示效果。

---

## 2. 导航架构

应用采用左侧常驻侧边栏（`NavigationView`）+ 顶部标题栏（`TitleBar`）结构：

### 2.1 顶部标题栏 (TitleBar)

* **应用图标**：显示 KeyPocket 图标
* **应用标题**：显示 "KeyPocket"
* **搜索框**：全局搜索功能（360px 宽度，居中显示）
* **返回按钮**：支持页面导航返回
* **侧边栏切换**：可折叠/展开侧边栏

### 2.2 常驻功能 (Fixed Navigation Items)

1. **Home (首页)**：全景概览与高频操作，以卡片形式展示所有服务商。
2. **Models (模型中心)**：跨服务商的模型汇总、搜索、筛选与收藏管理。
3. **Keys (密钥管理)**：统一管理所有 API 凭据，支持搜索、筛选和标签管理。

### 2.3 服务商组 (Providers)

* **分组标题**：使用 `NavigationViewItemHeader` 显示 "Providers"
* **动态列表**：根据用户配置的服务商（如 OpenAI, Anthropic, DeepSeek 等）动态生成导航项
* **添加入口**：固定显示 **"Add Provider (+)"** 项，方便用户随时添加新服务商
* **点击行为**：点击服务商项进入该服务商的详细设置页面（Provider Settings Page）

---

## 3. 页面详细设计

### 3.1 首页 (Home) - "服务商卡片仪表盘"

#### 布局方式

* 采用 **瀑布流布局**（`StaggeredPanel`）
* 期望列宽：300px
* 列间距/行间距：12px
* 外边距：12px

#### 空状态设计 (Empty State)

当没有任何服务商配置时，显示全屏居中的引导页：

* 大图标（64px，使用 Glyph `&#xE78B;`）
* 提示文字："No Providers Configured"
* 操作按钮："Add Your First Provider"（Accent 样式）

#### 服务商卡片结构

每个卡片包含以下部分（从上到下）：

##### 1. Header (身份行)

* **布局**：Grid (Auto, *, Auto)
* **左侧**：
    * 服务商图标（20x20）- 支持 FontIcon 或自定义图片
    * 服务商名称（BodyStrong 样式）
* **右侧**：状态指示灯（8x8 圆形，颜色根据状态变化）

##### 2. Description (描述行)

* **显示条件**：仅在有描述时显示
* **样式**：Caption 样式，灰色字体
* **行为**：单行截断（TextTrimming）

##### 3. API Mode & BaseURL (API 模式与端点)

* **显示条件**：仅在配置了 BaseURL 时显示
* **API Mode 标签**：
    * 圆角边框（4px）
    * 半透明背景（颜色透明度 15%）
    * 文字颜色根据 API 类型变化
    * 字体：11px，SemiBold
* **BaseURL 文本**：
    * Accent 颜色
    * 最大宽度 180px，超出截断
    * 字体：11px
* **复制按钮**：小型复制按钮，可快速复制 BaseURL

##### 4. Models Area (常用模型区)

* **展示形式**：`WrapPanel` 横向排列
* **模型胶囊按钮**：
    * 显示模型名称（如 `GPT-4`）
    * 圆角：12px
    * 内边距：10px 水平，4px 垂直
    * 字体：12px
    * 默认背景：`ControlFillColorSecondaryBrush`
    * 悬停背景：`AccentFillColorSecondaryBrush`
    * 悬停文字：反色（`TextFillColorInverseBrush`）
* **交互**：点击复制模型 ID
* **提示**：Tooltip 显示完整模型 ID

##### 5. Keys Area (密钥列表区)

* **展示形式**：垂直 `StackPanel`
* **单个密钥行**：
    * 整体为可点击按钮
    * 背景：`LayerFillColorDefaultBrush`
    * 边框：1px，`SurfaceStrokeColorDefaultBrush`
    * 圆角：4px
    * 内边距：8px 水平，6px 垂直
    * 底部间距：8px
* **密钥行布局**（Grid 5列）：
    1. **Tag 标签**（可选）：
        * 仅在有标签时显示
        * 圆角边框（3px）
        * 半透明背景（颜色透明度 15%）
        * 支持智能配色（见标签配色方案）
        * 字体：10px，SemiBold
    2. **Key 前缀**：等宽字体（Consolas），显示如 `sk-`
    3. **中间遮罩**：星号序列，字符间距 200，居中对齐
    4. **Key 后缀**：等宽字体（Consolas），显示最后几位
    5. **复制按钮**：小型图标按钮
* **交互**：点击整行或按钮均可复制完整密钥

#### 卡片视觉效果

* 背景：`LayerOnMicaBaseAltFillColorDefaultBrush`
* 边框：1px，`SurfaceStrokeColorDefaultBrush`
* 圆角：8px
* 内边距：16px
* 阴影：使用 `ThemeShadow`，Z轴偏移 8
* 交互：点击卡片跳转到服务商设置页

---

### 3.2 模型中心 (Models) - "全局模型管理"

#### 页面布局

* 外边距：24px
* 行间距：24px

#### 顶部控制栏

* **搜索框**（左侧）：
    * 宽度：300px
    * 占位符："Search models..."
    * 实时搜索（UpdateSourceTrigger=PropertyChanged）
* **能力筛选**（中间）：
    * 下拉框（140px 宽）
    * 选项：All, Chat, Embedding 等
* **排序选项**（中间）：
    * 下拉框（140px 宽）
    * 选项：Name, Provider, Price 等
* **收藏筛选**（右侧）：
    * 切换按钮（ToggleButton）
    * 图标：星标（`&#xE735;`）
    * 文字："Favorites"

#### 模型卡片网格

* **布局**：`GridView`
* **卡片尺寸**：280px 宽 × 170px 高
* **卡片背景**：`CardBackgroundFillColorDefaultBrush`
* **卡片边框**：1px，`CardStrokeColorDefaultBrush`
* **圆角**：8px

#### 单个模型卡片结构（从上到下）

##### 1. Header (服务商信息 + 收藏)

* **左侧**：服务商名称（Caption 样式，70% 不透明度）
* **右侧**：收藏按钮
    * 未收藏：空心星标（`&#xE734;`）
    * 已收藏：实心星标（`&#xE735;`，金色 `#F2C811`）

##### 2. Model Name (模型名称)

* 样式：Subtitle
* 行为：单行截断
* Tooltip：显示完整名称

##### 3. Tags & Pricing (标签与价格)

* **能力标签**：
    * "Chat" / "Embedding"
    * 圆角边框（4px）
    * 背景：`SystemFillColorSolidNeutralBackgroundBrush`
    * 内边距：6px 水平，2px 垂直
* **价格信息**：
    * 格式："In: $X.XX  Out: $X.XX"
    * Caption 样式
    * "In:" / "Out:" 标签 60% 不透明度

##### 4. Footer (模型 ID + 复制)

* **ID 显示区**：
    * 背景：`LayerOnAcrylicFillColorDefaultBrush`
    * 圆角：4px
    * 内边距：8px 水平，4px 垂直
    * 字体：Consolas，12px
    * 80% 不透明度
    * 单行截断
* **复制按钮**：小型复制按钮

---

### 3.3 密钥中心 (Keys) - "全局密钥管理"

#### 页面布局

* 外边距：24px
* 行间距：24px

#### 顶部控制栏

* **搜索框**（左侧）：
    * 宽度：300px
    * 占位符："Search keys (provider, tag)..."
    * 实时搜索
* **排序选项**（中间）：
    * 下拉框（140px 宽）
    * 选项：Provider, Created Date, Tag 等
* **收藏筛选**（右侧）：
    * 切换按钮
    * 图标：星标
    * 文字："Favorites"

#### 密钥卡片网格

* **布局**：`GridView`
* **卡片尺寸**：320px 宽 × 180px 高
* **卡片背景**：`CardBackgroundFillColorDefaultBrush`
* **卡片边框**：1px，`CardStrokeColorDefaultBrush`
* **圆角**：8px

#### 单个密钥卡片结构（从上到下）

##### 1. Header (服务商名称 + 收藏)

* **左侧**：服务商名称（Caption 样式，70% 不透明度，单行截断）
* **右侧**：收藏按钮（同模型卡片）

##### 2. Key Content (密钥内容)

* **显示区域**：垂直居中
* **字体**：Consolas，16px
* **内容**：脱敏显示（如 `sk-****AbCd`）
* **行为**：单行截断

##### 3. Footer (标签 + 操作按钮)

* **左侧 - 标签编辑区**：
    * 标签提示："Tag:"（60% 不透明度）
    * 标签输入框：
        * 透明背景
        * 无边框
        * 最小宽度：100px
        * 占位符："Add tag..."
        * 失焦时保存（UpdateSourceTrigger=LostFocus）
* **右侧 - 操作按钮组**：
    * **显示/隐藏按钮**：
        * 隐藏状态：眼睛图标（`&#xE890;`）
        * 显示状态：闭眼图标（`&#xE891;`）
    * **复制按钮**：复制完整密钥
* **右下角 - 创建时间**：
    * 字体：10px
    * 40% 不透明度
    * 不可交互（IsHitTestVisible=False）

---

### 3.4 服务商设置页 (Provider Settings) - "深度配置中心"

#### 页面布局

* **外边距**：40px 水平，32px 垂直
* **最大内容宽度**：800px（表单区域）
* **左侧标题宽度**：240px
* **列间距**：40px

#### 页面头部

* **主标题**："Provider Settings"（Title 样式）
* **副标题**："Manage your provider details, API keys, and model configurations."（Body 样式，次要颜色）
* **分隔线**：1px 高，底部间距 32px

#### 分节结构

采用 **两栏布局**（左侧标题 + 右侧内容）+ **粘性标题**（Sticky Headers）设计：

##### Section 1: General (基本信息)

**左侧标题区**：

* 主标题："General"（Subtitle 样式）
* 副标题："Basic provider information."（Caption 样式，次要颜色）

**右侧表单区**：

1. **Name（名称）**：
    * 标签："Name"（BodyStrong 样式）
    * 输入框：TextBox，双向绑定
2. **Icon（图标）**：
    * 标签："Icon"
    * 按钮组：
        * "Change Icon"（默认样式）
        * "Remove Icon"（仅在有自定义图标时显示）
3. **Base URL（基础 URL）**：
    * 标签："Base URL"
    * 输入框：TextBox，占位符 "https://api.openai.com/v1"
    * 提示文字："The base URL of the API provider."（Caption 样式）
4. **API Mode（API 模式）**：
    * 标签："API Mode"
    * 下拉框：ComboBox，选项如 OpenAI, Azure, Custom 等
5. **Description（描述）**：
    * 标签："Description"
    * 输入框：多行 TextBox，高度 80px
6. **保存按钮**：
    * "Save Changes"（Accent 样式，右对齐）

##### Section 2: API Keys (密钥管理)

**左侧标题区**：

* 主标题："API Keys"
* 副标题："Manage access keys for this provider."

**右侧内容区**：

* **列表**：使用 `ListView`，支持拖拽排序
    * `CanReorderItems="True"`
    * `AllowDrop="True"`
    * `CanDragItems="True"`
    * 禁用垂直滚动（`ScrollViewer.VerticalScrollMode="Disabled"`）

* **单个密钥项**（两种状态）：

  **只读状态**：
    * 脱敏显示（等宽字体 Consolas）
    * 右侧按钮组：
        1. **标签按钮/输入**：
            * 无标签时：显示添加图标（`&#xE8EC;`）
            * 有标签时：显示标签文字（可点击编辑）
            * 编辑时：显示 TextBox（80px 宽）
        2. **复制按钮**
        3. **收藏按钮**：空心/实心星标切换
        4. **删除按钮**：红色垃圾桶图标（`&#xE74D;`）

  **编辑状态**（新增密钥时）：
    * 密码输入框（PasswordBox）
    * 占位符："Enter API Key (sk-...)"
    * 右侧按钮：
        * "Confirm"（Accent 样式）
        * "Cancel"
    * 支持 Enter 键确认，Escape 键取消

* **添加按钮**："Add API Key"

##### Section 3: Models (模型配置)

**左侧标题区**：

* 主标题:"Models"
* 副标题:"Configure available models."

**右侧内容区**：

* **列表**：使用 `ListView`，支持拖拽排序（同 API Keys）

* **单个模型项**（两种状态）：

  **只读状态**：
    * 模型名称（BodyStrong 样式）
    * 右侧按钮组：
        1. **复制按钮**：复制模型 ID
        2. **编辑按钮**：铅笔图标（`&#xE70F;`）
        3. **收藏按钮**
        4. **删除按钮**

  **编辑状态**（新增/编辑模型时）：
    * **第一行**：
        * Model ID（左侧）：TextBox，占位符 "e.g. gpt-4"
        * Display Name（右侧）：TextBox，占位符 "e.g. GPT-4"
    * **第二行**：
        * Input Price（左侧）：NumberBox，占位符 "e.g. 0.03"，单位 "$/M tokens"
        * Output Price（右侧）：NumberBox，占位符 "e.g. 0.06"
    * **第三行**：
        * 右对齐按钮组：
            * "Confirm"（Accent 样式）
            * "Cancel"
    * 支持 Enter 键确认，Escape 键取消

* **添加按钮组**：
    * "Add Model"
    * "Fetch Models"（带 Tooltip："Fetch available models from provider"）

##### Section 4: Danger Zone (危险区域)

* **标题**："Danger Zone"（Subtitle 样式，红色）
* **边框容器**：
    * 边框颜色：红色（`SystemFillColorCriticalBrush`）
    * 边框宽度：1px
    * 圆角：6px
    * 内边距：16px
* **内容布局**（Grid 两列）：
    * **左侧**：
        * 主文字："Delete this provider"（BodyStrong 样式）
        * 警告文字："Once you delete a provider, there is no going back. Please be certain."（Caption 样式）
    * **右侧**：
        * 删除按钮："Delete this provider"
        * 背景：红色
        * 文字：白色
        * 无边框

#### 粘性标题实现

* 使用 Canvas 覆盖层（`StickyHeadersCanvas`）
* 监听 ScrollViewer 的 ViewChanged 事件
* 动态显示/隐藏对应分节的标题

---

## 4. 视觉规范

### 4.1 背景与材质

* **应用背景**：开启 `MicaBackdrop`，增强通透感
* **卡片背景**：
    * 首页：`LayerOnMicaBaseAltFillColorDefaultBrush`
    * Models/Keys 页：`CardBackgroundFillColorDefaultBrush`
* **输入框背景**：`LayerFillColorDefaultBrush`

### 4.2 色彩系统

#### 主题色

* 使用 WinUI 标准的 Accent Color（强调色）
* 按钮悬停：`AccentFillColorSecondaryBrush`
* 按钮按下：`AccentFillColorTertiaryBrush`

#### 状态色

* **成功/正常**：`#0F7B0F`（绿色）
* **警告**：`#9D5D00`（橙色）
* **严重/危险**：`#C42B1C`（红色）

#### 标签配色方案（API Key Tags）

系统会识别标签中的关键字并显示对应颜色（支持中英文）：

| 颜色     | 匹配关键字                            | 建议用途      | 色值示例 |
|:-------|:---------------------------------|:----------|:-----|
| **蓝色** | `开发`, `测试`, `dev`, `test`        | 开发或测试环境   | -    |
| **绿色** | `正式`, `生产`, `prod`, `production` | 生产环境      | -    |
| **紫色** | `免费`, `试用`, `free`, `trial`      | 免费或试用 Key | -    |
| **黄色** | `付费`, `收费`, `paid`, `premium`    | 付费或高级 Key | -    |
| **橙色** | `临时`, `暂存`, `temp`, `staging`    | 短期临时 Key  | -    |
| **灰色** | `备份`, `bak`, `backup` 或无匹配       | 冗余备份/默认   | -    |

**匹配规则**：

* 标签内容只需包含上述关键字即可触发配色（如 `dev-key` 会显示为蓝色）
* 标签颜色会随系统主题（亮色/暗色）自动调整显示亮度

### 4.3 圆角规范

* **卡片/容器**：8px
* **按钮/输入框**：4px
* **模型胶囊**：12px
* **小标签**：3-4px

### 4.4 间距规范

* **页面外边距**：
    * 首页：12px
    * Models/Keys 页：24px
    * Settings 页：40px 水平，32px 垂直
* **卡片内边距**：
    * 首页卡片：16px
    * Models/Keys 卡片：16px
* **元素间距**：
    * 小间距：4-8px
    * 中间距：12-16px
    * 大间距：24-40px

### 4.5 阴影与深度

* **卡片阴影**：使用 `ThemeShadow`
* **Z轴偏移**：
    * 首页卡片：8（`Translation="0,0,8"`）
    * 其他卡片：根据需要调整

### 4.6 动效

* **页面切换**：使用 `EntranceNavigationTransitionInfo`
* **卡片悬停**：轻微位移或阴影加深
* **按钮交互**：背景色平滑过渡

### 4.7 字体规范

* **标题**：Title / Subtitle 样式
* **正文**：Body / BodyStrong 样式
* **说明文字**：Caption 样式
* **等宽字体**：Consolas, Courier New, Monospace（用于密钥、模型 ID）

---

## 5. 交互流程

### 5.1 添加服务商 (Add Provider)

**触发方式**：

1. 首页空状态的 "Add Your First Provider" 按钮
2. 侧边栏的 "Add Provider" 导航项

**实现方式**：使用 **ContentDialog (模态弹窗)**

**设计理由**：

* **轻量感**：添加操作通常应快速完成，弹窗不会打断用户当前的浏览上下文
* **连贯性**：填完信息后立即关闭弹窗，用户能直接看到新卡片在首页"刷"出来，反馈即时

**弹窗内容**（建议）：

1. **选择模板**：展示支持的服务商图标网格（OpenAI, Azure, DeepSeek, Ollama 等）
2. **核心字段**：
    * Name（默认回填服务商名）
    * API Key（必填）
3. **高级配置**：提供"配置更多"链接，或仅在保存后引导用户去设置页

### 5.2 编辑服务商

**触发方式**：

1. 首页点击服务商卡片
2. 侧边栏点击服务商导航项

**目标页面**：Provider Settings Page

### 5.3 管理密钥

**添加密钥**：

1. 在 Provider Settings 页点击 "Add API Key"
2. 列表中插入编辑状态的新项
3. 输入密钥后点击 "Confirm" 或按 Enter 键
4. 取消则点击 "Cancel" 或按 Escape 键

**编辑标签**：

1. 点击标签按钮或添加图标
2. 显示 TextBox 输入框
3. 输入完成后失焦自动保存

**拖拽排序**：

* 直接拖动密钥项进行重新排序
* 松开鼠标后自动保存新顺序

### 5.4 管理模型

**添加模型**：

1. 点击 "Add Model" 按钮
2. 列表中插入编辑状态的新项
3. 填写 Model ID、Display Name、价格信息
4. 点击 "Confirm" 或按 Enter 键保存

**编辑模型**：

1. 点击模型项的编辑按钮
2. 切换到编辑状态
3. 修改信息后确认或取消

**获取模型列表**：

* 点击 "Fetch Models" 按钮
* 从服务商 API 自动获取可用模型列表
* 用户可选择导入

### 5.5 收藏功能

**模型收藏**：

* 在 Models 页或 Provider Settings 页点击星标按钮
* 收藏的模型会显示在首页对应服务商卡片的模型区域

**密钥收藏**：

* 在 Keys 页或 Provider Settings 页点击星标按钮
* 收藏的密钥优先显示

### 5.6 复制功能

**一键复制**：

* 模型 ID：点击模型胶囊或复制按钮
* 密钥：点击密钥行或复制按钮
* BaseURL：点击复制按钮

**视觉反馈**：

* 复制按钮应提供视觉反馈（如短暂变色或显示 "Copied" 提示）

---

## 6. 响应式设计

### 6.1 首页瀑布流

* **期望列宽**：300px
* **自动调整**：根据窗口宽度自动计算列数
* **最小列数**：1 列
* **最大列数**：无限制（取决于窗口宽度）

### 6.2 网格视图

* **Models 页卡片**：280px × 170px
* **Keys 页卡片**：320px × 180px
* **自动换行**：GridView 自动处理

### 6.3 设置页表单

* **最大宽度**：800px
* **居中对齐**：在大屏幕上保持可读性
* **左侧标题**：固定 240px 宽度

---

## 7. 可访问性 (Accessibility)

### 7.1 键盘导航

* 所有交互元素支持 Tab 键导航
* Enter 键确认，Escape 键取消
* 方向键在列表中移动

### 7.2 屏幕阅读器

* 为图标按钮提供 Tooltip
* 为复杂控件提供 AutomationProperties

### 7.3 对比度

* 确保文字与背景有足够对比度
* 支持高对比度主题

---

## 8. 性能优化

### 8.1 虚拟化

* GridView 和 ListView 自动启用虚拟化
* 仅渲染可见区域的项

### 8.2 延迟加载

* 大量数据时考虑分页或增量加载
* 图标按需加载

### 8.3 动画性能

* 使用 Composition API 实现流畅动画
* 避免在滚动时触发复杂动画

---

## 9. 设计原则总结

> [!IMPORTANT]
> **首页定位**：Home 页面应始终保持"仪表盘"属性，而非"管理器"。如果某个功能需要用户思考超过 3 秒，它就不应该出现在 Home 上。

> [!TIP]
> **渐进式披露**：将复杂配置隐藏在二级页面（Provider Settings），首页只展示最关键的信息和高频操作。

> [!NOTE]
> **一致性优先**：所有页面应遵循相同的视觉语言和交互模式，确保用户学习成本最低。

---

*本文档基于 KeyPocket 当前实现（2026-01-22）编写，反映了实际的 UI 架构和设计决策。*
