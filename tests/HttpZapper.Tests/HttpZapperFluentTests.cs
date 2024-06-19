using System.Net;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace HttpZapper.Tests;

public class HttpZapperFluentTests(HttpZapperIocFixture fixture) 
    : IClassFixture<HttpZapperIocFixture>
{
    [Fact]
    public void Should_allow_set_service_name()
    {
        using var scope = fixture.Scope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpZapper>();
        var givenClientWrapper = scope.ServiceProvider.GetRequiredService<IHttpClientWrapper>();
        
        HttpRequestMessage? gotMsg = null;
        givenClientWrapper.Send(TestConstants.ServiceNames.ApiBooks, Arg.Any<HttpRequestMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            })
            .AndDoes(x =>
            {
                gotMsg = x.Arg<HttpRequestMessage>();
            });
        
        sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books")
            .QueryString("limit", "1")
            .QueryString("author", "test author")
            .QueryStrings([("skip","2")])
            .Header("header-1", "a")
            .Header("header-2", "b")
            .Timeout(1000)
            .Retry(1)
            .Get();

        gotMsg.ShouldNotBeNull();
        gotMsg.RequestUri!.AbsoluteUri.ShouldBe("http://api-books.prod.com.au/books?limit=1&author=test%20author&skip=2");
        gotMsg.Headers.ShouldContain(x => x.Key == "header-1" && string.Join(",",x.Value) == "a");
        gotMsg.Headers.ShouldContain(x => x.Key == "header-2" && string.Join(",",x.Value) == "b");
    }
    
    [Fact]
    public void Should_allow_set_baseurl_and_no_service_name()
    {
        using var scope = fixture.Scope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpZapper>();
        var givenClientWrapper = scope.ServiceProvider.GetRequiredService<IHttpClientWrapper>();
        
        HttpRequestMessage? gotMsg = null;
        givenClientWrapper.Send(Arg.Any<string>(), Arg.Any<HttpRequestMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            })
            .AndDoes(x =>
            {
                gotMsg = x.Arg<HttpRequestMessage>();
            });
        
        sut.BaseUrl("http://api-books.prod.com.au")
            .Path("/books")
            .QueryString("limit", "1")
            .QueryString("author", "test author")
            .QueryStrings([("skip","2")])
            .QueryStrings(new
            {
                Name = "ruhul",
                Age = 100,
                OtherName = (string?)null
            })
            .Header("header-1", "a")
            .Header("header-2", "b")
            .Timeout(1000)
            .Retry(1)
            .Get();

        gotMsg.ShouldNotBeNull();
        gotMsg.RequestUri!.AbsoluteUri.ShouldBe("http://api-books.prod.com.au/books?limit=1&author=test%20author&skip=2&Name=ruhul&Age=100");
        gotMsg.Headers.ShouldContain(x => x.Key == "header-1" && string.Join(",",x.Value) == "a");
        gotMsg.Headers.ShouldContain(x => x.Key == "header-2" && string.Join(",",x.Value) == "b");
    }
}