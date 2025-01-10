using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PSTFuze.Services;

public interface IDialogService
{
    Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);
    Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);
    Task ShowMessageAsync(string message, string title);
}