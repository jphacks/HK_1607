using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;

namespace e_mo
{
    static class Program
    {
        public static string argData;
        /// <summary>
        /// Main
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // URLを受け取る
            Console.WriteLine("args.length:" + args.Length);
            if (args.Length > 0)
            {
                string arg1 = args[0]; // URL全体を取得
                string param = Regex.Match(arg1, @"\+?:(.*)").Groups[1].Value; //パラメータの取り出し
                if (Regex.IsMatch(arg1, "[&=]"))
                {
                    arg1 = HttpUtility.UrlDecode(arg1); // URLデコードする
                    Console.WriteLine(arg1);
                }
                string[] argwork = arg1.Split(':');
                argData = argwork[1];
            }
            //argData = "s1234"; // LocalTest用
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            PXCMSession session = PXCMSession.CreateInstance();
            if (session != null)
            {
                Application.Run(new MainForm(session));
                session.Dispose();
            }
        }
    }
}
