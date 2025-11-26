using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Code2PromptGUI.Models
{
    public class ProcessRunner
    {
        public event Action<string>? OutputReceived;

        public event Action<string>? ErrorReceived;

        public event Action<int>? ProcessExited;

        private readonly string _toolPath;

        public ProcessRunner()
        {
            // 获取内置工具的路径
            _toolPath = GetEmbeddedToolPath();
        }

        public async Task RunProcessAsync(string arguments)
        {
            if (!File.Exists(_toolPath))
            {
                ErrorReceived?.Invoke($"Tool not found: {_toolPath}");
                ProcessExited?.Invoke(1);
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _toolPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory,
                // 设置环境变量以避免某些问题
                Environment =
                {
                    ["RUST_BACKTRACE"] = "1" // 启用详细的错误回溯
                }
            };

            using var process = new Process { StartInfo = processStartInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ErrorReceived?.Invoke(e.Data);
                }
            };

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                ProcessExited?.Invoke(process.ExitCode);
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Process error: {ex.Message}");
                ProcessExited?.Invoke(1);
            }
        }

        private string GetEmbeddedToolPath()
        {
            // 尝试多种可能的路径
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Tools", "code2prompt.exe"),
                Path.Combine(AppContext.BaseDirectory, "code2prompt.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "Tools", "code2prompt.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "code2prompt.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // 如果找不到，返回第一个路径（将在运行时产生错误）
            return possiblePaths[0];
        }
    }
}
