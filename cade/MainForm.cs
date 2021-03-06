using cade.Properties;
using System.IO.Ports;

namespace cade
{
    public partial class MainForm : Form
    {
        private Settings settings = new();
        private Execute cmd;
        public string Port
        {
            get { return cmbPort.Text.Trim(); }
            set { cmbPort.Text = value; }
        }
        public MainForm()
        {
            InitializeComponent();
        }
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0219: // WM_DEVICECHANGE
                    InitSerialPorts();
                    break;
            }
            base.WndProc(ref m);
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            InitSerialPorts();
            InitMCUs();
            InitFormats();
            LoadConfigs();

            cmd = new Execute();
            cmd.Load(this.rtxtConsole);
        }
        private void InitSerialPorts()
        {
            cmbPort.Items.Clear();
            cmbPort.Items.AddRange(SerialPort.GetPortNames());
            if (cmbPort.Items.Count > 0)
            {
                cmbPort.SelectedIndex = 0;
            }
        }
        private void InitMCUs()
        {
            cmbMCU.Items.Clear();
            cmbMCU.Items.Add(new Chip("ATmega32U4", "avr109", "m32u4", 32768, 1024));
            cmbMCU.Items.Add(new Chip("ATmega16U2", "avr109", "m16u2", 16384, 512));
            cmbMCU.Items.Add(new Chip("Teensy2pp", "teensy", "usb1286", 32768, 1024));
            cmbMCU.SelectedIndex = 0;
        }

        private void InitFormats()
        {
            cmbFormat.Items.Clear();
            cmbFormat.Items.Add(new Format("Auto", "a"));
            cmbFormat.Items.Add(new Format("Intel Hex", "i"));
#if DEBUG
            cmbFormat.Items.Add(new Format("Moto S-record", "s"));
#endif
            cmbFormat.Items.Add(new Format("Raw", "r"));
            cmbFormat.SelectedIndex = 0;

        }
        private void LoadConfigs()
        {
            txtFilePath.Text = settings.filepath;
            cmbMCU.SelectedIndex = settings.device;
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                txtFilePath.Text = openFileDialog1.FileName;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] path = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            var ext = Path.GetExtension(path[0]);
            if (ext != ".hex" && ext != ".eep" && ext != ".bin") return;
            txtFilePath.Text = path[0];
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            settings.filepath = txtFilePath.Text;
            settings.device = cmbMCU.SelectedIndex;
            settings.Save();
        }

        private void btnProgram_Click(object sender, EventArgs e)
        {
            if (txtFilePath.Text.Length == 0)
            {
                MessageBox.Show("?????????Flash??????", this.Text);
                return;
            }
            if (cmbPort.SelectedIndex < 0)
            {
                MessageBox.Show("?????????????????????", this.Text);
                return;
            }
            if (!File.Exists(txtFilePath.Text))
            {
                MessageBox.Show("???????????????", this.Text);
                return;
            }
            if (new FileInfo(txtFilePath.Text).Length == 0)
            {
                MessageBox.Show("?????????????????????", this.Text);
                return;
            }
            if(ckbAdv.Checked)
            {
                if(ckbRead.Checked)
                {
                    // cannot be auto
                    if("a" == (cmbFormat.SelectedItem as Format)?.FormatType)
                    {
                        MessageBox.Show("??????????????????????????????Auto", this.Text);
                        return;
                    }
                }
            }
            var b = cmd.ExecCommand(GetArgs());
            if (b != 0)
            {
                MessageBox.Show("????????????");
            }
        }

        private string GetArgs()
        {
            var flashType = "flash"; // flash,eeprom
            if (ckbAdv.Checked && ckbEEPROM.Checked)
            {
                flashType = "eeprom";
            }
            var option = "w"; // w,r,v
            if (ckbAdv.Checked && ckbRead.Checked)
            {
                option = "r";
            }
            var flashFormat = (cmbFormat.SelectedItem as Format).FormatType;

            var args = "";
            args += " -c " + (cmbMCU.SelectedItem as Chip).Programer; // programmer
            args += " -p " + (cmbMCU.SelectedItem as Chip).DeviceName; // partno
            args += " -P " + cmbPort.Text; // port
            args += " -D "; // Disable auto erase for flash
            args += $" -U {flashType}:{option}:\"{txtFilePath.Text}\":{flashFormat}";
            return args;
        }

        private void btnErase_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("????????????????????????????\n?????????????????????????????????????????????????????????????????????", "??????", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                var args = "";
                args += " -c " + (cmbMCU.SelectedItem as Chip).Programer; // programmer
                args += " -p " + (cmbMCU.SelectedItem as Chip).DeviceName; // partno
                args += " -P " + cmbPort.Text; // port
                args += " -e";
                cmd.ExecCommand(args);
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show
                (@"1??????????????????????????????(????????????)
2???Port???????????????(???????????????PC???[????????????/??????RST???GND]????????????)
 - ?????????PC??????????????????USB???????????????????????????
3???Device????????????????????????
4?????????FLASH!
????????????????????????<Thank you>?????????");
        }

        private void ??????ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtxtConsole.Clear();
        }

        private void ckbAdv_CheckedChanged(object sender, EventArgs e)
        {
            gpAdv.Visible = ckbAdv.Checked;
            if(gpAdv.Visible)
            {
                FlashOptCheckedChanged(sender, e);
            }
            else
            {
                btnProgram.Text = "FLASH";
            }
        }

        private void FlashOptCheckedChanged(object sender, EventArgs e)
        {
            var txt = "FLASH";
            if (ckbEEPROM.Checked)
            {
                txt = "EEPROM";
            }
            if (ckbRead.Checked)
            {
                txt += "(r)";
            }

            btnProgram.Text = txt;
        }
    }
}