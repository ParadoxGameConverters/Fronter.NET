using Avalonia.Controls;
using commonItems;
using Fronter.Extensions;
using Fronter.Models.Configuration;
using Fronter.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels;

/// <summary>
///     The PathPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
public class PathPickerViewModel : ViewModelBase {
	public PathPickerViewModel(Configuration config) {
		RequiredFolders = new ObservableCollection<RequiredFolder>(config.RequiredFolders);
		RequiredFiles = new ObservableCollection<RequiredFile>(config.RequiredFiles);
	}

	public ObservableCollection<RequiredFolder> RequiredFolders { get; }
	public ObservableCollection<RequiredFile> RequiredFiles { get; }

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
		var dlg = new OpenFileDialog {
			Title = TranslationSource.Instance[file.DisplayName]
		};
		if (file.InitialDirectory is not null) {
			dlg.Directory = file.InitialDirectory;
		} else if (!string.IsNullOrEmpty(file.Value)) {
			dlg.Directory = CommonFunctions.GetPath(file.Value);
		}

		var filter = new FileDialogFilter {Extensions = {file.AllowedExtension.TrimStart('*', '.')}};
		if (dlg.Filters is null) {
			dlg.Filters = new List<FileDialogFilter> {filter};
		} else {
			dlg.Filters.Add(filter);
		}

		var result = await dlg.ShowAsync(MainWindow.Instance);
		if (result is not null) {
			file.Value = result[0];
		}
	}
}