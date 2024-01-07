using System.ComponentModel.Composition;
using DistributedLedgers.ConsoleHost.Common;
using JSSoft.Commands;
using JSSoft.Terminals;

namespace DistributedLedgers.ConsoleHost.Commands.Chapter2;

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
        var serverService1 = new ServerMessageService("server1");
        var serverService2 = new ServerMessageService("server2");
        Out.WriteLine("Nodes initializing.");
        await using var server1 = await SimpleServer.CreateAsync(serverPorts[0], serverService1, cancellationToken);
        await using var server2 = await SimpleServer.CreateAsync(serverPorts[1], serverService2, cancellationToken);
        await using var serializer = await Serializer.CreateAsync(serializerPort, serverPorts, cancellationToken);
        await using var client1 = await SimpleClient.CreateAsync(serializerPort, clientService1, cancellationToken);
        await using var client2 = await SimpleClient.CreateAsync(serializerPort, clientService2, cancellationToken);
        Out.WriteLine("Nodes initialized.");

        await Parallel.ForAsync(0, Sentences.Length, async (i, cancellationToken) =>
        {
            await clientService1.SendMessageAsync(i, Sentences[i], cancellationToken);
            await clientService2.SendMessageAsync(i, Sentences[i], cancellationToken);
        });

        var tsb = new TerminalStringBuilder();
        var messages1 = serverService1.Messages;
        var messages2 = serverService2.Messages;
        tsb.IsBold = true;
        tsb.AppendLine(serverService1.Name);
        tsb.IsBold = false;
        for (var i = 0; i < messages1.Length; i++)
        {
            tsb.AppendLine($"    {messages1[i]}");
        }
        tsb.AppendLine();
        tsb.IsBold = true;
        tsb.AppendLine(serverService2.Name);
        tsb.IsBold = false;
        for (var i = 0; i < messages2.Length; i++)
        {
            tsb.AppendLine($"    {messages2[i]}");
        }
        await Out.WriteAsync(tsb.ToString());
    }
}
