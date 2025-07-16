using System.Drawing;
using System.Windows.Forms; // Necesario para la referencia a Form y ClientSize

namespace GrupalNaves
{
    public class HUD
    {
        private int vidaActual;
        private int puntajeActual;
        private Form formularioPrincipal; // Referencia al formulario para obtener dimensiones

        // Caches para Font y Brush (creados una sola vez)
        private Font hudFont;
        private SolidBrush hudBrush;

        // Constructor
        public HUD(Form form, int vidaInicial)
        {
            this.formularioPrincipal = form;
            this.vidaActual = vidaInicial;
            this.puntajeActual = 0;

            // Inicializar Font y Brush una sola vez
            hudFont = new Font("Arial", 24, FontStyle.Bold);
            hudBrush = new SolidBrush(Color.White); // Color para el texto del HUD
        }

        // Método para actualizar la vida (solo cambia el valor interno)
        public void ActualizarVida(int nuevaVida)
        {
            if (this.vidaActual != nuevaVida) // Solo actualiza si hay un cambio real
            {
                this.vidaActual = nuevaVida;
            }
        }

        // Método para agregar puntaje (solo cambia el valor interno)
        public void AgregarPuntaje(int puntos)
        {
            this.puntajeActual += puntos;
        }

        // Método para dibujar el HUD directamente en el contexto gráfico del formulario
        public void Dibujar(Graphics g)
        {
            // Dibuja la vida en la esquina superior izquierda
            string textoVida = $"Vida: {vidaActual}";
            g.DrawString(textoVida, hudFont, hudBrush, 10, 10); // 10px de margen

            // Dibuja el puntaje en la esquina superior derecha
            string textoPuntaje = $"Puntaje: {puntajeActual}";

            // Medir el tamaño del texto para posicionarlo correctamente
            SizeF textSize = g.MeasureString(textoPuntaje, hudFont);
            float xPuntaje = formularioPrincipal.ClientSize.Width - textSize.Width - 10; // Margen derecho
            g.DrawString(textoPuntaje, hudFont, hudBrush, xPuntaje, 10); // 10px de margen superior
        }

        // Método para liberar recursos
        public void Dispose()
        {
            hudFont?.Dispose();
            hudBrush?.Dispose();
        }
    }
}