using Gateway.Infrastructure.Interfaces;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using Gateway.Infrastructure.Configurations;

namespace Gateway.Infrastructure.Persistences.Repositories.MongoDB
{
    /// <summary>
    /// Repositorio encargado de la persistencia de los registros de auditoría del API Gateway en la base de datos MongoDB.
    /// Implementa <see cref="IAuditoriaRepository"/>.
    /// </summary>
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly IMongoCollection<BsonDocument> AuditoriaColexion;
        private readonly ILog Log;
        public AuditoriaRepository(AuditoriaDbConfig mongoConfig, ILog log)
        {
            AuditoriaColexion = mongoConfig.db.GetCollection<BsonDocument>("auditoriaGateway");
            Log = log;
        }

        #region InsertarAuditoriaGateway(string idUsuario, string level, string tipo, string mensaje)
        /// <summary>
        /// Inserta un nuevo documento de auditoría en la colección 'auditoriaPagos'.
        /// </summary>
        /// <param name="idUsuario">ID del usuario relacionado con el evento (puede ser nulo/vacío si el evento no está autenticado).</param>
        /// <param name="level">Nivel de criticidad del evento (p. ej., INFO, ERROR, WARN).</param>
        /// <param name="tipo">Tipo de evento o acción que se está auditando (p. ej., "LoginSuccess", "ApiCallFailed").</param>
        /// <param name="mensaje">Mensaje detallado o cuerpo del registro de auditoría.</param>
        /// <returns>Tarea asíncrona completada (no retorna valor).</returns>
        /// <exception cref="ArgumentException">Lanzada si ocurre un error al intentar la inserción en MongoDB.</exception>
        public async Task InsertarAuditoriaGateway(string idUsuario, string level, string tipo, string mensaje)
        {
            var docId = Guid.NewGuid().ToString();
            Log.Debug($"[GATEWAY AUDIT] Iniciando inserción de auditoría de gateway. Usuario: {idUsuario}, Level: {level}, Tipo: {tipo}. ID Doc: {docId}");

            try
            {
                var documento = new BsonDocument
                {
                    { "_id",  docId},
                    { "idUsuario", idUsuario},
                    { "level", level},
                    { "tipo", tipo},
                    { "mensaje", mensaje},
                    { "timestamp", DateTime.Now}
                };
                await AuditoriaColexion.InsertOneAsync(documento);
                Log.Info($"[GATEWAY AUDIT] Documento de auditoría de gateway insertado exitosamente. ID de Documento: {docId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[GATEWAY AUDIT] Error crítico al intentar insertar la auditoría de gateway. Usuario: {idUsuario}, " +
                          $"Tipo: {tipo}, Mensaje: {mensaje}.", ex);
                throw new ArgumentException("Error al insertar la auditoria", ex);
            }
        }
        #endregion
    }
}
