namespace Top.Logging
{
    /// <summary>
    /// How much a message matters, ordered so a writer can drop everything
    /// below the level it cares about.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
    }
}
