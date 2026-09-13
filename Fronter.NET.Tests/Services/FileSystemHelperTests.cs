using Fronter.Services;
using System;
using System.IO;
using Xunit;

namespace Fronter.Tests.Services;

public class FileSystemHelperTests {
	[Fact]
	public void TryDeleteFolder_ReturnsTrueWhenDirectoryDoesNotExist() {
		var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Assert.False(Directory.Exists(nonExistentPath));
		Assert.True(FileSystemHelper.TryDeleteFolder(nonExistentPath));
	}

	[Fact]
	public void TryDeleteFolder_DeletesNormalDirectory() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(Path.Combine(tempDir, "sub"));
		File.WriteAllText(Path.Combine(tempDir, "sub", "file.txt"), "content");
		try {
			Assert.True(FileSystemHelper.TryDeleteFolder(tempDir));
			Assert.False(Directory.Exists(tempDir));
		} finally {
			Cleanup(tempDir);
		}
	}

	[Fact]
	public void TryDeleteFolder_DeletesDirectoryWithReadOnlyFiles() {
		// Simulates the OneDrive failure: read-only files inside a deeply nested
		// tree cause Directory.Delete(recursive:true) to throw access-denied.
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(),
			"OneDrive", "Documents", "Paradox Interactive", "Crusader Kings III", "mod");
		var deepDir = Path.Combine(tempDir, "test_save", "common", "court_positions", "types");
		Directory.CreateDirectory(deepDir);

		var file1 = Path.Combine(deepDir, "feudal_court_position.txt");
		var file2 = Path.Combine(tempDir, "test_save", "common", "vanilla_override.txt");
		File.WriteAllText(file1, "data");
		File.WriteAllText(file2, "data");

		// Mark files as read-only, which is what makes Directory.Delete(recursive:true) fail.
		File.SetAttributes(file1, FileAttributes.ReadOnly);
		File.SetAttributes(file2, FileAttributes.ReadOnly);

		// Also mark the inner directory ReadOnly (as OneDrive can do).
		File.SetAttributes(deepDir, File.GetAttributes(deepDir) | FileAttributes.ReadOnly);

		try {
			Assert.True(FileSystemHelper.TryDeleteFolder(Path.GetDirectoryName(tempDir)!));
			Assert.False(Directory.Exists(tempDir));
		} finally {
			Cleanup(Path.GetDirectoryName(tempDir)!);
		}
	}

	[Fact]
	public void TryDeleteFolder_DeletesDeeplyNestedReadOnlyTree() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var level3 = Path.Combine(tempDir, "a", "b", "c");
		Directory.CreateDirectory(level3);

		var files = new[] {
			Path.Combine(tempDir, "root.txt"),
			Path.Combine(tempDir, "a", "mid.txt"),
			Path.Combine(level3, "deep.txt"),
		};
		foreach (var f in files) {
			File.WriteAllText(f, "x");
			File.SetAttributes(f, FileAttributes.ReadOnly);
		}

		try {
			Assert.True(FileSystemHelper.TryDeleteFolder(tempDir));
			Assert.False(Directory.Exists(tempDir));
		} finally {
			Cleanup(tempDir);
		}
	}

	/// <summary>Clears read-only attributes and removes the directory if it still exists after a failed test.</summary>
	private static void Cleanup(string path) {
		if (!Directory.Exists(path)) {
			return;
		}
		foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) {
			File.SetAttributes(file, FileAttributes.Normal);
		}
		Directory.Delete(path, recursive: true);
	}
}
