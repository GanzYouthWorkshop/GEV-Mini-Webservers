using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEV.Web
{
    public class PathExtensions
    {
        public static string Combine(string path1, string path2)
        {
            if (path1 == null)
            {
                return path2;
            }
            else if (path2 == null)
            {
                return path1;
            }
            else
            {
                return path1.Trim().TrimEnd(System.IO.Path.DirectorySeparatorChar)
                    + System.IO.Path.DirectorySeparatorChar
                    + path2.Trim().TrimStart(System.IO.Path.DirectorySeparatorChar);
            }
        }

        public static string Combine(string path1, string path2, string path3)
        {
            return Combine(Combine(path1, path2), path3);
        }
    }
}
