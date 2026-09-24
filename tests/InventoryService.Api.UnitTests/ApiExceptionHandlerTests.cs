using InventoryService.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.UnitTests;

public sealed class ApiExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> KnownExceptions => new()
    {
        { new NotFoundException("Item 'x' was not found."), StatusCodes.Status404NotFound, "Not found" },
        { new ConflictException("Not enough stock."), StatusCodes.Status409Conflict, "Conflict" },
        { new BadRequestException("Quantity is too large."), StatusCodes.Status400BadRequest, "Bad request" }
    };

    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task TryHandle_KnownException_WritesMatchingProblemDetails(Exception exception, int expectedStatus, string expectedTitle)
    {
        var problemDetails = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus, problemDetails.Written!.Status);
        Assert.Equal(expectedTitle, problemDetails.Written.Title);
        Assert.Equal(exception.Message, problemDetails.Written.Detail);
    }

    [Fact]
    public async Task TryHandle_UnknownException_IsLeftToTheDefaultHandler()
    {
        var problemDetails = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("secret internal detail"), CancellationToken.None);

        Assert.False(handled);
        Assert.Null(problemDetails.Written);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
