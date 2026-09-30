using Microsoft.Data.SqlClient;
using System.Data;

namespace Arquitectura_Seguridad_Backend
{
    public class Datos
    {
        /* Modelo de Datos: Crea una clase/entidad que represente un procedimiento almacenado. Esta debe recibir como parámetros: el hash del código,
        el correo del usuario, y un DateTime exacto de expiración (por ejemplo, la hora actual + 5 minutos). */
        public string CodigoHash { get; set; }
        public string Correo { get; set; }
        public DateTime Expiracion { get; set; }
        static string connectionString = "Data Source=localhost,1433;Initial Catalog=recuperacionDB;User ID=SA;Password=Mypassword*;Pooling=False;Trust Server Certificate=True;Authentication=SqlPassword";

        public Datos(string codigoHash, string correo, int minutosValidez = 5)
        {
            CodigoHash = codigoHash;
            Correo = correo;
            Expiracion = DateTime.Now.AddMinutes(minutosValidez);
        }
        static SqlConnection ConexionSQL()
        {
            SqlConnection sqlconn = new SqlConnection(connectionString);
            sqlconn.Open();
            return sqlconn;
        }
        public static bool EjecutarNonQuery(string sp, SqlParameter[] parametros = null)
        {
            try
            {
                SqlConnection sqlconn = ConexionSQL();
                SqlCommand command = new SqlCommand(sp, sqlconn);
                command.CommandType = CommandType.StoredProcedure;

                if (parametros != null)
                {
                    command.Parameters.AddRange(parametros);
                }
                command.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error en SQL Server: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general: {ex.Message}");
                return false;
            }
            
        }
        static public void GuardarCodigoRecuperacion(string correo, string codigoHash, DateTime expiracion)
        {
            string spNombre = "sp_GuardarCodigoRecuperacion";

            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = correo },
                new SqlParameter("@CodigoHash", SqlDbType.Char, 64) { Value = codigoHash },
                new SqlParameter("@FechaExpiracion", SqlDbType.DateTime2) { Value = expiracion }
            };
            bool exito = EjecutarNonQuery(spNombre, parametros);
            if (exito)
            {
                Console.WriteLine($"Código de recuperación guardado con éxito.");
            }
        }
        static public (bool esValido, string mensaje) ValidarCodigoRecuperacion(string correo, string codigoHash)
        {
            string spNombre = "sp_ValidarCodigoRecuperacion";

            SqlParameter pEsValido = new SqlParameter("@EsValido", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Output };

            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = correo },
                new SqlParameter("@CodigoHash", SqlDbType.Char, 64) { Value = codigoHash },
                pEsValido,
                pMensaje
            };

            bool exito = EjecutarNonQuery(spNombre, parametros);

            if (exito)
            {
                bool esValido = pEsValido.Value != DBNull.Value && (bool)pEsValido.Value;
                string mensaje = pMensaje.Value?.ToString() ?? "Sin respuesta de BD";

                return (esValido, mensaje);
            }
            return (false, "Error al comunicar con la base de datos.");
        }
        static public void ListarUsuarios()
        {
            using SqlConnection sqlconn = ConexionSQL(); 
            string sql = "SELECT * FROM Usuarios";

            using SqlCommand command = new SqlCommand(sql, sqlconn);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine(reader["Nombre"]);
            }
        }
    }
}
