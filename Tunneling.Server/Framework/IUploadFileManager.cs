using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.IO.Pipelines;
using Tunneling.Server.Infrastructure;

namespace Tunneling.Server.Framework
{
    public interface IUploadFileManager
    {
        Task<Tuple<string?, string>> SaveViaMultipartReaderAsync(string boundary, Stream contentStream, TempFileManager tempFileManager, CancellationToken cancellationToken);
    }

    public class UploadFileManagerService : IUploadFileManager
    {
        public const int BufferSize = 16 * 1024 * 1024; // 16 MB buffer size
        private readonly ILogger<UploadFileManagerService> _logger;

        public UploadFileManagerService(ILogger<UploadFileManagerService> logger)
        {
            _logger = logger;
        }

        public async Task<Tuple<string?, string>> SaveViaMultipartReaderAsync(string boundary, Stream contentStream, TempFileManager tempFileManager, CancellationToken cancellationToken)
        {
            string? fileName = string.Empty;
            //接收到的上传文件先写入临时文件
            string targetFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            
            //用来保证临时文件会被清除
            tempFileManager.SetFileName(fileName);

            using var outputFileStream = new FileStream(
                                                path: targetFilePath,
                                                mode: FileMode.Create,
                                                access: FileAccess.Write,
                                                share: FileShare.None,
                                                bufferSize: BufferSize,
                                                useAsync: true);

            var reader = new MultipartReader(boundary, contentStream);

            MultipartSection? section;
            long totalBytesRead = 0;

            // 循环读取数据
            while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition != null && contentDisposition.IsFileDisposition())
                {
                    fileName = contentDisposition.FileName.Value;

                    _logger.LogInformation("文件上传 文件名: {fileName}", fileName);

                    // 写数据入文件
                    await section.Body.CopyToAsync(outputFileStream, cancellationToken);
                    totalBytesRead += section.Body.Length;


                }
            }

            _logger.LogInformation("文件上传完成. 总共: {totalBytesRead} bytes.", totalBytesRead);
            return Tuple.Create(fileName, targetFilePath);
        }
    }
}
