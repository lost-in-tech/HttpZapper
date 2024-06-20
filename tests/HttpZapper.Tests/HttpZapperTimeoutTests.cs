using System.Net;
using HttpZapper.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;

namespace HttpZapper.Tests;

public class HttpZapperTimeoutTests(HttpZapperIocFixture fixture)
    : IClassFixture<HttpZapperIocFixture>
{
    [Fact]
    public async Task Send_should_timeout()
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
            .Retry(0)
            .Get<HttpZapperWithSerializerTests.BookDto>();
            
        gotRsp.StatusCode.ShouldBe(HttpStatusCode.RequestTimeout);
        gotRsp.Headers.ShouldContain(x => x.Name == "x-failure-desc" && x.Value == "Failed because of TimeoutRejectedException");
        fakeHttpClient.TotalInvoked.ShouldBe(1);
    }
}