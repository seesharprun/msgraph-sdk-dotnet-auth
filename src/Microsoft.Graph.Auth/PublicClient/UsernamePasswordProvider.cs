﻿// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

namespace Microsoft.Graph.Auth
{
    using Microsoft.Identity.Client;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security;
    using System.Threading.Tasks;

    /// <summary>
    /// An <see cref="IAuthenticationProvider"/> implementation using MSAL.Net to acquire token by username and password.
    /// This only works with work and school accounts.
    /// </summary>
    public class UsernamePasswordProvider : MsalAuthenticationBase<IPublicClientApplication>, IAuthenticationProvider
    {
        /// <summary>
        /// Constructs a new <see cref="UsernamePasswordProvider"/>. This provider is NOT RECOMMENDED because it exposes the users password.
        /// We recommend you use <see cref="IntegratedWindowsAuthenticationProvider"/> instead.
        /// </summary>
        /// <param name="publicClientApplication">A <see cref="IPublicClientApplication"/> to pass to <see cref="DeviceCodeProvider"/> for authentication.</param>
        /// <param name="scopes">Scopes required to access Microsoft Graph. This defaults to https://graph.microsoft.com/.default when none is set.</param>
        public UsernamePasswordProvider(
            IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes = null)
            : base(scopes)
        {
            ClientApplication = publicClientApplication ?? throw new AuthenticationException(
                    new Error
                    {
                        Code = ErrorConstants.Codes.InvalidRequest,
                        Message = string.Format(ErrorConstants.Message.NullValue, nameof(publicClientApplication))
                    });
        }

        /// <summary>
        /// Creates a new <see cref="IPublicClientApplication"/>
        /// </summary>
        /// <param name="clientId">Client ID (also known as <i>Application ID</i>) of the application as registered in the application registration portal (https://aka.ms/msal-net-register-app).</param>
        /// <param name="tenant">Tenant to sign-in users. This defaults to <see cref="AadAuthorityAudience.AzureAdMultipleOrgs" /> if none is specified.</param>
        /// <param name="cloud">A <see cref="AzureCloudInstance"/> which identifies the cloud endpoint to use as the authority. This defaults to the public cloud <see cref="AzureCloudInstance.AzurePublic"/> (https://login.microsoftonline.com).</param>
        /// <returns>A <see cref="IPublicClientApplication"/></returns>
        /// <exception cref="AuthenticationException"/>
        public static IPublicClientApplication CreateClientApplication(string clientId,
            string tenant = null,
            AzureCloudInstance cloud = AzureCloudInstance.AzurePublic)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new AuthenticationException(
                    new Error
                    {
                        Code = ErrorConstants.Codes.InvalidRequest,
                        Message = string.Format(ErrorConstants.Message.NullValue, nameof(clientId))
                    });

            var builder = PublicClientApplicationBuilder
                .Create(clientId);

            if (tenant != null)
                builder = builder.WithAuthority(cloud, tenant);
            else
                builder = builder.WithAuthority(cloud, AadAuthorityAudience.AzureAdMultipleOrgs);

            return builder.Build();
        }

        /// <summary>
        /// Adds an authentication header to the incoming request by checking the application's <see cref="TokenCache"/>
        /// for an unexpired access token. If a token is not found or expired, it gets a new one.
        /// </summary>
        /// <param name="httpRequestMessage">A <see cref="HttpRequestMessage"/> to authenticate.</param>
        public async Task AuthenticateRequestAsync(HttpRequestMessage httpRequestMessage)
        {
            GraphRequestContext requestContext = httpRequestMessage.GetRequestContext();
            MsalAuthenticationProviderOption msalAuthProviderOption = httpRequestMessage.GetMsalAuthProviderOption();
            AuthenticationResult authenticationResult = await GetAccessTokenSilentAsync(msalAuthProviderOption);

            if (authenticationResult == null)
            {
                authenticationResult = await GetNewAccessTokenAsync(msalAuthProviderOption);
            }

            if (!string.IsNullOrEmpty(authenticationResult.AccessToken))
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(CoreConstants.Headers.Bearer, authenticationResult.AccessToken);
        }

        private async Task<AuthenticationResult> GetNewAccessTokenAsync(MsalAuthenticationProviderOption msalAuthProviderOption)
        {
            AuthenticationResult authenticationResult = null;
            int retryCount = 0;
            do
            {
                try
                {
                    authenticationResult = await ClientApplication.AcquireTokenByUsernamePassword(msalAuthProviderOption.Scopes ?? Scopes, msalAuthProviderOption.UserAccount?.Email, ToSecureString(msalAuthProviderOption.Password))
                        .ExecuteAsync();
                    break;
                }
                catch (MsalServiceException serviceException)
                {
                    if (serviceException.ErrorCode == ErrorConstants.Codes.TemporarilyUnavailable)
                    {
                        TimeSpan delay = this.GetRetryAfter(serviceException);
                        retryCount++;
                        // pause execution
                        await Task.Delay(delay);
                    }
                    else
                    {
                        throw new AuthenticationException(
                            new Error
                            {
                                Code = ErrorConstants.Codes.GeneralException,
                                Message = ErrorConstants.Message.UnexpectedMsalException
                            },
                            serviceException);
                    }
                }
                catch (Exception exception)
                {
                    throw new AuthenticationException(
                            new Error
                            {
                                Code = ErrorConstants.Codes.GeneralException,
                                Message = ErrorConstants.Message.UnexpectedException
                            },
                            exception);
                }

            } while (retryCount < MaxRetry);

            return authenticationResult;
        }

        private SecureString ToSecureString(string password)
        {
            if (string.IsNullOrEmpty(password))
                return null;

            SecureString secureString = new SecureString();
            foreach (char item in password)
            {
                secureString.AppendChar(item);
            }
            return secureString;
        }
    }
}
