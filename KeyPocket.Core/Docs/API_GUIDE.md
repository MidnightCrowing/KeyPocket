# API 文档 - KeyPocket.Core

本文档介绍了 `KeyPocket.Core` 类库的主要 API，包括领域模型、业务服务、加密机制和存储系统的使用。

---

## 1. 领域模型 (Models)

### 1.1 Provider（服务商）

服务商模型，是存储结构的核心单元。

**属性列表**：

| 属性                  | 类型                | 说明                                                             |
|---------------------|-------------------|----------------------------------------------------------------|
| `Id`                | `Guid`            | 唯一标识符，自动生成                                                     |
| `Name`              | `string`          | 服务商名称（如 "OpenAI"）                                              |
| `Description`       | `string?`         | 可选描述信息                                                         |
| `Type`              | `string`          | API 模式（如 "OpenAI API", "Claude API", "Custom"），默认 "OpenAI API" |
| `ApiBaseUrl`        | `string?`         | API 基础地址（如 "https://api.openai.com/v1"）                        |
| `ApiKeys`           | `List<ApiKey>`    | 关联的密钥列表                                                        |
| `Models`            | `List<ModelInfo>` | 关联的模型列表                                                        |
| `FavoriteModelIds`  | `HashSet<string>` | 收藏的模型 ID 集合                                                    |
| `FavoriteApiKeyIds` | `HashSet<Guid>`   | 收藏的密钥 ID 集合                                                    |
| `IconPath`          | `string?`         | 自定义图标的相对路径（如 "Icons/my-icon.png"）                              |

---

### 1.2 ApiKey（API 密钥）

API Key 模型，支持加密存储。

**属性列表**：

| 属性             | 类型          | 说明                                |
|----------------|-------------|-----------------------------------|
| `Id`           | `Guid`      | 唯一标识符，自动生成                        |
| `ProviderId`   | `Guid`      | 所属服务商 ID                          |
| `EncryptedKey` | `string`    | 加密后的密钥内容（使用 DPAPI 加密）             |
| `Tag`          | `string?`   | 标签（如 "free", "production", "dev"） |
| `IsDisabled`   | `bool`      | 是否禁用该密钥                           |
| `CreatedAt`    | `DateTime`  | 创建时间，默认为当前时间                      |
| `LastUsedAt`   | `DateTime?` | 最后使用时间（可空）                        |
| `IsFavorite`   | `bool`      | 是否被收藏                             |

**标签配色方案**：

标签会根据关键字自动应用预设颜色（支持中英文）：

- **蓝色**：`开发`, `测试`, `dev`, `test`
- **绿色**：`正式`, `生产`, `prod`, `production`
- **紫色**：`免费`, `试用`, `free`, `trial`
- **黄色**：`付费`, `收费`, `paid`, `premium`
- **橙色**：`临时`, `暂存`, `temp`, `staging`
- **灰色**：`备份`, `bak`, `backup` 或无匹配关键字

---

### 1.3 ModelInfo（AI 模型信息）

AI 模型信息，包含定价和能力标识。

**属性列表**：

| 属性                      | 类型         | 说明                                   |
|-------------------------|------------|--------------------------------------|
| `Id`                    | `string`   | 模型唯一 ID（如 "gpt-4o", "claude-3-opus"） |
| `ProviderId`            | `Guid`     | 所属服务商 ID                             |
| `DisplayName`           | `string`   | 显示名称（如 "GPT-4 Omni"）                 |
| `InputPricePerMTokens`  | `decimal?` | 输入每百万 Token 的价格（美元）                  |
| `OutputPricePerMTokens` | `decimal?` | 输出每百万 Token 的价格（美元）                  |
| `IsDeprecated`          | `bool`     | 是否已弃用                                |
| `IsChatModel`           | `bool`     | 是否为聊天模型                              |
| `IsEmbeddingModel`      | `bool`     | 是否为嵌入模型                              |
| `IsFavorite`            | `bool`     | 是否被收藏                                |

---

