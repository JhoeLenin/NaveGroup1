using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GrupalNaves
{
    public class Movimiento
    {
        private readonly Naves nave;
        private readonly Control superficieJuego;
        private readonly System.Windows.Forms.Timer temporizadorMovimiento;

        // Cambiamos a float para todos los cálculos
        private const float VelocidadBase = 15f; // Nota la 'f' para indicar float
        private float velocidadActual = VelocidadBase;


        // Diccionario que mantiene el estado de teclas específicas (presionadas o no).
        // Cada tecla está asociada a un valor booleano: true si está presionada, false si no.
        private readonly Dictionary<Keys, bool> teclasPresionadas = new Dictionary<Keys, bool>
        {
            [Keys.W] = false, // Tecla W (mover arriba)
            [Keys.Up] = false, // Flecha arriba
            [Keys.A] = false, // Tecla A (mover izquierda)
            [Keys.Left] = false, // Flecha izquierda
            [Keys.S] = false, // Tecla S (mover abajo)
            [Keys.Down] = false, // Flecha abajo
            [Keys.D] = false, // Tecla D (mover derecha)
            [Keys.Right] = false // Flecha derecha
        };

        // Constructor de la clase Movimiento. Recibe la nave que se moverá y el control donde se dibuja el juego.
        public Movimiento(Naves nave, Control superficieJuego)
        {
            this.nave = nave; // Guarda la referencia de la nave a mover
            this.superficieJuego = superficieJuego; // Guarda el área (control) donde se moverá la nave

            // Se enlazan los eventos del teclado a los métodos manejadores
            superficieJuego.KeyDown += SuperficieJuego_KeyDown; // Cuando se presiona una tecla
            superficieJuego.KeyUp += SuperficieJuego_KeyUp; // Cuando se suelta una tecla

            // Permite que el control capture las teclas especiales como flechas (de lo contrario, Windows las ignora en algunos controles)
            superficieJuego.PreviewKeyDown += (s, e) => e.IsInputKey = true;

            // Se configura un temporizador para controlar el movimiento de la nave a intervalos regulares (aproximadamente 60 veces por segundo)
            temporizadorMovimiento = new System.Windows.Forms.Timer
            {
                Interval = 16 // ≈60 FPS
            };
            // Se asocia el evento Tick del temporizador al método que actualiza el movimiento
            temporizadorMovimiento.Tick += TemporizadorMovimiento_Tick;
        }

        // Método para iniciar el temporizador de movimiento
        public void IniciarMovimiento()
        {
            temporizadorMovimiento.Start(); // Comienza a ejecutar el evento Tick periódicamente
        }

        // Método para detener el temporizador de movimiento
        public void DetenerMovimiento()
        {
            temporizadorMovimiento.Stop(); // Detiene la ejecución del evento Tick
        }

        // Evento que se ejecuta cuando una tecla es presionada
        private void SuperficieJuego_KeyDown(object sender, KeyEventArgs e)
        {
            // Verifica si la tecla presionada está registrada en el diccionario
            if (teclasPresionadas.ContainsKey(e.KeyCode))
            {
                // Marca la tecla como presionada
                teclasPresionadas[e.KeyCode] = true;
            }
        }

        private void SuperficieJuego_KeyUp(object sender, KeyEventArgs e)
        {
            if (teclasPresionadas.ContainsKey(e.KeyCode))
            {
                teclasPresionadas[e.KeyCode] = false;
            }
        }

        private void TemporizadorMovimiento_Tick(object sender, EventArgs e)
        {
            // Calcular dirección del movimiento
            float deltaX = 0f, deltaY = 0f;

            if (teclasPresionadas[Keys.W] || teclasPresionadas[Keys.Up]) deltaY -= 1f;
            if (teclasPresionadas[Keys.S] || teclasPresionadas[Keys.Down]) deltaY += 1f;
            if (teclasPresionadas[Keys.A] || teclasPresionadas[Keys.Left]) deltaX -= 1f;
            if (teclasPresionadas[Keys.D] || teclasPresionadas[Keys.Right]) deltaX += 1f;

            // Normalizar movimiento diagonal
            if (deltaX != 0f || deltaY != 0f)
            {
                float magnitude = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                deltaX /= magnitude;
                deltaY /= magnitude;

                // Aceleración suave
                velocidadActual = Math.Min(velocidadActual + 0.5f, VelocidadBase);
                deltaX *= velocidadActual;
                deltaY *= velocidadActual;
            }
            else
            {
                // Fricción al soltar las teclas
                velocidadActual = VelocidadBase * 0.3f;
            }

            // Aplicar movimiento (PosX y PosY ahora son float)
            nave.PosX += deltaX;
            nave.PosY += deltaY;

            // Limitar posición dentro de los bordes
            float margen = 20f * nave.Escala;
            nave.PosX = Math.Clamp(nave.PosX, margen, superficieJuego.ClientSize.Width - margen);
            nave.PosY = Math.Clamp(nave.PosY, margen, superficieJuego.ClientSize.Height - margen);

            superficieJuego.Invalidate();
        }
    }
}