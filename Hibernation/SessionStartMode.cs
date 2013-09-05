namespace Hibernation
{
    internal enum SessionStartMode
    {
        Unknown = 0,
        HttpRequest = 1,
        AtuoTransactionScope = 2,
        Manual = 4
    }
}