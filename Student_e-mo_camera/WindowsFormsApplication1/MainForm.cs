using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;

namespace e_mo
{
    public partial class MainForm : Form
    {
        /* 列挙型のLabel, 下に出るStoppedとかかな？ */
        public enum Label
        {
            StatusLabel,
            AlertsLabel,
            ReportLabel
        };

        

        public PXCMSession Session; // カメラセンサーとのセッション
        public volatile bool Stopped = false; // カメラの稼働状態保持
        /* ProfileメニューのItemリストの辞書 */
        public readonly Dictionary<PXCMFaceConfiguration.TrackingModeType, string> FaceModesMap =
        new Dictionary<PXCMFaceConfiguration.TrackingModeType, string>()
        {
          { PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH , "3D Tracking" },
          { PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR, "2D Tracking" },
          { PXCMFaceConfiguration.TrackingModeType.FACE_MODE_IR, "IR Tracking" },
          { PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_STILL, "2D Still" }
        };

        private readonly object m_bitmapLock = new object();
        private readonly FaceTextOrganizer m_faceTextOrganizer; // 画面に出力する文字を管理しているクラス
        private Dictionary<String, ModuleSettings> m_moduleSettings; // 画面右から設定できる情報を保持
        private IEnumerable<CheckBox> m_modulesCheckBoxes; // 画面右に設置されるCheckBoxのインスタンスをリストで保持
        private Bitmap m_bitmap;
        private Tuple<PXCMImage.ImageInfo, PXCMRangeF32> m_selectedColorResolution; // Colorメニューの選択中Itemを持つ
        private volatile bool m_closing; // ウィンドウの閉じるフラグ??
        private static ToolStripMenuItem m_deviceMenuItem; // DeviceメニューのItemリスト
        private static ToolStripMenuItem m_moduleMenuItem; // ModuleメニューのItemリスト

        public MainForm(PXCMSession session)
        {
            InitializeComponent();

            m_faceTextOrganizer = new FaceTextOrganizer(); // インスタンス生成
            m_deviceMenuItem = new ToolStripMenuItem("Device"); // Deviceメニューのインスタンス生成
            m_moduleMenuItem = new ToolStripMenuItem("Module"); // Moduleメニューのインスタンス生成
            Session = session;

            CreateResolutionMap();
            PopulateDeviceMenu();
            PopulateModuleMenu();
            PopulateProfileMenu();

            InitializeUserSettings();
            InitializeCheckboxes();
            DisableUnsupportedAlgos();

            FormClosing += MainForm_FormClosing;
            Panel.Paint += Panel_Paint;

            // deviceが一つの時は自動スタート
            if(m_deviceMenuItem.DropDownItems.Count == 1)
            {
                Start_Click(null, null); // 起動時にカメラをonにする
            
            }// else if(m_deviceMenuItem.DropDownItems.Count == 0)
            //{
            //    UpdateStatus("カメラを接続\nしてください。", Label.SmileLabel);
            //} else
            //{
            //    UpdateStatus("Deviceから\nカメラを選択\nしてください。", Label.SmileLabel);
            //}
        }

        /*
         * 設定情報の初期化処理
         * 設定系で最初に必要な処理をとりあえずやってくれる
         */
        private void InitializeUserSettings()
        {
            m_moduleSettings = new Dictionary<String, ModuleSettings>(); // 設定情報を保持する変数の初期化

            /* 画面右から設定できる情報の一覧を持つリストを生成 */
            var moduleList = new List<String>
            {
            };

            SetDefaultSettings(); // デバイス名に応じて設定情報のオンオフを切り替えるメソッド
        }

        /*
         * DeviceメニューのチェックされたItemに該当するデバイス名を返す
         */
        private string GetDeviceName()
        {
            var deviceMenuString = GetCheckedDevice(); // DeviceメニューのチェックされたItemを取得

            /* チェックされたItemがNULLだった場合 */
            if (deviceMenuString == null)
                return "InvalidCamera";

            /* チェックされたItemがR200 または DS4だった場合 */
            if (deviceMenuString.Contains("R200") || deviceMenuString.Contains("DS4"))
            {
                return "DS4";
            }

            return "IVcam"; // いずれも該当しなかった場合、普通のカメラ
        }

