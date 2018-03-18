using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GEV.Web.Cache;

namespace GEV.Web.Server
{
    public class RequestHandler
    {
        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB
        private const int BUF_SIZE = 4096;

        private TcpClient m_Socket { get; }
        private WebServer m_Server { get; }

        private Stream m_InputStream;
        public StreamWriter OutputStream { get; private set; }

        public HTTPRequest RequestData { get; set; }


        public RequestHandler(TcpClient s, WebServer srv)
        {
            this.m_Socket = s;
            this.m_Server = srv;
        }

        private string StreamReadLine(Stream inputStream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }

        internal void Process()
        {
            try
            {
                using (this.m_InputStream = new BufferedStream(this.m_Socket.GetStream()))
                using (this.OutputStream = new StreamWriter(new BufferedStream(this.m_Socket.GetStream())))
                {
                    try
                    {
                        ParseRequest();
                        ReadHeaders();
                        if (this.RequestData.HTTPMethod.Equals("GET"))
                        {
                            HandleGETRequest();
                        }
                        else if (this.RequestData.HTTPMethod.Equals("POST"))
                        {
                            HandlePOSTRequest();
                        }
                    }
                    catch (Exception)
                    {
                        WriteHeaders(HTTPStatus.Code.ServerError);
                    }

                    try
                    {
                        OutputStream.Flush();
                    }
                    catch (Exception) { }
                }
            }
            catch { }
            
        }

        private void ParseRequest()
        {
            string request = StreamReadLine(m_InputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }

            string[] URLElements = tokens[1].Split('?');

            this.RequestData = new HTTPRequest();
            this.RequestData.HTTPMethod = tokens[0].ToUpper();
            this.RequestData.URL = URLElements[0];
            this.RequestData.ProtocolVersion = tokens[2];

            if(URLElements.Length > 1)
            {
                string[] arguments = Uri.UnescapeDataString(URLElements[1]).Split('&');
                foreach(string arg in arguments)
                {
                    string[] argument = arg.Split('=');
                    if(argument.Length < 2)
                    {
                        argument = new string[2] { argument[0], "" };
                    }
                    this.RequestData.Arguments.Add(argument[0], argument[1]);
                }
            }

            this.RequestData.LocalURL = this.RequestData.URL.Replace('/', '\\');
            if(this.RequestData.LocalURL == "\\" || this.RequestData.LocalURL == "")
            {
                this.RequestData.LocalURL = "index.html";
            }
        }

        private void ReadHeaders()
        {
            String line;
            while ((line = StreamReadLine(m_InputStream)) != null)
            {
                if (line.Equals(""))
                {
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);
                this.RequestData.Headers[name] = value;
            }
        }

        private void HandleGETRequest()
        {
            foreach(KeyValuePair<string, CompiledScriptRequestHandler> csrh in this.m_Server.CompiledRequestHandlers)
            {
                if(csrh.Key == this.RequestData.URL)
                {
                    csrh.Value(this);
                    return;
                }
            }

            CachedFile file = null;
            if (this.m_Server.UseCache)
            {
                file = this.m_Server.Cache.GetFile(PathExtensions.Combine(this.m_Server.HomeDirectory, this.RequestData.LocalURL));
            }
            else
            {
                file = new CachedFile()
                {
                    FileName = PathExtensions.Combine(this.m_Server.HomeDirectory, this.RequestData.LocalURL)
                };
                file.ReadFileContents();
            }
            if (file.Valid)
            {
                this.WriteSuccess();
                this.OutputStream.BaseStream.Write(file.Data, 0, file.Data.Length);
            }
            else
            {
                this.WriteFailure();
            }
        }

        private void HandlePOSTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.RequestData.Headers.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.RequestData.Headers["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(String.Format("POST Content-Length({0}) too big for this simple server", content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    int numread = this.m_InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }

            //TODO: handle POST at all -> handle POST well
            this.WriteSuccess();
        }

        public void WriteSuccess(string contentType = "text/html")
        {
            this.OutputStream.WriteLine("HTTP/1.0 200 OK");
            this.OutputStream.WriteLine("Content-Type: " + contentType);
            this.OutputStream.WriteLine("Connection: close");
            this.OutputStream.WriteLine("");
        }

        public void WriteFailure()
        {
            this.OutputStream.WriteLine("HTTP/1.0 404 File not found");
            this.OutputStream.WriteLine("Connection: close");
            this.OutputStream.WriteLine("");
        }

        public void WriteHeaders(HTTPStatus.Code StatusCode, string contentType = "")
        {
            this.OutputStream.Write("HTTP/1.0 " + HTTPStatus.Headers[(int)StatusCode]);
            if(contentType != "" && contentType != null)
            {
                this.OutputStream.WriteLine("Content-Type: " + contentType);
            }
            this.OutputStream.WriteLine("Connection: close");
            this.OutputStream.WriteLine("");
        }
    }
}
