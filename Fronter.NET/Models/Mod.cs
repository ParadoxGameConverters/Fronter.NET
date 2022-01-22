using commonItems;
using Fronter.NET.ViewModels;

namespace Fronter.Models;

public class Mod : ViewModelBase {
	public Mod(string modPath) {
		var parser = new Parser();
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);

		parser.ParseFile(modPath);
		FileName = CommonFunctions.TrimPath(modPath);
	}
	public string Name { get; private set; } = string.Empty;
	public string FileName { get; private set; }
}