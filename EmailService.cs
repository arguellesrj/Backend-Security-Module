using System.Net;
using System.Net.Mail;

namespace Arquitectura_Seguridad_Backend
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private static readonly int[] ports = new int[] { 25, 465, 587, 2525 };

        public EmailService(EmailSettings settings)
        {
            _settings = settings;
        }

        public bool EnviarCodigoRecuperacion(string destinatario, string codigo)
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_settings.SenderEmail, _settings.SenderName);
            message.To.Add(new MailAddress(destinatario));
            message.Subject = "Código de Recuperación de Contraseña";

            message.Body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #2b6cb0;'>Recuperación de Contraseña</h2>
                    <p>Has solicitado restablecer tu contraseña. Utiliza el siguiente código de verificación:</p>
                    <div style='background-color: #f7fafc; padding: 15px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #2d3748;'>{codigo}</span>
                    </div>
                    <p>Este código expira en <b>5 minutos</b>.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 20px;' />
                    <p style='font-size: 12px; color: #718096;'>Si no solicitaste este cambio, puedes ignorar este mensaje de manera segura.</p>
                </div>";

            message.IsBodyHtml = true;

            string ultimoError = "";

            foreach (int puerto in ports)
            {
                try
                {
                    using var smtpClient = new SmtpClient(_settings.SmtpServer, puerto)
                    {
                        Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                        EnableSsl = puerto != 25 // el puerto 25 normalmente va sin SSL
                    };

                    smtpClient.Send(message);

                    Console.WriteLine($"[Correo enviado exitosamente por el puerto {puerto}]");
                    return true;
                }
                catch (Exception ex)
                {
                    // AQUÍ está la lógica de rotación: si falla, guardamos el error
                    // y el foreach pasa automáticamente al siguiente puerto
                    ultimoError = ex.Message;
                    Console.WriteLine($"[Falló el puerto {puerto}]: {ex.Message}");
                    continue;
                }
            }

            Console.WriteLine($"[Error al enviar correo tras probar todos los puertos]: {ultimoError}");
            return false;
        }
    }
}