using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static IdentityModel.OidcClient.OidcClientOptions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UwpSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly OidcClient _oidcClient;
        private readonly OidcClientOptions _oidcClientOptions;
        private readonly Action<string> _writeLine;
        private Action _clearText;
        private string _accessToken;
        private string _idToken;
        public MainPage()
        {
            this.InitializeComponent();
            _writeLine = (text) => txtOutput.Text += text + "\n";
            _clearText = () => txtOutput.Text = "";
            _oidcClientOptions = new OidcClientOptions
            {
                Authority = "{Authority}",
                ClientId = "{clientId}",
                Scope = "openid profile",
                RedirectUri = "{redirectUri}",
                PostLogoutRedirectUri = "{postLogoutRedirectUri}",
                Flow = AuthenticationFlow.AuthorizationCode,
                ResponseMode = AuthorizeResponseMode.Redirect,
                Browser = new DefaultSystemBrowser(),//DefaultWebAuthenticationBrowser(),
            };

            _oidcClient = new OidcClient(_oidcClientOptions);
        }

        private async void BtnSignIn_ClickAsync(object sender, RoutedEventArgs e)
        {
            _clearText();
            _writeLine("Starting login...");

            var loginResult = await _oidcClient.LoginAsync(new LoginRequest());

            if (loginResult.IsError)
            {
                _writeLine($"An error occurred during login: {loginResult.Error}");
                return;
            }

            _accessToken = loginResult.AccessToken;
            _idToken = loginResult.IdentityToken;

            _writeLine($"id_token: {loginResult.IdentityToken}");
            _writeLine($"access_token: {loginResult.AccessToken}");
            _writeLine($"refresh_token: {loginResult.RefreshToken}");

            _writeLine($"name: {loginResult.User.FindFirst(c => c.Type == "name")?.Value}");
            _writeLine($"email: {loginResult.User.FindFirst(c => c.Type == "email")?.Value}");

            foreach (var claim in loginResult.User.Claims)
            {
                _writeLine($"{claim.Type} = {claim.Value}");
            }
        }

        private async void BtnUserInfo_ClickAsync(object sender, RoutedEventArgs e)
        {
            _clearText();

            if (string.IsNullOrEmpty(_accessToken))
            {
                _writeLine("You need to be logged in to get user info");
                return;
            }

            _writeLine("Getting user info...");
            var userInfoResult = await _oidcClient.GetUserInfoAsync(_accessToken);

            if (userInfoResult.IsError)
            {
                _writeLine($"An error occurred getting user info: {userInfoResult.Error}");
                return;
            }

            foreach (var claim in userInfoResult.Claims)
            {
                _writeLine($"{claim.Type} = {claim.Value}");
            }
        }

        private async void BtnSignOut_ClickAsync(object sender, RoutedEventArgs e)
        {
            _clearText();
            _writeLine("Starting logout...");

            var logoutParameters = new Dictionary<string, string>
            {
                { "id_token_hint", _idToken},
                { "post_logout_redirect_uri", _oidcClient.Options.RedirectUri }
            };

            var endSessionUrl = new RequestUrl($"{_oidcClientOptions.Authority}oauth2/v1/logout").Create(logoutParameters);

            var logoutRequest = new LogoutRequest();
            var browserOptions = new BrowserOptions(endSessionUrl, _oidcClient.Options.PostLogoutRedirectUri ?? string.Empty)
            {
                Timeout = TimeSpan.FromSeconds(logoutRequest.BrowserTimeout),
                DisplayMode = logoutRequest.BrowserDisplayMode
            };

            var browserResult = await _oidcClient.Options.Browser.InvokeAsync(browserOptions);

            _accessToken = null;
            _idToken = null;
            _writeLine(browserResult.ToString());
        }
    }
}
