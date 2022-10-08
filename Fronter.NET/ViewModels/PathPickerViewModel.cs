using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using commonItems;
using Fronter.Extensions;
using Fronter.Models.Configuration;
using Fronter.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace Fronter.ViewModels;

/// <summary>
///     The PathPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
public class PathPickerViewModel : ViewModelBase {
	public PathPickerViewModel(Configuration config) {
		RequiredFolders = new ObservableCollection<RequiredFolder>(config.RequiredFolders);
		RequiredFiles = new ObservableCollection<RequiredFile>(config.RequiredFiles);
		
		// Create reactive commands.
		OpenFolderDialogCommand = ReactiveCommand.Create<RequiredFolder>(OpenFolderDialog);
		OpenFileDialogCommand = ReactiveCommand.Create<RequiredFile>(OpenFileDialog);
	}

	public ObservableCollection<RequiredFolder> RequiredFolders { get; }
	public ObservableCollection<RequiredFile> RequiredFiles { get; }
	
	public ReactiveCommand<RequiredFolder, Unit> OpenFolderDialogCommand { get; }
	public ReactiveCommand<RequiredFile, Unit> OpenFileDialogCommand { get; }

	public async void OpenFolderDialog(RequiredFolder folder) {
		var dlg = new OpenFolderDialog {
			Title = TranslationSource.Instance[folder.DisplayName],
			Directory = string.IsNullOrEmpty(folder.Value) ? null : folder.Value
		};

		var result = await dlg.ShowAsync(MainWindow.Instance);
		if (result is not null) {
			folder.Value = result;
		}
	}
	public async void OpenFileDialog(RequiredFile file) {
		var options = new FilePickerOpenOptions {
			Title = TranslationSource.Instance[file.DisplayName],
			AllowMultiple = false
		};
		
		if (file.InitialDirectory is not null) {
			options.SuggestedStartLocation = new BclStorageFolder(file.InitialDirectory);
		} else if (!string.IsNullOrEmpty(file.Value)) {
			options.SuggestedStartLocation = new BclStorageFolder(CommonFunctions.GetPath(file.Value));
		}

		var fileType = new FilePickerFileType(file.AllowedExtension.TrimStart('*', '.')) {
			Patterns = new[] {file.AllowedExtension}
		};
		options.FileTypeFilter = new List<FilePickerFileType> {fileType};

		var window = MainWindow.Instance;
		var result = await window.StorageProvider.OpenFilePickerAsync(options);
		var selectedFile = result.FirstOrDefault(defaultValue: null);
		if (selectedFile is null) {
			Logger.Warn($"{file.Name}: no file selected!");
			return;
		}

		if (!selectedFile.TryGetUri(out var uri)) {
			Logger.Warn($"Can't set file path: selected file \"{selectedFile.Name}\" has no path!");
			return;
		}
		if (!uri.IsAbsoluteUri) {
			Logger.Warn($"Can't set file path: uri of file \"{selectedFile.Name}\" is not absolute!");
		}
		var absolutePath = uri.LocalPath;
		file.Value = absolutePath;
	}
}