using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace GrupalNaves
{
    public class Menu
    {
        public event Action<TipoAvion> NaveSeleccionada;

        public void MostrarMenu()
        {
            using (var menuForm = new Form())
            {
                // **Configuración básica del formulario**
                menuForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                menuForm.StartPosition = FormStartPosition.CenterScreen;
                menuForm.Text = "¡Prepara tu nave para la batalla!";
                menuForm.Width = 450;
                menuForm.Height = 500;
                menuForm.MaximizeBox = false;
                menuForm.MinimizeBox = false;
                menuForm.ShowInTaskbar = false;
                menuForm.BackColor = Color.FromArgb(28, 28, 28);
                menuForm.Padding = new Padding(10);

                // **Etiqueta de título mejorada**
                var titleLabel = new Label()
                {
                    Text = "¡SELECCIONA TU NAVE!",
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    ForeColor = Color.Gold,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 10, 0, 15)
                };

                // **Panel para agrupar los controles de selección**
                var selectionPanel = new Panel()
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10), // Padding interno para los controles
                    BackColor = Color.Transparent
                };

                // **Etiqueta para el ComboBox**
                var label = new Label()
                {
                    Text = "Elige tu nave espacial:",
                    Width = 250, // Se ajustará con AutoSize, pero lo mantenemos para referencia
                    ForeColor = Color.Orange, // Cambiado a Blanco puro para asegurar contraste
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    AutoSize = true // Importante para que el Label tome el tamaño de su texto
                };

                // **ComboBox mejorado**
                var comboBox = new ComboBox()
                {
                    Width = 250,
                    // Height = 100, // Esta altura es para el ComboBox en sí, no para la lista desplegable. 
                    // Un valor estándar es suficiente, o simplemente no la fijes.
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    Font = new Font("Aptos", 11, FontStyle.Regular)
                };

                foreach (TipoAvion tipo in Enum.GetValues(typeof(TipoAvion)))
                {
                    comboBox.Items.Add(tipo.ToString());
                }
                comboBox.SelectedIndex = 0;

                // **PictureBox para la previsualización**
                var pictureBoxPreview = new PictureBox()
                {
                    Width = 250,
                    Height = 180,
                    BorderStyle = BorderStyle.Fixed3D,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(40, 40, 40)
                };

                comboBox.SelectedIndexChanged += (s, e) =>
                {
                    TipoAvion tipoSeleccionadoEnCombo = (TipoAvion)comboBox.SelectedIndex;
                    ActualizarPreviewNave(pictureBoxPreview, tipoSeleccionadoEnCombo);
                };

                // **Botón de acción mejorado**
                var selectButton = new Button()
                {
                    Text = "¡ESCOGER ESTA NAVE!",
                    Width = 250,
                    Height = 45,
                    BackColor = Color.LimeGreen,
                    ForeColor = Color.White,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                selectButton.FlatAppearance.BorderSize = 0;

                // Variable para saber si se hizo clic en el botón de selección
                bool naveSeleccionadaPorBoton = false;

                selectButton.Click += (s, e) =>
                {
                    TipoAvion seleccionFinal = (TipoAvion)comboBox.SelectedIndex;
                    NaveSeleccionada?.Invoke(seleccionFinal);
                    naveSeleccionadaPorBoton = true; // Se marcó que se seleccionó por el botón
                    menuForm.Close();
                };

                // Evento FormClosing para controlar el cierre
                menuForm.FormClosing += (s, e) =>
                {
                    // Si la nave NO fue seleccionada por el botón, cancela el cierre de la aplicación.
                    // Esto evita que Form1 se abra si se cierra la ventana manualmente.
                    if (!naveSeleccionadaPorBoton)
                    {
                        // Si quieres que el programa completo se cierre si el usuario cierra el menú
                        // sin seleccionar una nave, puedes usar Application.Exit();
                        Application.Exit();
                        // O si solo quieres cancelar el cierre de este menú y que el usuario decida:
                        // e.Cancel = true; 
                        // MessageBox.Show("Debes escoger una nave para continuar o cerrar la aplicación.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };


                // **Agregar controles al panel de selección en un orden lógico**
                selectionPanel.Controls.Add(label);
                selectionPanel.Controls.Add(comboBox);
                selectionPanel.Controls.Add(pictureBoxPreview);
                selectionPanel.Controls.Add(selectButton);

                // **Agregar el título y el panel de selección al formulario**
                menuForm.Controls.Add(titleLabel);
                menuForm.Controls.Add(selectionPanel);

                // **Posicionamiento central de los controles dentro del panel**
                menuForm.Load += (s, e) =>
                {
                    int controlWidth = 250; // Ancho consistente para los controles
                    int panelCenterX = (selectionPanel.Width - controlWidth) / 2;

                    // Posicionamiento vertical dentro del panel
                    label.Left = panelCenterX;
                    label.Top = label.Bottom + 35; // Inicio en la parte superior del panel

                    comboBox.Left = panelCenterX;
                    comboBox.Top = label.Bottom + 25; // Ajuste para un buen espaciado

                    pictureBoxPreview.Left = panelCenterX;
                    pictureBoxPreview.Top = comboBox.Bottom + 20;

                    selectButton.Left = panelCenterX;
                    selectButton.Top = pictureBoxPreview.Bottom + 25;
                };

                ActualizarPreviewNave(pictureBoxPreview, (TipoAvion)comboBox.SelectedIndex);

                menuForm.ShowDialog();
            }
        }

        private void ActualizarPreviewNave(PictureBox pb, TipoAvion tipo)
        {
            try
            {
                Naves navePreview = new Naves(tipo)
                {
                    PosX = pb.Width / 2,
                    PosY = pb.Height / 2,
                    Escala = 0.5f // Ajustar la escala para que se vea mejor
                };

                Bitmap bmp = new Bitmap(pb.Width, pb.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(pb.BackColor);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Asegúrate de que tu método Dibujar en la clase Naves 
                    // toma el tercer y cuarto parámetro (drawX, drawY) o se ajusta a PosX, PosY.
                    // Si tu clase Naves tiene AnchoBase y AltoBase (recomendado):
                    // int scaledWidth = (int)(navePreview.AnchoBase * navePreview.Escala);
                    // int scaledHeight = (int)(navePreview.AltoBase * navePreview.Escala);
                    // int drawX = (pb.Width - scaledWidth) / 2;
                    // int drawY = (pb.Height - scaledHeight) / 2;
                    // navePreview.Dibujar(g, navePreview.Escala, drawX, drawY);

                    // Si no tienes AnchoBase/AltoBase y tu método Dibujar espera solo Graphics y Escala,
                    // y usa PosX/PosY internamente:
                    navePreview.Dibujar(g, navePreview.Escala); // Usará navePreview.PosX y navePreview.PosY

                    // Si tu método Dibujar espera solo Graphics, Escala, y no usa PosX/PosY de la clase:
                    // int estimatedBaseSize = 100; // Ajusta este valor si tus naves son más grandes/pequeñas
                    // int drawX = (int)((pb.Width / 2) - (navePreview.Escala * estimatedBaseSize / 2));
                    // int drawY = (int)((pb.Height / 2) - (navePreview.Escala * estimatedBaseSize / 2));
                    // navePreview.Dibujar(g, navePreview.Escala, drawX, drawY); // Si tu Dibujar tiene (Graphics, float, int, int)
                }
                pb.Image = bmp;
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Error al cargar archivos para la previsualización: {ex.Message}\nAsegúrate de que los archivos 'bordes.txt' y 'coloreados.txt' para el avión '{tipo}' existen en la carpeta 'Assets\\Naves\\{tipo}'.", "Error de Archivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Image = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al previsualizar la nave: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Image = null;
            }
        }
    }
}