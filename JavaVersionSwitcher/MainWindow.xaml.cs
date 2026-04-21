using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace JavaVersionSwitcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _isSwitching;
    private Window? _switchingDialog;

    private static readonly string[] JavaProcessNames =
    {
        "java",
        "javaw",
        "javac"
    };

    private static readonly string[] IdeAndBuildToolProcessNames =
    {
        "idea64",
        "idea",
        "studio64",
        "eclipse",
        "netbeans64",
        "netbeans",
        "code",
        "devenv",
        "mvn",
        "mvnw",
        "gradle",
        "gradlew"
    };

    // Java版本路径配置
    private static readonly IReadOnlyDictionary<string, string> JavaVersionPaths = new Dictionary<string, string>
    {
        {"Java7", "D:\\java\\jdk-7"},
        {"Java8_32", "D:\\java\\jdk-8_32"},
        {"Java8", "D:\\java\\jdk-8"},
        {"Java11", "D:\\java\\jdk-11.0.2"},
        {"Java17", "D:\\java\\jdk-17.0.12"},
        {"Java25", "D:\\java\\jdk-25.0.2"}
    };
    
    // Java版本映射：将java -version输出映射到按钮名称
    private static readonly Dictionary<string, string> JavaVersionMapping = new Dictionary<string, string>
    {
        {"1.7", "Java7"},
        {"1.8", "Java8"},
        {"11", "Java11"},
        {"17", "Java17"},
        {"25", "Java25"}
    };
    
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }
    
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckCurrentJavaVersionAsync();
    }
    
    // 检查当前Java版本（通过执行 java -version 获取真实版本，并结合 JAVA_HOME）
    private async Task CheckCurrentJavaVersionAsync()
    {
        try
        {
            // 获取系统环境变量中的JAVA_HOME
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine) ?? string.Empty;
            
            // 尝试执行 java -version 获取真实生效版本
            string rawVersion = await GetActualJavaVersionAsync();
            string displayVersion = "未检测到Java";

            if (!string.IsNullOrEmpty(rawVersion))
            {
                // 如果能成功执行 java -version，则使用映射逻辑
                displayVersion = MapJavaVersionWithPath(rawVersion, javaHome);
            }
            else
            {
                // 如果 java -version 执行失败，降级使用 JAVA_HOME 判断
                displayVersion = GetJavaVersionFromPath(javaHome);
            }
            
            // 更新UI
            CurrentJavaVersion.Text = displayVersion;
            
            // 更新按钮状态，高亮当前版本对应的按钮
            if (displayVersion != "未检测到Java")
            {
                UpdateButtonStates(displayVersion);
            }
            else
            {
                ResetButtonStates();
            }
        }
        catch (Exception ex)
        {
            CurrentJavaVersion.Text = "检测失败";
            ResetButtonStates();
            MessageBox.Show($"检测Java版本失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 异步执行 java -version 并提取版本号
    private async Task<string> GetActualJavaVersionAsync()
    {
        try
        {
            using Process process = new Process();
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = "-version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true; // java -version 输出在 stderr
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                // 匹配 java version "17.0.12" 或 openjdk version "11.0.2"
                var match = System.Text.RegularExpressions.Regex.Match(output, @"(?:java|openjdk) version ""([^""]+)""");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch
        {
            // 执行失败（如环境变量未配置 java）
        }
        return string.Empty;
    }
    
    // 根据JAVA_HOME路径获取Java版本名称
    private string GetJavaVersionFromPath(string javaHome)
    {
        string normalizedJavaHome = javaHome?.TrimEnd('\\', '/') ?? string.Empty;
        
        // 查找匹配的版本路径
        foreach (var kvp in JavaVersionPaths)
        {
            string normalizedPath = kvp.Value.TrimEnd('\\', '/');
            if (normalizedJavaHome.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }
        
        // 如果没有匹配的路径，返回默认信息
        return string.IsNullOrEmpty(normalizedJavaHome) ? "未检测到Java" : normalizedJavaHome;
    }
    
    // 切换Java版本
    private async Task SwitchJavaVersionAsync(string versionName)
    {
        if (_isSwitching)
        {
            return;
        }

        try
        {
            BeginSwitchingUi(versionName);

            if (!JavaVersionPaths.TryGetValue(versionName, out string? javaPath))
            {
                EndSwitchingUi();
                MessageBox.Show($"未找到 {versionName} 的路径配置。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            EnvironmentPrecheckResult precheckResult = await Task.Run(async () =>
                await RunEnvironmentPrecheckAsync(versionName, javaPath));
            if (!precheckResult.IsSuccess)
            {
                EndSwitchingUi();

                if (!precheckResult.CanForceContinue)
                {
                    MessageBox.Show(precheckResult.Message, precheckResult.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult forceContinue = MessageBox.Show(
                    $"{precheckResult.Message}\n\n是否强制继续切换？\n选择“是”将忽略当前预检问题并继续执行切换。",
                    precheckResult.Title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (forceContinue != MessageBoxResult.Yes)
                {
                    return;
                }

                BeginSwitchingUi(versionName);
            }
            
            await Task.Run(() =>
            {
                // 立即更新JAVA_HOME
                Environment.SetEnvironmentVariable("JAVA_HOME", javaPath, EnvironmentVariableTarget.Machine);
                
                // 立即更新PATH，确保Java bin目录在PATH中
                string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
                string newPath = UpdatePathWithJavaHome(currentPath, javaPath);
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);

                // 自动广播环境变量变更通知
                RefreshEnvironmentVariables();
            });
            
            // 立即更新UI显示
            CurrentJavaVersion.Text = versionName;
            UpdateButtonStates(versionName);
            
            // 显示成功消息
            EndSwitchingUi();
            MessageBox.Show($"成功切换到{versionName}", "切换成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
        }
        catch (Exception ex)
        {
            EndSwitchingUi();
            MessageBox.Show($"切换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            EndSwitchingUi();
        }
    }

    private async Task<EnvironmentPrecheckResult> RunEnvironmentPrecheckAsync(string versionName, string javaPath)
    {
        string normalizedJavaPath = NormalizePath(javaPath);

        bool directoryExists = Directory.Exists(normalizedJavaPath);

        if (!directoryExists)
        {
            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"目标 Java 安装目录不存在：{normalizedJavaPath}\n\n修复建议：检查路径配置是否正确，确认该 JDK/JRE 已安装。");
        }

        bool hasEntries = Directory.EnumerateFileSystemEntries(normalizedJavaPath).Any();

        if (!hasEntries)
        {
            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"目标 Java 安装目录为空：{normalizedJavaPath}\n\n修复建议：重新安装该版本 Java，或修正到实际安装目录。");
        }

        string javaExecutablePath = GetJavaExecutablePath(normalizedJavaPath);
        bool javaExecutableExists = File.Exists(javaExecutablePath);

        if (!javaExecutableExists)
        {
            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"未找到可执行文件：{javaExecutablePath}\n\n修复建议：确认 {normalizedJavaPath}\\bin 下存在 java.exe。");
        }

        JavaCommandResult versionCheckResult = await ExecuteJavaVersionAsync(javaExecutablePath);
        if (!versionCheckResult.IsSuccess)
        {
            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"无法在目标环境中执行 java -version：{javaExecutablePath}\n\n详细原因：{versionCheckResult.ErrorMessage}\n\n修复建议：确认该 java.exe 可正常启动，并检查是否被安全软件或权限策略阻止。");
        }

        string actualVersionName = MapJavaVersionWithPath(versionCheckResult.Version, normalizedJavaPath);
        if (!string.Equals(actualVersionName, versionName, StringComparison.OrdinalIgnoreCase))
        {
            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"目标环境的版本校验未通过。\n\n预期版本：{versionName}\n实际识别：{actualVersionName}\njava -version 输出：\n{versionCheckResult.Output}\n修复建议：检查路径配置是否指向了错误的 JDK/JRE 目录。");
        }

        List<RunningProcessInfo> runningJavaProcesses = FindRunningJavaProcesses();
        if (runningJavaProcesses.Count > 0)
        {
            string occupiedSummary = string.Join(
                "\n",
                runningJavaProcesses.Select(process => $"- {process.ProcessName} (PID: {process.ProcessId})"));

            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"检测到 Java 相关进程仍在运行，已中止切换：\n{occupiedSummary}\n\n修复建议：先关闭这些 Java 进程，再重新执行切换。",
                canForceContinue: true);
        }

        List<string> blockingToolProcesses = FindBlockingToolProcesses();
        if (blockingToolProcesses.Count > 0)
        {
            string processSummary = string.Join("\n", blockingToolProcesses.Select(name => $"- {name}"));
            return EnvironmentPrecheckResult.Fail(
                "切换前预检失败",
                $"检测到 IDE 或构建工具仍在运行：\n{processSummary}\n\n修复建议：请先关闭相关 IDE、Maven、Gradle 等工具，再执行版本切换。",
                canForceContinue: true);
        }

        return EnvironmentPrecheckResult.Success();
    }
    
    // 更新PATH，确保Java bin目录在PATH中
    private string UpdatePathWithJavaHome(string currentPath, string javaHome)
    {
        string javaBinPath = System.IO.Path.Combine(javaHome, "bin");
        List<string> pathParts = currentPath
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();
        
        // 移除所有包含java、jdk、jre的路径
        pathParts.RemoveAll(IsJavaPathEntry);
        
        // 添加新的Java bin路径
        pathParts.Insert(0, javaBinPath);
        
        return string.Join(";", pathParts);
    }
    
    // 刷新环境变量
    private void RefreshEnvironmentVariables()
    {
        try
        {
            // 使用WM_SETTINGCHANGE消息通知系统环境变量已更改
            IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
            uint WM_SETTINGCHANGE = 0x001A;
            uint SMTO_ABORTIFHUNG = 0x0002;
            
            IntPtr result;
            NativeMethods.SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 1000, out result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"刷新环境变量失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BeginSwitchingUi(string versionName)
    {
        _isSwitching = true;
        SetVersionButtonsEnabled(false);
        Cursor = Cursors.Wait;

        _switchingDialog = CreateSwitchingDialog(versionName);
        _switchingDialog.Show();
    }

    private void EndSwitchingUi()
    {
        _isSwitching = false;
        _switchingDialog?.Close();
        _switchingDialog = null;
        SetVersionButtonsEnabled(true);
        Cursor = null;
    }

    private Window CreateSwitchingDialog(string versionName)
    {
        Window dialog = new Window
        {
            Owner = this,
            Title = "正在切换",
            Width = 320,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            ShowInTaskbar = false,
            Topmost = true,
            Background = Brushes.White,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock
                    {
                        Text = $"正在切换到 {versionName}",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.Black,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    new ProgressBar
                    {
                        IsIndeterminate = true,
                        Height = 16,
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    new TextBlock
                    {
                        Text = "请等待切换完成，期间无法点击其他版本按钮。",
                        FontSize = 12,
                        Foreground = Brushes.DimGray,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

        dialog.Closing += (_, e) =>
        {
            if (_isSwitching)
            {
                e.Cancel = true;
            }
        };

        return dialog;
    }
    
    // 按钮点击事件
    private async void BtnJava7_Click(object sender, RoutedEventArgs e)
    {
        await SwitchJavaVersionAsync("Java7");
    }
    
    private async void BtnJava8_32_Click(object sender, RoutedEventArgs e)
    {
        await SwitchJavaVersionAsync("Java8_32");
    }
    
    private async void BtnJava8_Click(object sender, RoutedEventArgs e)
    {
        await SwitchJavaVersionAsync("Java8");
    }
    
    private async void BtnJava11_Click(object sender, RoutedEventArgs e)
    {
        await SwitchJavaVersionAsync("Java11");
    }
    
    private async void BtnJava17_Click(object sender, RoutedEventArgs e)
    {
        await SwitchJavaVersionAsync("Java17");
    }
    
    private async void BtnJava25_Click(object sender, RoutedEventArgs e)
    {
        await SwitchJavaVersionAsync("Java25");
    }

    // 将java -version输出映射为显示名称，考虑JAVA_HOME路径
    private string MapJavaVersionWithPath(string rawVersion, string javaHome)
    {
        // 首先检查JAVA_HOME路径，区分Java8_32和Java8
        foreach (var kvp in JavaVersionPaths)
        {
            if (javaHome.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }
        
        // 如果没有匹配的路径，使用版本号映射
        return MapJavaVersion(rawVersion);
    }
    
    // 将java -version输出映射为显示名称
    private string MapJavaVersion(string rawVersion)
    {
        // 提取版本号前缀（例如：1.8.0_361 → 1.8, 11.0.23 → 11, 17.0.12 → 17, 25.0.1 → 25）
        string versionPrefix = rawVersion;
        
        if (rawVersion.StartsWith("1."))
        {
            // Java 7和8的版本格式：1.7.x 或 1.8.x
            versionPrefix = rawVersion.Substring(0, 3); // 取"1.7"或"1.8"
        }
        else
        {
            // Java 11及以上版本格式：11.x.x 或 17.x.x 或 25.x.x
            int dotIndex = rawVersion.IndexOf('.');
            if (dotIndex > 0)
            {
                versionPrefix = rawVersion.Substring(0, dotIndex); // 取"11"、"17"或"25"
            }
        }
        
        // 查找映射的显示名称
        if (JavaVersionMapping.TryGetValue(versionPrefix, out string? displayName))
        {
            return displayName;
        }
        
        // 如果没有匹配的映射，返回原始版本号
        return rawVersion;
    }
    
    // 更新按钮状态，高亮当前版本
    private void UpdateButtonStates(string displayVersion)
    {
        // 重置所有按钮样式
        ResetButtonStates();
        
        // 根据当前显示版本高亮对应的按钮
        Button? currentButton = displayVersion switch
        {
            "Java7" => BtnJava7,
            "Java8_32" => BtnJava8_32,
            "Java8" => BtnJava8,
            "Java11" => BtnJava11,
            "Java17" => BtnJava17,
            "Java25" => BtnJava25,
            _ => null
        };
        
        if (currentButton != null)
        {
            // 高亮当前版本按钮
            currentButton.Background = Brushes.Green;
        }
    }
    
    // 重置所有按钮状态
    private void ResetButtonStates()
    {
        // 重置所有按钮背景色为默认样式
        BtnJava7.Background = null;
        BtnJava8_32.Background = null;
        BtnJava8.Background = null;
        BtnJava11.Background = null;
        BtnJava17.Background = null;
        BtnJava25.Background = null;
        
        // 应用默认样式
        BtnJava7.Style = (Style)FindResource("PrimaryButtonStyle");
        BtnJava8_32.Style = (Style)FindResource("PrimaryButtonStyle");
        BtnJava8.Style = (Style)FindResource("PrimaryButtonStyle");
        BtnJava11.Style = (Style)FindResource("PrimaryButtonStyle");
        BtnJava17.Style = (Style)FindResource("PrimaryButtonStyle");
        BtnJava25.Style = (Style)FindResource("PrimaryButtonStyle");
    }

    private void SetVersionButtonsEnabled(bool isEnabled)
    {
        BtnJava7.IsEnabled = isEnabled;
        BtnJava8_32.IsEnabled = isEnabled;
        BtnJava8.IsEnabled = isEnabled;
        BtnJava11.IsEnabled = isEnabled;
        BtnJava17.IsEnabled = isEnabled;
        BtnJava25.IsEnabled = isEnabled;
    }

    private static string GetJavaExecutablePath(string javaHome)
    {
        return Path.Combine(javaHome, "bin", "java.exe");
    }

    private static bool IsJavaPathEntry(string pathEntry)
    {
        string expandedPath = Environment.ExpandEnvironmentVariables(pathEntry.Trim().Trim('"'));
        return expandedPath.Contains("java", StringComparison.OrdinalIgnoreCase)
            || expandedPath.Contains("jdk", StringComparison.OrdinalIgnoreCase)
            || expandedPath.Contains("jre", StringComparison.OrdinalIgnoreCase);
    }

    private static List<RunningProcessInfo> FindRunningJavaProcesses()
    {
        List<RunningProcessInfo> results = new List<RunningProcessInfo>();

        foreach (string processName in JavaProcessNames)
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try
                {
                    results.Add(new RunningProcessInfo(process.ProcessName, process.Id));
                }
                catch (InvalidOperationException)
                {
                    // 进程可能在枚举期间退出，忽略即可。
                }
            }
        }

        return results
            .DistinctBy(process => process.ProcessId)
            .OrderBy(process => process.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(process => process.ProcessId)
            .ToList();
    }

    private static List<string> FindBlockingToolProcesses()
    {
        return Process.GetProcesses()
            .Select(process =>
            {
                try
                {
                    return process.ProcessName;
                }
                catch
                {
                    return null;
                }
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => IdeAndBuildToolProcessNames.Contains(name!, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'))).TrimEnd('\\');
    }

    private async Task<JavaCommandResult> ExecuteJavaVersionAsync(string javaExecutablePath)
    {
        try
        {
            using Process process = new Process();
            process.StartInfo.FileName = javaExecutablePath;
            process.StartInfo.Arguments = "-version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string standardOutput = await process.StandardOutput.ReadToEndAsync();
            string standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            string combinedOutput = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
            if (string.IsNullOrWhiteSpace(combinedOutput))
            {
                combinedOutput = standardOutput;
            }

            if (process.ExitCode != 0)
            {
                return JavaCommandResult.Fail($"退出码：{process.ExitCode}", combinedOutput);
            }

            var match = System.Text.RegularExpressions.Regex.Match(combinedOutput, @"(?:java|openjdk) version ""([^""]+)""");
            if (!match.Success)
            {
                return JavaCommandResult.Fail("输出格式不符合预期。", combinedOutput);
            }

            return JavaCommandResult.Success(match.Groups[1].Value, combinedOutput);
        }
        catch (Exception ex)
        {
            return JavaCommandResult.Fail(ex.Message, string.Empty);
        }
    }
}

// 本地方法定义
internal sealed record EnvironmentPrecheckResult(bool IsSuccess, string Title, string Message, bool CanForceContinue)
{
    public static EnvironmentPrecheckResult Success()
    {
        return new EnvironmentPrecheckResult(true, string.Empty, string.Empty, false);
    }

    public static EnvironmentPrecheckResult Fail(string title, string message, bool canForceContinue = false)
    {
        return new EnvironmentPrecheckResult(false, title, message, canForceContinue);
    }
}

internal sealed record JavaCommandResult(bool IsSuccess, string Version, string Output, string ErrorMessage)
{
    public static JavaCommandResult Success(string version, string output)
    {
        return new JavaCommandResult(true, version, output, string.Empty);
    }

    public static JavaCommandResult Fail(string errorMessage, string output)
    {
        return new JavaCommandResult(false, string.Empty, output, errorMessage);
    }
}

internal sealed record RunningProcessInfo(string ProcessName, int ProcessId);

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
}
