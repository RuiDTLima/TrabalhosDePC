using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BiggestFileSearch.BiggestFileSearch;

namespace FIleSearch {
    public partial class FileSearchForm : Form {
        private CancellationTokenSource cancelationTokenSource;
        public FileSearchForm() {
            InitializeComponent();
            cancelationTokenSource = new CancellationTokenSource();
        }

        private void Form1_Load(object sender, EventArgs e) {
        }

        private void textBox3_TextChanged(object sender, EventArgs e) {
        }

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
                GetBiggestFiles(searchDirectory, quantityFiles, cancelationToken)
                );
            
            backgroundTask.ContinueWith((result) => {
                if(cancelationToken.IsCancellationRequested)
                    results.AppendText("Foi cancelada");
                var files = result.Result;
                filesFound.Text = files.Item2.ToString();
                //results.AppendText(string.Format("It was found {0} files\n", files.Item2));
                foreach (string file in files.Item1) {
                    results.AppendText(file + "\n");
                }
                start.Enabled = true;
                cancel.Enabled = false;
            });
        }

        private void Cancel_Click(object sender, EventArgs e) {
            cancelationTokenSource.Cancel();
            cancel.Enabled = false;
            start.Enabled = true;
        }
    }
}