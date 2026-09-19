namespace Phainein;

public class ReadWriteLock : ReaderWriterLockSlim
{
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public ReadWriteLock(LockRecursionPolicy policy = LockRecursionPolicy.NoRecursion) : base(policy)
    {
        // Todo: The idea is to use encapsulation over basic sync object of the .NET framework and add an observability layer
    }
}