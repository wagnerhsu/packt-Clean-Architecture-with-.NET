using Infrastructure.Helpers;

namespace Infrastructure.UnitTests.Helpers;

public class TypeNameHelperTests
{
    [Fact]
    public void Should_Return_Simple_Type_Name()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(string));

        Assert.Equal("String", result);
    }

    [Fact]
    public void Should_Return_Generic_Type_Name()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(List<int>));

        Assert.Equal("List<Int32>", result);
    }

    [Fact]
    public void Should_Handle_Multiple_Generic_Arguments()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(Dictionary<string, int>));

        Assert.Equal("Dictionary<String, Int32>", result);
    }

    [Fact]
    public void Should_Handle_Nested_Generic_Types()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(Dictionary<string, List<int>>));

        Assert.Equal("Dictionary<String, List<Int32>>", result);
    }

    [Fact]
    public void Should_Handle_Nullable_Types()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(int?));

        Assert.Equal("Int32?", result);
    }

    [Fact]
    public void Should_Handle_Array_Types()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(int[]));

        Assert.Equal("Int32[]", result);
    }

    [Fact]
    public void Should_Handle_Nested_Array_And_Generic_Types()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(List<int[]>));

        Assert.Equal("List<Int32[]>", result);
    }

    [Fact]
    public void Should_Handle_ValueTuple_Types()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof((int, string)));

        Assert.Equal("(Int32, String)", result);
    }

    [Fact]
    public void Should_Handle_Nested_ValueTuple_Types()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof((int, (string, bool))));

        Assert.Equal("(Int32, (String, Boolean))", result);
    }

    [Fact]
    public void Should_Handle_Generic_With_Tuple()
    {
        var result = TypeNameHelper.GetFriendlyName(typeof(Dictionary<string, (int, bool)>));

        Assert.Equal("Dictionary<String, (Int32, Boolean)>", result);
    }

    [Fact]
    public void Should_Return_Same_Result_On_Repeated_Calls()
    {
        var first = TypeNameHelper.GetFriendlyName(typeof(List<int>));
        var second = TypeNameHelper.GetFriendlyName(typeof(List<int>));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Should_Throw_When_Type_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => TypeNameHelper.GetFriendlyName(null!));
    }
}