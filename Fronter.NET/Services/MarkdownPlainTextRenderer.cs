using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fronter.Services;

internal static class MarkdownPlainTextRenderer {
	private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
	private const RegexOptions DefaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private static Regex R(string pattern) => new(pattern, DefaultRegexOptions, RegexTimeout);

	private static readonly Regex HeadingRegex = R(@"^\s{0,3}#{1,6}\s+(?<text>.*)$");
	private static readonly Regex UnorderedListRegex = R(@"^\s{0,3}[-*+]\s+(?<text>.*)$");
	private static readonly Regex OrderedListRegex = R(@"^\s{0,3}(?<num>\d+)[\.\)]\s+(?<text>.*)$");
	private static readonly Regex BlockquoteRegex = R(@"^\s{0,3}>\s?(?<text>.*)$");
	private static readonly Regex TrailingHeadingHashesRegex = R(@"\s+#+\s*$");

	private static readonly Regex ImageRegex = R(@"!\[(?<alt>[^\]]*)\]\((?<url>[^\)]*)\)");
	private static readonly Regex LinkRegex = R(@"\[(?<text>[^\]]+)\]\((?<url>[^\)]*)\)");

	private static readonly Regex BoldAsteriskRegex = R(@"\*\*(?<text>[^*]+)\*\*");
	private static readonly Regex BoldUnderscoreRegex = R(@"__(?<text>[^_]+)__");
	private static readonly Regex ItalicAsteriskRegex = R(@"(?<!\*)\*(?<text>[^*]+)\*(?!\*)");
	private static readonly Regex ItalicUnderscoreRegex = R(@"(?<!_)_(?<text>[^_]+)_(?!_)");

	private static readonly Regex InlineHtmlTagRegex = R(@"<[^>]+>");
	private static readonly Regex HorizontalRuleRegex = R(@"^\s{0,3}(-{3,}|\*{3,}|_{3,})\s*$");

	public static string Render(string? markdown) {
		if (string.IsNullOrWhiteSpace(markdown)) {
			return string.Empty;
		}

		string normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
		string[] lines = normalized.Split('\n');
		var outputLines = RenderLines(lines);
		return string.Join(Environment.NewLine, CollapseBlankLines(outputLines)).Trim();
	}

	private static List<string> RenderLines(string[] lines) {
		var outputLines = new List<string>(capacity: lines.Length);
		bool inFencedCodeBlock = false;
		bool previousOutputWasHeading = false;

		foreach (string line in lines.Select(l => l.TrimEnd())) {
			if (IsFencedCodeBlockDelimiter(line)) {
				inFencedCodeBlock = !inFencedCodeBlock;
				continue; // omit the fences themselves
			}

			if (inFencedCodeBlock) {
				outputLines.Add(line);
				previousOutputWasHeading = false;
				continue;
			}

			if (string.IsNullOrWhiteSpace(line)) {
				if (!previousOutputWasHeading) {
					outputLines.Add(string.Empty);
				}
				continue;
			}

			if (HorizontalRuleRegex.IsMatch(line)) {
				continue;
			}

			if (TryRenderHeading(line, out string heading)) {
				outputLines.Add(heading);
				previousOutputWasHeading = true;
				continue;
			}

			if (TryRenderUnorderedListItem(line, out string ulItem)) {
				outputLines.Add(ulItem);
				previousOutputWasHeading = false;
				continue;
			}

			if (TryRenderOrderedListItem(line, out string olItem)) {
				outputLines.Add(olItem);
				previousOutputWasHeading = false;
				continue;
			}

			if (TryRenderBlockquote(line, out string quote)) {
				outputLines.Add(quote);
				previousOutputWasHeading = false;
				continue;
			}

			outputLines.Add(ProcessInline(line).Trim());
			previousOutputWasHeading = false;
		}

		return outputLines;
	}

	private static bool TryRenderHeading(string line, out string rendered) {
		var headingMatch = HeadingRegex.Match(line);
		if (!headingMatch.Success) {
			rendered = string.Empty;
			return false;
		}

		string headingText = headingMatch.Groups["text"].Value;
		headingText = TrailingHeadingHashesRegex.Replace(headingText, string.Empty);
		rendered = ProcessInline(headingText).Trim();
		return true;
	}

