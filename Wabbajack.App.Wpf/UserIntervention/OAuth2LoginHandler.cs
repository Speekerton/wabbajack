using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Wabbajack.DTOs.Interventions;
using Wabbajack.DTOs.Logins;
using Wabbajack.Messages;
using Wabbajack.Models;
using Wabbajack.Networking.Http.Interfaces;
using Wabbajack.Services.OSIntegrated;

namespace Wabbajack.UserIntervention;

public abstract class OAuth2LoginHandler<TIntervention, TLoginType> : WebUserInterventionBase<TIntervention>
    where TIntervention : IUserIntervention
    where TLoginType : OAuth2LoginState, new()
{
    private readonly HttpClient _httpClient;
    private readonly ITokenProvider<TLoginType> _tokenProvider;

    public OAuth2LoginHandler(ILogger logger, HttpClient httpClient,
        ITokenProvider<TLoginType> tokenProvider, WebBrowserVM browserVM, CefService service) : base(logger, browserVM, service)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public override async Task Begin()
    {
        Messages.NavigateTo.Send(Browser);
        var tlogin = new TLoginType();

        await Driver.WaitForInitialized();

        using var handler = Driver.WithSchemeHandler(uri => uri.Scheme == "wabbajack");

        UpdateStatus($"Please log in and allow Wabbajack to access your {tlogin.SiteName} account");

        var scopes = string.Join(" ", tlogin.Scopes);
        var state = Guid.NewGuid().ToString();

        await NavigateTo(new Uri(tlogin.AuthorizationEndpoint + $"?response_type=code&client_id={tlogin.ClientID}&state={state}&scope={scopes}"));

        var uri = await handler.Task.WaitAsync(Message.Token);

        var cookies = await Driver.GetCookies(tlogin.AuthorizationEndpoint.Host);

        var parsed = HttpUtility.ParseQueryString(uri.Query);
        if (parsed.Get("state") != state)
        {
            Logger.LogCritical("Bad OAuth state, this shouldn't happen");
            throw new Exception("Bad OAuth State");
        }

        if (parsed.Get("code") == null)
        {
            Logger.LogCritical("Bad code result from OAuth");
            throw new Exception("Bad code result from OAuth");
        }

        var authCode = parsed.Get("code");

        var formData = new KeyValuePair<string?, string?>[]
        {
            new("grant_type", "authorization_code"),
            new("code", authCode),
            new("client_id", tlogin.ClientID)
        };

        var msg = new HttpRequestMessage();
        msg.Method = HttpMethod.Post;
        msg.RequestUri = tlogin.TokenEndpoint;
        msg.Headers.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
        msg.Headers.Add("Cookie", string.Join(";", cookies.Select(c => $"{c.Name}={c.Value}")));
        msg.Content = new FormUrlEncodedContent(formData.ToList());

        using var response = await _httpClient.SendAsync(msg, Message.Token);
        var data = await response.Content.ReadFromJsonAsync<OAuthResultState>(cancellationToken: Message.Token);

        await _tokenProvider.SetToken(new TLoginType
        {
            Cookies = cookies,
            ResultState = data!
        });
        
        Messages.NavigateTo.Send(PrevPane);
    }
}