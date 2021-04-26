using System;
using System.Threading.Tasks;
using ExampleGrainGenerator;
using Proto;
using static System.Threading.Tasks.Task;
#pragma warning disable 1998

class HelloGrain : HelloGrainBase
{
    private HelloGrainState _state;

    public HelloGrain(IContext ctx) : base(ctx)
    {
    } 

    public override Task OnStarted()
    {
        _state = new HelloGrainState();
        Console.WriteLine("Started");
        return CompletedTask;
    }

    public override Task<HelloResponse> SayHello(HelloRequest request) =>
        FromResult(new HelloResponse()
        {
            Message = $"Hello {request.Name}, pretty cool, right?"
        });

    public override Task<GetCurrentStateResponse> GetCurrentState(GetCurrentStateRequest request) =>
        FromResult(new GetCurrentStateResponse()
        {
            State = _state
        });
}