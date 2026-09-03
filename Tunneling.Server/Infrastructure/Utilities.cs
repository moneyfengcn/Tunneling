using Microsoft.AspNetCore.StaticFiles;
 
using System;

namespace Tunneling.Server.Infrastructure
{
    static public class Utilities
    {


        static private readonly FileExtensionContentTypeProvider _mimeProvider = new();

        static public string GetMimeType(string fileName)
        {
            if (_mimeProvider.TryGetContentType(fileName, out var contentType))
                return contentType;

            return "application/octet-stream";
        }

        static public string Format(this TimeSpan ts )
        {
            if (ts.Days > 0)
            {
                return string.Format("{0} {1} {2:D2}:{3:D2}:{4:D2}",
                                     ts.Days, "天", ts.Hours, ts.Minutes, ts.Seconds);
            }
            else
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
            }
        }
        static public string FlowRateFormat(this ulong s)
        {
            const double TB = 1024d * 1024d * 1024d * 1024d;
            const double GB = 1024d * 1024d * 1024d;
            const double MB = 1024d * 1024d;
            const double KB = 1024d;

            double value = (double)s;

            if (value > TB)
            {
                return (value / TB).ToString("0.0000") + " TB";
            }
            if (value > GB)
            {
                return (value / GB).ToString("0.000") + " GB";
            }
            else if (value > MB)
            {
                return (value / MB).ToString("0.00") + " MB";
            }
            else if (value > KB)
            {
                return (value / KB).ToString("0.00") + " KB";
            }
            else
            {
                return s.ToString();
            }
        }
    }
}
