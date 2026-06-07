using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OptiCore.Models;
using OptiCore.Services;

namespace OptiCore.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _statusMessage = "Ready";
        private object? _currentView;
        private string _activeNav = "hardware";

        public HardwareProfile? Hardware { get; set; }
        public SystemMetrics Metrics { get; } = new();

        public HardwareDetectionService HardwareService { get; } = new();
        public DecisionEngineService DecisionEngine { get; } = new();
        public BackupService BackupService { get; } = new();
        public MetricsService MetricsService { get; } = new();
        public RollbackService RollbackService { get; } = new();
        public ReportService ReportService { get; } = new();

        public List<OptimizationItem> Optimizations { get; set; } = new();
        public string SessionId { get; private set; } = new BackupService().NewSessionId();

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public string ActiveNav
        {
            get => _activeNav;
            set { _activeNav = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
