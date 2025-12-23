using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        // Константы
        const int SCREEN_WIDTH = 800;
        const int SCREEN_HEIGHT = 600;
        const int CENTER_X = 400;
        const int CENTER_Y = 300;
        const double SPHERE_RADIUS = 100;
        const double SECTOR_ANGLE = 90;
        const int POINTS_COUNT = 36;

        // Структуры
        struct Point3D
        {
            public double x, y, z;
        }

        struct Point2D
        {
            public int x, y;
        }

        // Переменные
        Point3D[] SpherePoints = new Point3D[POINTS_COUNT + 1];
        Point2D[] ScreenPoints = new Point2D[POINTS_COUNT + 1];
        double Angle = 0;
        Thread animationThread;
        bool isRunning = true;
        Bitmap bufferBitmap;
        Graphics bufferGraphics;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Вращающаяся фигура";
            this.ClientSize = new Size(SCREEN_WIDTH, SCREEN_HEIGHT);
            this.DoubleBuffered = true;
            this.Paint += Form1_Paint;
            this.FormClosing += Form1_FormClosing;

            bufferBitmap = new Bitmap(SCREEN_WIDTH, SCREEN_HEIGHT);
            bufferGraphics = Graphics.FromImage(bufferBitmap);

            InitSphere();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            animationThread = new Thread(AnimationLoop);
            animationThread.IsBackground = true;
            animationThread.Start();
        }

        void InitSphere()
        {
            int rings = 6;
            int pointsPerRing = POINTS_COUNT / rings;
            double maxTheta = Math.PI * SECTOR_ANGLE / 180;

            int idx = 1;
            for (int j = 0; j < rings; j++)
            {
                double theta = maxTheta * j / (rings - 1);
                for (int i = 0; i < pointsPerRing; i++)
                {
                    double phi = 2 * Math.PI * i / pointsPerRing;

                    SpherePoints[idx].x = SPHERE_RADIUS * Math.Sin(theta) * Math.Cos(phi);
                    SpherePoints[idx].y = SPHERE_RADIUS * Math.Sin(theta) * Math.Sin(phi);
                    SpherePoints[idx].z = SPHERE_RADIUS * Math.Cos(theta);

                    idx++;
                    if (idx > POINTS_COUNT) return;
                }
            }
        }

        void RotatePoint(ref Point3D p, double angle)
        {
            double cosA = Math.Cos(angle);
            double sinA = Math.Sin(angle);

            // Вращение вокруг оси Z
            double tempX = p.x * cosA - p.y * sinA;
            double tempY = p.x * sinA + p.y * cosA;

            p.x = tempX;
            p.y = tempY;

            double cos45 = 0.7071;
            double sin45 = 0.7071;

            tempX = p.x * cos45 - p.z * sin45;
            tempY = p.z * cos45 + p.x * sin45;

            p.x = tempX;
            p.z = tempY;
        }

        void ProjectPoint(Point3D p3D, out int x2D, out int y2D)
        {
            double scale = 500 / (500 + p3D.z);
            x2D = CENTER_X + (int)Math.Round(p3D.x * scale);
            y2D = CENTER_Y - (int)Math.Round(p3D.y * scale);
        }

        void DrawSphere(Graphics g)
        {

            bufferGraphics.Clear(Color.Black);

            for (int i = 1; i <= POINTS_COUNT; i++)
            {
                Point3D p = SpherePoints[i];
                RotatePoint(ref p, Angle);
                SpherePoints[i] = p;
                ProjectPoint(p, out ScreenPoints[i].x, out ScreenPoints[i].y);
            }

            int rings = 6;
            int pointsPerRing = POINTS_COUNT / rings;

            for (int j = 0; j < rings; j++)
            {
                for (int i = 0; i < pointsPerRing; i++)
                {
                    int idx1 = j * pointsPerRing + i + 1;
                    int idx2 = j * pointsPerRing + ((i + 1) % pointsPerRing) + 1;

                    if (idx1 <= POINTS_COUNT)
                    {
                        bufferGraphics.FillEllipse(Brushes.Yellow,
                            ScreenPoints[idx1].x - 2, ScreenPoints[idx1].y - 2, 4, 4);
                    }

                    if (idx1 <= POINTS_COUNT && idx2 <= POINTS_COUNT)
                    {
                        bufferGraphics.DrawLine(Pens.Green,
                            ScreenPoints[idx1].x, ScreenPoints[idx1].y,
                            ScreenPoints[idx2].x, ScreenPoints[idx2].y);
                    }
                }
            }

            for (int i = 1; i <= pointsPerRing; i++)
            {
                for (int j = 0; j < rings - 1; j++)
                {
                    int idx1 = j * pointsPerRing + i;
                    int idx2 = (j + 1) * pointsPerRing + i;

                    if (idx1 <= POINTS_COUNT && idx2 <= POINTS_COUNT)
                    {
                        bufferGraphics.DrawLine(Pens.Cyan,
                            ScreenPoints[idx1].x, ScreenPoints[idx1].y,
                            ScreenPoints[idx2].x, ScreenPoints[idx2].y);
                    }
                }
            }

            g.DrawImage(bufferBitmap, 0, 0);
        }

        void AnimationLoop()
        {
            int frameDelay = 200;
            while (isRunning)
            {
                try
                {
                    Angle += Math.PI / 12; // 15 градусов
                    if (Angle > 2 * Math.PI)
                        Angle -= 2 * Math.PI;

                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke(new Action(() =>
                        {
                            if (!this.IsDisposed)
                            {
                                this.Refresh();
                            }
                        }));
                    }
                    else
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    Thread.Sleep(frameDelay);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawSphere(e.Graphics);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isRunning = false;
            if (animationThread != null && animationThread.IsAlive)
            {
                animationThread.Join(500);
            }

            CleanupResources();
        }

        private void CleanupResources()
        {
            if (bufferGraphics != null)
            {
                bufferGraphics.Dispose();
                bufferGraphics = null;
            }
            if (bufferBitmap != null)
            {
                bufferBitmap.Dispose();
                bufferBitmap = null;
            }
        }
    }
}