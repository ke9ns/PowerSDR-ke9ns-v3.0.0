using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Concurrent;

namespace PowerSDR
{
    public partial class CWX
    {
        private NeuralCwDecoder cwNeural;
        private CheckBox cwNeuralOn;
        private NumericUpDown cwNeuralGate;
        private Label cwNeuralStatus;
        private TextBox cwNeuralText;
        private Timer cwNeuralTimer;
        private readonly ConcurrentQueue<string> cwNeuralMessages = new ConcurrentQueue<string>();

        private void InitializeNeuralCwPanel()
        {
            label4.Visible = label5.Visible = label6.Visible = label7.Visible = false;
            // Keep KE9NS's existing transmit editor intact; append the same RX
            // controls used in Thetis CWX below it rather than replacing TX controls.
            var panel = new GroupBox { Text = "CW neural RX (RX1)", Location = new Point(12, 330), Size = new Size(701, 120) };
            cwNeuralOn = new CheckBox { Text = "Enable", Location = new Point(10, 20), AutoSize = true };
            var gateLabel = new Label { Text = "Gate", Location = new Point(95, 23), AutoSize = true };
            cwNeuralGate = new NumericUpDown { Location = new Point(133, 19), Width = 58,
                Minimum = 2, Maximum = 20, DecimalPlaces = 1, Increment = 0.5M, Value = 4 };
            cwNeuralStatus = new Label { Text = "Off", Location = new Point(202, 23), Size = new Size(395, 20) };
            var clear = new Button { Text = "Clear RX", Location = new Point(610, 17), Size = new Size(75, 24) };
            cwNeuralText = new TextBox { Location = new Point(10, 48), Size = new Size(675, 60),
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(14, 23, 28), ForeColor = Color.LightGreen };
            panel.Controls.AddRange(new Control[] { cwNeuralOn, gateLabel, cwNeuralGate, cwNeuralStatus, clear, cwNeuralText });
            Controls.Add(panel);
            ClientSize = new Size(Math.Max(ClientSize.Width, 725), 462);
            clear.Click += delegate { string ignored; while (cwNeuralMessages.TryDequeue(out ignored)) { } cwNeuralText.Clear(); };
            cwNeuralGate.ValueChanged += delegate { if (cwNeural != null) cwNeural.KeyingGate = (double)cwNeuralGate.Value; };
            cwNeuralOn.CheckedChanged += delegate
            {
                if (!cwNeuralOn.Checked) { StopNeuralCw(); cwNeuralStatus.Text = "Off"; return; }
                try
                {
                    cwNeural = new NeuralCwDecoder(Audio.SampleRate1, (double)cwNeuralGate.Value);
                    cwNeural.TextReceived += delegate(string text) { if (cwNeuralMessages.Count < 128) cwNeuralMessages.Enqueue(text); };
                    cwNeural.Start();
                    DspBackend.CwAudioSink = cwNeural.Capture;
                    cwNeuralTimer.Start();
                }
                catch (Exception ex) { StopNeuralCw(); cwNeuralOn.Checked = false; cwNeuralStatus.Text = ex.Message; }
            };
            cwNeuralTimer = new Timer { Interval = 500 };
            cwNeuralTimer.Tick += delegate
            {
                string text;
                while (cwNeuralMessages.TryDequeue(out text)) cwNeuralText.AppendText(text);
                if (cwNeuralText.TextLength > 16000) cwNeuralText.Text = cwNeuralText.Text.Substring(cwNeuralText.TextLength - 8000);
                if (cwNeural == null) return;
                if (cwNeural.Error != null) { DspBackend.CwAudioSink = null; cwNeuralStatus.Text = cwNeural.Error; return; }
                bool cw = console.RX1DSPMode == DSPMode.CWL || console.RX1DSPMode == DSPMode.CWU;
                cwNeuralStatus.Text = !cw || console.MOX ? "Select CWL/CWU on RX1; TX pauses decoding" :
                    !cwNeural.HasFullWindow ? "Buffering RX audio (6 s)" :
                    String.Format("{0} | {1:0} Hz | key {2:0.0}/{3:0.0}",
                        cwNeural.KeyingRatio >= cwNeural.KeyingGate ? "Listening" : "Below gate",
                        cwNeural.ToneHz, cwNeural.KeyingRatio, cwNeural.KeyingGate);
            };
        }

        private void StopNeuralCw()
        {
            DspBackend.CwAudioSink = null;
            if (cwNeuralTimer != null) cwNeuralTimer.Stop();
            if (cwNeural != null) { cwNeural.Dispose(); cwNeural = null; }
            string ignored; while (cwNeuralMessages.TryDequeue(out ignored)) { }
        }

        private void DisposeNeuralCwPanel()
        {
            StopNeuralCw();
            if (cwNeuralTimer != null) { cwNeuralTimer.Dispose(); cwNeuralTimer = null; }
        }
    }
}
