using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace EndpointDesktop
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<HistoryItem> _history = new();
        private List<ActionCategory> _categories = new();

        public MainWindow()
        {
            InitializeComponent();
            MachineNameText.Text = Environment.MachineName;
            HistoryList.ItemsSource = _history;
            LoadActions();
        }

        private void LoadActions()
        {
            // Define action categories (same as web app)
            _categories = new List<ActionCategory>
            {
                new ActionCategory
                {
                    Id = "network",
                    Title = "Network & Connectivity",
                    Description = "Network diagnostics and fixes",
                    Icon = "🌐",
                    Actions = new List<ActionItem>
                    {
                        new ActionItem { Id = "flush-dns", Name = "Flush DNS Cache", Description = "Clears the local DNS resolver cache", Script = "Clear-DnsClientCache; Write-Output 'DNS cache cleared successfully'; ipconfig /displaydns | Select-Object -First 5", RequiresAdmin = false },
                        new ActionItem { Id = "release-renew", Name = "Release/Renew IP", Description = "Releases and renews DHCP IP address", Script = "ipconfig /release; Start-Sleep -Seconds 2; ipconfig /renew; ipconfig | Select-String 'IPv4|Subnet|Gateway'", RequiresAdmin = false },
                        new ActionItem { Id = "netstat", Name = "Active Connections", Description = "Shows active network connections", Script = "Get-NetTCPConnection -State Established | Select-Object LocalAddress, LocalPort, RemoteAddress, RemotePort, OwningProcess | Format-Table -AutoSize", RequiresAdmin = false },
                        new ActionItem { Id = "ping-test", Name = "Connectivity Test", Description = "Tests connectivity to common endpoints", Script = "$targets = @('8.8.8.8','1.1.1.1','google.com'); foreach ($t in $targets) { $ping = Test-Connection $t -Count 2 -ErrorAction SilentlyContinue; if ($ping) { Write-Output \"$t : $([math]::Round(($ping | Measure-Object ResponseTime -Average).Average))ms\" } else { Write-Output \"$t : UNREACHABLE\" } }", RequiresAdmin = false }
                    }
                },
                new ActionCategory
                {
                    Id = "sccm",
                    Title = "SCCM / ConfigMgr",
                    Description = "SCCM client operations",
                    Icon = "🔧",
                    Actions = new List<ActionItem>
                    {
                        new ActionItem { Id = "sccm-actions", Name = "Trigger All SCCM Actions", Description = "Triggers all SCCM client actions", Script = "$actions = @('{00000000-0000-0000-0000-000000000021}','{00000000-0000-0000-0000-000000000022}','{00000000-0000-0000-0000-000000000001}','{00000000-0000-0000-0000-000000000002}'); foreach ($a in $actions) { Invoke-WmiMethod -Namespace root\\ccm -Class SMS_Client -Name TriggerSchedule -ArgumentList $a -ErrorAction SilentlyContinue | Out-Null }; Write-Output 'All SCCM actions triggered'", RequiresAdmin = false },
                        new ActionItem { Id = "sccm-cache", Name = "Clear SCCM Cache", Description = "Clears the SCCM client cache", Script = "$cm = New-Object -ComObject UIResource.UIResourceMgr; $cache = $cm.GetCacheInfo(); $cache.GetCacheElements() | ForEach-Object { $cache.DeleteCacheElement($_.CacheElementID) }; Write-Output 'SCCM cache cleared'", RequiresAdmin = true },
                        new ActionItem { Id = "sccm-repair", Name = "Repair SCCM Client", Description = "Repairs the SCCM client installation", Script = "& 'C:\\Windows\\CCM\\ccmrepair.exe'; Write-Output 'SCCM client repair initiated'", RequiresAdmin = true }
                    }
                },
                new ActionCategory
                {
                    Id = "system",
                    Title = "System Information",
                    Description = "View system details",
                    Icon = "💻",
                    Actions = new List<ActionItem>
                    {
                        new ActionItem { Id = "sys-info", Name = "System Info", Description = "Shows basic system information", Script = "$os = Get-CimInstance Win32_OperatingSystem; $cs = Get-CimInstance Win32_ComputerSystem; Write-Output \"Computer: $($cs.Name)\"; Write-Output \"OS: $($os.Caption)\"; Write-Output \"Version: $($os.Version)\"; Write-Output \"RAM: $([math]::Round($cs.TotalPhysicalMemory/1GB, 2)) GB\"; Write-Output \"Manufacturer: $($cs.Manufacturer)\"; Write-Output \"Model: $($cs.Model)\"", RequiresAdmin = false },
                        new ActionItem { Id = "disk-space", Name = "Disk Space", Description = "Shows disk usage for all drives", Script = "Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Used -ne $null } | ForEach-Object { $total = [math]::Round(($_.Used + $_.Free) / 1GB, 1); $free = [math]::Round($_.Free / 1GB, 1); $pct = [math]::Round(($_.Used / ($_.Used + $_.Free)) * 100, 0); Write-Output \"Drive $($_.Name): $free GB free of $total GB ($pct% used)\" }", RequiresAdmin = false },
                        new ActionItem { Id = "uptime", Name = "System Uptime", Description = "Shows how long the system has been running", Script = "$boot = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime; $uptime = (Get-Date) - $boot; Write-Output \"Last Boot: $boot\"; Write-Output \"Uptime: $($uptime.Days) days, $($uptime.Hours) hours, $($uptime.Minutes) minutes\"", RequiresAdmin = false }
                    }
                },
                new ActionCategory
                {
                    Id = "cleanup",
                    Title = "Disk Cleanup",
                    Description = "Free up disk space",
                    Icon = "🧹",
                    Actions = new List<ActionItem>
                    {
                        new ActionItem { Id = "clear-temp", Name = "Clear Temp Files", Description = "Clears user and system temp folders", Script = "$before = [math]::Round((Get-PSDrive C).Free / 1GB, 2); Remove-Item \"$env:TEMP\\*\" -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\\Windows\\Temp\\*' -Recurse -Force -ErrorAction SilentlyContinue; $after = [math]::Round((Get-PSDrive C).Free / 1GB, 2); Write-Output \"Temp files cleared. Space recovered: $([math]::Round($after - $before, 2)) GB\"", RequiresAdmin = true },
                        new ActionItem { Id = "clear-updates", Name = "Clear Windows Update Cache", Description = "Clears the Windows Update download cache", Script = "Stop-Service wuauserv -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\\Windows\\SoftwareDistribution\\Download\\*' -Recurse -Force -ErrorAction SilentlyContinue; Start-Service wuauserv; Write-Output 'Windows Update cache cleared'", RequiresAdmin = true },
                        new ActionItem { Id = "empty-bin", Name = "Empty Recycle Bin", Description = "Empties the recycle bin for all users", Script = "Clear-RecycleBin -Force -ErrorAction SilentlyContinue; Write-Output 'Recycle bin emptied'", RequiresAdmin = false }
                    }
                },
                new ActionCategory
                {
                    Id = "services",
                    Title = "Services",
                    Description = "Manage Windows services",
                    Icon = "⚡",
                    Actions = new List<ActionItem>
                    {
                        new ActionItem { Id = "restart-spooler", Name = "Restart Print Spooler", Description = "Restarts the print spooler service", Script = "Restart-Service Spooler -Force; Get-Service Spooler | Select-Object Name, Status | Format-Table", RequiresAdmin = true },
                        new ActionItem { Id = "restart-wmi", Name = "Restart WMI", Description = "Restarts Windows Management Instrumentation", Script = "Restart-Service Winmgmt -Force; Write-Output 'WMI service restarted'", RequiresAdmin = true },
                        new ActionItem { Id = "list-services", Name = "List Running Services", Description = "Shows all running services", Script = "Get-Service | Where-Object { $_.Status -eq 'Running' } | Select-Object Name, DisplayName | Format-Table -AutoSize", RequiresAdmin = false }
                    }
                }
            };

            BuildUI();
        }

        private void BuildUI()
        {
            ActionsPanel.Children.Clear();

            foreach (var category in _categories)
            {
                // Category header
                var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
                header.Children.Add(new TextBlock { Text = category.Icon, FontSize = 20, Margin = new Thickness(0, 0, 10, 0) });
                header.Children.Add(new TextBlock { Text = category.Title, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextPrimary") });
                ActionsPanel.Children.Add(header);

                // Category description
                ActionsPanel.Children.Add(new TextBlock 
                { 
                    Text = category.Description, 
                    Foreground = (Brush)FindResource("TextMuted"), 
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 12) 
                });

                // Actions wrap panel
                var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 24) };

                foreach (var action in category.Actions)
                {
                    var card = CreateActionCard(action);
                    wrapPanel.Children.Add(card);
                }

                ActionsPanel.Children.Add(wrapPanel);
            }
        }

        private Border CreateActionCard(ActionItem action)
        {
            var card = new Border
            {
                Width = 280,
                Margin = new Thickness(0, 0, 12, 12),
                Background = (Brush)FindResource("BgCard"),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title row
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal };
            titlePanel.Children.Add(new TextBlock 
            { 
                Text = action.Name, 
                FontSize = 14, 
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary")
            });
            if (action.RequiresAdmin)
            {
                titlePanel.Children.Add(new TextBlock 
                { 
                    Text = "🔒", 
                    FontSize = 12, 
                    Margin = new Thickness(6, 0, 0, 0),
                    ToolTip = "Requires admin privileges"
                });
            }
            Grid.SetRow(titlePanel, 0);
            grid.Children.Add(titlePanel);

            // Description
            var desc = new TextBlock
            {
                Text = action.Description,
                Foreground = (Brush)FindResource("TextMuted"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 12)
            };
            Grid.SetRow(desc, 1);
            grid.Children.Add(desc);

            // Run button
            var runBtn = new Button
            {
                Content = "▶ Run",
                Style = (Style)FindResource("PrimaryButton"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = action
            };
            runBtn.Click += RunAction_Click;
            Grid.SetRow(runBtn, 2);
            grid.Children.Add(runBtn);

            card.Child = grid;

            // Hover effect
            card.MouseEnter += (s, e) => card.Background = (Brush)FindResource("BgCardHover");
            card.MouseLeave += (s, e) => card.Background = (Brush)FindResource("BgCard");

            return card;
        }

        private async void RunAction_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var action = (ActionItem)button.Tag;

            button.IsEnabled = false;
            button.Content = "⏳ Running...";
            OutputText.Text = $"Executing: {action.Name}...\n\n";

            var historyItem = new HistoryItem
            {
                ActionName = action.Name,
                StartTime = DateTime.Now,
                Status = "Running"
            };
            _history.Insert(0, historyItem);

            try
            {
                var result = await ExecutePowerShell(action.Script);
                
                OutputText.Text += result.Output;
                if (!string.IsNullOrEmpty(result.Error))
                {
                    OutputText.Text += $"\n\nErrors:\n{result.Error}";
                }
                OutputText.Text += $"\n\n✓ Completed in {result.DurationMs}ms";

                historyItem.Status = result.Success ? "Success" : "Failed";
                historyItem.DurationMs = result.DurationMs;
            }
            catch (Exception ex)
            {
                OutputText.Text += $"\n\n❌ Error: {ex.Message}";
                historyItem.Status = "Failed";
            }

            // Refresh history display
            HistoryList.ItemsSource = null;
            HistoryList.ItemsSource = _history;

            button.IsEnabled = true;
            button.Content = "▶ Run";
        }

        private async Task<PowerShellResult> ExecutePowerShell(string script)
        {
            var result = new PowerShellResult();
            var sw = Stopwatch.StartNew();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    throw new Exception("Failed to start PowerShell process");
                }

                result.Output = await process.StandardOutput.ReadToEndAsync();
                result.Error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                sw.Stop();
                result.DurationMs = (int)sw.ElapsedMilliseconds;
                result.Success = process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Error = ex.Message;
                result.DurationMs = (int)sw.ElapsedMilliseconds;
                result.Success = false;
            }

            return result;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
        }
    }

    // Models
    public class ActionCategory
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<ActionItem> Actions { get; set; } = new();
    }

    public class ActionItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Script { get; set; } = "";
        public bool RequiresAdmin { get; set; }
    }

    public class HistoryItem : INotifyPropertyChanged
    {
        public string ActionName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string Status { get; set; } = "";
        public int DurationMs { get; set; }

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - StartTime;
                if (diff.TotalSeconds < 60) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return StartTime.ToShortDateString();
            }
        }

        public Brush StatusColor => Status switch
        {
            "Success" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            "Failed" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            "Running" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
        };

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class PowerShellResult
    {
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public bool Success { get; set; }
        public int DurationMs { get; set; }
    }
}
