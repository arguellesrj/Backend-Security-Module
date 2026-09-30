/*
El Reto:
Crea el backend seguro para el sistema de recuperación, integrando hashing y estructurando las consultas como si fueras a conectarlo a SQL Server y a un servidor SMTP.

Hashing: Cuando el sistema genere el código de 6 dígitos, no lo guardes en texto plano. Crea un método que pase ese código por un algoritmo de hash criptográfico 
(como SHA256).

Modelo de Datos: Crea una clase/entidad que represente un procedimiento almacenado. Esta debe recibir como parámetros: el hash del código, el correo del usuario, y 
un DateTime exacto de expiración (por ejemplo, la hora actual + 5 minutos).

Lógica de Verificación: Crea un método que reciba el intento del usuario. El método debe: hashear el intento de código, buscar en tu lista/base de datos simulada 
si ese hash coincide, y finalmente validar mediante código si el DateTime actual es menor a la fecha de expiración guardada.

(Opcional): Deja armada la estructura de clases y métodos donde normalmente inyectarías MailKit para el envío por SMTP.
*/
using System.Security.Cryptography;
using System.Text;
using Arquitectura_Seguridad_Backend;

Random rnd = new Random();
HashSet<string> codigo = new HashSet<string>();
while (codigo.Count < 6) { codigo.Add(rnd.Next(0, 10).ToString()); }

string codigoHash = HashCode(string.Join("", codigo.ToArray()));

static string HashCode(string code)
{
    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
    return Convert.ToHexString(hash);
}

Console.WriteLine("=== SISTEMA DE RECUPERACIÓN DE CONTRASEÑA ===");
bool salir = false;
string correo;
do
{
    Console.Write("Ingrese su correo electrónico: ");
    correo = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrEmpty(correo))
    {
        Console.WriteLine("Por favor, ingrese un correo.");
    }
    else
    {
        salir = true;
    }
} while (!salir);
DateTime expiracion = DateTime.Now.AddMinutes(5);
Datos.GuardarCodigoRecuperacion(correo, codigoHash, expiracion);

// Console.WriteLine("\nEnviando código de verificación a Mailtrap...");

// var settings = new EmailSettings();
// var emailService = new EmailService(settings);

// bool enviado = emailService.EnviarCodigoRecuperacion(correo, string.Join("", codigo.ToArray()));

// if (enviado)
// {
//     Console.WriteLine("¡Correo enviado! Revisa tu bandeja de Mailtrap en el navegador.");
// }
// else
// {
//     Console.WriteLine("No se pudo enviar el correo. Revisa la configuración.");
// }
Console.WriteLine($"\n[SIMULACIÓN] Tu código generado es: {string.Join("", codigo.ToArray())}");

Console.WriteLine("\n------------------------------------------------");
while (true)
{
    Console.Write("\nIngrese el código recibido en Mailtrap (o 'salir'): ");
    string input = Console.ReadLine()?.Trim() ?? "";

    if (input.Equals("salir", StringComparison.OrdinalIgnoreCase) || input.Equals("s", StringComparison.OrdinalIgnoreCase)) break;

    // Hasheamos el intento del usuario
    string inputHash = HashCode(input);

    // Validamos contra SQL Server a través del Stored Procedure
    var (esValido, mensaje) = Datos.ValidarCodigoRecuperacion(correo, inputHash);

    Console.WriteLine($"[Servidor SQL]: {mensaje}");

    if (esValido)
    {
        Console.WriteLine("\n¡Acceso Concedido! Puedes cambiar tu contraseña ahora.");
        break;
    }
    else
    {
        Console.WriteLine("\nCódigo incorrecto o expirado. Intenta nuevamente.");
    }
}