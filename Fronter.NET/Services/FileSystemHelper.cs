using System;
using System.IO;

namespace Fronter.Services;

/// <summary>
/// Filesystem helpers that handle edge cases such as OneDrive-backed paths
/// where cloud-sync clients set blocking attributes on entries that prevent
/// standard recursive deletion.
/// </summary>
internal static class FileSystemHelper {
	/// <summary>
	/// Attempts to delete a directory and all its contents.
	/// When the standard delete fails with an access or IO error (common on
	/// OneDrive-backed folders because of ReadOnly attributes set by the sync
	/// client), falls back to clearing all blocking attributes on every entry
	/// in the tree before retrying.
	/// </summary>
	/// <returns>True if the folder was deleted or did not exist, false otherwise.</returns>
	internal static bool TryDeleteFolder(string folderPath) {
		if (!Directory.Exists(folderPath)) {
			return true;
		}

		try {
			Directory.Delete(folderPath, recursive: true);
			return true;
		} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
			// OneDrive and similar cloud-sync clients can set ReadOnly (and other blocking)
			// attributes on files and directories, causing Directory.Delete to fail with
			// access-denied. Clear all attributes in the tree and retry.
			return TryDeleteFolderWithAttributeReset(folderPath);
		}
	}

	private static bool TryDeleteFolderWithAttributeReset(string folderPath) {
		try {
			ResetAttributesRecursive(new DirectoryInfo(folderPath));
			Directory.Delete(folderPath, recursive: true);
			return true;
		} catch {
			return false;
		}
	}

	private static void ResetAttributesRecursive(DirectoryInfo directory) {
		// Clear blocking attributes on the directory itself.
		directory.Attributes &= ~FileAttributes.ReadOnly;

		foreach (FileInfo file in directory.EnumerateFiles()) {
			file.IsReadOnly = false;
		}

		foreach (DirectoryInfo subDir in directory.EnumerateDirectories()) {
			// Reparse points (junctions, symlinks) should be deleted as directory entries
			// rather than traversed — we must not recurse into what they point at.
			if (subDir.Attributes.HasFlag(FileAttributes.ReparsePoint)) {
				Directory.Delete(subDir.FullName);
			} else {
				ResetAttributesRecursive(subDir);
			}
		}
	}
}
