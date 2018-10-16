using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Dwscdv3.WebArchiver.Addons;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Dwscdv3.WebArchiver
{
    [Command("WebArchiver",
        Description = "An web archiving framework")]
    [HelpOption]
    public class App
    {
        private const string AddonsDirectory = "Addons";
        private const string CookiesFile = "Cookies.json";

        #region Arguments & Options
        [Argument(0, Name = "Add-on name")]
        private string AddonName { get; set; }

        [Option(CommandOptionType.SingleValue,
            Template = "--timing-scale",
            Description = "(1.0)x -- The timing scale of firewall avoiding mechanism.")]
        private double TimingScale { get; } = 1.0;

        [Option(CommandOptionType.SingleValue,
            Template = "--timeout",
            Description = "(5000)ms -- The timeout before cancelling a request.")]
        private int Timeout { get; } = 5000;
        #endregion

        static Task Main(string[] args) => CommandLineApplication.ExecuteAsync<App>(args);

        private async Task OnExecuteAsync(CommandLineApplication app)
        {
            if (AddonName is null)
            {
                AddonName = ChooseAddon();
                if (string.IsNullOrWhiteSpace(AddonName))
                {
                    Console.WriteLine("Can't find any add-on.");
                    Environment.Exit(100);
                }
            }

            var addon = GetAddonInstance();
            addon.CookieContainer = DeserializeCookies();
            addon.TimingScale = TimingScale;
            addon.Timeout = Timeout;
            await addon.OnExecuteAsync();
        }

        private string ChooseAddon()
        {
            var addons = SearchAllAddons();
            return addons.Count > 0 ? Choose(addons, "Select an add-on", retry: true) : null;
        }
        private AddonBase GetAddonInstance()
        {
            var assembly = Assembly.LoadFrom(Path.Combine(AddonsDirectory, AddonName, $"{AddonName}.dll"));
            return assembly.GetTypes()
                .First(t => typeof(AddonBase).IsAssignableFrom(t))
                .InvokeMember(null, BindingFlags.CreateInstance, null, null, null) as AddonBase;
        }
        private List<string> SearchAllAddons()
        {
            var addons = new List<string>();
            foreach (var dir in Directory.EnumerateDirectories(AddonsDirectory))
            {
                var addonName = Path.GetFileName(dir);
                if (File.Exists(Path.Combine(dir, $"{addonName}.dll")))
                    addons.Add(addonName);
            }
            return addons;
        }

        private CookieContainer DeserializeCookies()
        {
            var cookieContainer = new CookieContainer();
            var cookiesFilePath = Path.Combine(AddonsDirectory, AddonName, CookiesFile);
            try
            {
                if (File.Exists(cookiesFilePath))
                {
                    var cookies = JsonConvert.DeserializeObject<IEnumerable<Cookie>>(File.ReadAllText(cookiesFilePath));
                    foreach (var cookie in cookies)
                    {
                        cookieContainer.Add(cookie);
                    }
                }
            }
            catch
            {
                Console.WriteLine($"WARNING: Can't resolve {cookiesFilePath}. Program will keep running, however, some authenticated resources may failed to access.");
            }
            return cookieContainer;
        }

        private T Choose<T>(
            IList<T> options,
            string promptText = "Select one of these",
            bool retry = false,
            int indexStart = 1)
        {
            var index = indexStart;
            foreach (var option in options)
            {
                Console.WriteLine($"{index}\t{option}");
                index += 1;
            }
            do
            {
                Console.Write($"{promptText}: ");
                if (int.TryParse(Console.ReadLine(), out var i) &&
                    i >= indexStart && i < options.Count + indexStart)
                    return options[i - indexStart];
                else if (retry)
                    Console.WriteLine("Not a valid value, please try again.");
                else
                    Console.WriteLine($"Not a valid value, use {default(T)} instead.");
            }
            while (retry);
            return default;
        }
        private T Choose<T>(string promptText = "Select one of these", bool retry = false, int indexStart = 0)
            where T : Enum
            => Choose(Enum.GetValues(typeof(T)).Cast<T>().ToArray(), promptText, retry, indexStart);
    }
}
