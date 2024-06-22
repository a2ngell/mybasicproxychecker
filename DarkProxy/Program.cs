using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Leaf.xNet;
using Leaf.xNet.Services.Cloudflare;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http;

namespace DarkProxy
{
    class Program
    {
        private static string date;
        public static List<string> Proxies = new List<string>();
        public static List<string> WorkingProxies = new List<string>();
        public static List<string> NonWorkingProxies = new List<string>();
        public static int currentworker = 0;
        public static int amountchecked = 1;
        public static int timeout = 5000;
        public static List<string> typelist = new List<string> { "Http", "socks4", "socks5" };
        static void Watermark()
        {
            SetColour("Red");
            Console.WriteLine("  *******       **     *******   **   **");
            Console.WriteLine(" /**////**     ****   /**////** /**  ** ");
            Console.WriteLine(" /**    /**   **//**  /**   /** /** **  ");
            Console.WriteLine(" /**    /**  **  //** /*******  /****   ");
            Console.WriteLine(" /**    /** **********/**///**  /**/**  ");
            Console.WriteLine(" /**    ** /**//////**/**  //** /**//** ");
            Console.WriteLine(" /*******  /**     /**/**   //**/** //**");
            Console.WriteLine(" ///////   //      // //     // //   // ");
            SetColour("White");
        }

        static void SetColour(string colour)
        {
            switch (colour)
            {
                case "Red":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "White":
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "Lime":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            string date = DateTime.Now.ToLongDateString();

            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            if (!Directory.Exists($"Logs\\{date}"))
            {
                Directory.CreateDirectory($"Logs\\{date}");
            }

            string[] proxyTypes = { "HTTP", "SOCKS4", "SOCKS5" };
            foreach (var proxyType in proxyTypes)
            {
                for (int i = 0; i <= 5000; i += 1000)
                {
                    string folderPath = $"Logs\\{date}\\{i}ms";
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    string filePath = $"{folderPath}\\{proxyType}.txt";
                    File.AppendAllText(filePath, string.Empty); 
                }
            }

            File.WriteAllLines($"Logs\\{date}\\Working.txt", WorkingProxies);
            File.WriteAllLines($"Logs\\{date}\\Proxies.txt", Proxies);
        }
        static void save(string proxy, float ms, int type)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            if (!Directory.Exists($"Logs\\{date}"))
            {
                Directory.CreateDirectory($"Logs\\{date}");
            }

            string proxyType = typelist[type];

            int responseTimeRange = (int)(ms / 1000) * 1000;
            string folderPath = $"Logs\\{date}\\{responseTimeRange}ms";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = $"{folderPath}\\{proxyType}.txt";
            File.AppendAllText(filePath, $"{proxy}\n");

            File.AppendAllText($"Logs\\{date}\\{proxyType} Working.txt", proxy + "\n");
            File.WriteAllLines($"Logs\\{date}\\Working.txt", WorkingProxies);
            File.WriteAllLines($"Logs\\{date}\\Proxies.txt", Proxies);
        }


        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            Watermark();
            if (string.IsNullOrEmpty(args.ToString()) || args.Length <= 0)
            {
                SetColour("Red");
                Console.WriteLine("Please Select Mode! \n");
                Console.WriteLine("1.Select Proxy From File \n");
                Console.WriteLine("2.Auto Get Proxy From ProxyScrape \n");
                int proxymode = Convert.ToInt32(Console.ReadLine());
                if (proxymode == 1)
                {
                    Console.WriteLine("Please drag your proxy list onto the exe!");
                    string proxypath = args.Length > 0 ? args[0] : Console.ReadLine().Trim('"');
                    if (!File.Exists(proxypath))
                    {
                        Console.WriteLine("File not found!");
                        return;
                    }
                    foreach (string line in File.ReadAllLines(proxypath.ToString()))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains(':'))
                        {
                            continue;
                        }
                        var found = line.Split(':');
                        var proxy = found[0].ToString();
                        var port = found[1].ToString();
                        Proxies.Add(proxy + ":" + port);
                        Console.Title = $"Dark Proxy Checker | Loaded: {Proxies.Count.ToString()} proxies";
                    }
                }
                if (proxymode == 2) GetProxies();
                Console.WriteLine("Please Enter Thread Count (Default 50)");

