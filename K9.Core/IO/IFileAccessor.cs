using System.IO;

namespace K9.IO
{
    public interface IFileAccessor
    {
        public uint GetBlockSize();
        public bool ValidConnection();
        public Stream GetReader();
        public Stream GetWriter();
    }
}