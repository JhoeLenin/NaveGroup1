using System;
using System.Drawing;
using System.IO;

namespace GrupalNaves
{
    // Clase que representa el fondo del juego, y se encarga de cargarlo, escalarlo y gestionarlooooo
    public class FondoJuego : IDisposable
    {
        // Imagen redimensionada del fondo, ajustada al tamaño de la ventana
        private Bitmap fondoBuffer;
        // Imagen original del fondo, sin redimensionar
        private Bitmap fondoOriginal;
        // Tamaño actual de la ventana del juego
        private Size tamañoVentana;

        // Constructor: recibe la ruta de la imagen y el tamaño inicial de la ventana del juego
        public FondoJuego(string rutaImagen, Size tamañoInicial)
        {
            // Valida que se haya proporcionado una ruta de imagen válida
            if (string.IsNullOrEmpty(rutaImagen))
                throw new ArgumentNullException(nameof(rutaImagen));

            // Intenta cargar la imagen y preparar el fondo escalado
            try
            {
                // Carga la imagen original del fondo desde el archivo
                fondoOriginal = new Bitmap(rutaImagen);
                // Guarda el tamaño de la ventana para calcular el escalado
                tamañoVentana = tamañoInicial;
                // Escala la imagen original al tamaño de la ventana del juego
                RedimensionarFondo();
            }
            catch (Exception ex)
            {
                // Si ocurre un error (archivo no encontrado, formato inválido, etc.), lanza una excepción con un mensaje más descriptivo
                throw new Exception($"Error al cargar la imagen de fondo: {ex.Message}", ex);
            }
        }

        // Método para actualizar el tamaño de la ventana del juego
        // y redimensionar el fondo de acuerdo al nuevo tamaño
        public void CambiarTamaño(Size nuevoTamaño)
        {
            // Verifica que el nuevo tamaño sea válido (ancho y alto mayores a 0)
            if (nuevoTamaño.Width <= 0 || nuevoTamaño.Height <= 0)
                return;
            // Actualiza el tamaño interno del fondo
            tamañoVentana = nuevoTamaño;
            // Vuelve a escalar la imagen de fondo al nuevo tamaño
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

        // Método para dibujar el fondo en la superficie gráfica del juego
        public void Dibujar(Graphics g)
        {
            // Si el fondo redimensionado existe, se dibuja en la posición (0,0)
            if (fondoBuffer != null)
            {
                g.DrawImageUnscaled(fondoBuffer, 0, 0);
            }
            else
            {
                // Si no se ha cargado el fondo, se limpia la pantalla con color negro
                g.Clear(Color.Black);
            }
        }

        // Método para liberar los recursos de imagen cuando ya no se necesiten
        public void Dispose()
        {
            // Libera la imagen redimensionada del fondo si existe
            fondoBuffer?.Dispose();
            // Libera la imagen original del fondo si existe
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