using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace PokemonMonitor
{
    public class MainForm : Form
    {
        private System.Windows.Forms.Timer updateTimer;
        private ListView queueListView;
        private Label lblTotalBattles;
        private Label lblInjuredPokemon;
        private Label lblActiveArenas;
        private Button btnShutdown;
        
        private IntPtr hMapFile = IntPtr.Zero;
        private IntPtr pView = IntPtr.Zero;
        private const int BUFFER_SIZE = 10;
        
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        private struct PokemonRequest
        {
            public int pokemon_id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)] public string nome;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string tipo;
            public int nivel;
            public int prioridade;
            public int arena_destino;
            public int timestamp; // time_t (provavel 32-bit no MinGW clássico)
            public int processado;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        private struct SharedMemory
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = BUFFER_SIZE)]
            public PokemonRequest[] requests;
            public int front;
            public int rear;
            public int count;
            public int arenas_ocupadas;
            public int total_batalhas;
            public int pokemon_feridos;
            public int shutdown;
            public int trainers_active;
            public int arenas_active;
        }

        public MainForm()
        {
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
            updateTimer = new System.Windows.Forms.Timer { Interval = 1000 }; // Atualizar a cada 1 segundo
            updateTimer.Tick += (s, e) => UpdateDisplay();
            updateTimer.Start();
        }

        private bool EnsureMapping()
        {
            if (pView != IntPtr.Zero)
                return true;

            hMapFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, false, SHM_NAME);
            if (hMapFile == IntPtr.Zero)
                return false;

            pView = MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, 0);
            if (pView == IntPtr.Zero)
            {
                CloseHandle(hMapFile);
                hMapFile = IntPtr.Zero;
                return false;
            }
            return true;
        }

        private void UpdateDisplay()
        {
            try
            {
                if (!EnsureMapping())
                    return;

                var shm = ReadSharedMemory();

                // Atualizar UI
                lblTotalBattles.Text = $"Total de Batalhas: {shm.total_batalhas}";
                lblInjuredPokemon.Text = $"Pokémon Feridos Atendidos: {shm.pokemon_feridos}";
                lblActiveArenas.Text = $"Arenas Ocupadas: {shm.arenas_ocupadas}/3";

                PopulateQueueFromSharedMemory(shm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao ler memória compartilhada: {ex.Message}");
            }
        }

        private SharedMemory ReadSharedMemory()
        {
            int sizeOfReq = Marshal.SizeOf<PokemonRequest>();
            int reqsOffset = 0;
            var shm = new SharedMemory
            {
                requests = new PokemonRequest[BUFFER_SIZE]
            };
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                IntPtr ptr = IntPtr.Add(pView, reqsOffset + i * sizeOfReq);
                shm.requests[i] = Marshal.PtrToStructure<PokemonRequest>(ptr);
            }
            int baseOffset = reqsOffset + BUFFER_SIZE * sizeOfReq;
            shm.front = Marshal.ReadInt32(pView, baseOffset + 0);
            shm.rear = Marshal.ReadInt32(pView, baseOffset + 4);
            shm.count = Marshal.ReadInt32(pView, baseOffset + 8);
            shm.arenas_ocupadas = Marshal.ReadInt32(pView, baseOffset + 12);
            shm.total_batalhas = Marshal.ReadInt32(pView, baseOffset + 16);
            shm.pokemon_feridos = Marshal.ReadInt32(pView, baseOffset + 20);
            shm.shutdown = Marshal.ReadInt32(pView, baseOffset + 24);
            shm.trainers_active = Marshal.ReadInt32(pView, baseOffset + 28);
            shm.arenas_active = Marshal.ReadInt32(pView, baseOffset + 32);
            return shm;
        }

        private void PopulateQueueFromSharedMemory(SharedMemory shm)
        {
            queueListView.BeginUpdate();
            try
            {
                queueListView.Items.Clear();
                int idx = shm.front;
                for (int i = 0; i < shm.count && i < BUFFER_SIZE; i++)
                {
                    var req = shm.requests[(idx + i) % BUFFER_SIZE];
                    var item = new ListViewItem(req.pokemon_id.ToString());
                    item.SubItems.Add(req.nome ?? "");
                    item.SubItems.Add(req.tipo ?? "");
                    item.SubItems.Add(req.nivel.ToString());
                    item.SubItems.Add(req.prioridade > 0 ? "Sim" : "Não");
                    item.SubItems.Add(req.arena_destino.ToString());

                    if (req.timestamp > 0)
                    {
                        try
                        {
                            var dt = DateTimeOffset.FromUnixTimeSeconds(req.timestamp).ToLocalTime().DateTime;
                            item.SubItems.Add(dt.ToString("HH:mm:ss"));
                        }
                        catch
                        {
                            item.SubItems.Add("");
                        }
                    }
                    else
                    {
                        item.SubItems.Add("");
                    }

                    queueListView.Items.Add(item);
                }
            }
            finally
            {
                queueListView.EndUpdate();
            }
        }

        private void ShutdownSystem()
        {
            if (MessageBox.Show("Deseja realmente desligar o sistema?", "Confirmação", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                updateTimer.Stop();

                try
                {
                    if (EnsureMapping())
                    {
                        int sizeOfReq = Marshal.SizeOf<PokemonRequest>();
                        int baseOffset = BUFFER_SIZE * sizeOfReq;
                        int shutdownOffset = baseOffset + 24;
                        Marshal.WriteInt32(pView, shutdownOffset, 1);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro ao sinalizar shutdown: {ex.Message}");
                }

                Application.Exit();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (pView != IntPtr.Zero)
            {
                UnmapViewOfFile(pView);
                pView = IntPtr.Zero;
            }
            if (hMapFile != IntPtr.Zero)
            {
                CloseHandle(hMapFile);
                hMapFile = IntPtr.Zero;
            }
        }
    }
}