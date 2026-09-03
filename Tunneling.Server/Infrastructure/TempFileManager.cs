namespace Tunneling.Server.Infrastructure
{
    //用来保证退出时自动删除文件
    public class TempFileManager : IDisposable
    {
        private string? _fileName;

        public void SetFileName(string fileName)
        {
            _fileName = fileName;
        }
        public void Dispose()
        {
            if (File.Exists(_fileName))
            {
                File.Delete(_fileName);
            }

        }
    }
}
