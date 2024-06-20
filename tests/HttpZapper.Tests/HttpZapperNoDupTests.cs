using System.Net;
using System.Text;
using Bolt.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace HttpZapper.Tests;

public class HttpZapperNoDupTests(HttpZapperIocFixture fixture) 
    : IClassFixture<HttpZapperIocFixture>
{
    private HttpContent BuildContent()
    {
        var data = new HttpZapperWithSerializerTests.BookDto
        {
            Id = "1",
            Isbn = "ISBN-123",
            Title = "testing",
            Price = 34.56
        };

        var str = data.SerializeToJson() ?? string.Empty;

        return new StringContent(
            str,
            Encoding.UTF8,
            "application/json");
    }

    [Fact]
    public async Task Get_should_allow_only_once_per_request_when_url_is_same()
    {
        using var scope = fixture.Scope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpZapper>();
        var givenClient = scope.ServiceProvider.GetRequiredService<IHttpClientWrapper>();
        givenClient.Send(TestConstants.ServiceNames.ApiBooks,
                Arg.Any<HttpRequestMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = BuildContent()
                }, new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = BuildContent()
                },
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = BuildContent()
                },
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = BuildContent()
                });


        var rspTask1 = sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books/1")
            .QueryString("deleted", "true")
            .Get<HttpZapperWithSerializerTests.BookDto>();

        var rspTask2 = sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books/1")
            .QueryString("deleted", "true")
            .Get<HttpZapperWithSerializerTests.BookDto>();

        var rspTask3 = sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books/1")
            .QueryString("deleted", "true")
            .Get<HttpZapperWithSerializerTests.BookDto>();

        var rspTask4 = sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books/1")
            .QueryString("deleted", "true")
            .Get<HttpZapperWithSerializerTests.BookDto>();

        await Task.WhenAll(rspTask1, rspTask2, rspTask3, rspTask4);

        var rsp = rspTask1.Result;

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        rsp.Content.ShouldNotBeNull();
        rsp.Content.Id.ShouldBe("1");

        await givenClient.Received(1).Send(TestConstants.ServiceNames.ApiBooks,
            Arg.Any<HttpRequestMessage>(),
            Arg.Any<CancellationToken>());
    }
}