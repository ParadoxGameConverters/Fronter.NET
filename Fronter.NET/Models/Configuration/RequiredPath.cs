using Avalonia.Data;
using Fronter.ViewModels;
using ReactiveUI;
using System.IO;

namespace Fronter.Models.Configuration;

public class RequiredPath : ViewModelBase {
	public bool Mandatory { get; protected set; } = false;
	public virtual bool Outputtable { get; protected set; } = false;
	public string Name { get; protected set; } = string.Empty;
	private string displayName = string.Empty;
	public string DisplayName {
		get => displayName;
		protected set => this.RaiseAndSetIfChanged(ref displayName, value);
	}

	public string Tooltip { get; protected set; } = string.Empty;

	public string SearchPathType { get; protected set; } = string.Empty;
	public string SearchPath { get; protected set; } = string.Empty;
	
	public string valueStr = string.Empty;
	public string Value {
		get => valueStr;
		set {
			if (!Directory.Exists(value)) {
				throw new DataValidationException($"Directory does not exist!"); // TODO: REMOVE DEBUG
			}
			this.RaiseAndSetIfChanged(ref valueStr, value);
		}
	}
}