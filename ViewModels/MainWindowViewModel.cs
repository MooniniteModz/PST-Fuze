using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTFuze.Models;
using PSTFuze.Services;

namespace PSTFuze.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IPstMergerService _pstMergerService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cancellationTokenSource;
    
    [ObservableProperty]
    private ObservableCollection<PstFileModel> _pstFiles = new();
    
    [ObservableProperty]
    private string? _outputLocation;
    
    [ObservableProperty]
    private bool _isMerging;
    
    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = "Ready to merge PST files...";

    public MainWindowViewModel()
    {
        _pstMergerService = new PstMergerService();
        _dialogService = new DialogService();
    }

    [RelayCommand]
    private async Task AddFiles()
    {
        var options = new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PST Files")
                {
                    Patterns = new[] { "*.pst" },
                    MimeTypes = new[] { "application/vnd.ms-outlook" }
                }
            }
        };

        var files = await _dialogService.OpenFilePickerAsync(options);

        foreach (var file in files)
        {
            var filePath = file.Path.LocalPath;
            PstFiles.Add(new PstFileModel
            {
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                Status = "Ready"
            });
            StatusMessage = $"Added PST file: {filePath}";
        }
    }

    [RelayCommand]
    private void RemoveFile(PstFileModel file)
    {
        PstFiles.Remove(file);
        StatusMessage = $"Removed PST file: {file.FilePath}";
    }

    [RelayCommand]
    private async Task SelectOutput()
    {
        var options = new FilePickerSaveOptions
        {
            DefaultExtension = "pst",
            FileTypeChoices = new[] 
            { 
                new FilePickerFileType("PST File")
                {
                    Patterns = new[] { "*.pst" },
                    MimeTypes = new[] { "application/vnd.ms-outlook" }
                }
            }
        };

        var file = await _dialogService.SaveFilePickerAsync(options);

        if (file != null)
        {
            OutputLocation = file.Path.LocalPath;
            StatusMessage = $"Selected output location: {OutputLocation}";
        }
    }

    [RelayCommand]
    private async Task CancelMerge()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            StatusMessage = "Canceling merge operation...";
        }
    }

    [RelayCommand]
    private async Task Merge()
    {
        if (PstFiles.Count < 2)
        {
            await _dialogService.ShowMessageAsync("Please add at least two PST files to merge.", "Warning");
            return;
        }

        if (string.IsNullOrEmpty(OutputLocation))
        {
            await _dialogService.ShowMessageAsync("Please select an output location.", "Warning");
            return;
        }

        IsMerging = true;
        Progress = 0;
        StatusMessage = "Starting PST merge operation...";

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<(double percentage, string message)>(update => 
            {
                Progress = update.percentage;
                StatusMessage = update.message;
            });

            await _pstMergerService.MergePstFilesAsync(
                PstFiles.Select(f => f.FilePath).ToList(),
                OutputLocation,
                progress,
                _cancellationTokenSource.Token
            );

            StatusMessage = "Merge completed successfully!";
            await _dialogService.ShowMessageAsync("PST files merged successfully!", "Success");
            PstFiles.Clear();
            OutputLocation = null;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Merge operation canceled.";
            await _dialogService.ShowMessageAsync("Merge operation was canceled.", "Operation Canceled");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync($"Error merging PST files: {ex.Message}", "Error");
        }
        finally
        {
            IsMerging = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}