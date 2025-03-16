using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using AutoHotkey.Interop;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Drawing.Imaging;
using System.Drawing;

namespace WpfApp1
{
    public partial class MainWindow : System.Windows.Window
    {
        private bool isRunning = true;
        private readonly Random random = new Random();
        private CancellationTokenSource _shieldCancellationTokenSource;
        private CancellationTokenSource _qiyuanCancellationTokenSource;
        private CancellationTokenSource _autoPickupCancellationTokenSource;
        private bool _isAutoPickupRunning = false;

        // 初始化 AutoHotkey 引擎
        AutoHotkeyEngine ahk = AutoHotkeyEngine.Instance;

        // 原始分辨率（3840x2160）的坐标
        private static readonly List<Tuple<int, int>> coordinates = new List<Tuple<int, int>>
        {
            Tuple.Create(350, 2115),
            Tuple.Create(410, 2062),
            Tuple.Create(440, 1980),
            Tuple.Create(446, 1925),
            Tuple.Create(420, 1850),
            Tuple.Create(400, 1820),
            Tuple.Create(375, 1796),
            Tuple.Create(340, 1773),
            Tuple.Create(300, 1755),
            Tuple.Create(268, 1750)
        };

        // 当前分辨率
        private int currentWidth = 3840;
        private int currentHeight = 2160;

        // drug_speed 配置
        private int drugSpeed = 0;
        private int drugTime = 0;

        public MainWindow()
        {
            InitializeComponent();

            // 检测管理员权限
            if (!IsRunningAsAdmin())
            {
                MessageBox.Show("请以管理员权限运行此程序！", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown(); // 关闭程序
                return;
            }

            LoadSettingsFromXml(); // 初始化时读取配置
        }

        // 检测是否以管理员权限运行
        private bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void LoadSettingsFromXml()
        {
            try
            {
                // 读取 XML 文件
                string xmlPath = "Settings.xml";
                if (File.Exists(xmlPath))
                {
                    var doc = XDocument.Load(xmlPath);
                    var resolution = doc.Element("Settings")?.Element("Resolution");
                    if (resolution != null)
                    {
                        currentWidth = int.Parse(resolution.Element("Width")?.Value ?? "3840");
                        currentHeight = int.Parse(resolution.Element("Height")?.Value ?? "2160");
                    }

                    // 读取 drug_speed
                    var otherConfig = doc.Element("Settings")?.Element("OtherConfig");
                    if (otherConfig != null)
                    {
                        drugSpeed = int.Parse(otherConfig.Element("drug_speed")?.Value ?? "0");
                        drugTime = int.Parse(otherConfig.Element("drug_time")?.Value ?? "0");
                    }
                }
                else
                {
                    // 如果文件不存在，使用默认值
                    currentWidth = 3840;
                    currentHeight = 2160;
                    drugSpeed = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置失败: {ex.Message}");
                // 使用默认值
                currentWidth = 3840;
                currentHeight = 2160;
                drugSpeed = 0;
            }
        }

 

        private Tuple<int, int> MapCoordinates(int originalX, int originalY)
        {
            double scaleX = currentWidth / 3840.0;
            double scaleY = currentHeight / 2160.0;

            int mappedX = (int)(originalX * scaleX);
            int mappedY = (int)(originalY * scaleY);

            return Tuple.Create(mappedX, mappedY);
        }

        private void ShielCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _shieldCancellationTokenSource = new CancellationTokenSource();
            var token = _shieldCancellationTokenSource.Token;
            Task.Run(() => ShieldKeep(token));
        }

        private void ShielCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _shieldCancellationTokenSource?.Cancel();
        }

        private void qiyuanCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _qiyuanCancellationTokenSource = new CancellationTokenSource();
            var token = _qiyuanCancellationTokenSource.Token;
            Task.Run(() => qiyuanKeep(token));
        }

