namespace PowerThreadPool_Net20
{
    /// <summary>
    /// Represents a function that returns a result
    /// </summary>
    /// <typeparam name="TResult">The type of the result</typeparam>
    /// <returns>The result of the function</returns>
    public delegate TResult Func<TResult>();

    /// <summary>
    /// Represents an action that takes no parameters
    /// </summary>
    public delegate void Action();
}
