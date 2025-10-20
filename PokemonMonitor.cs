using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace PokemonMonitor
{
    public partial class MainForm : Form
    {
        private Timer updateTimer;
        private ListView queueListView;
        private Label lblTotalBattles;
        private Label lblInjuredPokemon;
        private Label lblActiveArenas;
        private Button btnShutdown;
        
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);
        
        [DllImport("kernel32.dll")]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, 
                                          uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        
        [DllImport("kernel32.dll")]
        static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
        
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);
        
        const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        const string SHM_NAME = "Local\\PokemonExpedition";

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            StartMonitoring();
        }

        private void SetupUI()
        {
            this.Text = "Centro de Expedição Pokémon - Monitor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Painel de estatísticas
            var statsPanel = new Panel { Dock = DockStyle.Top, Height = 100, BorderStyle = BorderStyle.FixedSingle };
            
            lblTotalBattles = new Label { Text = "Total de Batalhas: 0", Location = new Point(10, 10), AutoSize = true };
            lblInjuredPokemon = new Label { Text = "Pokémon Feridos Atendidos: 0", Location = new Point(10, 30), AutoSize = true };
            lblActiveArenas = new Label { Text = "Arenas Ocupadas: 0/3", Location = new Point(10, 50), AutoSize = true };
            
            statsPanel.Controls.AddRange(new Control[] { lblTotalBattles, lblInjuredPokemon, lblActiveArenas });

            // ListView para fila
            queueListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };
            
            queueListView.Columns.Add("ID", 80);
            queueListView.Columns.Add("Nome", 100);
            queueListView.Columns.Add("Tipo", 80);
            queueListView.Columns.Add("Nível", 60);
            queueListView.Columns.Add("Prioridade", 80);
            queueListView.Columns.Add("Arena", 60);
            queueListView.Columns.Add("Timestamp", 120);

            // Botões de controle
            var controlPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            btnShutdown = new Button { Text = "Desligar Sistema", Location = new Point(10, 10), Size = new Size(100, 30) };
            btnShutdown.Click += (s, e) => ShutdownSystem();
            
            controlPanel.Controls.Add(btnShutdown);

            this.Controls.AddRange(new Control[] { queueListView, statsPanel, controlPanel });
        }

        private void StartMonitoring()
        {
            updateTimer = new Timer { Interval = 1000 }; // Atualizar a cada 1 segundo
            updateTimer.Tick += (s, e) => UpdateDisplay();
            updateTimer.Start();
        }

        private unsafe void UpdateDisplay()
        {
            try
            {
                IntPtr hMapFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, false, SHM_NAME);
                if (hMapFile == IntPtr.Zero) return;

                IntPtr pData = MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, 0);
                if (pData == IntPtr.Zero)
                {
                    CloseHandle(hMapFile);
                    return;
                }

                // Estrutura da memória compartilhada (simplificada para demo)
                int* data = (int*)pData;
                
                // Atualizar UI na thread principal
                this.Invoke(new Action(() =>
                {
                    lblTotalBattles.Text = $"Total de Batalhas: {data[8]}"; // total_batalhas
                    lblInjuredPokemon.Text = $"Pokémon Feridos Atendidos: {data[9]}"; // pokemon_feridos
                    lblActiveArenas.Text = $"Arenas Ocupadas: {data[6]}/3"; // arenas_ocupadas
                    
                    // Simular dados da fila (em implementação real, ler da memória compartilhada)
                    UpdateQueueSimulation();
                }));

                UnmapViewOfFile(pData);
                CloseHandle(hMapFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao acessar memória compartilhada: {ex.Message}");
            }
        }

        private void UpdateQueueSimulation()
        {
            // Simulação - em implementação real, ler da memória compartilhada
            queueListView.Items.Clear();
            
            var random = new Random();
            string[] tipos = { "Fogo", "Água", "Planta", "Elétrico", "Psíquico" };
            string[] nomes = { "Charmander", "Squirtle", "Bulbasaur", "Pikachu", "Abra" };
            
            for (int i = 0; i < random.Next(0, 8); i++)
            {
                var item = new ListViewItem(random.Next(1000, 9999).ToString());
                item.SubItems.Add(nomes[random.Next(nomes.Length)]);
                item.SubItems.Add(tipos[random.Next(tipos.Length)]);
                item.SubItems.Add(random.Next(1, 50).ToString());
                item.SubItems.Add(random.Next(10) == 0 ? "Sim" : "Não");
                item.SubItems.Add(random.Next(0, 3).ToString());
                item.SubItems.Add($"{random.Next(10, 23)}:{random.Next(10, 59)}");
                
                queueListView.Items.Add(item);
            }
        }

        private void ShutdownSystem()
        {
            if (MessageBox.Show("Deseja realmente desligar o sistema?", "Confirmação", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                updateTimer.Stop();
                
                // Sinalizar shutdown via memória compartilhada
                IntPtr hMapFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, false, SHM_NAME);
                if (hMapFile != IntPtr.Zero)
                {
                    IntPtr pData = MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, 0);
                    if (pData != IntPtr.Zero)
                    {
                        int* data = (int*)pData;
                        data[10] = 1; // shutdown = true
                        UnmapViewOfFile(pData);
                    }
                    CloseHandle(hMapFile);
                }
                
                Application.Exit();
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}