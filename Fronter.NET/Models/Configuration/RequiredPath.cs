using Fronter.ViewModels;
using ReactiveUI;

namespace Fronter.Models.Configuration;

internal class RequiredPath : ViewModelBase {
	public bool Mandatory { get; protected set; } = false;
	public virtual bool Outputtable { get; protected set; } = false;
	public string Name { get; protected set; } = string.Empty;

	public string DisplayName {
		get;
		protected set => this.RaiseAndSetIfChanged(ref field, value);
	} = string.Empty;

	public string? Tooltip { get; protected set; }

	public string SearchPathType { get; protected set; } = string.Empty;
	public string SearchPath { get; protected set; } = string.Empty;

	public virtual string Value {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = string.Empty;
}