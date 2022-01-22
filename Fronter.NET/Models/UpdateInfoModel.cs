using Fronter.ViewModels;

namespace Fronter.Models {
	public class UpdateInfoModel : ViewModelBase {
		public string Version { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string? ZipUrl { get; set; }
	}
}
