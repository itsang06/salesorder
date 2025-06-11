using System.Collections.Generic;

namespace Sys.Common
{
    internal static class SystemConfig
    {
        public const string EncryptPass = "nProgrammer@$^194";
        public const string SwaggerName = "OD Sale Order";

        public static List<string> CORS = new List<string>()
        {
            "*"
        };

        public const string CorsName = "nProx Origin";

        #region JWT

        // public const string JWT_KEY = "sw8lnp04kZ0XvLF9NeBg";
        public const string JWT_KEY = "THIS-IS-USED TO SIGN AND VERIFY___JWT TOKENS, REPLACE IT WITH YOUR OWN SECRET, IT CAN BE ANY STRING";
        public const int JWT_ACC_EXPIRE = 230;
        public const int JWT_REF_EXPIRE = 43200;

        #endregion JWT
    }
}