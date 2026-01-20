# API 文档 - KeyPocket.Core

本文档介绍了 `KeyPocket.Core` 类库的主要 API，包括领域模型、业务服务和存储机制的使用。

## 领域模型 (Models)

### 1. Provider
供应商模型，是存储结构的核心单元。
- `Guid Id`: 唯一标识符。
- `string Name`: 供应商名称（如 "OpenAI"）。
- `string? Description`: 可选描述信息。
- `List<ApiKey> ApiKeys`: 关联的密钥列表。
- `List<ModelInfo> Models`: 关联的模型列表。
- `HashSet<string> FavoriteModelIds`: 收藏的模型 ID 集合。
- `HashSet<Guid> FavoriteApiKeyIds`: 收藏的密钥 ID 集合。

### 2. ApiKey
API Key 模型。
- `Guid Id`: 唯一标识符。
- `Guid ProviderId`: 所属供应商 ID。
- `string Name`: 密钥名称（如 "工作用 Key"）。
- `string EncryptedKey`: 加密后的密钥内容（存入 JSON 前自动加密）。
- `List<string> Tags`: 标签列表（如 "free", "production"）。
- `bool IsDisabled`: 是否禁用。
- `DateTime CreatedAt`: 创建时间。
- `DateTime? LastUsedAt`: 最后使用时间（可空）。

### 3. ModelInfo
AI 模型信息。
- `string Id`: 模型唯一 ID（如 "gpt-4o"）。
- `Guid ProviderId`: 所属供应商 ID。
- `string DisplayName`: 显示名称。
- `decimal? InputPricePerMTokens`: 输入每百万 Token 的价格。
- `decimal? OutputPricePerMTokens`: 输出每百万 Token 的价格。
- `bool IsDeprecated`: 是否已弃用。
- `bool IsChatModel`: 是否为聊天模型。
- `bool IsEmbeddingModel`: 是否为嵌入模型。

---

## 业务服务 (Services)

所有的 Service 建议通过依赖注入 (DI) 使用。

### 1. ProviderService
负责供应商的增删改查。
- `GetAllProviders()`: 获取所有供应商列表。
- `CreateProvider(string name, string? description)`: 创建新供应商。
- `RemoveProvider(Guid id)`: 删除供应商。

### 2. ApiKeyService
负责密钥管理，集成了 **DPAPI** 加密。
- `AddApiKey(Guid providerId, string name, string plainKey, ...)`: 添加密钥（传入明文，自动存为密文）。
- `GetDecryptedKey(Guid providerId, Guid apiKeyId)`: 获取解密后的明文密钥（用于发送 API 请求前）。
- `SetDisabled(Guid providerId, Guid apiKeyId, bool isDisabled)`: 禁用密钥。

### 3. ModelService
负责 AI 模型的定价和列表管理。
- `GetModels(Guid providerId)`: 获取指定供应商的模型列表。
- `UpdatePricing(...)`: 更新模型的输入/输出价格。

### 4. FavoriteService
负责收藏逻辑。
- `ToggleModelFavorite(Guid providerId, string modelId)`: 切换模型的收藏状态。
- `ToggleApiKeyFavorite(Guid providerId, Guid apiKeyId)`: 切换密钥的收藏状态。

---

## 存储与设置 (Storage)

### JsonFileStorageProvider
- **构造函数**: `JsonFileStorageProvider(string filePath)`
- **主要逻辑**: 使用 `Load()` 加载整个配置，使用 `Save()` 进行原子化覆盖写入。

---

## 快速上手示例

### 初始化与添加供应商
```csharp
// 1. 初始化存储层
var storage = new JsonFileStorageProvider("C:\\Path\\To\\Config.json");

// 2. 初始化核心服务
var providerService = new ProviderService(storage);
var crypto = new DpapiSecretProtector();
var apiKeyService = new ApiKeyService(storage, crypto);

// 3. 创建供应商并添加 Key
providerService.CreateProvider("OpenAI");
var provider = providerService.GetAllProviders().First();

apiKeyService.AddApiKey(provider.Id, "My Work Key", "sk-xxxxxx");
```

### 获取明文 Key 进行调用
```csharp
// 仅在需要使用时解密，不在内存中长期驻留明文
string plainKey = apiKeyService.GetDecryptedKey(providerId, apiKeyId);
// 使用 plainKey 发送网络请求...
```

> [!IMPORTANT]
> **关于安全性**: 
> - `KeyPocket.Core` 使用 Windows DPAPI。这意味着配置文件在一个用户的账户下加密，只有该用户（或运行在该用户上下文下的进程）可以解密。
