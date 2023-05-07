using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Natuki
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args;
            if (args.Length == 0)
            {
                //var aaa = new NarouAnalysisToolLib.HttpClientHelper();
                //aaa.GetAsync("https://kasasagi.hinaproject.com/access/chapter/ncode/n0016hw/?date=2023-04-22").Wait();

                //System.Threading.ThreadPool.QueueUserWorkItem(state =>
                //{
                //    var th = new NarouAnalysisToolLib.TaskHelper("n0016hw", DateTime.Today.AddDays(-3));
                //    th.Run();
                //});
                //System.Threading.Thread.Sleep(60000);

                var window = new MainWindow();
                Current.MainWindow = window;
                window.InitializeComponent();
                window.Show();
            }
            else
            {
                var argsEnumerator = e.Args.GetEnumerator();

                while (argsEnumerator.MoveNext())
                {

                }
            }
        }

        private static async Task<string> GetData()
        {
            var request = new System.Net.Http.HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://kasasagi.hinaproject.com/access/chapter/ncode/n0016hw/?date=2023-04-22")
            };
            var HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36");

            var response = await HttpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

    }
}
