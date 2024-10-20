using System.Net;

namespace ESP_DoS_Tester;

internal class Program
{
    static private HttpClient client = new()
    {
        BaseAddress = new Uri("http://192.168.3.7"),
        Timeout = TimeSpan.FromSeconds(100),
    };
    static private string? getStressType()
    {
        Console.Write("Please choose the type of test (s: static,h: hello, p: pi, e: echo): ");
        return Console.ReadLine();
    }

    static private void stressStaticRequest()
    {
        var rsp = client.GetAsync("");
        rsp.Wait();
        var res = rsp.Result.Content.ReadAsStringAsync();
        res.Wait();
        Console.WriteLine(res.Result);
    }
    static private void stressHelloRequest()
    {
        var rsp = client.GetAsync("/hello");
        rsp.Wait();
        var res = rsp.Result.Content.ReadAsStringAsync();
        res.Wait();
        Console.WriteLine(res.Result);
    }
    static private void stressPiRequest()
    {
        var rsp = client.GetAsync("/pi");
        rsp.Wait();
        var res = rsp.Result.Content.ReadAsStringAsync();
        res.Wait();
        Console.WriteLine(res.Result);
    }
    static private void stressEchoRequest()
    {
        var rsp = client.GetAsync("/echo");
        rsp.Wait();
        var res = rsp.Result.Content.ReadAsStringAsync();
        res.Wait();
        Console.WriteLine(res.Result);
    }

    static void Main(string[] args)
    {
        client.DefaultRequestHeaders.Add("User-Agent", "c# example");
        string? resp;
        while ((resp = getStressType()) is not null)
        {
            switch (resp)
            {
                case "s":
                    stressStaticRequest();
                    break;
                case "h":
                    stressHelloRequest();
                    break;
                case "p":
                    stressPiRequest();
                    break;
                case "e":
                    stressEchoRequest();
                    break;
                default:
                    Console.WriteLine("Unknown command, please retry!");
                    break;
            }
        }
    }
}
