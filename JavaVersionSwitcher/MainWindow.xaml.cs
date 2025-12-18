using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
    // Java版本路径配置
    private static readonly IReadOnlyDictionary<string, string> JavaVersionPaths = new Dictionary<string, string>
    {
        {"Java7", "D:\\java\\jre-7u5--i586"},
        {"Java8_32", "D:\\java\\jdk8_32"},
        {"Java8", "D:\\java\\jdk-8u361"},
        {"Java11", "D:\\java\\jdk-11.0.23"},
        {"Java17", "D:\\java\\jdk-17.0.12"},
        {"Java25", "D:\\java\\jdk-25.0.1"}
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
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckCurrentJavaVersion();
    }
    
    // 检查当前Java版本（直接根据JAVA_HOME路径判断，不执行java.exe）
    private void CheckCurrentJavaVersion()
    {
        try
        {
            // 获取系统环境变量中的JAVA_HOME
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine) ?? string.Empty;
            
            // 直接根据JAVA_HOME路径匹配版本名称
            string displayVersion = GetJavaVersionFromPath(javaHome);
            
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
    
    // 根据JAVA_HOME路径获取Java版本名称
    private string GetJavaVersionFromPath(string javaHome)
    {
        // 查找匹配的版本路径
        foreach (var kvp in JavaVersionPaths)
        {
            if (javaHome.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }
        
        // 如果没有匹配的路径，返回默认信息
        return string.IsNullOrEmpty(javaHome) ? "未检测到Java" : javaHome;
    }
    
    // 切换Java版本
    private void SwitchJavaVersion(string versionName)
    {
        try
        {
            // 直接获取Java路径，不进行额外检查
            string javaPath = JavaVersionPaths[versionName];
            
            // 立即更新JAVA_HOME
            Environment.SetEnvironmentVariable("JAVA_HOME", javaPath, EnvironmentVariableTarget.Machine);
            
            // 立即更新PATH，确保Java bin目录在PATH中
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            string newPath = UpdatePathWithJavaHome(currentPath, javaPath);
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
            
            // 刷新当前进程的环境变量
            RefreshEnvironmentVariables();
            
            // 立即更新UI显示
            CurrentJavaVersion.Text = versionName;
            UpdateButtonStates(versionName);
            
            // 显示成功消息
            MessageBox.Show($"成功切换到{versionName}", "切换成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
        }
        catch (Exception ex)
        {
            MessageBox.Show($"切换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // 更新PATH，确保Java bin目录在PATH中
    private string UpdatePathWithJavaHome(string currentPath, string javaHome)
    {
        string javaBinPath = System.IO.Path.Combine(javaHome, "bin");
        List<string> pathParts = new List<string>(currentPath.Split(';'));
        
        // 移除所有包含java、jdk、jre的路径
        pathParts.RemoveAll(p => p.Contains("java") || p.Contains("jdk") || p.Contains("jre"));
        
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
    
    // 按钮点击事件
    private void BtnJava7_Click(object sender, RoutedEventArgs e)
    {
        SwitchJavaVersion("Java7");
    }
    
    private void BtnJava8_32_Click(object sender, RoutedEventArgs e)
    {
        SwitchJavaVersion("Java8_32");
    }
    
    private void BtnJava8_Click(object sender, RoutedEventArgs e)
    {
        SwitchJavaVersion("Java8");
    }
    
    private void BtnJava11_Click(object sender, RoutedEventArgs e)
    {
        SwitchJavaVersion("Java11");
    }
    
    private void BtnJava17_Click(object sender, RoutedEventArgs e)
    {
        SwitchJavaVersion("Java17");
    }
    
    private void BtnJava25_Click(object sender, RoutedEventArgs e)
    {
        SwitchJavaVersion("Java25");
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
}

// 本地方法定义
internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
}