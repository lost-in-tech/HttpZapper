using System.Net;
using HttpZapper.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace HttpZapper.Tests;

public class HttpZapperRetryTests(HttpZapperIocFixture fixture)
    : IClassFixture<HttpZapperIocFixture>
{
    [Fact]
    public async Task Send_should_retry_on_timeout_exception()
    {
        var sc = fixture.BuildServiceCollection();

        var fakeHttpClient = new FakeHttpClientWrapper(HttpStatusCode.OK, 100);
        
        sc.Replace(ServiceDescriptor.Scoped<IHttpClientWrapper>(_ => fakeHttpClient));

        var sp = sc.BuildServiceProvider();

        using var scope = sp.CreateScope();

        var sut = scope.ServiceProvider.GetRequiredService<IHttpZapper>();
        var gotRsp = await sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books")
            .Timeout(50)
            .Retry(2)
            .Get<HttpZapperWithSerializerTests.BookDto>();
            
        gotRsp.StatusCode.ShouldBe(HttpStatusCode.RequestTimeout);
        fakeHttpClient.TotalInvoked.ShouldBe(3);
    }
    
    [Theory]
    [InlineData(HttpStatusCode.OK, HttpStatusCode.OK, 1)]
    [InlineData(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, 3)]
    [InlineData(HttpStatusCode.NotFound, HttpStatusCode.NotFound, 1)]
    [InlineData(HttpStatusCode.GatewayTimeout, HttpStatusCode.GatewayTimeout, 3)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, 3)]
    public async Task Send_should_retry_on_transient_failure(HttpStatusCode statusCode, HttpStatusCode expectedStatusCode, int expectedCount)
    {
        var sc = fixture.BuildServiceCollection();

        var fakeHttpClient = new FakeHttpClientWrapper(statusCode, 0);
        
        sc.Replace(ServiceDescriptor.Scoped<IHttpClientWrapper>(_ => fakeHttpClient));

        var sp = sc.BuildServiceProvider();

        using var scope = sp.CreateScope();

        var sut = scope.ServiceProvider.GetRequiredService<IHttpZapper>();
        var gotRsp = await sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books")
            .Retry(2)
            .Get();
            
        gotRsp.StatusCode.ShouldBe(expectedStatusCode);
        fakeHttpClient.TotalInvoked.ShouldBe(expectedCount);
    }
}