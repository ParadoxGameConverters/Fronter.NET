using commonItems;
using Fronter.ViewModels;

namespace Fronter.Models.Configuration;

internal sealed class Mod : ViewModelBase {
	public Mod(string modPath) {
		var parser = new Parser();
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.IgnoreUnregisteredItems();

		parser.ParseFile(modPath);
		FileName = CommonFunctions.TrimPath(modPath);
	}
	public string Name { get; private set; } = string.Empty;
	public string FileName { get; }
	public bool Enabled { get; set; } = false;
}