        /*
         * カメラから判断してサポート外の機能無効化？
         */
        private void DisableUnsupportedAlgos()
        {
            string deviceMenuString = GetCheckedDevice();
        }

        /*
         * 画面右のチェックボックスの初期化処理
         */
        private void InitializeCheckboxes()
        {
            /* 各CheckBoxのインスタンスを持つリストを生成 */
            m_modulesCheckBoxes = new List<CheckBox>
            {
            };
        }

        public Dictionary<string, PXCMCapture.DeviceInfo> Devices { get; set; }
        public Dictionary<string, IEnumerable<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>> ColorResolutions { get; set; }

        private readonly List<Tuple<int, int>> SupportedColorResolutions = new List<Tuple<int, int>>
  {
  Tuple.Create(1920, 1080),
  Tuple.Create(1280, 720),
  Tuple.Create(960, 540),
  Tuple.Create(640, 480),
  Tuple.Create(640, 360),
  };

        private void CreateResolutionMap()
        {
            ColorResolutions = new Dictionary<string, IEnumerable<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>>();
            var desc = new PXCMSession.ImplDesc
            {
                group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR,
                subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE
            };

            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMCapture capture;
                if (Session.CreateImpl(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;

                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo info;
                    if (capture.QueryDeviceInfo(j, out info) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                    PXCMCapture.Device device = capture.CreateDevice(j);
                    if (device == null)
                    {
                        throw new Exception("PXCMCapture.Device null");
                    }
                    var deviceResolutions = new List<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>();

                    for (int k = 0; k < device.QueryStreamProfileSetNum(PXCMCapture.StreamType.STREAM_TYPE_COLOR); k++)
                    {
                        PXCMCapture.Device.StreamProfileSet profileSet;
                        device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_COLOR, k, out profileSet);
                        PXCMCapture.DeviceInfo dinfo;
                        device.QueryDeviceInfo(out dinfo);

                        var currentRes = new Tuple<PXCMImage.ImageInfo, PXCMRangeF32>(profileSet.color.imageInfo, profileSet.color.frameRate);

                        if (IsProfileSupported(profileSet, dinfo))
                            continue;

                        if (SupportedColorResolutions.Contains(new Tuple<int, int>(currentRes.Item1.width, currentRes.Item1.height)))
                            deviceResolutions.Add(currentRes);

                    }

                    try
                    {
                        ColorResolutions.Add(info.name, deviceResolutions);
                    }
                    catch (Exception e)
                    {
                        e.ToString();
                    }
                    device.Dispose();
                }

                capture.Dispose();
            }
        }

        private static bool IsProfileSupported(PXCMCapture.Device.StreamProfileSet profileSet, PXCMCapture.DeviceInfo dinfo)
        {
            return
            (profileSet.color.frameRate.min < 30) ||
            (dinfo != null && dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_DS4 &&
            (profileSet.color.imageInfo.width == 1920 || profileSet.color.frameRate.min > 30 || profileSet.color.imageInfo.format == PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2)) ||
            (profileSet.color.options == PXCMCapture.Device.StreamOption.STREAM_OPTION_UNRECTIFIED);
        }

        public void PopulateDeviceMenu()
        {
            Devices = new Dictionary<string, PXCMCapture.DeviceInfo>();
            var desc = new PXCMSession.ImplDesc
            {
                group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR,
                subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE
            };

            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMCapture capture;
                if (Session.CreateImpl(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;

                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo dinfo;
                    if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                    string name = dinfo.name;
                    if (Devices.ContainsKey(dinfo.name))
                        name += j;
                    Devices.Add(name, dinfo);
                    var sm1 = new ToolStripMenuItem(dinfo.name, null, Device_Item_Click);
                    m_deviceMenuItem.DropDownItems.Add(sm1);
                    sm1.Click += (sender, eventArgs) =>
                    {
                        DisableUnsupportedAlgos();
                    };
                }

                capture.Dispose();
            }

            if (m_deviceMenuItem.DropDownItems.Count > 0)
            {
                ((ToolStripMenuItem)m_deviceMenuItem.DropDownItems[0]).Checked = true;
                PopulateColorResolutionMenu(m_deviceMenuItem.DropDownItems[0].ToString());
            }

            try
            {
                MainMenu.Items.RemoveAt(0);
            }
            catch (NotSupportedException)
            {
                m_deviceMenuItem.Dispose();
                throw;
            }
            MainMenu.Items.Insert(0, m_deviceMenuItem);
        }

