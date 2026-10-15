using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSDR
{
    public partial class Setup
    {
        private readonly NumericUpDownTS[,] nnrControls = new NumericUpDownTS[2, 7];
        private bool nnrUpdating;

        private void InitializeNnrTab()
        {
            var page = new TabPage { Name = "tpWdspNNR", Text = "NNR", AutoScroll = true,
                UseVisualStyleBackColor = true };
            page.Controls.Add(new LabelTS { Text = "WDSP 2.10 - NR5 Standard / NR6 Premium (higher CPU usage)",
                Location = new Point(10, 8), Size = new Size(560, 22) });
            for (int receiver = 0; receiver < 2; receiver++)
                page.Controls.Add(CreateNnrGroup(receiver));
            page.Controls.Add(new LabelTS { Text = "Pre/Post-AGC position: DSP > NRs. Select NR5 or NR6 with the NR button.",
                Location = new Point(10, 282), Size = new Size(555, 28) });
            // Rebuild the collection before the native tab handle exists, as
            // Common.TabControlInsert does for the legacy ExtCtrl page.
            var pages = new System.Collections.Generic.List<TabPage>();
            foreach (TabPage existing in tcDSP.TabPages)
            {
                pages.Add(existing);
                if (existing == tpWdspNRs) pages.Add(page);
            }
            tcDSP.TabPages.Clear();
            tcDSP.TabPages.AddRange(pages.ToArray());
        }

        private GroupBoxTS CreateNnrGroup(int receiver)
        {
            var group = new GroupBoxTS { Text = receiver == 0 ? "RX1 / RX-S" : "RX2",
                Location = new Point(8 + receiver * 284, 30), Size = new Size(278, 248) };
            string[] labels = { "Mask floor (dB)", "Maximum gain (dB)", "Low-mask alpha", "Alpha knee (dB)",
                "Adaptation time (s)", "Attack (ms)", "Release (ms)" };
            decimal[] min = { -60, 0, 0, 0, .05m, 0, 0 };
            decimal[] max = { 0, 24, 4, 40, 30, 500, 500 };
            decimal[] defaults = { -25, 12, 1, 10, 2, 0, 0 };
            string[] tips = {
                "Less negative values preserve more voice and noise; more negative values suppress harder.",
                "Maximum gain after neural masking. Lower values can reduce harsh peaks.",
                "Values below 1 preserve weak voice components; above 1 suppress harder.",
                "Level below which Low-mask alpha is applied.",
                "Time constant of the neural input conditioner.",
                "Neural mask attack smoothing. Small values can reduce rough edges.",
                "Neural mask release smoothing. Higher values can reduce musical artifacts." };
            for (int i = 0; i < 7; i++)
            {
                group.Controls.Add(new LabelTS { Text = labels[i], Location = new Point(9, 24 + i * 26),
                    Size = new Size(158, 22) });
                var control = new NumericUpDownTS { Name = "udWdspNnr" + receiver + "Param" + i,
                    Location = new Point(174, 21 + i * 26), Size = new Size(90, 22),
                    Minimum = min[i], Maximum = max[i], Value = defaults[i],
                    DecimalPlaces = i == 2 || i == 4 ? 2 : 0,
                    Increment = i == 2 || i == 4 ? .05m : 1m };
                nnrControls[receiver, i] = control;
                toolTip1.SetToolTip(control, tips[i]);
                control.ValueChanged += delegate { ApplyNnrSettings(); };
                group.Controls.Add(control);
            }
            var gentle = new ButtonTS { Text = "Voice smooth", Location = new Point(12, 211), Size = new Size(120, 27) };
            var reset = new ButtonTS { Text = "Default", Location = new Point(144, 211), Size = new Size(120, 27) };
            gentle.Click += delegate { SetNnrPreset(receiver, new decimal[] { -18, 6, .75m, 12, 3, 20, 120 }); };
            reset.Click += delegate { SetNnrPreset(receiver, defaults); };
            group.Controls.Add(gentle);
            group.Controls.Add(reset);
            return group;
        }

        private void SetNnrPreset(int receiver, decimal[] values)
        {
            nnrUpdating = true;
            try { for (int i = 0; i < 7; i++) nnrControls[receiver, i].Value = values[i]; }
            finally { nnrUpdating = false; }
            ApplyNnrSettings();
        }

        private void ApplyNnrSettings()
        {
            if (initializing || nnrUpdating || nnrControls[1, 6] == null) return;
            for (int receiver = 0; receiver < 2; receiver++)
            {
                double[] values = new double[7];
                for (int i = 0; i < 7; i++) values[i] = (double)nnrControls[receiver, i].Value;
                DspBackend.SetNnrParameters(receiver, values);
            }
        }

        // Existing console shortcuts use the original tab indexes.
        internal void SelectLegacySetupTab(int index)
        {
            tcSetup.SelectedIndex = index;
        }
    }
}
