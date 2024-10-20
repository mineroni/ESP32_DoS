using System.Diagnostics;
using System.Net;

namespace ESP_DoS_Tester;

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
    private static object lockObj2 = new object();
    private static long respTimeSumFailed = 0, respTimeSumSucceed = 0;
    private static int reqSentNum = 0, respNumFailed = 0, respNumSucceeded = 0, respNumFailedTimeout = 0;
    private static Stopwatch sw = new Stopwatch();

    private static void trackResult(bool suc, long respTime)
    {
        lock (lockObj) 
        {
            if (suc)
            {
                respTimeSumSucceed += respTime;
                respNumSucceeded++;
            }
            else
            {
                respTimeSumFailed += respTime;
                respNumFailed++;
            } 
        }
    }
    private static void trackTimeoutFailed()
    {
        lock (lockObj2)
        {
            ++respNumFailedTimeout;
        }
    }

    private static void resetTracking()
    {
        respTimeSumFailed = 0;
        respTimeSumSucceed = 0;
        respNumFailed = 0;
        respNumFailedTimeout = 0;
        respNumSucceeded = 0;
        reqSentNum = 0;
        sw.Reset();
    }

    private static void printResult(int reqPS)
    {
        Console.Clear();
        Console.WriteLine($"Press ESC to stop\nPress \".\" to increment, \"-\" to decrement. Requests per second: {reqPS}");
        Console.WriteLine($"Elapsed time: {(int)sw.Elapsed.TotalSeconds}, Sent requests: {reqSentNum}\nSucceeded requests: {respNumSucceeded}, Average response time: {Math.Round(respTimeSumSucceed / (double)respNumSucceeded, 3)}ms\nFailed requests: {respNumFailed}, Failed response time: {Math.Round(respTimeSumFailed / (double)respNumFailed, 3)}ms\nFailed due to {cancelTime / 1000}s timeout: {respNumFailedTimeout}");
    }
    private static void printResultCooldown(int timeLeft)
    {
        Console.Clear();
        Console.WriteLine($"Waiting for response cooldown period.\nTime left: {timeLeft}");
        Console.WriteLine($"Elapsed time: {(int)sw.Elapsed.TotalSeconds}, Sent requests: {reqSentNum}\nSucceeded requests: {respNumSucceeded}, Average response time: {Math.Round(respTimeSumSucceed / (double)respNumSucceeded, 3)}ms\nFailed requests: {respNumFailed}, Failed response time: {Math.Round(respTimeSumFailed / (double)respNumFailed, 3)}ms\nFailed due to {cancelTime / 1000}s timeout: {respNumFailedTimeout}");
    }

    static private void stressNcRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/nc");
        rsp.Wait(cancelTime);
        stopWatch.Stop();
        if(rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackTimeoutFailed();
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
            trackTimeoutFailed();
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
            trackTimeoutFailed();
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
            trackTimeoutFailed();
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
            trackTimeoutFailed();
    }

    static void runTest(WaitCallback stressFunction)
    {
        Console.Write("How many requests do you want to send in a second? ");
        int countInASecond;
        while (!int.TryParse(Console.ReadLine(), out countInASecond))
        {
            Console.WriteLine("Input is incorrect!");
            Console.Write("How many requests do you want to send in a second? ");
        }
        TimeSpan sleepTime = TimeSpan.FromMicroseconds(1000000 / countInASecond);
        Console.WriteLine("Press ESC to stop");
        int i = 0;
        int lastElapsed = 0;
        double lastModifyTime = 0;
        int lastSentNum = 0;
        sw.Start();
        ConsoleKey lastKey;
        do
        {
            while (!Console.KeyAvailable)
            {
                ThreadPool.QueueUserWorkItem(stressFunction, i++);
                ++reqSentNum;

                if((sw.Elapsed.TotalSeconds - lastModifyTime) * countInASecond < (reqSentNum - lastSentNum))
                    Thread.Sleep(sleepTime);

                if ((int)sw.Elapsed.TotalSeconds > lastElapsed)
                { 
                    lastElapsed = (int)sw.Elapsed.TotalSeconds;
                    printResult(countInASecond);
                }
            }
            lastKey = Console.ReadKey(true).Key;
            if(lastKey == ConsoleKey.OemPeriod || lastKey == ConsoleKey.OemMinus)
            {
                if (lastKey == ConsoleKey.OemMinus && countInASecond > 1)
                    countInASecond -= countInASecond > 20 ? 20 : 1;
                else
                    countInASecond += countInASecond >= 20 ? 20 : 1;
                sleepTime = TimeSpan.FromMicroseconds(1000000 / countInASecond);
                lastModifyTime = sw.Elapsed.TotalSeconds;
                lastSentNum = reqSentNum;
                printResult(countInASecond);
            }
        } while (lastKey != ConsoleKey.Escape);
    }

    static void Main(string[] args)
    {
        client.DefaultRequestHeaders.Add("User-Agent", "C# example");
        ThreadPool.GetMaxThreads(out _, out int cpt);
        ThreadPool.SetMaxThreads(10000, cpt);
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
            for(int i = 0; i < 25; ++i)
            {
                printResultCooldown(25-i);
                Thread.Sleep(1000);
            }
            resetTracking();
        }
    }
}
