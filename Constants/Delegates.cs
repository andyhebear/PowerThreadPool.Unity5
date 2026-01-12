namespace PowerThreadPool_Net20
{
   
    #region .NET 3.5

    // All these delegate are built-in .NET 3.5
    // Comment/Remove them when compiling to .NET 3.5 to avoid ambiguity.

    public delegate void Action();
    //public delegate void Action<T1, T2>(T1 arg1,T2 arg2);
    //public delegate void Action<T1, T2, T3>(T1 arg1,T2 arg2,T3 arg3);
    //public delegate void Action<T1, T2, T3, T4>(T1 arg1,T2 arg2,T3 arg3,T4 arg4);

    public delegate TResult Func<TResult>();
    public delegate TResult Func<T, TResult>(T arg1);
    //public delegate TResult Func<T1, T2, TResult>(T1 arg1,T2 arg2);
    //public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1,T2 arg2,T3 arg3);
    //public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1,T2 arg2,T3 arg3,T4 arg4);

    #endregion
}
