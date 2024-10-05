using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;

namespace HttpZapper.Tests;

public class HttpMsgRequestFilterTests
{
    public class HeadersAddedInFilterShould(HttpZapperIocFixture fixture) : IClassFixture<HttpZapperIocFixture>
    {
        [Fact]
        public async Task Available_when_get_request_send()
        {
            var sc = fixture.BuildServiceCollection();
            var givenFilter = Substitute.For<IHttpMsgRequestFilter>();
            givenFilter.Filter(Arg.Any<HttpMsgRequest>()).Returns(info =>
            {
                var data = info.Arg<HttpMsgRequest>();
                return data with
                {
                    Headers = (data.Headers ?? [])
                                .Append(("x-test-1","test-1"))
                };
            });

            sc.TryAddTransient<IHttpMsgRequestFilter>(_ => givenFilter);
            
            
            var sp = sc.BuildServiceProvider();

            var sut = sp.GetRequiredService<IHttpZapper>();

            HttpRequestMessage? gotRequest = null;
            sp.GetRequiredService<IHttpClientWrapper>().Send(TestConstants.ServiceNames.ApiBooks,
                    Arg.Any<HttpRequestMessage>(),
                    Arg.Any<CancellationToken>())
                .Returns(new HttpResponseMessage(HttpStatusCode.OK))
                .AndDoes(callInfo =>
                {
                    gotRequest = callInfo.Arg<HttpRequestMessage>();
                });

            var rsp = await sut.Service(TestConstants.ServiceNames.ApiBooks)
                .Path("/books")
                .Get();
            
            rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
            gotRequest.ShouldNotBeNull();
            gotRequest.Headers.ShouldContain(x => x.Key == "x-test-1" && string.Join(",", x.Value) == "test-1");
        }
    }
}