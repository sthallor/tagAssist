﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace DeployRealTimeRigState.Misc
{
    public static class FileInfoExtensions
    {
        public static void CopyTo(this FileInfo file, FileInfo destination, Action<int> progressCallback)
        {
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize], buffer2 = new byte[bufferSize];
            var swap = false;
            var reportedProgress = 0;
            var len = file.Length;
            float flen = len;
            Task writer = null;

            using (var source = file.OpenRead())
            using (var dest = destination.OpenWrite())
            {
                dest.SetLength(source.Length);
                int read;
                for (long size = 0; size < len; size += read)
                {
                    int progress;
                    if ((progress = (int)(size / flen * 100)) != reportedProgress)
                        progressCallback(reportedProgress = progress);
                    read = source.Read(swap ? buffer : buffer2, 0, bufferSize);
                    writer?.Wait();
                    writer = dest.WriteAsync(swap ? buffer : buffer2, 0, read);
                    swap = !swap;
                }
                writer?.Wait();
            }
        }
    }
}