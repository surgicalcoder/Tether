namespace Tether
{
    internal interface ICheck
    {
        string Key { get; }
        object DoCheck();
    }
}