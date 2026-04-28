using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lazy_App_Codex_Core
{
    public class ScriptConfigRepository
    {
        private readonly string _configPath;

        public ScriptConfigRepository(string configPath)
        {
            _configPath = configPath;
        }

        public Dictionary<string, ScriptModel> Load()
        {
            if (!File.Exists(_configPath))
            {
                return new Dictionary<string, ScriptModel>();
            }

            string json = File.ReadAllText(_configPath);
            return JsonConvert.DeserializeObject<Dictionary<string, ScriptModel>>(json)
                   ?? new Dictionary<string, ScriptModel>();
        }
    }
}