        public void PopulateColorResolutionMenu(string deviceName)
        {
            bool foundDefaultResolution = false;
            var sm = new ToolStripMenuItem("Color");
            foreach (var resolution in ColorResolutions[deviceName])
            {
                var resText = PixelFormat2String(resolution.Item1.format) + " " + resolution.Item1.width + "x"
                + resolution.Item1.height + " " + resolution.Item2.max + " fps";
                var sm1 = new ToolStripMenuItem(resText, null);
                var selectedResolution = resolution;
                sm1.Click += (sender, eventArgs) =>
                {
                    m_selectedColorResolution = selectedResolution;
                    ColorResolution_Item_Click(sender);
                };

                sm.DropDownItems.Add(sm1);

                int width = selectedResolution.Item1.width;
                int height = selectedResolution.Item1.height;
                PXCMImage.PixelFormat format = selectedResolution.Item1.format;
                float fps = selectedResolution.Item2.min;

                if (DefaultCameraConfig.IsDefaultDeviceConfig(deviceName, width, height, format, fps))
                {
                    foundDefaultResolution = true;
                    sm1.Checked = true;
                    sm1.PerformClick();
                }
            }

            if (!foundDefaultResolution && sm.DropDownItems.Count > 0)
            {
                ((ToolStripMenuItem)sm.DropDownItems[0]).Checked = true;
                ((ToolStripMenuItem)sm.DropDownItems[0]).PerformClick();
            }

            try
            {
                MainMenu.Items.RemoveAt(1);
            }
            catch (NotSupportedException)
            {
                sm.Dispose();
                throw;
            }
            MainMenu.Items.Insert(1, sm);
        }

        //カメラによってのデフォルトの設定
        public class DefaultCameraConfig
        {
            public static bool IsDefaultDeviceConfig(string deviceName, int width, int height, PXCMImage.PixelFormat format, float fps)
            {
                if (deviceName.Contains("R200"))
                {
                    return width == DefaultDs4Width && height == DefaultDs4Height && format == DefaultDs4PixelFormat && fps == DefaultDs4Fps;
                }

                if (deviceName.Contains("F200") || deviceName.Contains("SR300"))
                {
                    return width == DefaultIvcamWidth && height == DefaultIvcamHeight && format == DefaultIvcamPixelFormat && fps == DefaultIvcamFps;
                }

                return false;
            }

            private static readonly int DefaultDs4Width = 640;
            private static readonly int DefaultDs4Height = 480;
            private static readonly PXCMImage.PixelFormat DefaultDs4PixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32;
            private static readonly float DefaultDs4Fps = 30f;

            private static readonly int DefaultIvcamWidth = 640;
            private static readonly int DefaultIvcamHeight = 360;
            private static readonly PXCMImage.PixelFormat DefaultIvcamPixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2;
            private static readonly float DefaultIvcamFps = 30f;
        }

        //Moduleメニュー関連
        private void PopulateModuleMenu()
        {
            var desc = new PXCMSession.ImplDesc();
            desc.cuids[0] = PXCMFaceModule.CUID;

            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                var mm1 = new ToolStripMenuItem(desc1.friendlyName, null, Module_Item_Click);
                m_moduleMenuItem.DropDownItems.Add(mm1);
            }
            if (m_moduleMenuItem.DropDownItems.Count > 0)
                ((ToolStripMenuItem)m_moduleMenuItem.DropDownItems[0]).Checked = true;
            try
            {
                MainMenu.Items.RemoveAt(2);
            }
            catch (NotSupportedException)
            {
                m_moduleMenuItem.Dispose();
                throw;
            }
            MainMenu.Items.Insert(2, m_moduleMenuItem);

        }

