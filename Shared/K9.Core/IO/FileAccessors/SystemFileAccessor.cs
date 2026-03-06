// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;

namespace K9.Core.IO.FileAccessors
{
	public class SystemFileAccessor : IFileAccessor
	{
		private readonly string m_FilePath;

		public SystemFileAccessor(string filePath)
		{
			m_FilePath = filePath;
		}

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
			Core.Log.WriteLine($"Open file stream for {m_FilePath} (R).", "FILE");
			return File.OpenRead(m_FilePath); ;
		}

		/// <inheritdoc />
		public Stream GetWriter()
		{
			Core.Log.WriteLine($"Open file stream for {m_FilePath} (W).", "FILE");
			return File.Create(m_FilePath);
		}
	}
}
