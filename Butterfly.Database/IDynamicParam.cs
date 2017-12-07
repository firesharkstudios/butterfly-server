namespace Butterfly.Database {
    /// <summary>
    /// Use to implement a parameter value that can change
    /// </summary>
    public interface IDynamicParam {
        object GetValue();
    }
}
