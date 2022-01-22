using ReactiveUI;

namespace Fronter.ViewModels {
	public class MarkdownViewModel : ReactiveObject {
		private string _MdText = "## 🚀 Features\n\n- Use platform-dependent slashes in more paths #543 by @IhateTrains";
		public string MdText {
			get => _MdText;
			set => this.RaiseAndSetIfChanged(ref _MdText, value);
		}
	}
}