        /*
         * Profileメニュー関連
         * いったんスルー
         */
        private void PopulateProfileMenu()
        {
            var pm = new ToolStripMenuItem("Profile");

            foreach (
            var trackingMode in FaceModesMap.Keys)
            {
                var pm1 = new ToolStripMenuItem(FaceModesMap[trackingMode], null, Profile_Item_Click);
                pm.DropDownItems.Add(pm1);

                if (trackingMode == PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR) //2d = defaultに変更
                {
                    pm1.Checked = true;
                }
            }
            try
            {
                MainMenu.Items.RemoveAt(3);
            }
            catch (NotSupportedException)
            {
                pm.Dispose();
                throw;
            }
            MainMenu.Items.Insert(3, pm);
        }

        /*
         * Colorメニュー関連
         * いったんスルー
         */
        private static string PixelFormat2String(PXCMImage.PixelFormat format)
        {
            switch (format)
            {
                case PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2:
                    return "YUY2";
                case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32:
                    return "RGB32";
                case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24:
                    return "RGB24";
            }
            return "NA";
        }

        /*
         * メニューのItemの状態が変化した際に呼ばれる(各メニューのクリックイベントから)
         */
        private void RadioCheck(object sender, string name)
        {
            /* メニューバーに設置された各メニューを巡回 */
            foreach (ToolStripMenuItem m in MainMenu.Items)
            {
                if (!m.Text.Equals(name)) continue; // 受け取ったメニュー名と一致しなければやり直し
                /* 一致したメニューのItemを巡回 */
                foreach (ToolStripMenuItem e1 in m.DropDownItems)
                {
                    e1.Checked = (sender == e1); // クリックイベントでとれた最新の状態に更新
                }
            }
        }

        /*
         * ColorメニューのItemがクリックされた時呼ばれる
         */
        private void ColorResolution_Item_Click(object sender)
        {
            RadioCheck(sender, "Color");
        }

        /*
         * ColorメニューのItemがクリックされた時呼ばれる
         * 2行目は後回し
         */
        private void Device_Item_Click(object sender, EventArgs e)
        {
            PopulateColorResolutionMenu(sender.ToString());
            RadioCheck(sender, "Device");
        }

        /*
         * ModuleメニューのItemがクリックされた時呼ばれる
         * 2行目は後回し
         */
        private void Module_Item_Click(object sender, EventArgs e)
        {
            RadioCheck(sender, "Module");
            PopulateProfileMenu();
        }

        /*
         * ProfileメニューのItemがクリックされた時呼ばれる
         */
        private void Profile_Item_Click(object sender, EventArgs e)
        {
            RadioCheck(sender, "Profile");
        }

        /*
         * スタートボタンがクリックされた時呼ばれる
         */
        private void Start_Click(object sender, EventArgs e)
        {
            MainMenu.Enabled = false; // メイン画面の機能を無効化
            Start.Enabled = false; // スタートボタンを無効化
            Stop.Enabled = true; // ストップボタンを有効化

            Stopped = false; // ストップフラグをオフに

            /* DoTrackingを新規スレッドで実行 */
            var thread = new Thread(DoTracking);
            thread.Start();
        }

        /*
         * FaceTracking->SimplePipelineの呼び出し
         */
        private void DoTracking()
        {
            var ft = new FaceTracking(this); // FaceTrackingのインスタンス生成
            ft.SimplePipeline(); // SimplePipeline呼び出し

            /* SimplePipelineの処理が終了した後(ストップとか)に実行される処理 */
            Invoke(new DoTrackingCompleted(() =>
            {
                DisableUnsupportedAlgos(); // サポート外の機能の無効化

                MainMenu.Enabled = true; // メイン画面を有効化
                Start.Enabled = true; // スタートボタンを有効化
                Stop.Enabled = false; // ストップボタンを無効化

                // Close()は標準搭載
                if (m_closing) Close(); // ウィンドウ閉じるフラグがオンの場合、ウィンドウを閉じる
            }));
        }

