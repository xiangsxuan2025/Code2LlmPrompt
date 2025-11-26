using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Code2PromptGUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Code2PromptGUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ProcessRunner _processRunner;
        private Window? _mainWindow;

        [ObservableProperty]
        private string _status = "Ready";

        [ObservableProperty]
        private string _path = ".";

        [ObservableProperty]
        private string _outputFile = "";

        [ObservableProperty]
        private bool _clipboard = true;

        [ObservableProperty]
        private string _includePatterns = "";

        [ObservableProperty]
        private string _excludePatterns = "";

        [ObservableProperty]
        private bool _followSymlinks;

        [ObservableProperty]
        private bool _hidden;

        [ObservableProperty]
        private bool _noIgnore;

        [ObservableProperty]
        private string _outputFormat = "markdown";

        [ObservableProperty]
        private string _template = "";

        [ObservableProperty]
        private bool _lineNumbers;

        [ObservableProperty]
        private bool _absolutePaths;

        [ObservableProperty]
        private bool _noCodeblock;

        [ObservableProperty]
        private bool _fullDirectoryTree;

        [ObservableProperty]
        private bool _diff;

        [ObservableProperty]
        private string _gitDiffBranches = "";

        [ObservableProperty]
        private string _gitLogBranches = "";

        [ObservableProperty]
        private string _encoding = "cl100k";

        [ObservableProperty]
        private string _tokenFormat = "format";

        [ObservableProperty]
        private bool _tokenMap;

        [ObservableProperty]
        private bool _quiet;

        [ObservableProperty]
        private string _output = "";

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private bool _hasOutput;

        [ObservableProperty]
        private string _tokenInfo = "";

        public ObservableCollection<string> OutputFormats { get; } = new()
        {
            "markdown", "json", "xml"
        };

        public ObservableCollection<string> Encodings { get; } = new()
        {
            "cl100k", "p50k", "p50k_edit", "r50k"
        };

        public ObservableCollection<string> TokenFormats { get; } = new()
        {
            "raw", "format"
        };

        public MainViewModel()
        {
            _processRunner = new ProcessRunner();
            _processRunner.OutputReceived += OnOutputReceived;
            _processRunner.ErrorReceived += OnErrorReceived;
            _processRunner.ProcessExited += OnProcessExited;
        }

        // 设置主窗口引用
        public void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        [RelayCommand]
        private async Task Generate()
        {
            if (IsProcessing) return;

            Output = "";
            HasOutput = false;
            TokenInfo = "";
            IsProcessing = true;
            Status = "Generating prompt...";

            try
            {
                var arguments = BuildArguments();
                await _processRunner.RunProcessAsync("code2prompt", arguments);
            }
            catch (Exception ex)
            {
                Output = $"Error: {ex.Message}";
                Status = "Error";
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task BrowsePath()
        {
            var folder = await BrowseFolderAsync();
            if (folder != null)
            {
                Path = folder;
            }
        }

        [RelayCommand]
        private async Task BrowseOutput()
        {
            var file = await SaveFileAsync("Prompt output", new[] { "*.md", "*.txt", "*" });
            if (file != null)
            {
                OutputFile = file;
            }
        }

        [RelayCommand]
        private async Task BrowseTemplate()
        {
            var file = await OpenFileAsync("Template files", new[] { "*.hbs", "*.md", "*.txt", "*" });
            if (file != null)
            {
                Template = file;
            }
        }

        [RelayCommand]
        private async Task CopyOutput()
        {
            if (string.IsNullOrEmpty(Output)) return;

            try
            {
                if (_mainWindow?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(Output);
                    Status = "Copied to clipboard";
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

        [RelayCommand]
        private async Task SaveOutput()
        {
            if (string.IsNullOrEmpty(Output)) return;

            var file = await SaveFileAsync("Save prompt output", new[] { "*.md", "*.txt", "*" });
            if (file != null)
            {
                try
                {
                    await File.WriteAllTextAsync(file, Output);
                    Status = $"Saved to {System.IO.Path.GetFileName(file)}";
                }
                catch (Exception ex)
                {
                    Status = $"Save failed: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void ClearOutput()
        {
            Output = "";
            HasOutput = false;
            TokenInfo = "";
            Status = "Ready";
        }

        private string BuildArguments()
        {
            var args = new System.Text.StringBuilder();

            // 基本路径
            if (!string.IsNullOrEmpty(Path) && Path != ".")
                args.Append($" \"{Path}\"");

            // 输出文件
            if (!string.IsNullOrEmpty(OutputFile))
                args.Append($" -O \"{OutputFile}\"");

            // 剪贴板
            if (Clipboard)
                args.Append(" -c");

            // 包含模式
            if (!string.IsNullOrEmpty(IncludePatterns))
            {
                foreach (var pattern in IncludePatterns.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(pattern))
                        args.Append($" -i \"{pattern.Trim()}\"");
                }
            }

            // 排除模式
            if (!string.IsNullOrEmpty(ExcludePatterns))
            {
                foreach (var pattern in ExcludePatterns.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(pattern))
                        args.Append($" -e \"{pattern.Trim()}\"");
                }
            }

            // 文件选项
            if (FollowSymlinks)
                args.Append(" -L");

            if (Hidden)
                args.Append(" --hidden");

            if (NoIgnore)
                args.Append(" --no-ignore");

            // 输出格式
            if (!string.IsNullOrEmpty(OutputFormat) && OutputFormat != "markdown")
                args.Append($" -F {OutputFormat}");

            // 模板
            if (!string.IsNullOrEmpty(Template))
                args.Append($" -t \"{Template}\"");

            // 显示选项
            if (LineNumbers)
                args.Append(" --line-numbers");

            if (AbsolutePaths)
                args.Append(" --absolute-paths");

            if (NoCodeblock)
                args.Append(" --no-codeblock");

            if (FullDirectoryTree)
                args.Append(" --full-directory-tree");

            // Git 集成
            if (Diff)
                args.Append(" --diff");

            if (!string.IsNullOrEmpty(GitDiffBranches))
            {
                var branches = GitDiffBranches.Split(',');
                if (branches.Length == 2)
                    args.Append($" --git-diff-branch {branches[0].Trim()},{branches[1].Trim()}");
            }

            if (!string.IsNullOrEmpty(GitLogBranches))
            {
                var branches = GitLogBranches.Split(',');
                if (branches.Length == 2)
                    args.Append($" --git-log-branch {branches[0].Trim()},{branches[1].Trim()}");
            }

            // Token 设置
            if (!string.IsNullOrEmpty(Encoding) && Encoding != "cl100k")
                args.Append($" --encoding {Encoding}");

            if (!string.IsNullOrEmpty(TokenFormat) && TokenFormat != "format")
                args.Append($" --token-format {TokenFormat}");

            if (TokenMap)
                args.Append(" --token-map");

            if (Quiet)
                args.Append(" -q");

            return args.ToString().Trim();
        }

        private void OnOutputReceived(string data)
        {
            Output += data + Environment.NewLine;
            HasOutput = true;

            // 提取token计数信息
            if (data.Contains("Token count:") || data.Contains("token_count"))
            {
                TokenInfo = ExtractTokenInfo(data);
            }
        }

        private void OnErrorReceived(string data)
        {
            Output += $"ERROR: {data}{Environment.NewLine}";
            HasOutput = true;
        }

        private void OnProcessExited(int exitCode)
        {
            IsProcessing = false;
            Status = exitCode == 0 ? "Completed" : "Failed";
        }

        private string ExtractTokenInfo(string data)
        {
            // 简单提取token信息
            if (data.Contains("Token count:"))
            {
                var start = data.IndexOf("Token count:") + "Token count:".Length;
                var end = data.IndexOf(",", start);
                if (end == -1) end = data.Length;
                return data.Substring(start, end - start).Trim();
            }
            return "";
        }

        private async Task<string?> BrowseFolderAsync()
        {
            var storageProvider = GetStorageProvider();
            if (storageProvider == null) return null;

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select directory to analyze",
                AllowMultiple = false
            });

            return folders.Count > 0 ? folders[0].Path.LocalPath : null;
        }

        private async Task<string?> OpenFileAsync(string title, string[] fileTypes)
        {
            var storageProvider = GetStorageProvider();
            if (storageProvider == null) return null;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = fileTypes.Select(ft =>
                    new FilePickerFileType(System.IO.Path.GetExtension(ft).TrimStart('.').ToUpper() + " Files")
                    {
                        Patterns = new[] { ft }
                    }).ToArray()
            });

            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }

        private async Task<string?> SaveFileAsync(string title, string[] fileTypes)
        {
            var storageProvider = GetStorageProvider();
            if (storageProvider == null) return null;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                FileTypeChoices = fileTypes.Select(ft =>
                    new FilePickerFileType(System.IO.Path.GetExtension(ft).TrimStart('.').ToUpper() + " Files")
                    {
                        Patterns = new[] { ft }
                    }).ToArray()
            });

            return file?.Path.LocalPath;
        }

        private IStorageProvider? GetStorageProvider()
        {
            return TopLevel.GetTopLevel(_mainWindow)?.StorageProvider;
        }
    }
}
