using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmtpServer.Helpers
{
    public static class SettingsHelper
    {
        public static int GetIntOrDefault(string key, int defaultValue)
        {
            int value;
            if (!int.TryParse(ConfigurationManager.AppSettings[key], out value))
            {
                value = defaultValue;
            }

            return value;
        }
    }
}
