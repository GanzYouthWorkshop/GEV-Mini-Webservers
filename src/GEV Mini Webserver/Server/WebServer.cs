using GEV.Web.Cache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GEV.Web.Server
{
    public class WebServer
    {
        public int Port { get; }
        public string HomeDirectory { get; }

        public FileCache Cache { get; }
        public bool UseCache { get; }

        private TcpListener m_Listener;

        private bool m_Running = true;
        private Thread m_Thread;

        public Dictionary<string, CompiledScriptRequestHandler> CompiledRequestHandlers { get; } = new Dictionary<string, CompiledScriptRequestHandler>();

        public WebServer(int port, string homeDirectory)
        {
            this.Port = port;
            this.HomeDirectory = homeDirectory;
            this.UseCache = false;
        }

        public WebServer(int port, string homeDirectory, int cacheSize, int maxCacheMegabytes)
        {
            this.Port = port;
            this.HomeDirectory = homeDirectory;
            this.UseCache = true;

            this.Cache = new FileCache()
            {
                MaximumFiles = cacheSize,
                MaximumFileMegabytes = maxCacheMegabytes
            };
        }

        public void Start()
        {
            this.m_Thread = new Thread(this.ServerRunner);
            this.m_Thread.IsBackground = true;
            this.m_Thread.SetApartmentState(ApartmentState.MTA);
            this.m_Thread.Start();
        }

        public void ServerRunner()
        {
            this.m_Listener = new TcpListener(IPAddress.Any, this.Port);
            this.m_Listener.Start();
            while (this.m_Running)
            {
                TcpClient s = this.m_Listener.AcceptTcpClient();
                RequestHandler handler = new RequestHandler(s, this);
                Thread thread = new Thread(new ThreadStart(handler.Process));
                thread.Start();
                Thread.Sleep(1);
            }
        }
    }
}
