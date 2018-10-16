using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dwscdv3.WebArchiver.Addons
{
    public abstract class AddonBase
    {
        protected HttpClient Http { get; }

        public CookieContainer CookieContainer { get; set; }
        public double TimingScale { get; set; }
        public int Timeout { get; set; }

        private Random random { get; } = new Random();

        public AddonBase()
        {
            Http = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                CookieContainer = CookieContainer
            });
        }

        public abstract Task OnExecuteAsync();
        
        /// <exception cref="HttpRequestException"/>
        /// <exception cref="TimeoutException"/>
        protected async Task<HttpResponseMessage> GetResponse(string url, int? overridenTimeout = null)
        {
            var task = Http.GetAsync(url);
            if (await Task.WhenAny(task, Task.Delay(overridenTimeout ?? Timeout)) == task)
                return await task;
            else throw new TimeoutException();
        }

        protected async Task Wait(TimeSpan time) => await Task.Delay(time * TimingScale);
        protected async Task Wait(TimeSpan min, TimeSpan max)
        {
            if (min > max)
                (min, max) = (max, min);
            await Task.Delay((min + random.NextDouble() * (max - min)) * TimingScale);
        }
        protected async Task Wait(uint ms) => await Wait(TimeSpan.FromMilliseconds(ms));
        protected async Task Wait(uint msMin, uint msMax) =>
            await Wait(TimeSpan.FromMilliseconds(msMin), TimeSpan.FromMilliseconds(msMax));
    }
}
