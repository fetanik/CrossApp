namespace Core.Threading;

public static class LinkedCancellation
{
    public static CancellationTokenSource Link(
        CancellationToken userToken,
        CancellationToken timeoutToken)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(
            userToken,
            timeoutToken);
    }
}
