using Avalonia;
using Avalonia.Themes.Fluent;
using System;
using System.Diagnostics;
using System.Runtime;

namespace Code2LlmPrompt
{
    /// <summary>
    /// 应用程序入口点类
    /// 负责配置和启动Avalonia应用程序
    /// </summary>
    internal sealed class Program
    {
        /// <summary>
        /// 应用程序主入口点
        /// 使用经典桌面生命周期启动应用
        /// </summary>
        /// <param name="args">命令行参数</param>
        [STAThread]
        public static void Main(string[] args)
        {
            // 启用内存诊断
            //GCSettings.LatencyMode = GCLatencyMode.Batch;

            // 创建诊断日志文件
            var dir= System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Code2LlmPrompt");
            System.IO.Directory.CreateDirectory(dir);
            var logPath = System.IO.Path.Combine(dir, $"Code2LlmPrompt_Diagnostic_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            Trace.Listeners.Add(new TextWriterTraceListener(logPath));
            Trace.AutoFlush = true;

            Debug.WriteLine($"=== 诊断开始于 {DateTime.Now} ===");
            Debug.WriteLine($"进程ID: {Process.GetCurrentProcess().Id}");
            Debug.WriteLine($"工作目录: {Environment.CurrentDirectory}");

            // 设置内存监控
            AppDomain.MonitoringIsEnabled = true;

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"应用程序崩溃: {ex}");
                throw;
            }
            finally
            {
                var domain = AppDomain.CurrentDomain;
                Debug.WriteLine($"内存统计:");
                Debug.WriteLine($"  总分配内存: {domain.MonitoringTotalAllocatedMemorySize / 1024 / 1024} MB");
                Debug.WriteLine($"  存活内存: {domain.MonitoringSurvivedMemorySize / 1024 / 1024} MB");
                Debug.WriteLine($"  GC 0: {AppDomain.MonitoringSurvivedProcessMemorySize / 1024 / 1024} MB");
            }
        }

        /// <summary>
        /// 构建Avalonia应用程序
        /// 配置平台检测、字体和日志
        /// </summary>
        /// <returns>配置好的AppBuilder实例</returns>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()  // 自动检测运行平台
                .WithInterFont()      // 使用Inter字体
                .LogToTrace();        // 启用跟踪日志
    }
}
