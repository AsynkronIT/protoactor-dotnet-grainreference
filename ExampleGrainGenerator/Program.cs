using System;
using ExampleGrainGenerator;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Cluster.Partition;
using Proto.Remote.GrpcCore;
using Proto.Remote;
using static Proto.CancellationTokens;

var system = new ActorSystem()
    .WithRemote(GrpcCoreRemoteConfig
        .BindToLocalhost()
        .WithProtoMessages(ExampleGrainGenerator.ProtosReflection.Descriptor))
    .WithCluster(ClusterConfig
        .Setup("MyCluster", new ConsulProvider(new ConsulProviderConfig()), new PartitionIdentityLookup())
        .WithExampleGrainsKinds()
    );

await system
    .Cluster()
    .StartMemberAsync();

var b = new Bbb(); 

//how should factories of interface to impl look?
//currently a hacky static factory
Grains.Factory<HelloGrainBase>.Create = (c, _, _) => new HelloGrain(c);

//how should access to generated grains look?
//currently this is an extension on Cluster for each kind
var helloGrain = system.Cluster().GetHelloGrain("MyGrain");

var res = await helloGrain.SayHello(new HelloRequest()
{
    Name = "Proto Potato"
}, WithTimeout(1000));

await helloGrain.GetCurrentState(new GetCurrentStateRequest(), WithTimeout(1000));
Console.WriteLine(res.Message);

