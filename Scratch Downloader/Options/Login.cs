using ScratchDL.CMD.Options.Base;
using System;
using System.Threading.Tasks;

namespace ScratchDL.CMD.Options
{
    public sealed class Login : ProgramOption_Base
    {
        public override string Title => "Login";
        public override string Description => "Logs into an account. You will be able to download private details about the account";

        public override async Task<bool> Run(ScratchAPI accessor)
        {
            if (accessor.LoggedIn)
            {
                Console.WriteLine("Already Logged in");
                return false;
            }

            string username = Utilities.GetStringFromConsole("Enter username to login to");
            string password = Utilities.GetPasswordInput("Enter password to login to: ");

            try
            {
                await accessor.Login(username, password);

                Console.WriteLine("Login Successful!");
            }
            catch (NoInternetException)
            {
                Console.WriteLine("Failed to login. Make sure you are conencted to the internet");
            }
            catch (LoginException e)
            {
                Console.WriteLine($"Status Code = {e.StatusCode}");
                Console.WriteLine("Failed to login. Make sure you entered your credentials correctly");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to login");
                Console.WriteLine(e);
            }

            return false;
        }
    }
}
