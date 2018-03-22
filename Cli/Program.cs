using System;
using System.Collections.Generic;
using System.Configuration;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using AWSSignatureV4_S3_Sample.Signers;
using AWSSignatureV4_S3_Sample.Util;

namespace Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = new Program();
            program.Run();
            Console.ReadKey();
        }

        async void Run()
        {
            var cognitoIdp = new AmazonCognitoIdentityProviderClient();
            var region = ConfigurationManager.AppSettings["region"];
            var userPoolId = ConfigurationManager.AppSettings["userPoolId"];
            var clientId = ConfigurationManager.AppSettings["clientId"];
            var identityPoolId = ConfigurationManager.AppSettings["identityPoolId"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            var apiId = ConfigurationManager.AppSettings["apiId"];
            var stage = ConfigurationManager.AppSettings["stage"];
            var method = ConfigurationManager.AppSettings["method"];
            var path = ConfigurationManager.AppSettings["path"];
            var querystring = ConfigurationManager.AppSettings["querystring"];

            try
            {
                var request = new AdminInitiateAuthRequest
                {
                    AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                    ClientId = clientId,
                    UserPoolId = userPoolId,
                    AuthParameters = { { "USERNAME", username }, { "PASSWORD", password } }
                };

                var response = await cognitoIdp.AdminInitiateAuthAsync(request);
                var idToken = response.AuthenticationResult.IdToken;

                var credentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.GetBySystemName(region));
                credentials.AddLogin("cognito-idp.ap-northeast-1.amazonaws.com/" + userPoolId, idToken);

                var immutableCredentials = await credentials.GetCredentialsAsync();

                var endpoint = String.Format("https://{0}.execute-api.{1}.amazonaws.com/{2}/{3}", apiId, region, stage, path);
                if (!String.IsNullOrWhiteSpace(querystring))
                {
                    endpoint = endpoint + "?" + querystring;
                }
                var uri = new Uri(endpoint);

                var headers = new Dictionary<string, string>
                {
                    { AWS4SignerBase.X_Amz_Content_SHA256, AWS4SignerBase.EMPTY_BODY_SHA256 },
                    { "X-Amz-Security-Token", immutableCredentials.Token },
                    { "content-type", "application/json" }
                };

                var signer = new AWS4SignerForAuthorizationHeader
                {
                    EndpointUri = uri,
                    HttpMethod = method,
                    Service = "execute-api",
                    Region = region
                };

                var authorization = signer.ComputeSignature(
                    headers,
                    querystring,
                    AWS4SignerBase.EMPTY_BODY_SHA256,
                    immutableCredentials.AccessKey,
                    immutableCredentials.SecretKey);

                headers.Add("Authorization", authorization);

                HttpHelpers.InvokeHttpRequest(uri, method, headers, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred.");
                Console.WriteLine(ex);
            }
        }
    }
}
