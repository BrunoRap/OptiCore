using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OptiCore.Models
{
    public enum ImpactLevel { High, Medium, Low }

    public class OptimizationItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isApplied;
        private bool _applyFailed;
        private bool _isExpanded;

        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public ImpactLevel ImpactLevel { get; set; }
        public bool IsApplicable { get; set; }
        public bool IsSafe { get; set; } = true;
        public bool RequiresReboot { get; set; }
        public bool RequiresAdmin { get; set; }
        public string CurrentState { get; set; } = "";
        public string TargetState { get; set; } = "";
        public string ApplicableReason { get; set; } = "";
        public string SkipReason { get; set; } = "";
        public string FailureReason { get; set; } = "";
        public bool HasWarning { get; set; }
        public string WarningText { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool IsApplied
        {
            get => _isApplied;
            set { _isApplied = value; OnPropertyChanged(); }
        }

        public bool ApplyFailed
        {
            get => _applyFailed;
            set { _applyFailed = value; OnPropertyChanged(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
