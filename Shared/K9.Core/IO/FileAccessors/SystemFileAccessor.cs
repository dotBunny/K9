// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;

namespace K9.Core.IO.FileAccessors;

public class SystemFileAccessor(string filePath) : IFileAccessor
{
    /// <inheritdoc />
	public uint GetBlockSize()
	{
		return 4096;
	}

	/// <inheritdoc />
	public int GetReadBufferSize()
	{
		return 4096;
	}

	/// <inheritdoc />
	public int GetWriteBufferSize()
	{
		return 4096;
	}

	/// <inheritdoc />
	public bool ValidConnection()
	{
		return true;
	}

	/// <inheritdoc />
	public Stream GetReader()
	{
		Log.WriteLine($"Open file stream for {filePath} (R).", "FILE");
		return File.OpenRead(filePath);
	}

	/// <inheritdoc />
	public Stream GetWriter()
	{
		Log.WriteLine($"Open file stream for {filePath} (W).", "FILE");
		return File.Create(filePath);
	}
}
