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
using NLog;

namespace WpfApp1
{
    public partial class MainWindow : System.Windows.Window
    {
        private bool isRunning = true;
        private readonly Random random = new Random();
        private CancellationTokenSource _shieldCancellationTokenSource;
        private CancellationTokenSource _qiyuanCancellationTokenSource;
        private CancellationTokenSource _life_keep_CancellationTokenSource;
        private bool _islife_keep_Running = false;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
        private int wlevel = 0;
        private int elevel = 0;

        private int hudun = 0;
        private int Signature = 0;
        
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
                        wlevel = int.Parse(otherConfig.Element("w_Level")?.Value ?? "0");
                        elevel = int.Parse(otherConfig.Element("e_Level")?.Value ?? "0");
                        hudun = int.Parse(otherConfig.Element("hudun")?.Value ?? "0");

                        Signature = int.Parse(otherConfig.Element("Signature")?.Value ?? "0");
                    }
                }
                else
                {
                    // 如果文件不存在，使用默认值
                    currentWidth = 3840;
                    currentHeight = 2160;
                    drugSpeed = 0;

                    wlevel = 0;
                    elevel = 0;
                    hudun = 0x80;
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
                    int delay = (int)(2900 * (1 + drugTime / 100.0) / (1 + drugSpeed / 100.0)); // 动态计算延迟
                    delay = (delay / 100) * 100;  // poe2 有取整计算
                    // Logger.Info($"delay {delay}");
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
                 Logger.Info("护盾已停止。");
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
                    if (CheckColorAndTriggerForQiyuan(index, 0x57, 20))
                    {
                        qiyuan_time = DateTime.Now;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                 Logger.Info("qiyuanKeep 已停止。");
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
            int targetR = (Signature >> 16) & 0xFF;
            int targetG = (Signature >> 8) & 0xFF;
            int targetB = Signature & 0xFF;

            // 计算颜色距离
            double distance = Math.Sqrt(Math.Pow(r - targetR, 2) + Math.Pow(g - targetG, 2) + Math.Pow(b - targetB, 2));
            Logger.Info($"distance {distance}。");
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
                Logger.Info($"color {color}");
                // ShieldKeep 不需要检测 skill_lum
                if (color < hudun)
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
            int targetR = (Signature >> 16) & 0xFF;
            int targetG = (Signature >> 8) & 0xFF;
            int targetB = Signature & 0xFF;

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
              
                var skill_lum = GetAreaAverageBrightness(new System.Drawing.Point(2973, 2043), new System.Drawing.Point(2996, 2083));
                int color = GetGrayAt(mappedCoord.Item1, mappedCoord.Item2);
                Logger.Info($"skill w {color}, {skill_lum}");
                // qiyuanKeep 需要检测 skill_lum
                if ((skill_lum > wlevel) && (color > 0x80))
                {
                    ahk.ExecRaw("Send, {w}"); // 按下 W 键
                    Thread.Sleep(delay);
                   // return true;
                }

              

               
                skill_lum = GetAreaAverageBrightness(new System.Drawing.Point(3077, 2052), new System.Drawing.Point(3102, 2083));
                color = GetGrayAt(mappedCoord.Item1, mappedCoord.Item2);
                Logger.Info($"skill e {color }, {skill_lum}");
                if ((skill_lum > elevel) && (color > 0x80))
                {
                    ahk.ExecRaw("Send, {e}"); // 按下 e 键
                    Thread.Sleep(delay);
                    // return true;
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


        private int GetAreaAverageBrightness(System.Drawing.Point point1, System.Drawing.Point  point2)
        {
            // 坐标映射（需要您根据实际映射逻辑补充）
            var mappedP1 = MapCoordinates(point1.X, point1.Y);
            var mappedP2 = MapCoordinates(point2.X, point2.Y);

            // 计算矩形区域
            int left = Math.Min(mappedP1.Item1, mappedP2.Item1);
            int top = Math.Min(mappedP1.Item2, mappedP2.Item2);
            int right = Math.Max(mappedP1.Item1, mappedP2.Item1);
            int bottom = Math.Max(mappedP1.Item2, mappedP2.Item2);
            int width = right - left;
            int height = bottom - top;

            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            SelectObject(hdcMem, hBitmap);

            // 拷贝屏幕区域到内存位图
            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, left, top, TernaryRasterOperations.SRCCOPY);

            // 获取位图数据
            Bitmap bitmap = Image.FromHbitmap(hBitmap);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            long totalBrightness = 0;
            int bytesPerPixel = 4; // 32bpp
            byte[] pixelBuffer = new byte[width * height * bytesPerPixel];

            // 将像素数据复制到数组
            Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            int blackPixelCount = 0;  // 用于统计黑色点的数量
            int threshold = 10;        // 定义黑色点的阈值，可以根据需要调整

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * bytesPerPixel;
                    int b = pixelBuffer[index];
                    int g = pixelBuffer[index + 1];
                    int r = pixelBuffer[index + 2];

                    // 检查是否为黑色点
                    if (r < threshold && g < threshold && b < threshold)
                    {
                        blackPixelCount++;
                    }
                }
            }

            // blackPixelCount 现在包含了图像中黑色点的数量


            // 释放资源
            bitmap.UnlockBits(bmpData);
            bitmap.Dispose();
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);

            return blackPixelCount;
        }



        private int GetHealthPercentage()
        {
            // 定义垂直线条的坐标 (x坐标相同)
            System.Drawing.Point topPoint = new System.Drawing.Point(245, 1787);
            System.Drawing.Point bottomPoint = new System.Drawing.Point(245, 2108);

            // 坐标映射（需要您根据实际映射逻辑补充）
            var mappedTop = MapCoordinates(topPoint.X, topPoint.Y);
            var mappedBottom = MapCoordinates(bottomPoint.X, bottomPoint.Y);

            // 计算线条区域 (宽度为1像素的垂直线)
            int left = mappedTop.Item1;
            int top = mappedTop.Item2;
            int right = left + 1; // 1像素宽
            int bottom = mappedBottom.Item2;
            int width = right - left;
            int height = bottom - top;

            // 获取屏幕DC
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            SelectObject(hdcMem, hBitmap);

            // 拷贝屏幕区域到内存位图
            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, left, top, TernaryRasterOperations.SRCCOPY);

            // 获取位图数据
            Bitmap bitmap = Image.FromHbitmap(hBitmap);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            int bytesPerPixel = 4; // 32bpp
            byte[] pixelBuffer = new byte[width * height * bytesPerPixel];

            // 将像素数据复制到数组
            Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            // 统计生命条的有效像素
            int healthPixelCount = 0;
            int totalHealthPixels = height; // 生命条的总高度

            // 定义颜色比例阈值
            float minDominanceRatio = 1.5f; // 最大值/最小值 > 此值才算有效颜色

            for (int y = 0; y < height; y++)
            {
                int index = y * width * bytesPerPixel; // 因为宽度是1，所以x=0

                int r = pixelBuffer[index + 2];
                int g = pixelBuffer[index + 1];
                int b = pixelBuffer[index];

                // 计算RGB最大值和最小值（+1防止除以零）
                int max = Math.Max(r, Math.Max(g, b)) + 1;
                int min = Math.Min(r, Math.Min(g, b)) + 1;

                // 简单比例判定：颜色是否足够"鲜艳"（非灰度）
                if ((float)max / min > minDominanceRatio)
                {
                    healthPixelCount++;
                }
            }

            // 计算生命值百分比
            int healthPercentage = (int)((float)healthPixelCount / totalHealthPixels * 100);
            healthPercentage = Math.Clamp(healthPercentage, 0, 100);

            // 释放资源
            bitmap.UnlockBits(bmpData);
            bitmap.Dispose();
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);

            return healthPercentage;
        }

        private int GetManaPercentage()
        {
            // 定义垂直线条的坐标 (x坐标相同)
            System.Drawing.Point topPoint = new System.Drawing.Point(3592, 1787);
            System.Drawing.Point bottomPoint = new System.Drawing.Point(3592, 2108);

            // 坐标映射（需要您根据实际映射逻辑补充）
            var mappedTop = MapCoordinates(topPoint.X, topPoint.Y);
            var mappedBottom = MapCoordinates(bottomPoint.X, bottomPoint.Y);

            // 计算线条区域 (宽度为1像素的垂直线)
            int left = mappedTop.Item1;
            int top = mappedTop.Item2;
            int right = left + 1; // 1像素宽
            int bottom = mappedBottom.Item2;
            int width = right - left;
            int height = bottom - top;

            // 获取屏幕DC
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            SelectObject(hdcMem, hBitmap);

            // 拷贝屏幕区域到内存位图
            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, left, top, TernaryRasterOperations.SRCCOPY);

            // 获取位图数据
            Bitmap bitmap = Image.FromHbitmap(hBitmap);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            int bytesPerPixel = 4; // 32bpp
            byte[] pixelBuffer = new byte[width * height * bytesPerPixel];

            // 将像素数据复制到数组
            Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

            // 统计生命条的有效像素
            int healthPixelCount = 0;
            int totalHealthPixels = height; // 生命条的总高度

            // 定义颜色比例阈值
            float minDominanceRatio = 1.5f; // 最大值/最小值 > 此值才算有效颜色

            for (int y = 0; y < height; y++)
            {
                int index = y * width * bytesPerPixel; // 因为宽度是1，所以x=0

                int r = pixelBuffer[index + 2];
                int g = pixelBuffer[index + 1];
                int b = pixelBuffer[index];

                // 计算RGB最大值和最小值（+1防止除以零）
                int max = Math.Max(r, Math.Max(g, b)) + 1;
                int min = Math.Min(r, Math.Min(g, b)) + 1;

                // 简单比例判定：颜色是否足够"鲜艳"（非灰度）
                if ((float)max / min > minDominanceRatio)
                {
                    healthPixelCount++;
                }
            }

            // 计算生命值百分比
            int healthPercentage = (int)((float)healthPixelCount / totalHealthPixels * 100);
            healthPercentage = Math.Clamp(healthPercentage, 0, 100);

            // 释放资源
            bitmap.UnlockBits(bmpData);
            bitmap.Dispose();
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);

            return healthPercentage;
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



        private void life_keep_Checked(object sender, RoutedEventArgs e)
        {
            if (_islife_keep_Running) return;

            _islife_keep_Running = true;
            _life_keep_CancellationTokenSource = new CancellationTokenSource();

            // 启动检测线程
            Task.Run(() => LifeKeepLoop(_life_keep_CancellationTokenSource.Token), _life_keep_CancellationTokenSource.Token);
        }

        private void life_keep__Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_islife_keep_Running) return;

            _islife_keep_Running = false;
            _life_keep_CancellationTokenSource?.Cancel();
        }
        private Point2f? _lastCenter = null; // 记录上一次的 center 值
        // 检测逻辑
        private void LifeKeepLoop(CancellationToken token)
        {
            while (_islife_keep_Running && !token.IsCancellationRequested)
            {
                try
                {
                    // 抓取屏幕
                  //  Mat screenCapture = CaptureScreen();


                    var coloxxr = GetColoAt(89, 1966);
                    int r = (coloxxr >> 16) & 0xFF;
                    int g = (coloxxr >> 8) & 0xFF;
                    int b = coloxxr & 0xFF;

                    // 目标颜色
                    int targetR =  (Signature >> 16) & 0xFF;
                    int targetG =  (Signature >> 8) & 0xFF;
                    int targetB = Signature & 0xFF;

                    // 计算颜色距离
                    double distance = Math.Sqrt(Math.Pow(r - targetR, 2) + Math.Pow(g - targetG, 2) + Math.Pow(b - targetB, 2));

                    // 如果颜色距离大于阈值，跳过本次循环
                    if (distance > 3)
                    {
                        Logger.Info($"Signature: {Signature}   coloxxr: {coloxxr}");
                        Thread.Sleep(500);
                        continue;
                    }

                    int life_level = (int)Dispatcher.Invoke(() => life_level_C.Value);
                    var life=  GetHealthPercentage();
                    Logger.Info($"life: {life}");
                    Trace.WriteLine($"life: {life}    life_level {life_level}  ");
                    if (life < life_level) {


                        int delay = (int)(2900 * (1 + drugTime / 100.0) / (1 + drugSpeed / 100.0)); // 动态计算延迟
                        delay = (delay / 100) * 100;  // poe2 有取整计算


                        ahk.ExecRaw("Send, {1}");
                        Thread.Sleep(delay);
                    }
                    Thread.Sleep(10);

                }
                catch (Exception ex)
                {
                     Logger.Info($"发生错误: {ex.Message}");
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
        private CancellationTokenSource _mana_keep_CancellationTokenSource;
        private bool _isMana_keep_Running = false;
        private void mana_keep_Checked(object sender, RoutedEventArgs e)
        {
            if (_isMana_keep_Running) return;

            _isMana_keep_Running = true;
            _mana_keep_CancellationTokenSource = new CancellationTokenSource();

            // 启动检测线程
            Task.Run(() => ManaKeepLoop(_mana_keep_CancellationTokenSource.Token), _mana_keep_CancellationTokenSource.Token);
        }

        private void mana_keep__Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isMana_keep_Running) return;

            _isMana_keep_Running = false;
            _mana_keep_CancellationTokenSource?.Cancel();
        }


        private void ManaKeepLoop(CancellationToken token)
        {
            while (_isMana_keep_Running && !token.IsCancellationRequested)
            {
                try
                {
                    // 抓取屏幕
                  //  Mat screenCapture = CaptureScreen();


                    var coloxxr = GetColoAt(89, 1966);
                    int r = (coloxxr >> 16) & 0xFF;
                    int g = (coloxxr >> 8) & 0xFF;
                    int b = coloxxr & 0xFF;

                    // 目标颜色
                    int targetR = (Signature >> 16) & 0xFF;
                    int targetG = (Signature >> 8) & 0xFF;
                    int targetB = Signature & 0xFF;

                    // 计算颜色距离
                    double distance = Math.Sqrt(Math.Pow(r - targetR, 2) + Math.Pow(g - targetG, 2) + Math.Pow(b - targetB, 2));

                    // 如果颜色距离大于阈值，跳过本次循环
                    if (distance > 3)
                    {
                        Logger.Info($"Signature: {Signature}   coloxxr: {coloxxr}");
                        Thread.Sleep(500);
                        continue;
                    }

                    int mana_level = (int)Dispatcher.Invoke(() => mana_level_C.Value);
                    var mana = GetManaPercentage();
                    Logger.Info($"mana: {mana}");
                    Trace.WriteLine($"mana: {mana}    mana_level {mana_level}  ");
                    if (mana < mana_level)
                    {


                        int delay = (int)(2900 * (1 + drugTime / 100.0) / (1 + drugSpeed / 100.0)); // 动态计算延迟
                        delay = (delay / 100) * 100;  // poe2 有取整计算


                        ahk.ExecRaw("Send, {2}");
                        Thread.Sleep(delay);
                    }
                    Thread.Sleep(10);

                }
                catch (Exception ex)
                {
                    Logger.Info($"发生错误: {ex.Message}");
                }

                // 1秒检测一次
                Thread.Sleep(20 + random.Next(0, 30));
            }
        }

    }
}
