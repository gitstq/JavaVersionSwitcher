using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace JavaVersionSwitcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 检查是否以管理员身份运行
        if (!IsRunningAsAdministrator())
        {
            // 以管理员身份重启应用
            RestartAsAdministrator();
            Shutdown();
            return;
        }
    }
    
    private bool IsRunningAsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    private void RestartAsAdministrator()
    {
        ProcessStartInfo processInfo = new ProcessStartInfo();
        processInfo.UseShellExecute = true;
        processInfo.FileName = Environment.ProcessPath;
        processInfo.Verb = "runas";
        
        try
        {
            Process.Start(processInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法以管理员身份启动应用程序: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