        /*
         * ストップボタンがクリックされた時呼ばれる
         */
        private void Stop_Click(object sender, EventArgs e)
        {
            Stopped = true; // ストップフラグをオンに
            UpdateStatus("カメラ停止中", Label.ReportLabel);
        }

        /*
         * DeviceメニューのチェックされているItemを返す
         */
        public string GetCheckedDevice()
        {
            return (from ToolStripMenuItem m in MainMenu.Items
                    where m.Text.Equals("Device")
                    from ToolStripMenuItem e in m.DropDownItems
                    where e.Checked
                    select e.Text).FirstOrDefault();
        }

        public PXCMCapture.DeviceInfo GetCheckedDeviceInfo()
        {
            foreach (ToolStripMenuItem m in MainMenu.Items)
            {
                if (!m.Text.Equals("Device")) continue;
                for (int i = 0; i < m.DropDownItems.Count; i++)
                {
                    if (((ToolStripMenuItem)m.DropDownItems[i]).Checked)
                        return Devices.ElementAt(i).Value;
                }
            }
            return new PXCMCapture.DeviceInfo();
        }

        /*
        * よくわからない
        * Colorメニューのチェックがついた項目を返す??
        */
        public Tuple<PXCMImage.ImageInfo, PXCMRangeF32> GetCheckedColorResolution()
        {
            return m_selectedColorResolution;
        }

        /*
        * Moduleメニューのチェックがついた項目のTextを返す
        */
        public string GetCheckedModule()
        {
            return (from ToolStripMenuItem m in MainMenu.Items
                    where m.Text.Equals("Module")
                    from ToolStripMenuItem e in m.DropDownItems
                    where e.Checked
                    select e.Text).FirstOrDefault();
        }

        /*
        * Profileメニューのチェックがついた項目のTextを返す
        */
        public string GetCheckedProfile()
        {
            /* ProfileメニューのItemを順に見ていく */
            foreach (var m in from ToolStripMenuItem m in MainMenu.Items where m.Text.Equals("Profile") select m)
            {
                for (var i = 0; i < m.DropDownItems.Count; i++)
                {
                    /* チェックがついている場合値を返して処理を終了 */
                    if (((ToolStripMenuItem)m.DropDownItems[i]).Checked)
                        return m.DropDownItems[i].Text;
                }
            }
            return "";
        }

