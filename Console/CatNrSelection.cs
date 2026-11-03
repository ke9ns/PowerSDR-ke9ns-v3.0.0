using System;
namespace PowerSDR
{
    public partial class Console
    {
        internal int CatNrSelection(bool secondReceiver, int? value)
        {
            if (value.HasValue && (value.Value < 0 || value.Value > 6))
                throw new ArgumentOutOfRangeException("value");
            if (IsDisposed || Disposing || !IsHandleCreated)
                throw new InvalidOperationException("Console is not ready for CAT.");
            if (InvokeRequired)
                return (int)Invoke(new Func<bool, int?, int>(CatNrSelection), secondReceiver, value);
            if (value.HasValue)
            {
                if (secondReceiver) ApplyExperimentalRX2NRMode(value.Value);
                else ApplyExperimentalRX1NRMode(value.Value);
            }
            return secondReceiver ? experimental_rx2_nr_mode : experimental_rx1_nr_mode;
        }
    }
}