                int threadcount = Convert.ToInt32(Console.ReadLine());
                if (threadcount <= 0) threadcount = 50;
                Console.WriteLine("Please Enter Timeout (Default 5000)");

                timeout = Convert.ToInt32(Console.ReadLine());
                if (timeout <= 0) timeout = 5000;
                date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                for (int i = 0; i < Proxies.Count; i++)
                {
                    while (currentworker >= threadcount)
                    {
                        Thread.Sleep(50);
                    }
                    if(currentworker < threadcount)
                    {
                        int index = i;
                        Thread myThread = new Thread(new ThreadStart(() => Check(Proxies[index])));
                        myThread.Start();
                        currentworker++;
                    }
                }
                while (amountchecked < Proxies.Count)
                {
                    Thread.Sleep(10);
                }

                SetColour("Lime");
                Console.WriteLine("PROXY CHECK DONE");
                Console.Read();
            }
            else{}
        }

        public static void GetProxies()
        {
            try
            {
                string[] urls = {
                "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks4&timeout=10000&country=all",
                "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all",
                "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=all"
            };

                using (HttpClient client = new HttpClient())
                {
                    foreach (var url in urls)
                    {
                        string proxyList = client.GetStringAsync(url).Result;
                        string[] proxies2 = proxyList.Split('\n');
                        foreach (var proxy in proxies2)
                        {
                            Proxies.Add(proxy.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }
        
        public static int goodproxyhttp = 0;
        public static int goodproxysocks4 = 0;
        public static int goodproxysocks5 = 0;
        public static void updatetitle()
        {
            Console.Title = $"Dark Proxy Checker | Loaded: {Proxies.Count} proxies | Working proxies: {WorkingProxies.Count} | Dead proxies: {amountchecked - WorkingProxies.Count} | Checked Proxy:{amountchecked} | HTTP: {goodproxyhttp} | SOCKS4: {goodproxysocks4} | SOCKS5: {goodproxysocks5}";
        }
        public static void Check(string proxy)
        {
            Task.Run(() =>
            {
                var httpRequest = new HttpRequest();
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        if (i == 0)
                        {
                            httpRequest.Proxy = HttpProxyClient.Parse(proxy);
                            httpRequest.Proxy.ConnectTimeout = timeout;
                            httpRequest.Proxy.ReadWriteTimeout = timeout;
                            httpRequest.KeepAliveTimeout = timeout;
                        }
                        else if (i == 1)
                        {
                            httpRequest.Proxy = Socks4ProxyClient.Parse(proxy);
                            httpRequest.Proxy.ConnectTimeout = timeout;
                            httpRequest.Proxy.ReadWriteTimeout = timeout;
                            httpRequest.KeepAliveTimeout = timeout;
                        }
                        else if (i == 2)
                        {
                            httpRequest.Proxy = Socks5ProxyClient.Parse(proxy);
                            httpRequest.Proxy.ConnectTimeout = timeout;
                            httpRequest.Proxy.ReadWriteTimeout = timeout;
                            httpRequest.KeepAliveTimeout = timeout;
                        }
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var request = httpRequest.Get("https://check-host.net/ip");
                        stopwatch.Stop();
                        if (request.StatusCode == Leaf.xNet.HttpStatusCode.OK)
                        {
                            SetColour("Lime");
                            Console.WriteLine($" [+]{proxy} | Working! | Type: {typelist[i]} | ResponseTime: {stopwatch.ElapsedMilliseconds} ");
                            WorkingProxies.Add(proxy);
                            if (i == 0) goodproxyhttp++;
                            else if (i == 1) goodproxysocks4++;
                            else if (i == 2) goodproxysocks5++;
                            updatetitle();
                            save(proxy, stopwatch.ElapsedMilliseconds, i);
                            break;
                        }
                        else
                        {
                            SetColour("Red");
                            NonWorkingProxies.Add(proxy);
                            Console.WriteLine($" [-]{proxy} | Another response! | Type {typelist[i]} response: {request.StatusCode}");
                            updatetitle();
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        SetColour("Red");
                        NonWorkingProxies.Add(proxy);
                        Console.WriteLine($" [-]{proxy} | No response! | Type {typelist[i]}");
                        updatetitle();
                    }
                }

                Interlocked.Increment(ref amountchecked);
                Interlocked.Decrement(ref currentworker);
            });
        }


    }
}
