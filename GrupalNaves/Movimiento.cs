using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GrupalNaves
{
    public class Movimiento
    {
        private Naves nave;
        private Control superficieJuego; // El formulario o panel donde se dibuja la nave
        private System.Windows.Forms.Timer temporizadorMovimiento; // Especifica el tipo
        private int velocidad = 5; // Velocidad de movimiento en píxeles

        // Diccionario para rastrear qué teclas están presionadas
        private Dictionary<Keys, bool> teclasPresionadas = new Dictionary<Keys, bool>();

        // Constructor
        public Movimiento(Naves nave, Control superficieJuego)
        {
            this.nave = nave;
            this.superficieJuego = superficieJuego;

            // Inicializar las teclas en falso
            teclasPresionadas[Keys.W] = false;
            teclasPresionadas[Keys.A] = false;
            teclasPresionadas[Keys.S] = false;
            teclasPresionadas[Keys.D] = false;

            // Configurar eventos de teclado
            this.superficieJuego.KeyDown += SuperficieJuego_KeyDown;
            this.superficieJuego.KeyUp += SuperficieJuego_KeyUp;

            // Configurar el temporizador para el movimiento continuo
            temporizadorMovimiento = new System.Windows.Forms.Timer(); // Especifica el tipotemporizadorMovimiento = new Timer();
            temporizadorMovimiento.Interval = 20; // Actualizar cada 20 ms (50 FPS)
            temporizadorMovimiento.Tick += TemporizadorMovimiento_Tick;
        }

        // Inicia el temporizador de movimiento
        public void IniciarMovimiento()
        {
            temporizadorMovimiento.Start();
        }

        // Detiene el temporizador de movimiento
        public void DetenerMovimiento()
        {
            temporizadorMovimiento.Stop();
        }

        // Manejador de evento cuando se presiona una tecla
        private void SuperficieJuego_KeyDown(object sender, KeyEventArgs e)
        {
            if (teclasPresionadas.ContainsKey(e.KeyCode))
            {
                teclasPresionadas[e.KeyCode] = true;
            }
        }

        // Manejador de evento cuando se suelta una tecla
        private void SuperficieJuego_KeyUp(object sender, KeyEventArgs e)
        {
            if (teclasPresionadas.ContainsKey(e.KeyCode))
            {
                teclasPresionadas[e.KeyCode] = false;
            }
        }

        // Este método se ejecuta cada vez que el temporizador "hace un tick"
        private void TemporizadorMovimiento_Tick(object sender, EventArgs e)
        {
            int deltaX = 0;
            int deltaY = 0;

            // Calcular el cambio en Y
            if (teclasPresionadas[Keys.W])
            {
                deltaY -= velocidad;
            }
            if (teclasPresionadas[Keys.S])
            {
                deltaY += velocidad;
            }

            // Calcular el cambio en X
            if (teclasPresionadas[Keys.A])
            {
                deltaX -= velocidad;
            }
            if (teclasPresionadas[Keys.D])
            {
                deltaX += velocidad;
            }

            // Normalizar movimiento diagonal
            // Si hay movimiento tanto en X como en Y (diagonal), ajustamos la velocidad
            // para que no sea más rápida que el movimiento horizontal o vertical.
            if (deltaX != 0 && deltaY != 0)
            {
                // Usamos el teorema de Pitágoras para mantener la velocidad constante en diagonal
                // velocidadDiagonal = velocidad / sqrt(2)
                double factorDiagonal = velocidad / Math.Sqrt(2);
                deltaX = (int)(Math.Sign(deltaX) * factorDiagonal);
                deltaY = (int)(Math.Sign(deltaY) * factorDiagonal);
            }

            // Actualizar la posición de la nave
            nave.PosX += deltaX;
            nave.PosY += deltaY;

            // Limitar la nave dentro de los límites del formulario
            // Se asume que la nave tiene un 'ancho' y 'alto' inherentes a su dibujo.
            // Para una aproximación simple, podríamos usar un tamaño fijo o estimar.
            // Para mayor precisión, Naves debería tener propiedades de Ancho y Alto.
            // Por ahora, usaremos un tamaño estimado para los límites.
            int anchoNaveEstimado = (int)(100 * nave.Escala); // Estimar tamaño basado en la escala
            int altoNaveEstimado = (int)(100 * nave.Escala); // Estimar tamaño basado en la escala

            nave.PosX = Math.Max(0, Math.Min(superficieJuego.ClientSize.Width - anchoNaveEstimado, nave.PosX));
            nave.PosY = Math.Max(0, Math.Min(superficieJuego.ClientSize.Height - altoNaveEstimado, nave.PosY));


            // Solicitar redibujar el formulario para mostrar la nueva posición de la nave
            superficieJuego.Invalidate();
        }
    }
}