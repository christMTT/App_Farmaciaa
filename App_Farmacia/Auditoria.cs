using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace App_Farmacia.Datos
{
    public class Auditoria
    {
        private static Auditoria _instancia;
        public static Auditoria Instancia =>
            _instancia ??= new Auditoria();

        private readonly IMongoCollection<BsonDocument> _logs;

        private Auditoria()
        {
            var client = new MongoClient(conexion.CadenaMongo);
            var db = client.GetDatabase("Farmacia_Audit");
            _logs = db.GetCollection<BsonDocument>("Registros");
        }

        public async Task RegistrarEdicionAsync(string entidad, int entidadId, string campo, object valorAnterior, object valorNuevo)
        {
            try
            {
                await _logs.InsertOneAsync(new BsonDocument
                {
                    ["tipo_evento"] = "edicion",
                    ["timestamp"] = DateTime.UtcNow,
                    ["usuario_id"] = Sesion.IdUsuario,
                    ["usuario_nombre"] = Sesion.NombreUsuario ?? "desconocido",
                    ["sucursal_id"] = Sesion.IdSucursal,
                    ["detalle"] = new BsonDocument
                    {
                        ["entidad"] = entidad,
                        ["entidad_id"] = entidadId,
                        ["campo"] = campo,
                        ["valor_anterior"] = BsonValue.Create(valorAnterior),
                        ["valor_nuevo"] = BsonValue.Create(valorNuevo)
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log edición falló: {ex.Message}");
            }
        }

        public async Task RegistrarRecetaAsync( int facturaId, BsonArray productos)
        {
            try
            {
                await _logs.InsertOneAsync(new BsonDocument
                {
                    ["tipo_evento"] = "venta_medicamento_receta",
                    ["timestamp"] = DateTime.UtcNow,
                    ["usuario_id"] = Sesion.IdUsuario,
                    ["usuario_nombre"] = Sesion.NombreUsuario ?? "desconocido",
                    ["sucursal_id"] = Sesion.IdSucursal,
                    ["detalle"] = new BsonDocument
                    {
                        ["factura_id"] = facturaId,
                        ["productos"] = productos
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log receta falló: {ex.Message}");
            }
        }
    }
}