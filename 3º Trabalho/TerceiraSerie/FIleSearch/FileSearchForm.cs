using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BiggestFileSearch.BiggestFileSearch;

namespace FIleSearch {
    public partial class FileSearchForm : Form {
        private CancellationTokenSource cancelationTokenSource; // o cancellationTokenSource sobre o qual pode ser cancelada a pesquisa pelo ficheiros
        private Progress<Tuple<Tuple<string, long>[], long>> progress;  // o progress sobre o qual são feitos os Report depois de processados os ficheiros do directorio currente

        public FileSearchForm() {
            InitializeComponent();
            cancelationTokenSource = new CancellationTokenSource();
            progress = new Progress<Tuple<Tuple<string, long>[], long>>((temporaryState) => {   // callback executado devido à chamada ao método Report sobre o progress.
                Tuple<string, long>[] temporaryBiggestFiles = temporaryState.Item1;
                long filesEncountered = temporaryState.Item2;

                filesFound.Text = filesEncountered.ToString();

                results.Clear();    // reinicia a lista de ficheiros apresentados
                foreach(Tuple<string, long> file in temporaryBiggestFiles) {
                    results.AppendText(file.Item1 + "\n");
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e) {
        }

        private void textBox3_TextChanged(object sender, EventArgs e) {
        }

        /**
         *  Evento executado sempre que o utilizador da aplicação clicar na tecla start. É desactivado o butão start,
         *  pois assim que se começa o processamento do directorio não pode ser recomeçado, devendo nesse caso ser cancelado
         *  com o butão aqui activado para depois começar a pesquisa. A chamada ao método de pesquisa pelos ficheiros é feito
         *  numa task nova pois na thread de UI não podem ser feitos processamentos demorados. Assim que a execução dessa task
         *  terminar, ou seja já foram encontrados os n maiores ficheiros daquele directorio, entao deve actualizada a vista
         *  de modo a reflectir o fim dessa operação 
         */
        private void Start_Click(object sender, EventArgs e) {
            start.Enabled = false;
            cancel.Enabled = true;
            string searchDirectory = directoryRoot.Text;
            string filesQuantity = numberOfFiles.Text;
            int quantityFiles;

            results.Clear();

            if(searchDirectory.Equals("")) {
                results.AppendText("Search Directory not provided\n");
            }
            if(filesQuantity.Equals("") || !int.TryParse(filesQuantity, out quantityFiles)) {
                results.AppendText("Number of Files must be provided and needs to be an int");
                return;
            }

            CancellationToken cancelationToken = cancelationTokenSource.Token;
            var backgroundTask = Task.Factory.StartNew<Tuple<string[], long>>(() =>
                GetBiggestFiles(searchDirectory, quantityFiles, cancelationToken, progress)
                );

            backgroundTask.ContinueWith((result) => {
                if(cancelationToken.IsCancellationRequested)
                    results.AppendText("Foi cancelada");
                var files = result.Result;
                filesFound.Text = files.Item2.ToString();
                results.Clear();
                foreach (string file in files.Item1) {
                    results.AppendText(file + "\n");
                }
                start.Enabled = true;
                cancel.Enabled = false;
            });
        }

        /**
         * Evento executado sempre que o utilizador carregar no butão cancel, é desactivado o butão cancelado
         * uma vez que não é possivel cancelar uma operação já cancelada e é activado o butão de star para 
         * recomeçar a pesquisa dos ficheiros. Para conseguir isso é chamado o método Cancel sobre o 
         * cancelationTokenSource passado ao método biggestFileSearch.
         */
        private void Cancel_Click(object sender, EventArgs e) {
            cancelationTokenSource.Cancel();
            cancel.Enabled = false;
            start.Enabled = true;
        }
    }
}