namespace HttpZapper;

public interface IHttpMsgRequestFilter
{
    HttpMsgRequest Filter(HttpMsgRequest request);
}