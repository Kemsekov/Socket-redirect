using System.Net;

//forward ports 10001 to 20001, 10002 to 20002, etc
//dotnet run "NET_ID" "path" 9993 "10001 10002 10003" "20001 20002 20003"

var remotePort      = args[0].Split(" ").Select(int.Parse).ToArray();
var localPort       = args[1].Split(" ").Select(int.Parse).ToArray();

if(remotePort.Length!=localPort.Length){
    throw new Exception("Remote ports and local ports must be same length");
}
if(remotePort.Length<1){
    throw new Exception("Need at least one pair of remote port and local port");
}

// var initer = new ZeroTierInit(netIdStr,storage,zeroTierPort);
// initer.CreateNode();
var socketCom = new SocketsCommunication();

System.Console.WriteLine("Listening ports...");
foreach(var pair in remotePort.Zip(localPort)){
    socketCom.ForwardPort(pair.First,pair.Second);
}
System.Console.WriteLine("Press enter to end program");
Console.ReadLine();
socketCom.Stop();

