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
        Console.Write("Please choose the type of test (n: no content, s: static, h: hello, p: pi, e: echo): ");
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

        lastTimedOutResponse = 0;
        lastResponseTime = 0;
        searchValue = true;
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

    static int lastRespSucceed = 0, lastRespTimeout = 0;
    private static void logResult(int reqPS)
    {
        StreamWriter sw = new StreamWriter(logFileName, true);
        sw.WriteLine($"{DateTime.Now.ToLocalTime().ToString("HH:mm:ss")},{reqPS},{respNumSucceeded - lastRespSucceed},{respNumFailedTimeout - lastRespTimeout}");
        lastRespSucceed = respNumSucceeded;
        lastRespTimeout = respNumFailedTimeout;
        sw.Close();
    }

    static private async void stressNcRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/nc");
        try
        {
            //rsp.Wait(cancelTime);
            await rsp;
        }
        catch (Exception)
        {
            trackTimeoutFailed();
        }
        stopWatch.Stop();
        if(rsp.Status == TaskStatus.RanToCompletion && rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackTimeoutFailed();
    }

    static private async void stressStaticRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("");
        try
        {
            //rsp.Wait(cancelTime);
            await rsp;
        }
        catch (Exception)
        {
            trackTimeoutFailed();
        }
        stopWatch.Stop();
        if (rsp.Status == TaskStatus.RanToCompletion && rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackTimeoutFailed();
    }
    static private async void stressHelloRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/hello");
        try
        {
            //rsp.Wait(cancelTime);
            await rsp;
        }
        catch (Exception)
        {
            trackTimeoutFailed();
        }
        stopWatch.Stop();
        if (rsp.Status == TaskStatus.RanToCompletion && rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackTimeoutFailed();
    }
    static private async void stressPiRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.GetAsync("/pi");
        try
        {
            //rsp.Wait(cancelTime);
            await rsp;
        }
        catch (Exception)
        {
            trackTimeoutFailed();
        }
        stopWatch.Stop();
        if (rsp.Status == TaskStatus.RanToCompletion && rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackTimeoutFailed();
    }
    static private async void stressEchoRequest(object ID)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        var rsp = client.PostAsync("/echo", new StringContent("asaafadassaddafhghsfdgdfh"));
        try
        {
            await rsp;
        }
        catch (Exception)
        {
            trackTimeoutFailed();
        }
        stopWatch.Stop();
        if (rsp.Status == TaskStatus.RanToCompletion && rsp.IsCompleted)
            trackResult(rsp.Result.StatusCode == HttpStatusCode.OK, stopWatch.ElapsedMilliseconds);
        else
            trackTimeoutFailed();
    }

    private static int lastTimedOutResponse = 0, lastResponseTime = 0, lastTime = 0;
    private static bool searchValue = true;
    static int tryIncrement(int time)
    { 
        // Diagnostic time not reached
        if (time - lastTime <= 25 || !searchValue)
            return 0;

        lastTime = time;

        // dropped resource number extremely increased or response time atleast doubled
        if ((respNumFailedTimeout > lastTimedOutResponse*1.5+20 || lastResponseTime > 2*(respTimeSumSucceed / (double)respNumSucceeded)) && time > 100)
        {
            searchValue = false;
            return -1;
        }

        // server still responding, try to increase the load
        lastTimedOutResponse = respNumFailedTimeout;
        lastResponseTime = (int)(respTimeSumSucceed / (double)respNumSucceeded);
        return 1;
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


        Console.Write("Do you want to controll the packet number (y/n)? ");
        bool manualAttackConfig;
        string? inp = Console.ReadLine();
        while (!(inp?.Length == 1 && (inp?[0] == 'y' || inp?[0] == 'n')))
        {
            Console.WriteLine("Input is incorrect!");
            Console.Write("Do you want to controll the packet number (y/n)? ");
            inp = Console.ReadLine();
        }
        manualAttackConfig = inp[0] == 'y';

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
                //ThreadPool.QueueUserWorkItem(stressFunction, i++);
                stressFunction(i++);
                ++reqSentNum;

                if((sw.Elapsed.TotalSeconds - lastModifyTime) * countInASecond < (reqSentNum - lastSentNum))
                    Thread.Sleep(sleepTime);

                if ((int)sw.Elapsed.TotalSeconds > lastElapsed)
                {
                    lastElapsed = (int)sw.Elapsed.TotalSeconds;
                    printResult(countInASecond);
                    logResult(countInASecond);

                    if (!manualAttackConfig)
                    {
                        // Dynamicly change DDoS load
                        countInASecond += 3 * tryIncrement(lastElapsed);
                        sleepTime = TimeSpan.FromMicroseconds(1000000 / countInASecond);
                        lastModifyTime = sw.Elapsed.TotalSeconds;
                        lastSentNum = reqSentNum;
                    }
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

    static string logFileName = "";
    static void createLog(string test)
    {
        logFileName = $"log_{test}.csv";
        StreamWriter sw = new StreamWriter(logFileName);
        sw.WriteLine("Time,SentRequestsPerSecond,SusceededResponses,TimedoutResponses");
        sw.Close();
    }

    static void Main(string[] args)
    {
        client.DefaultRequestHeaders.Add("User-Agent", "C# example");
        ThreadPool.GetMaxThreads(out _, out int cpt);
        ThreadPool.SetMaxThreads(512, cpt);
        string? resp;
        while ((resp = getStressType()) is not null)
        {
            switch (resp)
            {
                case "n":
                    createLog("nc");
                    runTest(new WaitCallback(stressNcRequest));
                    break;
                case "s":
                    createLog("s");
                    runTest(new WaitCallback(stressStaticRequest));
                    break;
                case "h":
                    createLog("he");
                    runTest(new WaitCallback(stressHelloRequest));
                    break;
                case "p":
                    createLog("pi");
                    runTest(new WaitCallback(stressPiRequest));
                    break;
                case "e":
                    createLog("e");
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
                //if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                //    break;
            }
            //while (Console.KeyAvailable)
            //    Console.ReadKey();
            resetTracking();
        }
    }
}
