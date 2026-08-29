using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);

            using var provider = services.BuildServiceProvider();

            var OfficeExtractor = provider.GetRequiredService<MainExtractor>();

            Console.WriteLine("Hello, World!");
        }
    }
}
