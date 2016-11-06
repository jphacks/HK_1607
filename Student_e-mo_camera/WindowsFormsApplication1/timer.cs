using System.Runtime.InteropServices;

namespace e_mo
{
    class FPSTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long data);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long data);

        private MainForm form;
        private long freq, last;
        private int fps;
        private int cnt;
        
        public FPSTimer(MainForm mf)
        {
            form = mf;
            QueryPerformanceFrequency(out freq);
            fps = 0;
            QueryPerformanceCounter(out last);
            cnt = 0;
        }

        public void Tick(string text)
        {
            long now;
            QueryPerformanceCounter(out now);
            fps++;
            cnt++;
            if (now - last > freq) // update every second
            {
                last = now;
                form.UpdateStatus(text + " FPS=" + fps, MainForm.Label.StatusLabel);
                fps = 0;

            }
            //フレームで送信を変える
            //if (!form.Start.Enabled && cnt > 3)
            //{
            //    form.RunExpression();
            //    cnt = 0;
            //}
        }
    }
}
