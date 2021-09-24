using System.IO;

namespace K9.IO
{
    public interface IFileAccessor
    {
        public bool ValidConnection();
        public Stream Get();
    }
}