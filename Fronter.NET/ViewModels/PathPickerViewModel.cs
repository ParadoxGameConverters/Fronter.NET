using Avalonia.Platform.Storage;
using commonItems;
using Fronter.Extensions;
using Fronter.Models.Configuration;
using Fronter.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Fronter.ViewModels;

/// <summary>
///     The PathPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
internal sealed class PathPickerViewModel : ViewModelBase {
	public PathPickerViewModel(Config config) {
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

	private static async Task<IStorageFolder?> GetStartLocationForFile(RequiredFile file, IStorageProvider storageProvider) {
		string? path = null;
		if (file.InitialDirectory is not null) {
			path = file.InitialDirectory;
		} else if (!string.IsNullOrEmpty(file.Value)) {
			path = CommonFunctions.GetPath(file.Value);
		}

		if (string.IsNullOrEmpty(path)) {
			return null;
		}
		if (!Directory.Exists(path)) {
			return null;
		}

		return await storageProvider.TryGetFolderFromPathAsync(path);
	}

	private static async Task<IStorageFolder?> GetStartLocationForFolder(RequiredFolder folder, IStorageProvider storageProvider) {
		var folderPath = folder.Value;
		if (string.IsNullOrEmpty(folderPath)) {
			return null;
		}
		if (!Directory.Exists(folderPath)) {
			return null;
		}
		return await storageProvider.TryGetFolderFromPathAsync(folderPath);
	}

	public async void OpenFolderDialog(RequiredFolder folder) {
		var storageProvider = MainWindow.Instance.StorageProvider;

		var options = new FolderPickerOpenOptions {
			Title = TranslationSource.Instance[folder.DisplayName],
			SuggestedStartLocation = await GetStartLocationForFolder(folder, storageProvider),
		};

		var window = MainWindow.Instance;
		var result = await window.StorageProvider.OpenFolderPickerAsync(options);
		var selectedFile = result.FirstOrDefault(defaultValue: null);
		if (selectedFile is null) {
			Logger.Warn($"{folder.Name}: no folder selected!");
			return;
		}

		var selectedFileUri = selectedFile.Path;
		if (!selectedFileUri.IsAbsoluteUri) {
			Logger.Warn($"URI of folder \"{selectedFile.Name}\" is not absolute!");
		}
		var absolutePath = selectedFileUri.LocalPath;
		folder.Value = absolutePath;
	}
	public async void OpenFileDialog(RequiredFile file) {
		var storageProvider = MainWindow.Instance.StorageProvider;

		var options = new FilePickerOpenOptions {
			Title = TranslationSource.Instance[file.DisplayName],
			AllowMultiple = false,
			SuggestedStartLocation = await GetStartLocationForFile(file, storageProvider),
		};

		var fileType = new FilePickerFileType(file.AllowedExtension.TrimStart('*', '.')) {
			Patterns = new[] { file.AllowedExtension },
		};
		options.FileTypeFilter = new List<FilePickerFileType> { fileType };

		var result = await storageProvider.OpenFilePickerAsync(options);
		var selectedFile = result.FirstOrDefault(defaultValue: null);
		if (selectedFile is null) {
			Logger.Warn($"{file.Name}: no file selected!");
			return;
		}
		var selectedFileUri = selectedFile.Path;
		if (!selectedFileUri.IsAbsoluteUri) {
			Logger.Warn($"URI of file \"{selectedFile.Name}\" is not absolute!");
		}
		var absolutePath = selectedFileUri.LocalPath;
		file.Value = absolutePath;
	}
}