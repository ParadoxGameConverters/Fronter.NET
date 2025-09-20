using System;
using System.Security.Principal;

namespace Fronter.Services;

internal static class ElevatedPrivilegesDetector {
	public static bool IsAdministrator =>
		OperatingSystem.IsWindows() ?
			new WindowsPrincipal(WindowsIdentity.GetCurrent())
				.IsInRole(WindowsBuiltInRole.Administrator) :
			Mono.Unix.Native.Syscall.geteuid() == 0;
}