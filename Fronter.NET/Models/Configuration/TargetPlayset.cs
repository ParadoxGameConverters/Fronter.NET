using Fronter.Models.Database;

namespace Fronter.Models.Configuration; 

public class TargetPlayset(Playset playset) {
	public string Id => playset.Id;
	public string Name => playset.Name;
}