        private void qiyuanCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _qiyuanCancellationTokenSource?.Cancel();
        }

        private static DateTime qiyuan_time = DateTime.MinValue;

        private void ShieldKeep(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    // int index = (int)Dispatcher.Invoke(() => ShielLevel.Value);
                    int index = 9;
                    int delay = (int)(3000 * (1 + drugTime / 100.0) / (1 + drugSpeed / 100.0)); // 动态计算延迟
                    delay = ((delay / 100)-1) * 100;  // poe2 有取整计算
                    Trace.WriteLine($"delay {delay}");
                    // ShieldKeep 使用独立的逻辑
                    if (CheckColorAndTriggerForShield(index, 0x31, delay))
                    {
                        Thread.Sleep(5);
                    }
                    Thread.Sleep(5);
                }
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("护盾已停止。");
            }
        }

        private void qiyuanKeep(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    int index = (int)Dispatcher.Invoke(() => qiyuan_level.Value);

                    // qiyuanKeep 使用独立的逻辑
                    if (CheckColorAndTriggerForQiyuan(index, 0x57, 50))
                    {
                        qiyuan_time = DateTime.Now;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("qiyuanKeep 已停止。");
            }
        }

        private bool CheckColorAndTriggerForShield(int index, ushort keyCode, int delay)
        {
            // 获取颜色值
            var coloxxr = GetColoAt(89, 1966);
            int r = (coloxxr >> 16) & 0xFF;
            int g = (coloxxr >> 8) & 0xFF;
            int b = coloxxr & 0xFF;

            // 目标颜色
            int targetR = 36;
            int targetG = 37;
            int targetB = 37;

            // 计算颜色距离
            double distance = Math.Sqrt(Math.Pow(r - targetR, 2) + Math.Pow(g - targetG, 2) + Math.Pow(b - targetB, 2));

            // 如果颜色距离大于阈值，跳过本次循环
            if (distance > 3)
            {
                Thread.Sleep(500);
                return false;
            }

            // 确保 index 在 coordinates 的范围内
            if (index >= 0 && index < coordinates.Count)
            {
                var originalCoord = coordinates[index];
                var mappedCoord = MapCoordinates(originalCoord.Item1, originalCoord.Item2);

                // 获取灰度值
                int color = GetGrayAt(mappedCoord.Item1, mappedCoord.Item2);
               // Trace.WriteLine($"color {color}");
                // ShieldKeep 不需要检测 skill_lum
                if (color < 0x80)
                {
                    ahk.ExecRaw("Send, {1}"); // 按下 1 键
                    Thread.Sleep(delay);
                    return true;
                }
            }

            return false;
        }

        private bool CheckColorAndTriggerForQiyuan(int index, ushort keyCode, int delay)
        {
            // 获取颜色值
            var coloxxr = GetColoAt(89, 1966);
            int r = (coloxxr >> 16) & 0xFF;
            int g = (coloxxr >> 8) & 0xFF;
            int b = coloxxr & 0xFF;

            // 目标颜色
            int targetR = 36;
            int targetG = 37;
            int targetB = 37;

            // 计算颜色距离
            double distance = Math.Sqrt(Math.Pow(r - targetR, 2) + Math.Pow(g - targetG, 2) + Math.Pow(b - targetB, 2));

            // 如果颜色距离大于阈值，跳过本次循环
            if (distance > 3)
            {
                Thread.Sleep(500);
                return false;
            }

            // 确保 index 在 coordinates 的范围内
            if (index >= 0 && index < coordinates.Count)
            {
                var originalCoord = coordinates[index];
                var mappedCoord = MapCoordinates(originalCoord.Item1, originalCoord.Item2);

                // 获取灰度值
                int color = GetGrayAt(mappedCoord.Item1, mappedCoord.Item2);
                var skill_lum = GetGrayAt(2972, 2048);

                // qiyuanKeep 需要检测 skill_lum
                if ((skill_lum > 75) && (color > 0x80))
                {
                    ahk.ExecRaw("Send, {w}"); // 按下 W 键
                    Thread.Sleep(delay);
                    return true;
                }
            }

            return false;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern void ReleaseDC(IntPtr hWnd, IntPtr hdc);

        private int GetGrayAt(int originalX, int originalY)
        {
            // 在函数内部进行坐标映射
            var mappedCoord = MapCoordinates(originalX, originalY);
            int x = mappedCoord.Item1;
            int y = mappedCoord.Item2;

            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int pixelColor = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);

            int r = (pixelColor & 0x00FF0000) >> 16;
            int g = (pixelColor & 0x0000FF00) >> 8;
            int b = (pixelColor & 0x000000FF);

            int gray = (int)(0.299 * r + 0.587 * g + 0.114 * b);
            return gray;
        }

        private int GetColoAt(int originalX, int originalY)
        {
            // 在函数内部进行坐标映射
            var mappedCoord = MapCoordinates(originalX, originalY);
            int x = mappedCoord.Item1;
            int y = mappedCoord.Item2;

            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int pixelColor = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            return pixelColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        private void Window_Closed(object sender, EventArgs e)
        {
            isRunning = false;
        }

        private void autoPickup_Checked(object sender, RoutedEventArgs e)
        {
            if (_isAutoPickupRunning) return;

            _isAutoPickupRunning = true;
            _autoPickupCancellationTokenSource = new CancellationTokenSource();

            // 启动检测线程
            Task.Run(() => DetectWhiteRectangleWithRedText(_autoPickupCancellationTokenSource.Token), _autoPickupCancellationTokenSource.Token);
        }

        private void autoPickup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isAutoPickupRunning) return;

            _isAutoPickupRunning = false;
            _autoPickupCancellationTokenSource?.Cancel();
        }
        private Point2f? _lastCenter = null; // 记录上一次的 center 值
        // 检测逻辑
        private void DetectWhiteRectangleWithRedText(CancellationToken token)
        {
            while (_isAutoPickupRunning && !token.IsCancellationRequested)
            {
                try
                {
                    // 抓取屏幕
                    Mat screenCapture = CaptureScreen();


                    var coloxxr = GetColoAt(89, 1966);
                    int r = (coloxxr >> 16) & 0xFF;
                    int g = (coloxxr >> 8) & 0xFF;
                    int b = coloxxr & 0xFF;

                    // 目标颜色
                    int targetR = 36;
                    int targetG = 37;
                    int targetB = 37;

                    // 计算颜色距离
                    double distance = Math.Sqrt(Math.Pow(r - targetR, 2) + Math.Pow(g - targetG, 2) + Math.Pow(b - targetB, 2));

                    // 如果颜色距离大于阈值，跳过本次循环
                    if (distance > 3)
                    {
                        Thread.Sleep(500);
                        continue;
                    }


                    //// 缩放到 800x600
                    //Mat resizedImage = new Mat();
                    //Cv2.Resize(screenCapture, resizedImage, new OpenCvSharp.Size(800, 600));

                    // 保存为 BMP 文件
                    //  Cv2.ImWrite("resized_image.bmp", resizedImage);

                    // 1. 检测白色矩形框
                    Mat whiteMask = new Mat();
                    Scalar lowerWhite = new Scalar(255, 255, 255); // RGB(255, 255, 255)
                    Scalar upperWhite = new Scalar(255, 255, 255);
                    Cv2.InRange(screenCapture, lowerWhite, upperWhite, whiteMask);

                    var contours = Cv2.FindContoursAsArray(whiteMask, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

                    Point2f center = new Point2f();

                    foreach (var contour in contours)
                    {
                        var boundingRect = Cv2.BoundingRect(contour);
                        if((boundingRect.Width==1)|| (boundingRect.Height == 1)) {  continue; }

                        if (boundingRect.Width * boundingRect.Height > 5000 * (3840.0 / currentWidth) * (2160.0 / currentHeight)) // 缩放后的面积阈值
                        {
                            center = new Point2f(boundingRect.X + boundingRect.Width / 2, boundingRect.Y + boundingRect.Height / 2);

                            // 在 boundingRect 区域内检测红色文字
                            Mat whiteRoi = new Mat(screenCapture, boundingRect); // 提取白色矩形框区域

                            Scalar lowerRed = new Scalar(0, 0, 200); // 放宽红色阈值
                            Scalar upperRed = new Scalar(50, 50, 255);
                            Mat redMask = new Mat();
                            Cv2.InRange(whiteRoi, lowerRed, upperRed, redMask);

                            int redPixelCount = Cv2.CountNonZero(redMask);
                            if (redPixelCount > 10) // 红色像素数量阈值
                            {
                                Trace.WriteLine($"检测到白色矩形框内存在红色文字！中心坐标: ({center.X}, {center.Y})");

                                // 计算屏幕中心坐标
                                Point2f screenCenter = new Point2f(currentWidth / 2, currentHeight / 2);

                                // 计算 center 与屏幕中心的距离
                                 distance = Math.Sqrt(Math.Pow(center.X - screenCenter.X, 2) + Math.Pow(center.Y - screenCenter.Y, 2));
                                Trace.WriteLine($"检测到白色矩形框内存在红色文字！中心坐标: ({center.X}, {center.Y})   distance {distance}");
                                // 如果距离小于阈值（例如 20），则模拟鼠标左键点击
                                if (distance < 400 * (3840.0 / currentWidth) * (2160.0 / currentHeight))
                                {
                                    // 移动鼠标到 center 位置
                                    ahk.ExecRaw($"Click, {center.X}, {center.Y}, Left");
                                    Thread.Sleep(500 + random.Next(0, 20));
                                    Trace.WriteLine($"模拟鼠标左键点击中心坐标: ({center.X}, {center.Y})");

                                    // 如果当前 center 与上一次相同，则跳过
                                    if (_lastCenter.HasValue && _lastCenter.Value == center)
                                    {
                                        Trace.WriteLine("检测到相同的 center，跳过本次操作。");
                                        continue;
                                    }

                                    // 记录当前 center
                                    _lastCenter = center;
                                }

                            }
                            else
                            {
                                Trace.WriteLine("白色矩形框 【OK】内存在红色文字【NG】");
                            }


                          //  break;
                        }
                    }

                 
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"发生错误: {ex.Message}");
                }

                // 1秒检测一次
                Thread.Sleep(20 + random.Next(0, 30));
            }
        }

        // 抓取屏幕
        private Mat CaptureScreen()
        {
            IntPtr hdcSrc = GetDC(IntPtr.Zero);
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, currentWidth, currentHeight);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);
            BitBlt(hdcDest, 0, 0, currentWidth, currentHeight, hdcSrc, 0, 0, TernaryRasterOperations.SRCCOPY);
            SelectObject(hdcDest, hOld);
            DeleteDC(hdcDest);
            ReleaseDC(IntPtr.Zero, hdcSrc);

            // 从 HBitmap 创建 Bitmap
            var bitmap = System.Drawing.Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);

            // 将 Bitmap 转换为 24 位 RGB 格式
            var bitmap24bpp = ConvertTo24bpp(bitmap);

            // 将 Bitmap 转换为 Mat
            return BitmapToMat(bitmap);
        }
        // 将 Bitmap 转换为 24 位 RGB 格式
        private Bitmap ConvertTo24bpp(Bitmap bitmap)
        {
            if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                return bitmap; // 已经是 24 位 RGB 格式，直接返回
            }

            // 创建一个新的 24 位 RGB 格式的 Bitmap
            var bitmap24bpp = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);

            // 将原始 Bitmap 绘制到新的 Bitmap 上
            using (var g = Graphics.FromImage(bitmap24bpp))
            {
                g.DrawImage(bitmap, 0, 0);
            }

            return bitmap24bpp;
        }
        private Mat BitmapToMat(Bitmap bitmap)
        {
            // 锁定 Bitmap 数据
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            try
            {
                // 创建 Mat 对象
                Mat mat = new Mat(bitmap.Height, bitmap.Width, MatType.CV_8UC3);

                // 计算需要复制的字节数
                int byteCount = bitmapData.Stride * bitmapData.Height;

                // 使用 Buffer.MemoryCopy 复制数据
                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)bitmapData.Scan0, // 源指针
                        (void*)mat.DataPointer,  // 目标指针
                        byteCount,               // 目标缓冲区大小
                        byteCount                // 要复制的字节数
                    );
                }

                return mat;
            }
            finally
            {
                // 解锁 Bitmap 数据
                bitmap.UnlockBits(bitmapData);
            }
        }



        // GDI32 函数
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);





        private enum TernaryRasterOperations
        {
            SRCCOPY = 0x00CC0020
        }
    }
}