        /*
         * たぶんウィンドウ閉じる時に呼ばれる
         */
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stopped = true; // ストップフラグをオンに
            e.Cancel = Stop.Enabled;
            m_closing = true;
        }

        /*
        * よくわからない
        * ステータスの更新
        * 下のStoppedとかのことかな？
        */
        public void UpdateStatus(string status, Label label)
        {
            if (label == Label.StatusLabel)
                Status.Invoke(new UpdateStatusDelegate(delegate (string s) { StatusLabel.Text = s; }),
                new object[] { status });

            if (label == Label.AlertsLabel)
                Status.Invoke(new UpdateStatusDelegate(delegate (string s) { AlertsLabel.Text = s; }),
                new object[] { status });

            if (label == Label.ReportLabel)
                Status.Invoke(new UpdateStatusDelegate(delegate (string s) { ReportLabel.Text = s; }),
                    new object[] { status });
        }

        // 画像のスケールの設定
        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            lock (m_bitmapLock)
            {
                if (m_bitmap == null) return;
                e.Graphics.DrawImage(m_bitmap, Panel.ClientRectangle);
            }
        }

        //画像表示panelの更新
        public void UpdatePanel()
        {
            Panel.Invoke(new UpdatePanelDelegate(() => Panel.Invalidate()));
        }

        //画像の反映
        public void DrawBitmap(Bitmap picture)
        {
            lock (m_bitmapLock)
            {
                if (m_bitmap != null)
                {
                    m_bitmap.Dispose();
                }
                m_bitmap = new Bitmap(picture);
            }
        }

        /*
         * カメラの映像に変化が起きた際に、FaceTracking->SimplePipelineから呼ばれる
         * FaceTextOrganizer->ChangeFaceの呼び出し
         * DrawLocationやDraLandmark,DrawExpressionの呼び出し
         */
        public void DrawGraphics(PXCMFaceData moduleOutput)
        {
            Debug.Assert(moduleOutput != null);
            /* カメラから検出された人数分繰り返し */
            for (var i = 0; i < moduleOutput.QueryNumberOfDetectedFaces(); i++)
            {
                PXCMFaceData.Face face = moduleOutput.QueryFaceByIndex(i);
                if (face == null) throw new Exception("DrawGraphics::PXCMFaceData.Face null"); // 顔が検出できない場合Exeption

                lock (m_bitmapLock)
                {
                    // i + 1 人目, 顔情報(使えなさそうな文字列), 映像の幅と高さ
                    m_faceTextOrganizer.ChangeFace(i, face, m_bitmap.Height, m_bitmap.Width);
                }
                DrawExpressions(face);
            }
        }

        private delegate string GetFileDelegate();

        #region Modules Drawing

        private static readonly Assembly m_assembly = Assembly.GetExecutingAssembly();

        private readonly ResourceSet m_resources =
        new ResourceSet(m_assembly.GetManifestResourceStream(@"e_mo.Properties.Resources.resources"));

        /* 表情認識のパラメータ一覧(文字列)を持った辞書 */
        private readonly Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, string> m_expressionDictionary =
        new Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, string>
        {
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, @"MouthOpen"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, @"Smile"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS, @"Kiss"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_UP, @"Eyes_Turn_Up"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_DOWN, @"Eyes_Turn_Down"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_LEFT, @"Eyes_Turn_Left"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_RIGHT, @"Eyes_Turn_Right"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT, @"Eyes_Closed_Left"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT, @"Eyes_Closed_Right"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_RIGHT, @"Brow_Lowerer_Right"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_LEFT, @"Brow_Lowerer_Left"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_RIGHT, @"Brow_Raiser_Right"},
          {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_LEFT, @"Brow_Raiser_Left"}
        };

        /*
        * 表情・顔の位置の変化を検知した際に呼ばれる??
        */
        public static byte[] postData = null;
        public static int satisfaction = 0;
        private int smileCnt = 25;
        private int smileN = 20;
        public void DrawExpressions(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            // 映像がNULL または 表情認識チェックがOFFの場合処理を終了
            if (m_bitmap == null) return;

            // パラメータ取得
            PXCMFaceData.ExpressionsData expressionsOutput = face.QueryExpressions();
            // 値を正しく取得出来なかった場合処理を終了
            if (expressionsOutput == null) return;

            lock (m_bitmapLock)
            {
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                {
                    const int imageSizeWidth = 18;
                    const int imageSizeHeight = 18;

                    /* 座標 */
                    int positionX = m_faceTextOrganizer.ExpressionsLocation.X;
                    int positionXText = positionX + imageSizeWidth;
                    int positionY = m_faceTextOrganizer.ExpressionsLocation.Y;
                    int positionYText = positionY + imageSizeHeight / 4;

                    foreach (var expressionEntry in m_expressionDictionary)
                    {
                        PXCMFaceData.ExpressionsData.FaceExpression expression = expressionEntry.Key;
                        PXCMFaceData.ExpressionsData.FaceExpressionResult result;
                        bool status = expressionsOutput.QueryExpression(expression, out result);
                        if (!status) continue;
                        //Console.WriteLine("えくすぷれっしょん" + expression);
                        
                        using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                        {
                            var expressionText = String.Format("= {0}", result.intensity);
                            //graphics.DrawString(expressionText, font, brush, positionXText, positionYText);
                            //間隔をあける
                            smileCnt++;
                            if (smileCnt > smileN)
                            {
                                smileCnt = 0;
                                //smileである時に処理をする
                                if (expression.ToString() == "EXPRESSION_SMILE")
                                {
                                    satisfaction = result.intensity;
                                    RunExpression();
                                }
                            }
                            positionY += imageSizeHeight;
                            positionYText += imageSizeHeight;
                        }
                    }
                }
            }
        }

        // smiledataを送信する処理
        public void RunExpression()
        {
            try
            {
                //表示の更新
                UpdateStatus("カメラ起動中", Label.ReportLabel);
                //送るデータの作成
                var dic = new Dictionary<string, string>();
                dic["satisfaction"] = satisfaction.ToString();
                dic["userId"] = Program.argData.ToString();
                Console.WriteLine(dic["satisfaction"] + ":" + dic["userId"]);
                string param = "";
                foreach (string key in dic.Keys) param += String.Format("{0}={1}&", key, dic[key]);
                Console.WriteLine(param);
                postData = System.Text.Encoding.UTF8.GetBytes(param);
                //Requestの設定やら
                // string address = "http://localhost:3000/teacher/socket"; // ローカル用
                string address = "https://desolate-basin-88713.herokuapp.com/teacher/socket"; // Heroku
                //string address = "http://127.0.0.1:8080/TestHttpProject/Test01"; //murasan Local
                HttpWebRequest request = HttpWebRequest.Create(address) as HttpWebRequest; //httpが生きていないとエラー吐いて止まる
                request.ContentLength = postData.Length;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                request.Timeout = 10000; // タイムアウトに10秒

                // 送信
                // 非同期通信
                //request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);
                // 同期通信
                Stream rs = request.GetRequestStream();
                rs.Write(postData, 0, postData.Length);
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //postData = null;
                //GC.Collect(); // ガベージコレクション
            } catch(Exception e) {
                UpdateStatus("サーバとの通信ができませんでした", Label.ReportLabel);
            }
        }

        // 非同期通信のリクエスト
        private static void GetRequestStreamCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

            // postDataの設定
            if (postData != null)
            {
                Stream stream = request.EndGetRequestStream(asyncResult);
                stream.Write(postData, 0, postData.Length);
                stream.Close();
            }
            //request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);

        }

        // 非同期通信のレスポンス
        private static void GetResponseCallback(IAsyncResult asyncResult)
        {
            //HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

            //HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);
            //Stream stream = response.GetResponseStream();
            //StreamReader sr = new StreamReader(response);
        }

        #endregion

        private delegate void DoTrackingCompleted();

        private delegate void UpdatePanelDelegate();

        private delegate void UpdateStatusDelegate(string status);


        /*
        * 以下の3つのmoduleSettingsに値を設定するメソッドのいずれかにジャンプ
        */
        private void SetDefaultSettings()
        {
            // デバイス名の取得
            var deviceName = GetDeviceName();
            Console.WriteLine("デバイス名:" + deviceName);

            if (deviceName == "InvalidCamera")
                SetDefaultInvalidSettings();

            if (deviceName == "DS4")
                SetDefaultDs4Settings();

            // Profile, 右のチェックボックスに関係なくここ通ってる
            if (deviceName == "IVcam")
                SetDefaultIvcamSettings();
        }

        /*
        * invalid(無効) カメラがない
        * 全部false
        */
        private void SetDefaultInvalidSettings()
        {
            m_moduleSettings["Detection"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 0 };
            m_moduleSettings["Landmarks"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 0 };
            m_moduleSettings["Pose"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 0 };
            m_moduleSettings["Recognition"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 0 };
            m_moduleSettings["Expressions"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 0 };
            m_moduleSettings["Pulse"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 0 };
        }

        /*
        * Ivcam?? 普通のカメラ
        * Expressionだけtrueにして残し。他はinvalidの時のもあるけど使えなくしたため削除
        * 普通に起動したら一度ここ通る
        */
        private void SetDefaultIvcamSettings()
        {
            m_moduleSettings["Expressions"] = new ModuleSettings { IsEnabled = true, NumberOfFaces = 4 };
        }

        /*
        * Ds4 -> Expressionが使えない
        * Detection, Landmarksがtrue
        */
        private void SetDefaultDs4Settings()
        {
            m_moduleSettings["Expressions"] = new ModuleSettings { IsEnabled = false, NumberOfFaces = 4 };
        }

    }
}