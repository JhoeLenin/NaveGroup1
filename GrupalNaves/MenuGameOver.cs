using System;
using System.Drawing;
using System.Windows.Forms;

namespace GrupalNaves
{
    public class MenuGameOver
    {
        public event Action ReiniciarJuego;
        public event Action SalirDelJuego;
        public event Action VolverAlMenuPrincipal;

        public void MostrarMenuGameOver()
        {
            using (var gameOverForm = new Form())
            {
                // Configuración básica del formulario
                gameOverForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                gameOverForm.StartPosition = FormStartPosition.CenterScreen;
                gameOverForm.Text = "Juego Terminado";
                gameOverForm.Width = 420;
                gameOverForm.Height = 380;
                gameOverForm.MaximizeBox = false;
                gameOverForm.MinimizeBox = false;
                gameOverForm.BackColor = Color.FromArgb(28, 28, 28); // Mismo fondo oscuro que Menu.cs
                gameOverForm.Padding = new Padding(10);

                // Etiqueta de título "PERDISTE"
                var titleLabel = new Label()
                {
                    Text = "¡PERDISTE!",
                    Font = new Font("Arial", 22, FontStyle.Bold),
                    ForeColor = Color.Gold, // Un rojo vibrante para destacar la derrota
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 30, 0, 20)
                };
                gameOverForm.Controls.Add(titleLabel);

                // Panel para los botones (transparente para que el fondo del formulario se vea)
                var buttonPanel = new FlowLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true,
                    Location = new Point(0, titleLabel.Height),
                    Padding = new Padding(10),
                    BackColor = Color.Transparent, // Fondo transparente
                    Anchor = AnchorStyles.None
                };
                buttonPanel.ControlAdded += (s, e) =>
                {
                    e.Control.Anchor = AnchorStyles.None;
                };

                // Crear y configurar botones
                Action<Button, string, Action, Color> setupButton = (btn, text, action, backColor) =>
                {
                    btn.Text = text;
                    btn.Width = 200;
                    btn.Height = 60;
                    btn.Font = new Font("Arial", 12, FontStyle.Bold);
                    btn.ForeColor = Color.FromArgb(50, 50, 50); // Texto blanco para los botones
                    btn.BackColor = backColor; // Color de fondo personalizado
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Cursor = Cursors.Hand;
                    btn.Margin = new Padding(8);
                    btn.Click += (s, e) => { action?.Invoke(); gameOverForm.Close(); };
                    buttonPanel.Controls.Add(btn);
                };

                // Botón Reiniciar Juego (usando el verde de "ESCOGER ESTA NAVE!" de Menu.cs)
                var btnReiniciar = new Button();
                setupButton(btnReiniciar, "Reiniciar Juego", () => ReiniciarJuego?.Invoke(), Color.LightGreen);

                // Botón Volver al Menú Principal (usando el naranja de las etiquetas de Menu.cs)
                var btnMenuPrincipal = new Button();
                setupButton(btnMenuPrincipal, "Volver al Menú Principal", () => VolverAlMenuPrincipal?.Invoke(), Color.Gold);

                // Botón Salir del Juego (usando un rojo más oscuro para una acción final)
                var btnSalir = new Button();
                setupButton(btnSalir, "Salir del Juego", () => SalirDelJuego?.Invoke(), Color.LightSalmon); // Un rojo distinto al del título

                gameOverForm.Controls.Add(buttonPanel);

                // Centrar el panel de botones
                gameOverForm.Load += (s, e) =>
                {
                    buttonPanel.Left = (gameOverForm.ClientSize.Width - buttonPanel.Width) / 2;
                };

                gameOverForm.ShowDialog();
            }
        }
    }
}