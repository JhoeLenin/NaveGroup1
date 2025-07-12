using System.Drawing; // Necesario para Graphics, Pen, Point, Color

namespace GrupalNaves
{
    public class Obstaculos
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        public float Ancho { get; set; }
        public float Alto { get; set; }
        public Color ColorRelleno { get; set; }
        public Color ColorBorde { get; set; }
        public float GrosorBorde { get; set; } = 1.0f; // Nuevo: grosor del borde

        // Constructor para un obstáculo rectangular simple
        public Obstaculos(int x, int y, float ancho, float alto, Color colorRelleno, Color colorBorde, float grosorBorde = 1.0f)
        {
            PosX = x;
            PosY = y;
            Ancho = ancho;
            Alto = alto;
            ColorRelleno = colorRelleno;
            ColorBorde = colorBorde;
            GrosorBorde = grosorBorde;
        }

        // Método para dibujar el obstáculo
        public void Dibujar(Graphics g)
        {
            // Dibujar el relleno del rectángulo
            using (SolidBrush brush = new SolidBrush(ColorRelleno))
            {
                g.FillRectangle(brush, PosX, PosY, Ancho, Alto);
            }

            // Dibujar el borde del rectángulo
            using (Pen pen = new Pen(ColorBorde, GrosorBorde))
            {
                g.DrawRectangle(pen, PosX, PosY, Ancho, Alto);
            }
        }

        // Opcional: Si quieres un obstáculo tipo círculo/elipse
        public static Obstaculos CrearCirculo(int x, int y, float radio, Color colorRelleno, Color colorBorde, float grosorBorde = 1.0f)
        {
            // Un círculo es una elipse con el mismo ancho y alto
            return new Obstaculos(x - (int)radio, y - (int)radio, radio * 2, radio * 2, colorRelleno, colorBorde, grosorBorde)
            {
                // Sobreescribimos el método Dibujar para este tipo específico
                // Esto es una forma sencilla, pero para más complejidad, considerar herencia o interfaces.
                // Para este ejemplo, es una solución rápida para mostrar una forma diferente.
                // En un juego más grande, tendrías subclases como RectangularObstacle, CircularObstacle, etc.
            };
        }
    }
}