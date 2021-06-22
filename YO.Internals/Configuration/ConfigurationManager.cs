using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using DynamicData.Binding;
using Newtonsoft.Json;
using ReactiveUI;

namespace YO.Internals.Configuration
{
	public class ConfigurationManager : IConfigurationManager
	{
		private const string ConfigFileName = "config.json";
		
		private readonly Lazy<Configuration> _configuration;
		private readonly string _configFilePath;

		private readonly JsonSerializerSettings _serializerSettings = new()
		{
			Formatting = Formatting.Indented
		};

		public IConfiguration Configuration => _configuration.Value;
		public ICommand Save { get; }

		public ConfigurationManager()
		{
			var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
			_configFilePath = Path.Combine(appPath, ConfigFileName);
			_configuration = new Lazy<Configuration>(Load);

			Save = ReactiveCommand.Create<Configuration>(SaveImpl);
		}

		private void SaveImpl(Configuration configuration)
		{
			var serialized = JsonConvert.SerializeObject(configuration, _serializerSettings);
			File.WriteAllText(_configFilePath, serialized);
		}

		private Configuration Load()
		{
			var configuration = File.Exists(_configFilePath) 
				? LoadFromFile() 
				: new Configuration();

			configuration.WhenAnyPropertyChanged()
						 .InvokeCommand(Save);
			
			return configuration;
		}

		private Configuration LoadFromFile()
		{
			var fileContent = File.ReadAllText(_configFilePath);
			return JsonConvert.DeserializeObject<Configuration>(fileContent)!;
			
		}
	}
}