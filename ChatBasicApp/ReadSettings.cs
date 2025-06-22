using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;


namespace ChatBasicApp
{
    public static class ReadSettings
    {

        public static AppSettings Read()
        {
            
            string jsonFile = Path.Combine(AppContext.BaseDirectory, "Settings.json");
            string json = File.ReadAllText(jsonFile);

            var appSettings = JsonSerializer.Deserialize<AppSettings>(json);
            return appSettings;
        }

    }

    public struct AppSettings
    {
        public string RunMode { get; set; }
    }
}

