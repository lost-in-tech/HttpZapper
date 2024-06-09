# HttpZapper
Another Http client that handle duplicate request in same scope and provide fluent interface

- Handle duplicate request and make sure only one request happen per scope
- Provide a fluent interface to send http request
- Provide a service settings for each api we do request and can easily hookup Polly to handle retry/timeout and circuit breaker
