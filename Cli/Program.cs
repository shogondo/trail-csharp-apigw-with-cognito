using System;
using System.Configuration;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

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
            var userPoolId = ConfigurationManager.AppSettings["userPoolId"];
            var clientId = ConfigurationManager.AppSettings["clientId"];
            var identityPoolId = ConfigurationManager.AppSettings["identityPoolId"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];

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

                var credentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.APNortheast1);
                credentials.AddLogin("cognito-idp.ap-northeast-1.amazonaws.com/" + userPoolId, idToken);

                var immutableCredentials = await credentials.GetCredentialsAsync();
                Console.WriteLine(immutableCredentials.AccessKey);
                Console.WriteLine(immutableCredentials.SecretKey);
                Console.WriteLine(immutableCredentials.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred.");
                Console.WriteLine(ex);
            }
        }
    }
}
