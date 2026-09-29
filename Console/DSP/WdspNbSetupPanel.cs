using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSDR
{
    public partial class Setup
    {
        private NumericUpDownTS[] wdspNb1Controls, wdspNb2Controls;
        private ComboBoxTS comboWdspNb2Mode;

        private void InitializeWdspNbOptions()
        {
            // Preserve the original controls/database keys for legacy fallback,
            // but do not present two different NR or NB editors in WDSP mode.
            if (!DspBackend.ExperimentalWdspRequested) return;
            tpDSPOptions.SuspendLayout();
            tpDSPOptions.AutoScroll = true;
            grpDSPLMSNR.Visible = false;
            grpDSPNB.Visible = false;
            grpDSPNB2.Visible = false;
            btnRSTNR.Visible = btnRSTNB.Visible = btnRSTNB2.Visible = false;
            labelTS28.Visible = false;

            GroupBoxTS nb1 = NewGroup("NB1", 8, 8, 144, 236);
            nb1.Name = "grpWdspNb1";
            GroupBoxTS nb2 = NewGroup("NB2", 158, 8, 152, 236);
            nb2.Name = "grpWdspNb2";
            wdspNb1Controls = AddWdspNbControls(nb1, "1");
            wdspNb2Controls = AddWdspNbControls(nb2, "2");
            nb2.Controls.Add(NewLabel("Mode", 8, 157, 130));
            comboWdspNb2Mode = NewCombo("comboWdspNb2Mode", 8, 175, 136,
                new string[] { "Zero", "Sample hold", "Mean hold", "Hold sample", "Interpolation" }, 0);
            nb2.Controls.Add(comboWdspNb2Mode);
            toolTip1.SetToolTip(comboWdspNb2Mode, "Select how NB2 replaces detected impulse samples. Interpolation uses a linear transition.");
            ButtonTS reset1 = NewButton("Default", 36, 205, 72);
            ButtonTS reset2 = NewButton("Default", 40, 205, 72);
            reset1.Click += delegate { ResetWdspNb(wdspNb1Controls); };
            reset2.Click += delegate { ResetWdspNb(wdspNb2Controls); comboWdspNb2Mode.SelectedIndex = 0; };
            nb1.Controls.Add(reset1); nb2.Controls.Add(reset2);
            LabelTS nb1Note = NewLabel("Impulse blanking", 8, 168, 128);
            nb1.Controls.Add(nb1Note);
            tpDSPOptions.Controls.Add(nb1); tpDSPOptions.Controls.Add(nb2);

            grpDSPBufferSize.Location = new Point(316, 8);
            grpDSPLMSANF.Location = new Point(442, 8);
            grpDSPLMSANF.Width = 144;
            foreach (Control c in grpDSPLMSANF.Controls)
                if (c is NumericUpDown) c.Width = 78;
            btnRSTANF.Text = "Default";
            btnRSTANF.Location = new Point(478, 142);
            btnRSTANF.Size = new Size(72, 23);
            chkDSPRX2.Location = new Point(442, 176);
            chkDSPRX2.Size = new Size(144, 58);
            chkDSPRX2.Text = "Share legacy settings with RX2";
            toolTip1.SetToolTip(chkDSPRX2,
                "Shares legacy DSP parameters, including ANF, with RX2. WDSP NR and NB parameters are always shared; enable buttons remain independent.");
            grpDSPWindow.Location = new Point(442, 254);
            grpDSPWindow.Width = 144;
            chkDSPTXMeterPeak.Location = new Point(8, 274);
            chkDSPTXMeterPeak.Size = new Size(190, 32);
            labelTS7.Location = new Point(208, 274);
            udTNFWidth.Location = new Point(310, 280);
            LabelTS scope = NewLabel("NB1/NB2: RX1, RX-S and RX2. Times in ms.", 8, 250, 330);
            tpDSPOptions.Controls.Add(scope);
            comboWdspNb2Mode.SelectedIndexChanged += delegate { ApplyWdspNbSettings(); };
            tpDSPOptions.ResumeLayout(false);
        }

        private NumericUpDownTS[] AddWdspNbControls(GroupBoxTS group, string id)
        {
            string[] captions = { "Threshold", "Slew (ms)", "Lead (ms)", "Hang (ms)", "Avg (ms)" };
            string[] keys = { "Threshold", "Slew", "Lead", "Hang", "Average" };
            string[] tips = {
                "Impulse amplitude relative to the moving average. Lower values blank more aggressively.",
                "Transition time between normal signal and blanking (0 to 2 ms).",
                "Advance blanking before a detected impulse (0 to 2 ms).",
                "Continue blanking after a detected impulse (0 to 2 ms).",
                "Time constant of the background amplitude estimate (1 to 1000 ms)."
            };
            NumericUpDownTS[] controls = new NumericUpDownTS[5];
            for (int i = 0; i < controls.Length; i++)
            {
                int y = 22 + 27 * i;
                group.Controls.Add(NewLabel(captions[i], 8, y + 3, 62));
                NumericUpDownTS n = new NumericUpDownTS();
                n.Name = "udWdspNb" + id + keys[i];
                n.Location = new Point(74, y); n.Size = new Size(group.Width - 82, 20);
                n.DecimalPlaces = i == 0 || i == 4 ? 1 : 2;
                n.Minimum = i == 0 || i == 4 ? 1 : 0;
                n.Maximum = i == 0 ? 10000 : i == 4 ? 1000 : 2;
                n.Increment = i == 0 || i == 4 ? 1 : 0.01M;
                n.Value = i == 0 ? 30 : i == 4 ? 50 : 0.10M;
                toolTip1.SetToolTip(n, tips[i]);
                group.Controls.Add(n); controls[i] = n;
                n.ValueChanged += delegate { ApplyWdspNbSettings(); };
            }
            return controls;
        }

        private void ResetWdspNb(NumericUpDownTS[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
                controls[i].Value = i == 0 ? 30 : i == 4 ? 50 : 0.10M;
        }

        private void ApplyWdspNbSettings()
        {
            if (initializing || wdspNb1Controls == null || wdspNb2Controls == null || comboWdspNb2Mode == null) return;
            DspBackend.Nb1Settings = ReadWdspNb(wdspNb1Controls, 0);
            DspBackend.Nb2Settings = ReadWdspNb(wdspNb2Controls, Math.Max(0, comboWdspNb2Mode.SelectedIndex));
        }

        private WdspBlankerSettings ReadWdspNb(NumericUpDownTS[] c, int mode)
        {
            return new WdspBlankerSettings((double)c[0].Value, (double)c[1].Value / 1000,
                (double)c[2].Value / 1000, (double)c[3].Value / 1000, (double)c[4].Value / 1000, mode);
        }
    }
}
