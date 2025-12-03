using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code2LlmPrompt.ViewModels
{
    /// <summary>
    /// 主窗口视图模型 - 命令处理部分
    /// 处理所有用户交互命令
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// 切换高级模式命令
        /// </summary>
        [RelayCommand]
        private void ToggleAdvanced()
        {
            IsAdvancedMode = !IsAdvancedMode;

            if (_mainWindow != null)
            {
                if (IsAdvancedMode)
                {
                    _mainWindow.Width = 1200;
                    _mainWindow.Height = 800;
                }
                else
                {
                    _mainWindow.Width = 550;
                    _mainWindow.Height = 420;
                }
            }
        }

        /// <summary>
        /// 生成命令
        /// </summary>
        [RelayCommand]
        private async Task Generate()
        {
            if (IsProcessing) return;

            Output = "";
            ResultContent = "";
            IsProcessing = true;
            Status = "Generating prompt...";

            try
            {
                var arguments = BuildArguments();
                await _processRunner.RunProcessAsync(arguments);
            }
            catch (Exception ex)
            {
                Output = $"Error: {ex.Message}";
                Status = "Error";
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 浏览路径命令
        /// </summary>
        [RelayCommand]
        private async Task BrowsePath()
        {
            var folder = await BrowseFolderAsync();
            if (folder != null)
            {
                Path = folder;
            }
        }

        /// <summary>
        /// 浏览模板命令
        /// </summary>
        [RelayCommand]
        private async Task BrowseTemplate()
        {
            var file = await OpenFileAsync("Template files", new[] { "*.hbs", "*.md", "*.txt", "*" });
            if (file != null)
            {
                Template = file;
            }
        }

        /// <summary>
        /// 复制结果命令
        /// </summary>
        [RelayCommand]
        private async Task CopyResult()
        {
            if (string.IsNullOrEmpty(ResultContent)) return;

            try
            {
                if (_mainWindow?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(ResultContent);
                    Status = "Result copied to clipboard";
                }
                else
                {
                    Status = "Clipboard not available";
                }
            }
            catch (Exception ex)
            {
                Status = $"Copy failed: {ex.Message}";
            }
        }

        /// <summary>
        /// 保存结果命令
        /// </summary>
        [RelayCommand]
        private async Task SaveResult()
        {
            if (string.IsNullOrEmpty(ResultContent)) return;

            var file = await SaveFileAsync("Save result", new[] { "*.md", "*.txt", "*" });
            if (file != null)
            {
                try
                {
                    System.IO.File.Copy(OutputFileName,file);
                    Status = $"Result saved to {System.IO.Path.GetFileName(file)}";
                }
                catch (Exception ex)
                {
                    Status = $"Save failed: {ex.Message}";
                }
            }
        }
    }
}
