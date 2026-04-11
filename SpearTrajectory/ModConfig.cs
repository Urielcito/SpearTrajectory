using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace SpearTrajectory.Configuration
{
    public static class ModConfig
    {
        public static T ReadConfig<T>(ICoreAPI api, string configName) where T : class, IModConfig
        {
            try
            {
                T config = api.LoadModConfig<T>(configName);
                if (config == null)
                {
                    config = System.Activator.CreateInstance<T>();
                    api.StoreModConfig(config, configName);
                }
                return config;
            }
            catch
            {
                T config = System.Activator.CreateInstance<T>();
                api.StoreModConfig(config, configName);
                return config;
            }
        }

        public static void WriteConfig<T>(ICoreAPI api, string configName, T config) where T : class, IModConfig
        {
            api.StoreModConfig(config, configName);
        }
    }
}