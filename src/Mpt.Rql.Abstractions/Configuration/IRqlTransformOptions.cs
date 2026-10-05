namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlTransformOptions
{
    IRqlSettings Settings { get; }

    /// <summary>
    /// Registers a callback that receives the graph once it is built from the request and the defaults. Callbacks run
    /// in registration order, and the query is projected from the graph as they leave it.
    /// </summary>
    void OnGraphBuilt(Action<IRqlNode> callback);
}