### 1.4 KeyPocketConfig（配置根对象）

整个应用的配置根对象，用于 JSON 序列化。

**属性列表**：

| 属性          | 类型               | 说明      |
|-------------|------------------|---------|
| `Providers` | `List<Provider>` | 所有服务商列表 |

---

## 2. 业务服务 (Services)

所有的 Service 建议通过依赖注入 (DI) 使用。

### 2.1 ProviderService（服务商管理服务）

负责服务商的完整生命周期管理，包括 CRUD 操作、密钥管理和模型管理。

**构造函数**：

```csharp
public ProviderService(IStorageProvider storage, ISecretProtector secretProtector)
```

**服务商管理方法**：

| 方法                        | 参数                                         | 返回值              | 说明                 |
|---------------------------|--------------------------------------------|------------------|--------------------|
| `GetAllProviders()`       | -                                          | `List<Provider>` | 获取所有服务商列表          |
| `CreateProvider()`        | -                                          | `Provider`       | 创建默认配置的服务商（自动生成名称） |
| `CreateProvider(...)`     | `name`, `type`, `baseUrl?`, `description?` | `Provider`       | 创建指定配置的服务商         |
| `RemoveProvider(...)`     | `id`                                       | `void`           | 删除服务商（同时删除关联的图标文件） |
| `RenameProvider(...)`     | `id`, `newName`                            | `void`           | 重命名服务商             |
| `UpdateProvider(...)`     | `updatedProvider`                          | `void`           | 更新服务商的完整信息         |
| `UpdateProviderIcon(...)` | `providerId`, `iconPath?`                  | `void`           | 更新服务商图标路径          |

**密钥管理方法**：

| 方法                          | 参数                            | 返回值      | 说明                |
|-----------------------------|-------------------------------|----------|-------------------|
| `AddApiKey(...)`            | `providerId`, `key`           | `void`   | 添加密钥（传入明文，自动加密存储） |
| `UpdateApiKeyTag(...)`      | `providerId`, `keyId`, `tag?` | `void`   | 更新密钥标签            |
| `RemoveApiKey(...)`         | `providerId`, `keyId`         | `void`   | 删除密钥（同时从收藏列表移除）   |
| `ToggleFavoriteApiKey(...)` | `providerId`, `keyId`         | `void`   | 切换密钥的收藏状态         |
| `ToggleDisableApiKey(...)`  | `providerId`, `keyId`         | `void`   | 切换密钥的启用/禁用状态      |
| `GetDecryptedApiKey(...)`   | `providerId`, `keyId`         | `string` | 获取解密后的明文密钥        |

**模型管理方法**：

| 方法                         | 参数                      | 返回值    | 说明              |
|----------------------------|-------------------------|--------|-----------------|
| `AddModel(...)`            | `providerId`, `model`   | `void` | 添加模型（自动避免重复）    |
| `RemoveModel(...)`         | `providerId`, `modelId` | `void` | 删除模型（同时从收藏列表移除） |
| `ToggleFavoriteModel(...)` | `providerId`, `modelId` | `void` | 切换模型的收藏状态       |

---

## 3. 加密机制 (Crypto)

### 3.1 ISecretProtector（加密接口）

定义加密和解密的标准接口。

```csharp
public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}
```

---

### 3.2 DpapiSecretProtector（DPAPI 加密实现）

基于 Windows DPAPI 的加密实现，使用当前用户上下文进行加密。

**构造函数**：

```csharp
public DpapiSecretProtector()
```

**方法列表**：

| 方法               | 参数           | 返回值      | 说明                   |
|------------------|--------------|----------|----------------------|
| `Protect(...)`   | `plainText`  | `string` | 加密明文，返回 Base64 编码的密文 |
| `Unprotect(...)` | `cipherText` | `string` | 解密密文，返回明文（失败时返回空字符串） |

**安全特性**：

- 使用 `DataProtectionScope.CurrentUser`，密钥绑定到当前 Windows 用户
- 加密数据只能在同一用户账户下解密
- 不需要手动管理密钥，由 Windows 系统自动处理
- 可选的附加熵（Entropy）支持（当前实现为 `null`）

