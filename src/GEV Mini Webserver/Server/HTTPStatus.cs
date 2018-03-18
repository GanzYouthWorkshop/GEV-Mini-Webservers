using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEV.Web.Server
{
    public class HTTPStatus
    {
        public enum Code
        {
            OK = 200,
            NotFound = 404,
            ServerError = 500
        }

        public static Dictionary<int, string> Headers = new Dictionary<int, string>()
        {
            { 200, "200 OK" },
            { 404, "404 File not found" },
            { 500, "500 Internal Server Error" }
        };
    }
}
