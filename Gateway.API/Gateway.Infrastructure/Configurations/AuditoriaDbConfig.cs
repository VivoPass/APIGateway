using MongoDB.Driver;

namespace Gateway.Infrastructure.Configurations
{
    public class AuditoriaDbConfig
    {
        public MongoClient client;
        public IMongoDatabase db;

        public AuditoriaDbConfig()
        {
            try
            {
                string connectionUri = Environment.GetEnvironmentVariable("MONGODB_CNN");

                if (string.IsNullOrWhiteSpace(connectionUri))
                {
                    throw new ArgumentException("No se encontró la conexión de la Base de Datos");
                }

                var settings = MongoClientSettings.FromConnectionString(connectionUri);
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);

                client = new MongoClient(settings);

                string databaseName = Environment.GetEnvironmentVariable("MONGODB_NAME_AUDITORIAS");
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw new ArgumentException("No se encontró el nombre de la Base de Datos");
                }

                db = client.GetDatabase(databaseName);
            }
            catch (MongoException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
