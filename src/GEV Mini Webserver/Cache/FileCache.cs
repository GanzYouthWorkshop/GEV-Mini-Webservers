using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEV.Web.Cache
{
    public class FileCache
    {
        public int MaximumFiles { get; set; } = 20;
        public int MaximumFileMegabytes { get; set; } = 5;

        private List<CachedFile> m_FileData { get; } = new List<CachedFile>();

        public CachedFile GetFile(string path)
        {
            CachedFile file = this.m_FileData.FirstOrDefault(f => f.FileName == path);

            if(file == null)
            {
                file = new CachedFile();
                file.FileName = path;
                file.ReadFileContents();

                if(file.Data.Length <= (this.MaximumFileMegabytes * 1024 * 2014))
                {
                    this.m_FileData.Add(file);
                }

                if(this.m_FileData.Count > this.MaximumFiles)
                {
                    CachedFile oldest = file;
                    foreach (CachedFile f in this.m_FileData)
                    {
                        if(oldest.LastAccess > f.LastAccess)
                        {
                            oldest = f;
                        }
                    }
                    this.m_FileData.Remove(oldest);
                }
            }

            file.LastAccess = DateTime.UtcNow;

            return file;
        }

    }
}
