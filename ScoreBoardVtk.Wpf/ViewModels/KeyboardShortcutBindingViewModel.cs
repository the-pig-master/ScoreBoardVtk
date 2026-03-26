using System.Windows.Input;
using ScoreBoardVtk.Wpf.Infrastructure;
using ScoreBoardVtk.Wpf.Localization;
using ScoreBoardVtk.Wpf.Models;

namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class KeyboardShortcutBindingViewModel : ObservableObject
{
    private string _assignedKeyDisplay = LocalizationManager.Instance.GetString("ShortcutNotAssigned");
    private bool _isCapturing;
    private string _label = string.Empty;

    public KeyboardShortcutBindingViewModel(
        KeyboardShortcutAction action,
        string label,
        Action<KeyboardShortcutAction> beginAssign,
        Action<KeyboardShortcutAction> clear)
    {
        Action = action;
        Label = label;
        BeginAssignCommand = new RelayCommand(() => beginAssign(Action));
        ClearCommand = new RelayCommand(() => clear(Action));
    }

    public KeyboardShortcutAction Action { get; }

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public ICommand BeginAssignCommand { get; }

    public ICommand ClearCommand { get; }

    public string AssignedKeyDisplay
    {
        get => _assignedKeyDisplay;
        set => SetProperty(ref _assignedKeyDisplay, value);
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            if (SetProperty(ref _isCapturing, value))
            {
                OnPropertyChanged(nameof(AssignButtonText));
            }
        }
    }

    public string AssignButtonText => IsCapturing
        ? LocalizationManager.Instance.GetString("ShortcutAssignCapturing")
        : LocalizationManager.Instance.GetString("ShortcutAssign");

    public string ClearButtonText => LocalizationManager.Instance.GetString("ShortcutClear");

    public void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(AssignButtonText));
        OnPropertyChanged(nameof(ClearButtonText));
    }
}
