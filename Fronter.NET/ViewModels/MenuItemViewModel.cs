using ReactiveUI;
using System.Collections.Generic;

namespace Fronter.ViewModels;

public class MenuItemViewModel {
	public required string Header { get; set; }
	public required IReactiveCommand Command { get; set; }
	public required object CommandParameter { get; set; }
	public required IList<MenuItemViewModel> Items { get; set; }
}