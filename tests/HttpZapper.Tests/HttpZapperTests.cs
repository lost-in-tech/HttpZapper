using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace HttpZapper.Tests;

public class HttpZapperTests : IClassFixture<HttpZapperIocFixture>
{
    private readonly HttpZapperIocFixture _fixture;

    public HttpZapperTests(HttpZapperIocFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Send_should_do_get_requests_with_correct_data_and_retrieve_correct_data()
    {
        using var scope = _fixture.Scope();

        var sut = scope.ServiceProvider.GetRequiredService<IHttpZapper>();

        var serializer = scope.ServiceProvider.GetRequiredService<IHttpMessageSerializer>();
        var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientWrapper>();
        httpClient.ClearReceivedCalls();

        HttpRequestMessage? givenRequest = null;
        httpClient.Send(TestConstants.ServiceNames.ApiBooks, Arg.Any<HttpRequestMessage>(),
            Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(serializer.Serialize(new List<BookDto>
                {
                    new()
                    {
                        Id = "1",
                        Isbn = "1sbn-1",
                        Title = "title-1",
                        Price = 23.99
                    }
                }))
            }).AndDoes(x =>
            {
                givenRequest = x.Arg<HttpRequestMessage>();
            });

        var rsp = await sut.Service(TestConstants.ServiceNames.ApiBooks)
            .Path("/books")
            .QueryString("test-qs","1")
            .Header("test-h","1")
            .Get<BookDto[]>(CancellationToken.None);

        rsp.ShouldNotBeNull();
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        givenRequest.ShouldNotBeNull();
        givenRequest.RequestUri.ToString().ShouldBe("http://api-books.prod.com.au/books?test-qs=1");
        givenRequest.Headers.ShouldContain(x => x.Key == "test-h" && string.Join(",",x.Value) == "1");
        rsp.Content.ShouldContain(x => x.Id == "1");
        await httpClient.Received(1).Send(TestConstants.ServiceNames.ApiBooks, Arg.Any<HttpRequestMessage>(),
                Arg.Any<CancellationToken>());
        
    }

    public record BookDto
    {
        public required string Id { get; init; }
        public required string Isbn { get; init; }
        public required string Title { get; init; }
        public double? Price { get; init; }
    }
}