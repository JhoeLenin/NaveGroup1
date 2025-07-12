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
        

        private readonly Dictionary<Keys, bool> teclasPresionadas = new Dictionary<Keys, bool>
        {
            [Keys.W] = false,
            [Keys.Up] = false,
            [Keys.A] = false,
            [Keys.Left] = false,
            [Keys.S] = false,
            [Keys.Down] = false,
            [Keys.D] = false,
            [Keys.Right] = false
        };

        public Movimiento(Naves nave, Control superficieJuego)
        {
            this.nave = nave;
            this.superficieJuego = superficieJuego;

            superficieJuego.KeyDown += SuperficieJuego_KeyDown;
            superficieJuego.KeyUp += SuperficieJuego_KeyUp;
            superficieJuego.PreviewKeyDown += (s, e) => e.IsInputKey = true;

            temporizadorMovimiento = new System.Windows.Forms.Timer
            {
                Interval = 16 // ≈60 FPS
            };
            temporizadorMovimiento.Tick += TemporizadorMovimiento_Tick;
        }

        public void IniciarMovimiento()
        {
            temporizadorMovimiento.Start();
        }

        public void DetenerMovimiento()
        {
            temporizadorMovimiento.Stop();
        }

        private void SuperficieJuego_KeyDown(object sender, KeyEventArgs e)
        {
            if (teclasPresionadas.ContainsKey(e.KeyCode))
            {
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