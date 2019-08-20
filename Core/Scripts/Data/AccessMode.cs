using System;

namespace Coflnet
{
    [Flags]
	public enum AccessMode {
		/// <summary>
		/// Essentially blocks
		/// </summary>
		NONE = 0,
		READ = 1,
		WRITE = 2,
		READ_AND_WRITE = READ | WRITE,
		/// <summary>
		/// Permission to change others permission, includes read and write
		/// </summary>
		CHANGE_PERMISSIONS = 4,
		/// <summary>
		/// This level will be ignored
		/// </summary>
		ASNORMAL = 0xFF
	}
}