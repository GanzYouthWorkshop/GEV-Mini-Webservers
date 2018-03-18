using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEV.Web.Server
{
    public class HTTPRequest
    {
        public string HTTPMethod { get; set; }
        public string URL { get; set; }
        public string LocalURL { get; set; }
        public string ProtocolVersion { get; set; }
        public Hashtable Headers { get; } = new Hashtable();
        public Dictionary<string, string> Arguments { get; } = new Dictionary<string, string>();
    }
}