> [!IMPORTANT]
> **关于安全性**：
> - DPAPI 加密的数据在不同用户账户或不同机器上无法解密
> - 如果用户账户密码被重置，可能导致无法解密
> - 适合本地存储场景，不适合跨机器同步

---

## 4. 存储系统 (Storage)

### 4.1 IStorageProvider（存储接口）

定义存储的标准接口。

```csharp
public interface IStorageProvider
{
    KeyPocketConfig Load();
    void Save(KeyPocketConfig config);
}
```

---

### 4.2 JsonFileStorageProvider（JSON 文件存储实现）

基于 JSON 文件的存储实现，支持原子化写入。

**构造函数**：

```csharp
public JsonFileStorageProvider(string filePath)
```

**方法列表**：

| 方法          | 参数       | 返回值               | 说明                |
|-------------|----------|-------------------|-------------------|
| `Load()`    | -        | `KeyPocketConfig` | 加载配置（文件不存在时返回空配置） |
| `Save(...)` | `config` | `void`            | 保存配置（原子化写入）       |

**特性**：

- **JSON 格式化**：使用 `WriteIndented = true`，生成可读的 JSON
- **大小写不敏感**：使用 `PropertyNameCaseInsensitive = true`
- **原子化写入**：先写入临时文件（`.tmp`），再重命名覆盖原文件
- **自动创建目录**：如果目标目录不存在，自动创建
- **错误处理**：加载失败时返回空配置，保存失败时清理临时文件

**原子化写入流程**：

1. 序列化配置为 JSON 字符串
2. 写入临时文件（`filePath.tmp`）
3. 使用 `File.Replace()` 或 `File.Move()` 原子性地替换原文件
4. 发生错误时自动清理临时文件

---

## 5. 快速上手示例

### 5.1 初始化与添加服务商

```csharp
// 1. 初始化存储层
var storagePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "KeyPocket",
    "config.json"
);
var storage = new JsonFileStorageProvider(storagePath);

// 2. 初始化加密器
var crypto = new DpapiSecretProtector();

// 3. 初始化核心服务
var providerService = new ProviderService(storage, crypto);

// 4. 创建服务商
var provider = providerService.CreateProvider(
    name: "OpenAI",
    type: "OpenAI API",
    baseUrl: "https://api.openai.com/v1",
    description: "Official OpenAI API"
);

Console.WriteLine($"Created provider: {provider.Name} (ID: {provider.Id})");
```

---

### 5.2 添加和管理 API 密钥

```csharp
// 添加密钥（自动加密）
providerService.AddApiKey(provider.Id, "sk-your-secret-key-here");

// 获取所有服务商（包含密钥列表）
var providers = providerService.GetAllProviders();
var myProvider = providers.First(p => p.Name == "OpenAI");
var firstKey = myProvider.ApiKeys.First();

// 更新密钥标签
providerService.UpdateApiKeyTag(provider.Id, firstKey.Id, "production");

// 收藏密钥
providerService.ToggleFavoriteApiKey(provider.Id, firstKey.Id);

// 禁用密钥
providerService.ToggleDisableApiKey(provider.Id, firstKey.Id);

// 获取明文密钥（仅在需要使用时解密）
string plainKey = providerService.GetDecryptedApiKey(provider.Id, firstKey.Id);
Console.WriteLine($"Decrypted key: {plainKey}");

// 删除密钥
providerService.RemoveApiKey(provider.Id, firstKey.Id);
```

---

### 5.3 添加和管理模型

```csharp
// 创建模型信息
var model = new ModelInfo
{
    Id = "gpt-4o",
    DisplayName = "GPT-4 Omni",
    InputPricePerMTokens = 5.0m,
    OutputPricePerMTokens = 15.0m,
    IsChatModel = true,
    IsEmbeddingModel = false
};

// 添加模型
providerService.AddModel(provider.Id, model);

// 收藏模型
providerService.ToggleFavoriteModel(provider.Id, "gpt-4o");

// 删除模型
providerService.RemoveModel(provider.Id, "gpt-4o");
```

