using System;
using System.Drawing;
using System.IO;

namespace GrupalNaves
{
    public class FondoJuego : IDisposable
    {
        private Bitmap fondoBuffer;
        private Bitmap fondoOriginal;
        private Size tamañoVentana;

        public FondoJuego(string rutaImagen, Size tamañoInicial)
        {
            if (string.IsNullOrEmpty(rutaImagen))
                throw new ArgumentNullException(nameof(rutaImagen));

            // Cargar la imagen original
            try
            {
                fondoOriginal = new Bitmap(rutaImagen);
                tamañoVentana = tamañoInicial;
                RedimensionarFondo();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar la imagen de fondo: {ex.Message}", ex);
            }
        }

        public void CambiarTamaño(Size nuevoTamaño)
        {
            if (nuevoTamaño.Width <= 0 || nuevoTamaño.Height <= 0)
                return;

            tamañoVentana = nuevoTamaño;
            RedimensionarFondo();
        }

        private void RedimensionarFondo()
        {
            // Liberar el buffer anterior si existe
            fondoBuffer?.Dispose();

            // Crear nuevo buffer con el tamaño actual
            fondoBuffer = new Bitmap(tamañoVentana.Width, tamañoVentana.Height);

            using (Graphics g = Graphics.FromImage(fondoBuffer))
            {
                // Dibujar el fondo escalado
                g.DrawImage(fondoOriginal, 0, 0, tamañoVentana.Width, tamañoVentana.Height);
            }
        }

        public void Dibujar(Graphics g)
        {
            if (fondoBuffer != null)
            {
                g.DrawImageUnscaled(fondoBuffer, 0, 0);
            }
            else
            {
                g.Clear(Color.Black);
            }
        }

        public void Dispose()
        {
            fondoBuffer?.Dispose();
            fondoOriginal?.Dispose();
        }

        // Método estático para crear el fondo con la ruta relativa al proyecto
        public static FondoJuego CrearDesdeAssets(string nombreArchivo, Size tamañoInicial)
        {
            string path = Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "Assets", "Fondo", nombreArchivo);

            return new FondoJuego(path, tamañoInicial);
        }
    }
}