using ExpenseManagement.Core;

namespace ExpenseManagement.Tests;

public class ExpenseQueryTests
{
    [Fact]
    public void ExpenseQuery_HasSafeDefaults()
    {
        var query = new ExpenseQuery();
        Assert.Equal(1, query.Page);
        Assert.Equal(10, query.PageSize);
        Assert.Equal("date", query.SortBy);
        Assert.Equal("desc", query.SortDirection);
    }
}