---

### 5.4 更新服务商信息

```csharp
// 获取服务商
var provider = providerService.GetAllProviders()
    .First(p => p.Name == "OpenAI");

// 更新基本信息
provider.Description = "Updated description";
provider.ApiBaseUrl = "https://custom-proxy.com/v1";
provider.Type = "Custom";

// 保存更新
providerService.UpdateProvider(provider);

// 或者只更新图标
providerService.UpdateProviderIcon(provider.Id, "Icons/openai-custom.png");
```

---

### 5.5 完整的工作流程示例

```csharp
using KeyPocket.Core.Crypto;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.Core.Storage;

// 初始化
var storage = new JsonFileStorageProvider("config.json");
var crypto = new DpapiSecretProtector();
var providerService = new ProviderService(storage, crypto);

// 创建服务商
var openai = providerService.CreateProvider(
    "OpenAI",
    "OpenAI API",
    "https://api.openai.com/v1"
);

// 添加密钥
providerService.AddApiKey(openai.Id, "sk-prod-key-123");
providerService.AddApiKey(openai.Id, "sk-test-key-456");

// 更新密钥标签
var keys = providerService.GetAllProviders()
    .First(p => p.Id == openai.Id).ApiKeys;
providerService.UpdateApiKeyTag(openai.Id, keys[0].Id, "production");
providerService.UpdateApiKeyTag(openai.Id, keys[1].Id, "test");

// 添加模型
var gpt4 = new ModelInfo
{
    Id = "gpt-4",
    DisplayName = "GPT-4",
    InputPricePerMTokens = 30.0m,
    OutputPricePerMTokens = 60.0m,
    IsChatModel = true
};
providerService.AddModel(openai.Id, gpt4);

// 收藏常用模型
providerService.ToggleFavoriteModel(openai.Id, "gpt-4");

// 使用密钥
var prodKey = keys.First(k => k.Tag == "production");
string decryptedKey = providerService.GetDecryptedApiKey(openai.Id, prodKey.Id);
// 使用 decryptedKey 发送 API 请求...
```

---

## 6. 架构设计原则

### 6.1 单一职责

- **ProviderService**：服务商的完整生命周期管理
- **ModelService**：模型的独立管理（可选）
- **FavoriteService**：收藏功能的独立管理（可选）
- **ISecretProtector**：加密/解密的抽象
- **IStorageProvider**：存储的抽象

### 6.2 依赖注入

所有服务都通过构造函数注入依赖，便于测试和扩展：

```csharp
// 示例：在 WinUI 3 应用中注册服务
services.AddSingleton<IStorageProvider>(sp => 
    new JsonFileStorageProvider(configPath));
services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
services.AddSingleton<ProviderService>();
```

### 6.3 数据持久化

- 所有修改操作都会立即调用 `_storage.Save(config)`
- 采用"加载-修改-保存"模式，确保数据一致性
- 原子化写入避免数据损坏

### 6.4 安全性

- API 密钥使用 DPAPI 加密存储
- 明文密钥仅在需要时解密，不在内存中长期驻留
- 配置文件中只存储加密后的 Base64 字符串

---

## 7. 注意事项

> [!WARNING]
> **DPAPI 限制**：
> - 加密数据绑定到 Windows 用户账户
> - 无法在不同用户或不同机器间共享
> - 用户密码重置可能导致无法解密

> [!TIP]
> **性能优化**：
> - 频繁读取时可考虑缓存 `KeyPocketConfig`
> - 批量操作时可先修改内存对象，最后统一保存
> - 避免在循环中多次调用 `Save()`

> [!CAUTION]
> **并发安全**：
> - 当前实现不支持多进程并发写入
> - 如需支持，需添加文件锁机制

---

*本文档基于 KeyPocket.Core 当前实现（2026-01-22）编写，反映了实际的 API 设计和使用方式。*
