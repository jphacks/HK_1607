using System.Drawing;

namespace e_mo
{
    public class FaceTextOrganizer
    {
        private readonly Color[] m_colorList =
        {
            Color.Yellow, Color.Orange, Color.Lime, Color.Magenta, Color.Brown,
            Color.Turquoise, Color.DeepSkyBlue
        };

        private Color m_color;
        private Point m_expression;
        private Point m_faceId;
        private int m_imageWidth;
        private PXCMRectI32 m_rectangle;

        public PointF FaceIdLocation
        {
            get { return m_faceId; }
        }

        public Point ExpressionsLocation
        {
            get { return m_expression; }
        }

        public Rectangle RectangleLocation
        {
            get { return new Rectangle(m_rectangle.x, m_rectangle.y, m_rectangle.w, m_rectangle.h); }
        }

        public Color Colour
        {
            get { return m_color; }
        }

        public int FontSize
        {
            get { return CalculateDefiniteFontSize(); }
        }

        private int CalculateDefiniteFontSize()
        {
            switch (m_imageWidth)
            {
                case 640:
                case 848:
                case 960:
                    return 6;
                case 1280:
                    return 8;
                case 1920:
                    return 12;
                default:
                    return 12;
            }
        }

        public void ChangeFace(int faceIndex, PXCMFaceData.Face face, int imageHeight, int imageWidth)
        {
            const int threshold = 5;
            const int expressionThreshold = 55;
            const int faceTextWidth = 100;

            m_imageWidth = imageWidth;

            PXCMFaceData.DetectionData fdetectionData = face.QueryDetection();
            m_color = m_colorList[faceIndex % m_colorList.Length];

            if (fdetectionData == null)
            {
                int currentWidth = faceIndex * faceTextWidth;
            }
            else
            {
                fdetectionData.QueryBoundingRect(out m_rectangle);
                
                m_faceId.X = m_rectangle.x + threshold;
                m_faceId.Y = m_rectangle.y + threshold;

                m_expression.X = (m_rectangle.x + m_rectangle.w + expressionThreshold >= m_imageWidth)
                    ? (m_rectangle.x - expressionThreshold)
                    : (m_rectangle.x + m_rectangle.w + threshold);
                m_expression.Y = m_rectangle.y + threshold;
            }
        }
    }
}