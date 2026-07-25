namespace BimosVrInjector.Core.Tests;

internal sealed class Harness
{
    private int _pass;
    private int _fail;

    public void True(bool cond, string what) => Record(cond, what, "expected true");
    public void False(bool cond, string what) => Record(!cond, what, "expected false");
    public void NotNull(object? o, string what) => Record(o != null, what, "expected non-null");

    public void Eq<T>(T expected, T actual, string what)
        => Record(Equals(expected, actual), what, $"expected '{expected}', got '{actual}'");

    private void Record(bool ok, string what, string detail)
    {
        if (ok)
        {
            _pass++;
            Console.WriteLine($"  PASS  {what}");
        }
        else
        {
            _fail++;
            Console.WriteLine($"  FAIL  {what}  ({detail})");
        }
    }

    public int Report()
    {
        Console.WriteLine();
        Console.WriteLine($"===== {_pass} passed, {_fail} failed =====");
        return _fail == 0 ? 0 : 1;
    }
}
