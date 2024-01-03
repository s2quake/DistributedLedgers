using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Library.Commands;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter1;

[Export(typeof(ICommand))]
sealed partial class Algorithm_2_9Command : CommandAsyncBase
{
    public static readonly string[] Sentences =
    [
        "WE ARE PLANETARIUM",
        "WE ARE A COMMUNITY DRIVEN, WEB 3 GAMING COMPANY",
        "WE ARE PASSIONATELY OBSESSED TO DELIVER SCALABLE, MEANINGFUL IMPACT THROUGH OUR GAMING EXPERIENCES, WHERE YOU CAN CREATE, SHARE, OWN & EARN.",
        "WE INVITE YOU TO COME AS YOU ARE, TO EMPOWER, EXPAND, & SHARE YOUR CREATIVITY",
        "WE WELCOME & SUPPORT CREATORS, PLAYERS, BUILDERS, MODDERS & EXPLORERS",
        "BECAUSE WE BELIEVE COMMUNITIES CAN CREATE INFINITE POSSIBILITIES THROUGH DECENTRALIZED INNOVATIONS",
    ];

    public Algorithm_2_9Command()
        : base("alg-2-9")
    {
    }

    protected override async Task OnExecuteAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var serializerPort = PortUtility.GetPort();
        var serverPorts = PortUtility.GetPorts(2);
        var clientService1 = new ClientMessageService("client1");
        var clientService2 = new ClientMessageService("client2");
        Out.WriteLine("Nodes initializing.");
        await using var server1 = await SimpleServer.CreateAsync(serverPorts[0], new ServerMessageService("server1"));
        await using var server2 = await SimpleServer.CreateAsync(serverPorts[1], new ServerMessageService("server2"));
        await using var serializer = await Serializer.CreateAsync(serializerPort, serverPorts);
        await using var client1 = await SimpleClient.CreateAsync(serializerPort, clientService1);
        await using var client2 = await SimpleClient.CreateAsync(serializerPort, clientService2);
        Out.WriteLine("Nodes initialized.");

        var taskList = new List<Task>(Sentences.Length * 2);
        foreach (var item in Sentences)
        {
            taskList.Add(clientService1.SendMessageAsync(item, cancellationToken));
            taskList.Add(clientService2.SendMessageAsync(item, cancellationToken));
        }
        await Task.WhenAll(taskList);
    }
}
