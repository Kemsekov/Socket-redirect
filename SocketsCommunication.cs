
using System.Net;
using System.Net.Sockets;

public class SocketsCommunication
{
    public event Action<(Socket src, Socket dst)> OnRemoveSocket;
    public List<(Socket src, Socket dst)> Pairs { get; set; }
    public int BufferSize { get; }
    public int DelayTime { get; }
    bool runningForwarding = false;
    CancellationTokenSource _Cancellation;

    public SocketsCommunication(int bufferSize = 4096, int delayTime = 10)
    {
        Pairs = new();
        BufferSize = bufferSize;
        DelayTime = delayTime;
        OnRemoveSocket = ShutdownSockets;
        _Cancellation = new CancellationTokenSource();
    }
    void ShutdownSockets((Socket src, Socket dst) pair){
        try{
            pair.src.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            pair.src.Close();
        }
        finally{}

        try{
            pair.dst.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            pair.dst.Close();
        }
        finally{}
    }
    async void ForwardingMany(CancellationToken token)
    {
        lock(_Cancellation){
            if(runningForwarding) return;
            runningForwarding = true;
        }

        await Task.Yield();
        var buffer = new byte[BufferSize];
        while (!token.IsCancellationRequested)
        {
            if(Pairs.Count==0){
                lock(_Cancellation)
                    runningForwarding = false;
                return;
            }
            for (int i = 0; i < Pairs.Count; i++)
            {
                try
                {
                    var (src, dst) = Pairs[i];
                    var receivedBytes = src.Receive(buffer);
                    if (receivedBytes == 0) throw new Exception();
                    dst.Send(buffer, 0, receivedBytes, System.Net.Sockets.SocketFlags.None);
                }
                finally
                {
                    //on failed sockets communication or on zero-sized receive
                    //remove sockets
                    OnRemoveSocket(Pairs[i]);
                    Pairs.RemoveAt(i--);
                }
            }
            await Task.Delay(DelayTime);
        }
    }
    public void ForwardPort(int remotePort,int localPort){
        ForwardPort("0.0.0.0",remotePort,"127.0.0.1",localPort);
    }

    public async void ForwardPort(string remoteIp,int remotePort,string localIp, int localPort){
        await Task.Yield();
        var conName = $"{remoteIp}:{remotePort} -> {localIp}:{localPort}";
        while(!_Cancellation.IsCancellationRequested){
            Socket? server, client,con;
            try
            {
                server = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, 0);
                server.Bind(new IPEndPoint(IPAddress.Parse(remoteIp), remotePort));
                server.Listen(5);

                System.Console.WriteLine($"Waiting for connection on {conName}");
                con = server.Accept();
                System.Console.WriteLine($"Accepted connection on {conName}");


                client = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, 0);
                client.Connect(new IPEndPoint(IPAddress.Parse(localIp), localPort));
            }
            catch
            {
                System.Console.WriteLine($"Connection failed on {conName}");
                await Task.Delay(1000);
                return;
            }
            
            Pairs.AddRange([(con,client),(client,con)]);
            ForwardingMany(_Cancellation.Token);
        }
        System.Console.WriteLine($"Stopped on {conName}");
    }
    public void Stop(){
        _Cancellation.Cancel();
    }
}