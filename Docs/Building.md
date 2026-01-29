## Building from Source

### 1. Prerequisites

- [Visual Studio 2026](https://visualstudio.microsoft.com/vs/) with the following individual components installed:
    - Windows 11 SDK (10.0.26100.0)
    - .NET 10 SDK (version 10.0.102 or later)
    - Git for Windows
- [Windows App SDK 1.8](https://learn.microsoft.com/zh-cn/windows/apps/windows-app-sdk/downloads#current-releases)

### 2. Clone the Repository

```powershell
git clone https://github.com/MidnightCrowing/KeyPocket.git
cd KeyPocket

```

### 3. Build and Run

1. Locate and open the `KeyPocket.slnx` file to load the solution in Visual Studio (
   or [Rider](https://www.jetbrains.com/rider/)).
2. Verify the build configuration in the top toolbar:

* Configuration: Select `Debug` (for testing/debugging) or `Release` (for optimized performance).
* Platform: Select `x64` (recommended for most modern PCs) or `arm64` (for ARM devices like Surface Pro X).

3. Press `F5` or click the Start button to build and deploy the application.

**You’re good to go!**
