using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Tunneling.Server.Framework;
using Tunneling.Server.Infrastructure;
using static System.Collections.Specialized.BitVector32;

namespace Tunneling.Server.Controllers
{
    [Authorize]
    public class FileTransController : Controller
    {
        private readonly ILogger<FileTransController> _logger;
        private readonly IUploadFileManager uploadFileManager;
        public FileTransController(ILogger<FileTransController> logger, IUploadFileManager uploadFileManager)
        {
            _logger = logger;
            this.uploadFileManager = uploadFileManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var downloadsDir = Path.Combine(AppContext.BaseDirectory, "downloads");
            var files = Directory.GetFiles(downloadsDir)
                                .Select(a => new FileInfo(a))
                                .OrderByDescending(a => a.LastWriteTime)
                                .ToList();

            return View(files);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> Upload()
        {
            if (!Request.ContentType?.StartsWith("multipart/form-data") ?? true)
            {
                return BadRequest("The request does not contain valid multipart form data.");
            }

            var uploadPath = Path.Combine(AppContext.BaseDirectory, "downloads");

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary))
            {
                return BadRequest("Missing boundary in multipart form data.");
            }

            using var tempFileManager = new TempFileManager();

            var cancellationToken = HttpContext.RequestAborted;
            var result = await uploadFileManager.SaveViaMultipartReaderAsync(boundary, Request.Body, tempFileManager, cancellationToken);

            var fileName = Path.GetFileName(result.Item1);
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("上传数据中找不到文件名");
            }

            var savePath = Path.Combine(uploadPath, fileName);

            //如果存在同名文件，就先删除
            if (System.IO.File.Exists(savePath))
            {
                _logger.LogInformation("上传文件存在同名，删除旧文件:{FileName}", fileName);
                System.IO.File.Delete(savePath);
            }

            //将临时文件改到正式文件名
            System.IO.File.Move(result.Item2, savePath);
           
            return Ok();

        }
        //
        // 下载 不做权限 允许分享文件的下载url
        [AllowAnonymous]
        [HttpGet("/FileTrans/Downloads/{fileName}")]
        public IActionResult Downloads(string fileName)
        {
            _logger.LogInformation("请求下载文件:{FileName}", fileName);

            #region 防路径穿越      
            var downloadsDir = Path.Combine(AppContext.BaseDirectory, "downloads");
            var filePath = Path.GetFullPath(Path.Combine(downloadsDir, fileName));

            if (!filePath.StartsWith(Path.GetFullPath(downloadsDir) + Path.DirectorySeparatorChar))
            {
                return BadRequest("禁止访问该路径");
            }
            #endregion

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("文件不存在");
            }

            var mimeType = Utilities.GetMimeType(fileName);
            // 允许分片下载（enableRangeProcessing: true）
            return PhysicalFile(filePath, mimeType, fileName, enableRangeProcessing: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string fileName)
        { 
            var downloadsDir = Path.Combine(AppContext.BaseDirectory, "downloads");

            var filePath = Path.Combine(downloadsDir, fileName);
            // 防路径穿越
            if (!filePath.StartsWith(downloadsDir))
            {
                return BadRequest("文件名不合法");
            }

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation("删除文件：{FileName}", fileName);
            }
            return Ok();
        }
    }
}
