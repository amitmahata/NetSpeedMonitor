# Net Speed Monitor

A lightweight, high-performance, and modern network speed monitor for Windows that natively embeds itself directly into the Windows Taskbar. Built using **C#**, **.NET 8/9**, and **WPF (Windows Presentation Foundation)** with zero external dependencies and a completely tray-iconless design.

Developed and maintained by **[Amit Mahata](https://github.com/amitmahata)**.

---

## 📸 Screenshots

### Taskbar Widget — Close-up View
The speed monitor pill sits natively on the taskbar, just like built-in Windows icons (volume, battery, clock):

![Taskbar Widget Close-up](screenshots/taskbar_widget_close.png)

## ✨ Features

Unlike traditional widgets that float clumsily over windows or hide inside the system tray, **Net Speed Monitor** integrates natively into the Windows shell layout:

- **Native Taskbar Embedding**: Parented directly to the Windows Taskbar window (`Shell_TrayWnd`) using Win32 API (`SetParent`). It sits perfectly to the left of your system tray notification area/overflow arrow.
- **Perfect Z-Ordering**: It behaves exactly like a native taskbar element. Standard desktop windows seamlessly drag on top of it, and it stays perfectly aligned behind active windows.
- **Autohide & Fullscreen Support**:
  - Automatically slides down/up in unison with the taskbar when "Auto-hide taskbar" is enabled in Windows settings.
  - Automatically hides during fullscreen YouTube/Netflix videos or fullscreen games (natively inherits the taskbar's hiding state).
- **Glassmorphic Interactive Dashboard**: Double-clicking the widget slides up a gorgeous, premium, glassmorphic speed dashboard featuring:
  - Real-time rolling 60-second speed history line graph (rendered via high-contrast polyline on canvas).
  - Session statistics for total downloaded and uploaded traffic.
  - Active adapter information and elapsed session duration.
  - Smooth fade-in/fade-out animations with automatic focus-loss closure.
- **Familiar Bit-Based Speeds**: Enforces bit-based formatting (`Mb/s`, `Kb/s`, `b/s`) across dynamic auto-scaling and static unit selections, avoiding confusing capital `MB` notations.
- **No System Tray Clutter**: 100% tray-iconless experience. You can manage speed units, toggle details, or exit the application directly from the widget's right-click context menu.
- **Single-Instance Enforcement**: Utilizes a system-wide Mutex to prevent multiple running instances.

---

## 🎨 Asset & Assembly Metadata Integration

1. **Embedded Binary Properties**: 
   Your identity is officially baked into the compiled `NetSpeedMonitor.exe` binary properties. Right-clicking the executable and choosing **Properties -> Details** shows:
   * **Authors**: `Amit Mahata`
   * **Company**: `Amit Mahata`
   * **Copyright**: `Copyright © 2026 Amit Mahata`
   * **Product Name**: `Net Speed Monitor`
   * **Description**: `A lightweight real-time network speed monitor natively embedded in the Windows taskbar.`

2. **Self-Contained Embedded Icons**: 
   Using standard WPF Pack Resource URIs (`pack://application:,,,/app_logo.png`), all visual logo elements are compiled directly inside the executable. The `.exe` is 100% portable and can be moved anywhere on disk without requiring any loose image files.

3. **Branded Installer Graphics**: 
   The Inno Setup script (`setup.iss`) is fully branded with custom high-contrast, modern installer graphics:
   * **Welcome Banner** (`app_logo_banner.bmp`): A dark modern vertical banner for the Welcome and Finished screens.
   * **Header Icon** (`app_logo_small.bmp`): A custom `55x55` header logo replacing default Inno Setup graphics on all installer pages.

---

## 🛠 Prerequisites

- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Runtime**: [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or newer (Desktop Runtime)

---

## 🚀 Installation & Running

### 1. Build and Run from Source
If you have the .NET SDK installed, you can clone, build, and run the project immediately:

```powershell
# Clone the repository
git clone https://github.com/your-username/NetSpeedMonitor.git
cd NetSpeedMonitor

# Run the application
dotnet run
```

### 2. Generate a Single Portable Executable (.exe)
To compile a lightweight, portable single-file executable that does not require an installer:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

This compiles a single portable binary `NetSpeedMonitor.exe` in `bin\Release\net8.0-windows\win-x64\publish\`.

---

## 📦 Compiling the Release Installer (.exe)

We use **Inno Setup** to compile a professional, clean Windows installation wizard:

1. Download and install [Inno Setup](https://jrsoftware.org/isdl.php).
2. Publish the C# Release Binary first:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true
   ```
3. Open `setup.iss` inside Inno Setup.
4. Press **`F9`** (or click **Build -> Compile**).
5. The branded setup wizard `NetSpeedMonitorSetup.exe` will be generated inside the `Output` folder!

---

## ⚙ Run on Windows Startup

To have the speed monitor start automatically every time you boot Windows:

1. Press `Win + R` to open the **Run** dialog.
2. Type `shell:startup` and press **Enter**. This opens your Windows Startup folder.
3. Right-click inside the folder, select **New** → **Shortcut**.
4. Browse to the path of your compiled `NetSpeedMonitor.exe` and select it.
5. Click **Next** and **Finish**.

Now, the Net Speed Monitor will seamlessly launch and embed itself into your taskbar at Windows startup!

---

## 🧩 Architectural Insights

The application achieves native-grade taskbar parenting using clean Win32 P/Invoke integrations:
- `FindWindow` / `FindWindowEx`: Locates the parent Windows Taskbar (`Shell_TrayWnd`) and the notification tray area (`TrayNotifyWnd`).
- `SetParent`: Changes the top-level WPF window (`WS_POPUP`) into a native child window (`WS_CHILD`) of the taskbar.
- `SetWindowPos`: Repositions the widget exactly beside the system tray overflow arrow using physical pixel calculations.
- `GetWindowRect`: Reads physical screen positions to calculate active DPI scaling (`PresentationSource.FromVisual`) and center the detailed Speed Graph Popup perfectly above the widget.

---

## 📄 License

This project is open-source and free to use. 

Created with ❤️ by **Amit Mahata**. Feel free to fork, submit issues, or open pull requests!
