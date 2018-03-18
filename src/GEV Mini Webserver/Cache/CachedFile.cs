using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEV.Web.Cache
{
    public class CachedFile
    {
        public byte[] Data { get; set; }
        public DateTime LastAccess { get; set; }
        public string FileName { get; set; }

        public bool Valid { get; private set; }

        public void ReadFileContents()
        {
            if(File.Exists(this.FileName))
            {
                using (FileStream fs = File.Open(this.FileName, FileMode.Open))
                {
                    this.Data = new byte[fs.Length];
                    fs.Read(this.Data, 0, (int)fs.Length);
                }
                this.Valid = true;
            }
            else
            {
                this.Valid = false;
            }
        }
    }
}
