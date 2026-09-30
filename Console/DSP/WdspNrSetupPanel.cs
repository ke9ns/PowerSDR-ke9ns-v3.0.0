//=================================================================
// WdspNrSetupPanel.cs
// Compact WDSP noise-reduction setup page for PowerSDR KE9NS.
//=================================================================

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PowerSDR
{
    public partial class Setup
    {
        private TabPage tpWdspNRs;
        private ComboBoxTS comboWdspNrPosition;

        private NumericUpDownTS udWdspNr1Taps;
        private NumericUpDownTS udWdspNr1Delay;
        private NumericUpDownTS udWdspNr1Gain;
        private NumericUpDownTS udWdspNr1Leakage;

        private ComboBoxTS comboWdspNr2GainMethod;
        private ComboBoxTS comboWdspNr2NpeMethod;
        private CheckBoxTS chkWdspNr2Ae;
        private NumericUpDownTS udWdspNr2TrainThreshold;
        private NumericUpDownTS udWdspNr2TrainT2;
        private CheckBoxTS chkWdspNr2Post;
        private NumericUpDownTS udWdspNr2PostLevel;
        private NumericUpDownTS udWdspNr2PostFactor;
        private NumericUpDownTS udWdspNr2PostRate;
        private NumericUpDownTS udWdspNr2PostTaper;

        private LabelTS lblWdspNr3Model;
        private TextBoxTS txtWdspNr3ModelPath;
        private CheckBoxTS chkWdspNr3FixedGain;

        private NumericUpDownTS udWdspNr4Reduction;
        private NumericUpDownTS udWdspNr4Smoothing;
        private NumericUpDownTS udWdspNr4Whitening;
        private NumericUpDownTS udWdspNr4Rescale;
        private NumericUpDownTS udWdspNr4Threshold;
        private ComboBoxTS comboWdspNr4Algorithm;

        private void InitializeWdspNrTab()
        {
            tpWdspNRs = new TabPage();
            tpWdspNRs.Name = "tpWdspNRs";
            tpWdspNRs.Text = "NRs (WDSP)";
            tpWdspNRs.UseVisualStyleBackColor = true;
            tpWdspNRs.AutoScroll = true;

            LabelTS header = NewLabel("NR - RX1 / RX-S / RX2", 10, 10, 158);
            header.Font = new Font(header.Font, FontStyle.Bold);
            tpWdspNRs.Controls.Add(header);
            tpWdspNRs.Controls.Add(NewLabel("Position:", 170, 10, 55));

            comboWdspNrPosition = NewCombo("comboWdspNrPosition", 225, 6, 92,
                new string[] { "Pre-AGC", "Post-AGC" }, 1);
            toolTip1.SetToolTip(comboWdspNrPosition,
                "Select whether all four noise reducers run before or after AGC.");
            tpWdspNRs.Controls.Add(comboWdspNrPosition);

            LabelTS live = NewLabel("Settings apply in real time", 345, 10, 220);
            live.ForeColor = Color.DarkGreen;
            tpWdspNRs.Controls.Add(live);

            GroupBoxTS nr1 = NewGroup("NR1 - ANR", 8, 35, 156, 155);
            udWdspNr1Taps = AddNumeric(nr1, "Taps", "udWdspNr1Taps", 20,
                16, 256, 64, 0, 8);
            udWdspNr1Delay = AddNumeric(nr1, "Delay", "udWdspNr1Delay", 47,
                1, 128, 16, 0, 1);
            udWdspNr1Gain = AddNumeric(nr1, "Gain", "udWdspNr1Gain", 74,
                1, 1000, 100, 0, 1);
            udWdspNr1Leakage = AddNumeric(nr1, "Leak", "udWdspNr1Leakage", 101,
                1, 1000, 100, 0, 1);
            ButtonTS resetNr1 = NewButton("Default", 42, 126, 72);
            resetNr1.Click += delegate
            {
                udWdspNr1Taps.Value = 64;
                udWdspNr1Delay.Value = 16;
                udWdspNr1Gain.Value = 100;
                udWdspNr1Leakage.Value = 100;
            };
            nr1.Controls.Add(resetNr1);
            toolTip1.SetToolTip(udWdspNr1Gain, "Adaptation gain, displayed in 1e-6 units as in Thetis.");
            toolTip1.SetToolTip(udWdspNr1Leakage, "Filter leakage, displayed in 1e-3 units as in Thetis.");

            GroupBoxTS nr3 = NewGroup("NR3 - RNNoise", 8, 195, 156, 100);
            lblWdspNr3Model = NewLabel("Model: Default", 8, 20, 140);
            lblWdspNr3Model.AutoEllipsis = true;
            nr3.Controls.Add(lblWdspNr3Model);
            ButtonTS loadModel = NewButton("Model...", 8, 42, 68);
            loadModel.Click += WdspNr3LoadModel_Click;
            nr3.Controls.Add(loadModel);
            ButtonTS defaultModel = NewButton("Default", 80, 42, 68);
            defaultModel.Click += delegate
            {
                if (String.IsNullOrEmpty(txtWdspNr3ModelPath.Text)) ApplyWdspNrModel();
                else txtWdspNr3ModelPath.Text = String.Empty;
            };
            nr3.Controls.Add(defaultModel);
            chkWdspNr3FixedGain = new CheckBoxTS();
            chkWdspNr3FixedGain.Name = "chkWdspNr3FixedGain";
            chkWdspNr3FixedGain.Text = "Fixed gain";
            chkWdspNr3FixedGain.Checked = true;
            chkWdspNr3FixedGain.Location = new Point(9, 72);
            chkWdspNr3FixedGain.AutoSize = true;
            nr3.Controls.Add(chkWdspNr3FixedGain);
            toolTip1.SetToolTip(chkWdspNr3FixedGain,
                "Use the fixed gain recommended by RNNoise/VU3RDD.");

            GroupBoxTS nr2 = NewGroup("NR2 - EMNR", 170, 35, 212, 260);
            nr2.Controls.Add(NewLabel("Gain method", 8, 22, 76));
            comboWdspNr2GainMethod = NewCombo("comboWdspNr2GainMethod", 88, 18, 92,
                new string[] { "Linear", "Log", "Gamma", "Trained" }, 2);
            nr2.Controls.Add(comboWdspNr2GainMethod);
            nr2.Controls.Add(NewLabel("NPE method", 8, 50, 76));
            comboWdspNr2NpeMethod = NewCombo("comboWdspNr2NpeMethod", 88, 46, 92,
                new string[] { "OSMS", "MMSE", "NSTAT" }, 0);
            nr2.Controls.Add(comboWdspNr2NpeMethod);
            chkWdspNr2Ae = new CheckBoxTS();
            chkWdspNr2Ae.Name = "chkWdspNr2Ae";
            chkWdspNr2Ae.Text = "AE Filter";
            chkWdspNr2Ae.Checked = true;
            chkWdspNr2Ae.Location = new Point(10, 75);
            chkWdspNr2Ae.AutoSize = true;
            nr2.Controls.Add(chkWdspNr2Ae);
            udWdspNr2TrainThreshold = AddNumeric(nr2, "T1", "udWdspNr2TrainThreshold", 101,
                -200, 0, -50, 1, 5);
            udWdspNr2TrainT2 = AddNumeric(nr2, "T2", "udWdspNr2TrainT2", 128,
                0, 100, 20, 2, 1);
            toolTip1.SetToolTip(udWdspNr2TrainThreshold,
                "Primary threshold for the Trained method; higher values suppress more noise.");
            toolTip1.SetToolTip(udWdspNr2TrainT2,
                "Secondary threshold; lower values preserve very weak signals.");

            chkWdspNr2Post = new CheckBoxTS();
            chkWdspNr2Post.Name = "chkWdspNr2Post";
            chkWdspNr2Post.Text = "Noise post proc";
            chkWdspNr2Post.Location = new Point(10, 155);
            chkWdspNr2Post.AutoSize = true;
            nr2.Controls.Add(chkWdspNr2Post);
            udWdspNr2PostLevel = AddCompactNumeric(nr2, "Level", "udWdspNr2PostLevel",
                9, 180, 0, 100, 15);
            udWdspNr2PostFactor = AddCompactNumeric(nr2, "Factor", "udWdspNr2PostFactor",
                110, 180, 0, 100, 15);
            udWdspNr2PostRate = AddCompactNumeric(nr2, "Rate", "udWdspNr2PostRate",
                9, 209, 1, 100, 15);
            udWdspNr2PostTaper = AddCompactNumeric(nr2, "Taper", "udWdspNr2PostTaper",
                110, 209, 0, 100, 12);
            ButtonTS resetNr2 = NewButton("Default", 70, 234, 72);
            resetNr2.Click += delegate
            {
                comboWdspNr2GainMethod.SelectedIndex = 2;
                comboWdspNr2NpeMethod.SelectedIndex = 0;
                chkWdspNr2Ae.Checked = true;
                udWdspNr2TrainThreshold.Value = -5.0M;
                udWdspNr2TrainT2.Value = 0.20M;
                chkWdspNr2Post.Checked = false;
                udWdspNr2PostLevel.Value = 15;
                udWdspNr2PostFactor.Value = 15;
                udWdspNr2PostRate.Value = 15;
                udWdspNr2PostTaper.Value = 12;
            };
            nr2.Controls.Add(resetNr2);

            GroupBoxTS nr4 = NewGroup("NR4 - SpecBleach", 388, 35, 198, 260);
            udWdspNr4Reduction = AddNumeric(nr4, "Reduction", "udWdspNr4Reduction", 22,
                0, 200, 100, 1, 5);
            udWdspNr4Smoothing = AddNumeric(nr4, "Smoothing", "udWdspNr4Smoothing", 51,
                0, 1000, 0, 1, 10);
            udWdspNr4Whitening = AddNumeric(nr4, "Whitening", "udWdspNr4Whitening", 80,
                0, 1000, 0, 1, 10);
            udWdspNr4Rescale = AddNumeric(nr4, "Rescale", "udWdspNr4Rescale", 109,
                0, 120, 20, 1, 5);
            udWdspNr4Threshold = AddNumeric(nr4, "SNR thresh", "udWdspNr4Threshold", 138,
                -100, 100, -100, 1, 5);
            nr4.Controls.Add(NewLabel("Algorithm", 10, 170, 72));
            comboWdspNr4Algorithm = NewCombo("comboWdspNr4Algorithm", 92, 166, 96,
                new string[] { "Algo 1", "Algo 2", "Algo 3" }, 0);
            toolTip1.SetToolTip(comboWdspNr4Algorithm,
                "Algo 1: spectrum; Algo 2: bands; Algo 3: masking.");
            nr4.Controls.Add(comboWdspNr4Algorithm);
            LabelTS units = NewLabel("Reduction / Rescale / SNR: dB\nSmoothing / Whitening: %", 9, 195, 180);
            units.Height = 32;
            nr4.Controls.Add(units);
            ButtonTS resetNr4 = NewButton("Default", 63, 230, 72);
            resetNr4.Click += delegate
            {
                udWdspNr4Reduction.Value = 10.0M;
                udWdspNr4Smoothing.Value = 0.0M;
                udWdspNr4Whitening.Value = 0.0M;
                udWdspNr4Rescale.Value = 2.0M;
                udWdspNr4Threshold.Value = -10.0M;
                comboWdspNr4Algorithm.SelectedIndex = 0;
            };
            nr4.Controls.Add(resetNr4);

            tpWdspNRs.Controls.Add(nr1);
            tpWdspNRs.Controls.Add(nr2);
            tpWdspNRs.Controls.Add(nr3);
            tpWdspNRs.Controls.Add(nr4);

            // Hidden TS control makes the optional model path participate in the
            // existing automatic PowerSDR Options database persistence.
            txtWdspNr3ModelPath = new TextBoxTS();
            txtWdspNr3ModelPath.Name = "txtWdspNr3ModelPath";
            txtWdspNr3ModelPath.Visible = false;
            tpWdspNRs.Controls.Add(txtWdspNr3ModelPath);

            EventHandler changed = delegate { ApplyWdspNrSettings(); };
            comboWdspNrPosition.SelectedIndexChanged += changed;
            udWdspNr1Taps.ValueChanged += changed;
            udWdspNr1Delay.ValueChanged += changed;
            udWdspNr1Gain.ValueChanged += changed;
            udWdspNr1Leakage.ValueChanged += changed;
            comboWdspNr2GainMethod.SelectedIndexChanged += changed;
            comboWdspNr2GainMethod.SelectedIndexChanged += delegate { UpdateWdspNr2ControlState(); };
            comboWdspNr2NpeMethod.SelectedIndexChanged += changed;
            chkWdspNr2Ae.CheckedChanged += changed;
            udWdspNr2TrainThreshold.ValueChanged += changed;
            udWdspNr2TrainT2.ValueChanged += changed;
            chkWdspNr2Post.CheckedChanged += changed;
            chkWdspNr2Post.CheckedChanged += delegate { UpdateWdspNr2ControlState(); };
            udWdspNr2PostLevel.ValueChanged += changed;
            udWdspNr2PostFactor.ValueChanged += changed;
            udWdspNr2PostRate.ValueChanged += changed;
            udWdspNr2PostTaper.ValueChanged += changed;
            chkWdspNr3FixedGain.CheckedChanged += changed;
            udWdspNr4Reduction.ValueChanged += changed;
            udWdspNr4Smoothing.ValueChanged += changed;
            udWdspNr4Whitening.ValueChanged += changed;
            udWdspNr4Rescale.ValueChanged += changed;
            udWdspNr4Threshold.ValueChanged += changed;
            comboWdspNr4Algorithm.SelectedIndexChanged += changed;
            txtWdspNr3ModelPath.TextChanged += delegate
            {
                if (!initializing) ApplyWdspNrModel();
            };

            tcDSP.TabPages.Add(tpWdspNRs);
            UpdateWdspNr2ControlState();
        }

        private void ApplyWdspNrSettings()
        {
            if (initializing || comboWdspNrPosition == null) return;

            DspBackend.SetRx1NrParameters(
                Math.Max(0, comboWdspNrPosition.SelectedIndex),
                Decimal.ToInt32(udWdspNr1Taps.Value),
                Decimal.ToInt32(udWdspNr1Delay.Value),
                Decimal.ToInt32(udWdspNr1Gain.Value),
                Decimal.ToInt32(udWdspNr1Leakage.Value),
                Math.Max(0, comboWdspNr2GainMethod.SelectedIndex),
                Math.Max(0, comboWdspNr2NpeMethod.SelectedIndex),
                chkWdspNr2Ae.Checked,
                Decimal.ToInt32(udWdspNr2TrainThreshold.Value * 10M),
                Decimal.ToInt32(udWdspNr2TrainT2.Value * 100M),
                chkWdspNr2Post.Checked,
                Decimal.ToInt32(udWdspNr2PostLevel.Value),
                Decimal.ToInt32(udWdspNr2PostFactor.Value),
                Decimal.ToInt32(udWdspNr2PostRate.Value),
                Decimal.ToInt32(udWdspNr2PostTaper.Value),
                chkWdspNr3FixedGain.Checked,
                Decimal.ToInt32(udWdspNr4Reduction.Value * 10M),
                Decimal.ToInt32(udWdspNr4Smoothing.Value * 10M),
                Decimal.ToInt32(udWdspNr4Whitening.Value * 10M),
                Decimal.ToInt32(udWdspNr4Rescale.Value * 10M),
                Decimal.ToInt32(udWdspNr4Threshold.Value * 10M),
                Math.Max(0, comboWdspNr4Algorithm.SelectedIndex));
        }

        private void UpdateWdspNr2ControlState()
        {
            bool trained = comboWdspNr2GainMethod != null &&
                comboWdspNr2GainMethod.SelectedIndex == 3;
            if (udWdspNr2TrainThreshold != null) udWdspNr2TrainThreshold.Enabled = trained;
            if (udWdspNr2TrainT2 != null) udWdspNr2TrainT2.Enabled = trained;

            bool enabled = chkWdspNr2Post != null && chkWdspNr2Post.Checked;
            if (udWdspNr2PostLevel != null) udWdspNr2PostLevel.Enabled = enabled;
            if (udWdspNr2PostFactor != null) udWdspNr2PostFactor.Enabled = enabled;
            if (udWdspNr2PostRate != null) udWdspNr2PostRate.Enabled = enabled;
            if (udWdspNr2PostTaper != null) udWdspNr2PostTaper.Enabled = enabled;
        }

        private void WdspNr3LoadModel_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "RNNoise model binary (*.bin)|*.bin";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!ValidateRnnrModel(dialog.FileName))
                {
                    MessageBox.Show(this,
                        "The file is not a valid RNNoise model.",
                        "NR3 - Invalid model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (String.Equals(txtWdspNr3ModelPath.Text, dialog.FileName,
                    StringComparison.OrdinalIgnoreCase)) ApplyWdspNrModel();
                else txtWdspNr3ModelPath.Text = dialog.FileName;
            }
        }

        private void ApplyWdspNrModel()
        {
            if (txtWdspNr3ModelPath == null) return;
            string path = txtWdspNr3ModelPath.Text;
            if (!String.IsNullOrEmpty(path) && !File.Exists(path)) path = String.Empty;
            lblWdspNr3Model.Text = String.IsNullOrEmpty(path)
                ? "Model: Default" : "Model: " + Path.GetFileName(path);
            toolTip1.SetToolTip(lblWdspNr3Model,
                String.IsNullOrEmpty(path) ? "Built-in large RNNoise model." : path);
            DspBackend.LoadRnnrModel(path);
        }

        private static bool ValidateRnnrModel(string path)
        {
            try
            {
                const int blockHeaderSize = 64;
                using (FileStream stream = File.OpenRead(path))
                {
                    long remaining = stream.Length;
                    if (remaining == 0 || remaining % blockHeaderSize != 0) return false;
                    byte[] header = new byte[blockHeaderSize];
                    while (remaining > 0)
                    {
                        if (stream.Read(header, 0, blockHeaderSize) != blockHeaderSize) return false;
                        int size = BitConverter.ToInt32(header, 12);
                        int blockSize = BitConverter.ToInt32(header, 16);
                        if (size < 0 || blockSize < size || blockSize > remaining - blockHeaderSize)
                            return false;
                        if (header[63] != 0) return false;
                        stream.Seek(blockSize, SeekOrigin.Current);
                        remaining -= blockHeaderSize + blockSize;
                    }
                    return remaining == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static GroupBoxTS NewGroup(string text, int x, int y, int width, int height)
        {
            GroupBoxTS group = new GroupBoxTS();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(width, height);
            return group;
        }

        private static LabelTS NewLabel(string text, int x, int y, int width)
        {
            LabelTS label = new LabelTS();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, 18);
            return label;
        }

        private static ComboBoxTS NewCombo(string name, int x, int y, int width,
            string[] items, int selectedIndex)
        {
            ComboBoxTS combo = new ComboBoxTS();
            combo.Name = name;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Location = new Point(x, y);
            combo.Size = new Size(width, 21);
            combo.Items.AddRange(items);
            combo.SelectedIndex = selectedIndex;
            return combo;
        }

        private static ButtonTS NewButton(string text, int x, int y, int width)
        {
            ButtonTS button = new ButtonTS();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 23);
            return button;
        }

        private static NumericUpDownTS AddNumeric(Control parent, string labelText,
            string name, int y, decimal minimum, decimal maximum, decimal value,
            int decimals, decimal increment)
        {
            int fieldX = parent.Width < 170 ? 72 : 92;
            // Labels must stop BEFORE the edit box; overlapping labels hide
            // its leading digits and minus sign even when the value is valid.
            parent.Controls.Add(NewLabel(labelText, 9, y + 3, fieldX - 15));
            NumericUpDownTS numeric = new NumericUpDownTS();
            numeric.Name = name;
            numeric.Location = new Point(fieldX, y);
            numeric.Size = new Size(74, 20);
            numeric.Minimum = minimum / DecimalPow10(decimals);
            numeric.Maximum = maximum / DecimalPow10(decimals);
            numeric.DecimalPlaces = decimals;
            numeric.Increment = increment / DecimalPow10(decimals);
            numeric.Value = value / DecimalPow10(decimals);
            parent.Controls.Add(numeric);
            return numeric;
        }

        private static NumericUpDownTS AddCompactNumeric(Control parent, string labelText,
            string name, int x, int y, decimal minimum, decimal maximum, decimal value)
        {
            parent.Controls.Add(NewLabel(labelText, x, y + 3, 42));
            NumericUpDownTS numeric = new NumericUpDownTS();
            numeric.Name = name;
            numeric.Location = new Point(x + 43, y);
            numeric.Size = new Size(50, 20);
            numeric.Minimum = minimum;
            numeric.Maximum = maximum;
            numeric.Value = value;
            parent.Controls.Add(numeric);
            return numeric;
        }

        private static decimal DecimalPow10(int power)
        {
            decimal value = 1M;
            for (int i = 0; i < power; i++) value *= 10M;
            return value;
        }
    }
}
