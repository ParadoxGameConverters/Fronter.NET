using Fronter.Services;
using Xunit;

namespace Fronter.Tests.Services;

public class MarkdownPlainTextRendererTests {
	[Fact]
	public void RendersExampleReleaseNotesMarkdownToPlainText() {
		const string markdown = """
			## 🚀 Features

			- Regional offshoots for Neo-Minoan and Cretan #2857 by @Voldarius

			## 📦 Dependencies

			- Bump Meziantou.Analyzer from 2.0.267 to 2.0.270 #2866 by @dependabot[bot]
			""";

		const string expected = """
			🚀 Features
			- Regional offshoots for Neo-Minoan and Cretan #2857 by @Voldarius

			📦 Dependencies
			- Bump Meziantou.Analyzer from 2.0.267 to 2.0.270 #2866 by @dependabot[bot]
			""";

		Assert.Equal(expected.Trim(), MarkdownPlainTextRenderer.Render(markdown));
	}

	[Fact]
	public void StripsLinksButKeepsLinkText() {
		const string markdown = "See [docs](https://example.com/docs) please.";
		Assert.Equal("See docs please.", MarkdownPlainTextRenderer.Render(markdown));
	}

	[Fact]
	public void StripsEmphasisMarkers() {
		const string markdown = "This is **bold** and *italic* and __bold__ and _italic_.";
		Assert.Equal("This is bold and italic and bold and italic.", MarkdownPlainTextRenderer.Render(markdown));
	}

	[Fact]
	public void PreservesInlineCodeSpans() {
		const string markdown = "Pinned to `51ed8b3`.";
		Assert.Equal("Pinned to `51ed8b3`.", MarkdownPlainTextRenderer.Render(markdown));
	}

	[Fact]
	public void RemovesHeadingHashesAndTrailingHashes() {
		const string markdown = "### Title ###";
		Assert.Equal("Title", MarkdownPlainTextRenderer.Render(markdown));
	}
}
