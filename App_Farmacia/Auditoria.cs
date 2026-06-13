using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using System.Windows;

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

            var ping = db.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            System.Diagnostics.Debug.WriteLine("Conexión confirmada: " + ping.ToString());
        }

        public async Task RegistrarEdicionAsync(string entidad, int entidadId, string campo, object valorNuevo)
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
                        ["valor_nuevo"] = BsonValue.Create(valorNuevo)
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log edición falló: {ex.Message}");
            }
        }

        public async Task RegistrarRecetaAsync( int facturaId, BsonArray productos, string nDoc, string direccion)
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
                    ["nombreDoctor"] = nDoc,
                    ["direccionReceta"]=direccion,
                    ["detalle"] = new BsonDocument
                    {
                        ["factura_id"] = facturaId,
                        ["productos"] = productos
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error MongoDB: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"Log receta falló: {ex.Message}");
            }
        }
    }
}