using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace e_mo
{
    internal class FaceTracking
    {
        private readonly MainForm m_form;
        private FPSTimer m_timer;
        private bool m_wasConnected;

        public FaceTracking(MainForm form)
        {
            m_form = form;
        }

        private void DisplayDeviceConnection(bool isConnected)
        {
            if (isConnected && !m_wasConnected) m_form.UpdateStatus("Device Reconnected", MainForm.Label.StatusLabel);
            else if (!isConnected && m_wasConnected)
                m_form.UpdateStatus("Device Disconnected", MainForm.Label.StatusLabel);
            m_wasConnected = isConnected;
        }

        private void DisplayPicture(PXCMImage image)
        {
            PXCMImage.ImageData data;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return;
            m_form.DrawBitmap(data.ToBitmap(0, image.info.width, image.info.height));
            m_timer.Tick("");
            image.ReleaseAccess(data);
        }

        private void CheckForDepthStream(PXCMCapture.Device.StreamProfileSet profiles, PXCMFaceModule faceModule)
        {
            PXCMFaceConfiguration faceConfiguration = faceModule.CreateActiveConfiguration();
            if (faceConfiguration == null)
            {
                Debug.Assert(faceConfiguration != null);
                return;
            }

            PXCMFaceConfiguration.TrackingModeType trackingMode = faceConfiguration.GetTrackingMode();
            faceConfiguration.Dispose();

            if (trackingMode != PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH
                || trackingMode != PXCMFaceConfiguration.TrackingModeType.FACE_MODE_IR)
                return;
            if (profiles.depth.imageInfo.format == PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH) return;
            PXCMCapture.DeviceInfo dinfo;
            m_form.Devices.TryGetValue(m_form.GetCheckedDevice(), out dinfo);

            if (dinfo != null)
                MessageBox.Show(
                    String.Format("Depth stream is not supported for device: {0}. \nUsing 2D tracking", dinfo.name),
                    @"Face Tracking",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FaceAlertHandler(PXCMFaceData.AlertData alert)
        {
            m_form.UpdateStatus(alert.label.ToString(), MainForm.Label.StatusLabel);
        }

        public void SimplePipeline()
        {
            PXCMSenseManager pp = m_form.Session.CreateSenseManager();

            if (pp == null)
            {
                throw new Exception("PXCMSenseManager null");
            }

            PXCMCaptureManager captureMgr = pp.captureManager;
            if (captureMgr == null)
            {
                throw new Exception("PXCMCaptureManager null");
            }

            var selectedRes = m_form.GetCheckedColorResolution();
            if (selectedRes != null)
            {
                // Set active camera
                PXCMCapture.DeviceInfo deviceInfo;
                m_form.Devices.TryGetValue(m_form.GetCheckedDevice(), out deviceInfo);
                captureMgr.FilterByDeviceInfo(m_form.GetCheckedDeviceInfo());

                // activate filter only live/record mode , no need in playback mode
                var set = new PXCMCapture.Device.StreamProfileSet
                {
                    color =
                    {
                        frameRate = selectedRes.Item2,
                        imageInfo =
                        {
                            format = selectedRes.Item1.format,
                            height = selectedRes.Item1.height,
                            width = selectedRes.Item1.width
                        }
                    }
                };
            }

            // Set Module            
            pp.EnableFace();
            PXCMFaceModule faceModule = pp.QueryFace();
            if (faceModule == null)
            {
                Debug.Assert(faceModule != null);
                return;
            }

            PXCMFaceConfiguration moduleConfiguration = faceModule.CreateActiveConfiguration();

            if (moduleConfiguration == null)
            {
                Debug.Assert(moduleConfiguration != null);
                return;
            }

            var checkedProfile = m_form.GetCheckedProfile();
            var mode = m_form.FaceModesMap.First(x => x.Value == checkedProfile).Key;

            moduleConfiguration.SetTrackingMode(mode);

            moduleConfiguration.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_RIGHT_TO_LEFT;

            PXCMFaceConfiguration.ExpressionsConfiguration econfiguration = moduleConfiguration.QueryExpressions();
            if (econfiguration == null)
            {
                throw new Exception("ExpressionsConfiguration null");
            }
            econfiguration.properties.maxTrackedFaces = 4; //Expressionの最大認識量

            econfiguration.EnableAllExpressions();
            //if (m_form.IsExpressionsEnabled())
            //{ 
            econfiguration.Enable(); //Expressionsの強制有効化
            //}
            moduleConfiguration.EnableAllAlerts();
            moduleConfiguration.SubscribeAlert(FaceAlertHandler);

            pxcmStatus applyChangesStatus = moduleConfiguration.ApplyChanges();

            m_form.UpdateStatus("Init Started", MainForm.Label.StatusLabel);
            m_form.UpdateStatus("カメラ起動中", MainForm.Label.ReportLabel);

            if (applyChangesStatus < pxcmStatus.PXCM_STATUS_NO_ERROR || pp.Init() < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                m_form.UpdateStatus("Init Failed", MainForm.Label.StatusLabel);
                m_form.UpdateStatus("設定エラー：メニューから適切な設定をしてください。", MainForm.Label.ReportLabel);
            }
            else
            {
                using (PXCMFaceData moduleOutput = faceModule.CreateOutput())
                {
                    Debug.Assert(moduleOutput != null);
                    PXCMCapture.Device.StreamProfileSet profiles;
                    PXCMCapture.Device device = captureMgr.QueryDevice();

                    if (device == null)
                    {
                        m_form.UpdateStatus("カメラを接続してください。", MainForm.Label.ReportLabel);
                        throw new Exception("device null");
                    }

                    device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 0, out profiles);
                    CheckForDepthStream(profiles, faceModule);

                    m_form.UpdateStatus("Streaming", MainForm.Label.StatusLabel);
                    m_timer = new FPSTimer(m_form);

                    while (!m_form.Stopped)
                    {
                        if (pp.AcquireFrame(true) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                        var isConnected = pp.IsConnected();
                        DisplayDeviceConnection(isConnected);
                        if (isConnected)
                        {
                            var sample = pp.QueryFaceSample();
                            if (sample == null)
                            {
                                pp.ReleaseFrame();
                                continue;
                            }
                            switch (mode)
                            {
                                case PXCMFaceConfiguration.TrackingModeType.FACE_MODE_IR:
                                    if (sample.ir != null)
                                        DisplayPicture(sample.ir);
                                    break;
                                default:
                                    DisplayPicture(sample.color);
                                    break;
                            }

                            moduleOutput.Update();
                            PXCMFaceConfiguration.RecognitionConfiguration recognition = moduleConfiguration.QueryRecognition();
                            if (recognition == null)
                            {
                                pp.ReleaseFrame();
                                continue;
                            }

                            m_form.DrawGraphics(moduleOutput);
                            m_form.UpdatePanel();
                        }
                        pp.ReleaseFrame();
                    }

                }

                //             moduleConfiguration.UnsubscribeAlert(FaceAlertHandler);
                //             moduleConfiguration.ApplyChanges();
                m_form.UpdateStatus("Stopped", MainForm.Label.StatusLabel);
            }
            moduleConfiguration.Dispose();
            pp.Close();
            pp.Dispose();
        }
    }
}