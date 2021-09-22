using System.IO;

namespace K9.Services
{
    public interface IFileAccessor
    {
        public bool ValidConnection();
        public bool Get(ref MemoryStream stream);
    }
}