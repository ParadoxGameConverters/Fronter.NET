using Fronter.ViewModels;
using ReactiveUI;

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
	
	private string valueStr = string.Empty;
	public virtual string Value {
		get => valueStr;
		set => this.RaiseAndSetIfChanged(ref valueStr, value);
	}
}