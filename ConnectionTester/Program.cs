using System.Diagnostics;
using System.Net;

namespace ConnectionTester;

internal class Program
{
    const int cancelTime = 20000;
    static private HttpClient client = new()
    {
        BaseAddress = new Uri("http://192.168.3.7"),
        Timeout = TimeSpan.FromSeconds(25),
    };
    static private string? getStressType()
    {
        Console.Write("Please choose the type of test (n: no content, s: static,h: hello, p: pi, e: echo): ");
        return Console.ReadLine();
    }

    private static object lockObj = new object();
    private static long respTimeSumFailed = 0, respTimeSumSucceed = 0;
    private static int lastResponseTime = -1, respNumFailed = 0, respNumSucceeded = 0, respNumFailedTimeout = 0;
    private static Stopwatch sw = new Stopwatch();

    private static void trackResult(bool suc, long respTime)
    {
        lock (lockObj)
        {
            if(respTime < 0)
            {
                respTimeSumFailed++;
            }
            else if (suc)
            {
                respTimeSumSucceed += respTime;
                respNumSucceeded++;
                lastResponseTime = (int)respTime;
            }
            else
            {
                respTimeSumFailed += respTime;
                respNumFailed++;
            }
        }
    }

    private static void resetTracking()
    {
        respTimeSumFailed = 0;
        respTimeSumSucceed = 0;
        respNumFailed = 0;
        respNumFailedTimeout = 0;
        respNumSucceeded = 0;
        lastResponseTime = 0;
        sw.Reset();
    }

    private static void printResult()
    {
        Console.Clear();
        Console.WriteLine($"Press ESC to stop\n");
        Console.WriteLine($"Elapsed time: {(int)sw.Elapsed.TotalSeconds}, Last succeeded request response time: {lastResponseTime}ms\nSucceeded requests: {respNumSucceeded}, Average response time: {Math.Round(respTimeSumSucceed / (double)respNumSucceeded, 3)}ms\nFailed requests: {respNumFailed}, Failed response time: {Math.Round(respTimeSumFailed / (double)respNumFailed, 3)}ms\nFailed due to {cancelTime / 1000}s timeout: {respNumFailedTimeout}");
    }

    private static void printResultCooldown(int timeLeft)
    {
        Console.Clear();
        Console.WriteLine($"Waiting for response cooldown period. Hold ESC to force stop\nTime left: {timeLeft}");
        Console.WriteLine($"Elapsed time: {(int)sw.Elapsed.TotalSeconds}, Last succeeded request response time:  {lastResponseTime}ms\nSucceeded requests: {respNumSucceeded}, Average response time: {Math.Round(respTimeSumSucceed / (double)respNumSucceeded, 3)}ms\nFailed requests: {respNumFailed}, Failed response time: {Math.Round(respTimeSumFailed / (double)respNumFailed, 3)}ms\nFailed due to {cancelTime / 1000}s timeout: {respNumFailedTimeout}");
    }

    private static void logResult()
    {
        StreamWriter sw = new StreamWriter("log.csv", true);
        sw.WriteLine($"{DateTime.Now.ToLocalTime().ToString("HH:mm:ss")},{Math.Round(respTimeSumSucceed / (double)respNumSucceeded, 3)}");
        sw.Close();
    }

    static private void stressNcRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/nc");
        rsp.Wait(cancelTime);
        stopWatch.Stop();
        if (rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackResult(false, -1);
    }

    static private void stressStaticRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("");
        rsp.Wait(cancelTime);
        stopWatch.Stop();
        if (rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackResult(false, -1);
    }
    static private void stressHelloRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/hello");
        rsp.Wait(cancelTime);
        stopWatch.Stop();
        if (rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackResult(false, -1);
    }
    static private void stressPiRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/pi");
        rsp.Wait(cancelTime);
        stopWatch.Stop();
        if (rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackResult(false, -1);
    }
    static private void stressEchoRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.PostAsync("/echo", new StringContent("asaafadassaddafhghsfdgdfh"));
        rsp.Wait(cancelTime);
        stopWatch.Stop();
        if (rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackResult(false, -1);
    }

    static void runTest(WaitCallback stressFunction)
    {
        Console.WriteLine("Press ESC to stop");
        int i = 0, lastElapsed = 0;
        sw.Start();
        do
        {
            while (!Console.KeyAvailable)
            {
                stressFunction(i++);

                Thread.Sleep(1000);

                if ((int)sw.Elapsed.TotalSeconds > lastElapsed)
                {
                    lastElapsed = (int)sw.Elapsed.TotalSeconds;
                    printResult();
                    logResult();
                }
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
    }

    static void Main(string[] args)
    {
        StreamWriter sw = new StreamWriter("log.csv");
        sw.WriteLine("Time,ResponseTimeAverage");
        sw.Close();
        client.DefaultRequestHeaders.Add("User-Agent", "C# example");
        string? resp;
        while ((resp = getStressType()) is not null)
        {
            switch (resp)
            {
                case "n":
                    runTest(new WaitCallback(stressNcRequest));
                    break;
                case "s":
                    runTest(new WaitCallback(stressStaticRequest));
                    break;
                case "h":
                    runTest(new WaitCallback(stressHelloRequest));
                    break;
                case "p":
                    runTest(new WaitCallback(stressPiRequest));
                    break;
                case "e":
                    runTest(new WaitCallback(stressEchoRequest));
                    break;
                default:
                    Console.WriteLine("Unknown command, please retry!");
                    break;
            }
            for (int i = 0; i < 25; ++i)
            {
                printResultCooldown(25 - i);
                Thread.Sleep(1000);
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    break;
            }
            while (Console.KeyAvailable)
                Console.ReadKey();
            resetTracking();
        }
    }
}
