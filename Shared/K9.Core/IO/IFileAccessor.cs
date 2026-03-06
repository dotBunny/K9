// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.IO;

namespace K9.Core.IO
{
	public interface IFileAccessor
	{
		public enum Type
		{
			Default,
			SMB
		}
		public uint GetBlockSize();

		public int GetReadBufferSize();
		public int GetWriteBufferSize();
		public bool ValidConnection();
		public Stream GetReader();
		public Stream GetWriter();
	}
}