	private static bool TryRenderUnorderedListItem(string line, out string rendered) {
		var ulMatch = UnorderedListRegex.Match(line);
		if (!ulMatch.Success) {
			rendered = string.Empty;
			return false;
		}

		string itemText = ProcessInline(ulMatch.Groups["text"].Value).Trim();
		rendered = $"- {itemText}";
		return true;
	}

	private static bool TryRenderOrderedListItem(string line, out string rendered) {
		var olMatch = OrderedListRegex.Match(line);
		if (!olMatch.Success) {
			rendered = string.Empty;
			return false;
		}

		string num = olMatch.Groups["num"].Value;
		string itemText = ProcessInline(olMatch.Groups["text"].Value).Trim();
		rendered = $"{num}. {itemText}";
		return true;
	}

	private static bool TryRenderBlockquote(string line, out string rendered) {
		var quoteMatch = BlockquoteRegex.Match(line);
		if (!quoteMatch.Success) {
			rendered = string.Empty;
			return false;
		}

		rendered = ProcessInline(quoteMatch.Groups["text"].Value).Trim();
		return true;
	}

	private static bool IsFencedCodeBlockDelimiter(string line) {
		// CommonMark: fenced code blocks are at least 3 backticks (or tildes).
		var trimmed = line.TrimStart();
		return trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal);
	}

	private static IEnumerable<string> CollapseBlankLines(IReadOnlyList<string> lines) {
		bool previousWasBlank = false;
		foreach (string line in lines) {
			bool isBlank = string.IsNullOrWhiteSpace(line);
			if (isBlank) {
				if (previousWasBlank) {
					continue;
				}
				previousWasBlank = true;
				yield return string.Empty;
			} else {
				previousWasBlank = false;
				yield return line;
			}
		}
	}

	private static string ProcessInline(string input) {
		if (string.IsNullOrEmpty(input)) {
			return string.Empty;
		}

		// Avoid touching inline code spans (backticks) so we preserve things like `51ed8b3`.
		int firstBacktick = input.IndexOf('`');
		if (firstBacktick < 0) {
			return ProcessInlineOutsideCode(input);
		}

		// If backticks are unbalanced, treat as plain text.
		int backtickCount = input.Where(c => c == '`').Count();
		if (backtickCount % 2 != 0) {
			return ProcessInlineOutsideCode(input);
		}

		// Now process only the non-code segments.
		var rebuilt = new StringBuilder(input.Length);
		string[] parts = input.Split('`');
		for (int i = 0; i < parts.Length; i++) {
			if (i % 2 == 0) {
				rebuilt.Append(ProcessInlineOutsideCode(parts[i]));
			} else {
				rebuilt.Append('`');
				rebuilt.Append(parts[i]);
				rebuilt.Append('`');
			}
		}

		return rebuilt.ToString();
	}

	private static string ProcessInlineOutsideCode(string s) {
		string result = s;

		result = ImageRegex.Replace(result, m => m.Groups["alt"].Value);
		result = LinkRegex.Replace(result, m => m.Groups["text"].Value);

		result = BoldAsteriskRegex.Replace(result, m => m.Groups["text"].Value);
		result = BoldUnderscoreRegex.Replace(result, m => m.Groups["text"].Value);
		result = ItalicAsteriskRegex.Replace(result, m => m.Groups["text"].Value);
		result = ItalicUnderscoreRegex.Replace(result, m => m.Groups["text"].Value);

		result = InlineHtmlTagRegex.Replace(result, string.Empty);

		// Unescape a few common backslash escapes.
		result = result.Replace("\\*", "*", StringComparison.Ordinal)
			.Replace("\\_", "_", StringComparison.Ordinal)
			.Replace("\\[", "[", StringComparison.Ordinal)
			.Replace("\\]", "]", StringComparison.Ordinal)
			.Replace("\\`", "`", StringComparison.Ordinal);

		return result;
	}
}
