
using System;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Cluster;

namespace ExampleGrainGenerator
{
    public static class Grains
    {
        public static class Factory<T>
        {
            public static Func<IContext,string,string,T> Create;
        }
        
        public static (string,Props)[] GetClusterKinds()  => new[] { 
                ("HelloGrain", Props.FromProducer(() => new HelloGrainActor())),
            };
    }        
    
    public static class GrainExtensions
    {
        public static ClusterConfig WithMyGrainsKinds(this ClusterConfig config) => 
         config.WithClusterKinds(Grains.GetClusterKinds());
    
        public static HelloGrainClient GetHelloGrain(this Cluster cluster, string identity) => new(cluster, identity);
    }

    public abstract class HelloGrainBase
    {
        protected IContext Context {get;}
    
        protected HelloGrainBase(IContext context)
        {
            Context = context;
        }
        
        public virtual Task OnStarted() => Task.CompletedTask;
        public virtual Task OnStopping() => Task.CompletedTask;
        public virtual Task OnStopped() => Task.CompletedTask;
        public virtual Task OnReceive() => Task.CompletedTask;
    
        public abstract Task<HelloResponse> SayHello(HelloRequest request);
        public abstract Task<GetCurrentStateResponse> GetCurrentState(GetCurrentStateRequest request);
    }

    public class HelloGrainClient
    {
        private readonly string _id;
        private readonly Cluster _cluster;

        public HelloGrainClient(Cluster cluster, string id)
        {
            _id = id;
            _cluster = cluster;
        }

        public async Task<HelloResponse> SayHello(HelloRequest request, CancellationToken ct)
        {
            var gr = new GrainRequestMessage(0, request);
            //request the RPC method to be invoked
            var res = await _cluster.RequestAsync<object>(_id, "HelloGrain", gr, ct);

            return res switch
            {
                // normal response
                GrainResponseMessage grainResponse => (HelloResponse)grainResponse.ResponseMessage,
                // error response
                GrainErrorResponse grainErrorResponse => throw new Exception(grainErrorResponse.Err),
                // unsupported response
                _ => throw new NotSupportedException()
            };
        }
        public async Task<GetCurrentStateResponse> GetCurrentState(GetCurrentStateRequest request, CancellationToken ct)
        {
            var gr = new GrainRequestMessage(1, request);
            //request the RPC method to be invoked
            var res = await _cluster.RequestAsync<object>(_id, "HelloGrain", gr, ct);

            return res switch
            {
                // normal response
                GrainResponseMessage grainResponse => (GetCurrentStateResponse)grainResponse.ResponseMessage,
                // error response
                GrainErrorResponse grainErrorResponse => throw new Exception(grainErrorResponse.Err),
                // unsupported response
                _ => throw new NotSupportedException()
            };
        }
    }

    class HelloGrainActor : IActor
    {
        private HelloGrainBase _inner;

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case ClusterInit msg: 
                {
                    _inner = Grains.Factory<HelloGrainBase>.Create(context, msg.Identity, msg.Kind);
                    await _inner.OnStarted();
                    break;
                }
                case Stopping:
                {
                    await _inner.OnStopping();
                    break;
                }
                case Stopped:
                {
                    await _inner.OnStopped();
                    break;
                }    
                case GrainRequestMessage(var methodIndex, var r):
                {
                    switch (methodIndex)
                    {
                        case 0:
                        {                            
                            try
                            {
                                var res = await _inner.SayHello((HelloRequest)r);
                                var response = new GrainResponseMessage(res);                                
                                context.Respond(response);
                            }
                            catch (Exception x)
                            {
                                var grainErrorResponse = new GrainErrorResponse
                                {
                                    Err = x.ToString()
                                };
                                context.Respond(grainErrorResponse);
                            }

                            break;
                        }
                        case 1:
                        {                            
                            try
                            {
                                var res = await _inner.GetCurrentState((GetCurrentStateRequest)r);
                                var response = new GrainResponseMessage(res);                                
                                context.Respond(response);
                            }
                            catch (Exception x)
                            {
                                var grainErrorResponse = new GrainErrorResponse
                                {
                                    Err = x.ToString()
                                };
                                context.Respond(grainErrorResponse);
                            }

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    await _inner.OnReceive();
                    break;
                }
            }
        }
    }
}

